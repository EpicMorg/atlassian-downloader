
namespace EpicMorg.Atlassian.Downloader.Core;

using EpicMorg.Atlassian.Downloader.Core.Models;
using EpicMorg.Atlassian.Downloader.Models;
using Microsoft.Extensions.Logging;
using ReverseMarkdown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class AtlassianClient
{
    private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ILogger<AtlassianClient> _logger;
    private readonly HttpClient _client;

    public AtlassianClient(HttpClient client, ILogger<AtlassianClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    #region Public API Methods

    public async Task DownloadPluginAsync(string pluginId, DownloaderSettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentNullException(nameof(pluginId));
        }

        _logger.LogInformation("Starting plugin archival for {pluginId}", pluginId);
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);

        try
        {
            var allVersions = await GetAllPluginVersions(pluginId, cancellationToken);
            if (!allVersions.Any())
            {
                _logger.LogWarning("No versions found for plugin {pluginId}", pluginId);
                return;
            }

            _logger.LogInformation("Found a total of {count} versions. Processing...", allVersions.Count);

            var pluginInfo = await GetPluginInfo(pluginId, cancellationToken);
            if (pluginInfo is null)
            {
                _logger.LogError("Could not retrieve basic info for plugin {pluginId}", pluginId);
                return;
            }

            await DownloadPluginVersions(pluginInfo, allVersions, settings, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Plugin download process was canceled by the user.");
            throw; // Re-throw to allow the host to shut down gracefully
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the plugin download process for {pluginId}", pluginId);
        }
    }

    public async Task DownloadProductsAsync(DownloaderSettings settings, CancellationToken cancellationToken = default)
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        var feedUrls = GetFeedUrls(settings.CustomFeed);
        _logger.LogInformation("Product download task started.");

        foreach (var feedUrl in feedUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (_, versions) = await GetJson(feedUrl, settings.ProductVersion, cancellationToken);
            await DownloadFilesFromFeed(feedUrl, versions, settings, cancellationToken);
        }
    }

    public async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetProductDataAsync(string feedUrl, DownloaderSettings settings, CancellationToken cancellationToken)
    {
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        return await GetJson(feedUrl, settings.ProductVersion, cancellationToken);
    }

    public IReadOnlyList<string> GetProductFeedUrls(DownloaderSettings settings) => GetFeedUrls(settings.CustomFeed);

    #endregion


    #region Private Helpers - Plugins

    private async Task<MarketplacePlugin?> GetPluginInfo(string pluginId, CancellationToken cancellationToken)
    {
        var pluginInfoUrl = $"https://marketplace.atlassian.com/rest/2/addons/{pluginId}";
        _logger.LogDebug("Getting plugin info from: {url}", pluginInfoUrl);
        var pluginInfoJson = await _client.GetStringAsync(pluginInfoUrl, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<MarketplacePlugin>(pluginInfoJson, jsonOptions);
    }

    private async Task<List<AddonVersionSummary>> GetAllPluginVersions(string pluginId, CancellationToken cancellationToken)
    {
        var allVersions = new List<AddonVersionSummary>();
        var nextUrl = $"https://marketplace.atlassian.com/rest/2/addons/{pluginId}/versions";

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug("Fetching versions from: {url}", nextUrl);

            try
            {
                var responseJson = await _client.GetStringAsync(nextUrl, cancellationToken).ConfigureAwait(false);
                var page = JsonSerializer.Deserialize<AddonVersionCollection>(responseJson, jsonOptions);

                if (page?.Embedded?.Versions != null)
                {
                    allVersions.AddRange(page.Embedded.Versions);
                }

                // Prepare URL for the next iteration
                nextUrl = page?.Links?.Next?.Href;
                if (nextUrl != null && !nextUrl.StartsWith("http"))
                {
                    nextUrl = $"https://marketplace.atlassian.com{nextUrl}";
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // This is an expected "error" if the pluginId is invalid.
                // Log a clear message and break the loop.
                _logger.LogError("Plugin with ID '{pluginId}' not found on Atlassian Marketplace (404).", pluginId);
                nextUrl = null; // This will stop the do-while loop
            }
            // All other exceptions (network errors, other HTTP errors) will bubble up and be handled
            // by the top-level try-catch, which is the correct behavior.

        } while (!string.IsNullOrWhiteSpace(nextUrl));

        return allVersions;
    }

    private async Task DownloadPluginVersions(MarketplacePlugin plugin, IEnumerable<AddonVersionSummary> versions, DownloaderSettings settings, CancellationToken cancellationToken)
    {
        foreach (var version in versions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (version.Deployment?.Server != true && version.Deployment?.DataCenter != true)
            {
                _logger.LogDebug("Skipping version {versionName} (not for Server/DC)", version.Name);
                continue;
            }

            var versionDetail = await GetDetailedVersionInfo(plugin, version, cancellationToken);
            if (versionDetail is null || !versionDetail.Value.CompatibleProducts.Any())
            {
                continue;
            }

            _logger.LogInformation("Plugin {pluginKey} v{version} is compatible with: {products}", plugin.Key, version.Name, string.Join(", ", versionDetail.Value.CompatibleProducts));

            foreach (var product in versionDetail.Value.CompatibleProducts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sanitizedPluginName = SanitizeFolderName(plugin.Name ?? plugin.Key ?? "unknown-plugin");
                var versionDir = Path.Combine(settings.OutputDir, "plugins", product, sanitizedPluginName, SanitizeFolderName(version.Name ?? "unknown-version"));

                if (!Directory.Exists(versionDir)) Directory.CreateDirectory(versionDir);

                var readmePath = Path.Combine(versionDir, "readme.md");
                await File.WriteAllTextAsync(readmePath, versionDetail.Value.ReadmeContent ?? "No release notes.", cancellationToken);

                if (string.IsNullOrWhiteSpace(versionDetail.Value.DownloadUrl))
                {
                    _logger.LogWarning("No download URL found for version {version}", version.Name);
                    continue;
                }

                var fileName = await GetActualFileNameAsync(versionDetail.Value.DownloadUrl, plugin.Key, version.Name, cancellationToken);
                var outputFile = Path.Combine(versionDir, fileName);

                if (File.Exists(outputFile) && settings.SkipFileCheck)
                {
                    _logger.LogInformation("File {outputFile} already exists and skip check is enabled. Skipping.", outputFile);
                    continue;
                }

                try
                {
                    if (settings.RandomizeDelay)
                    {
                        var delay = Random.Shared.Next(settings.MinDelay, settings.MaxDelay);
                        _logger.LogDebug("Throttling: Waiting {delay}ms before downloading plugin file...", delay);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    await DownloadPluginFile(versionDetail.Value.DownloadUrl, outputFile, settings, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                    {
                        _logger.LogError("Failed to download {pluginName} v{version} for {product}. Reason: {message}", plugin.Name, version.Name, product, ex.Message);
                    }
                }
            }
        }
    }

    private async Task<(string? DownloadUrl, string[] CompatibleProducts, string? ReadmeContent)?> GetDetailedVersionInfo(MarketplacePlugin plugin, AddonVersionSummary version, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(version.Links?.Self?.Href)) return null;

        try
        {
            var selfUrl = version.Links.Self.Href.StartsWith("http")
                ? version.Links.Self.Href
                : $"https://marketplace.atlassian.com{version.Links.Self.Href}";

            var detailJson = await _client.GetStringAsync(selfUrl, cancellationToken);
            var detailVersion = JsonSerializer.Deserialize<AddonVersionDetail>(detailJson, jsonOptions);
            if (detailVersion is null) return null;

            var downloadUrl = detailVersion.Embedded?.Artifact?.Links?.Binary?.Href;
            if (downloadUrl != null && !downloadUrl.StartsWith("http"))
            {
                downloadUrl = $"https://marketplace.atlassian.com{downloadUrl}";
            }

            var compatibleProducts = detailVersion.Compatibilities?
                .Where(c => c.Application != null && (c.Hosting?.DataCenter != null || c.Hosting?.Server != null))
                .Select(c => c.Application!)
                .Distinct()
                .ToArray() ?? Array.Empty<string>();

            var compatibilityLines = detailVersion.Compatibilities?
                .Where(c => c.Application != null && (c.Hosting?.DataCenter != null || c.Hosting?.Server != null))
                .Select(c =>
                {
                    var range = c.Hosting?.DataCenter ?? c.Hosting?.Server;
                    var min = range?.Min?.Version;
                    var max = range?.Max?.Version;
                    return $"* **{c.Application?.ToUpperInvariant()}**: {min} - {max}";
                }) ?? Enumerable.Empty<string>();

            var converter = new Converter();
            var htmlNotes = detailVersion.Text?.ReleaseNotes ?? "No release notes provided for this version.";
            var markdownNotes = converter.Convert(htmlNotes);

            var readmeContent = $"""
            # Release Notes for {plugin.Name} v{detailVersion.Name}

            ## Key Information
            * **Plugin Key**: `{plugin.Key}`
            * **Version**: `{detailVersion.Name}`
            * **Release Date**: `{detailVersion.Release?.Date ?? "N/A"}`

            ## Compatibility
            {string.Join("\n", compatibilityLines)}

            ## Release Notes
            {markdownNotes}
            """;

            return (downloadUrl, compatibleProducts, readmeContent);
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound || httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogWarning("Could not get details for version {versionName} (HTTP {statusCode}). Skipping.", version.Name, httpEx.StatusCode);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed info for version {version}", version.Name);
            return null;
        }
    }

    private static string SanitizeFolderName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Trim();
    }

    private async Task<string> GetActualFileNameAsync(string downloadUrl, string? pluginKey, string? version, CancellationToken cancellationToken)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
            var headResponse = await _client.SendAsync(headRequest, cancellationToken);
            headResponse.EnsureSuccessStatusCode();

            if (headResponse.Content.Headers.ContentDisposition?.FileName != null)
            {
                var fileName = headResponse.Content.Headers.ContentDisposition.FileName.Trim('\"');
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    _logger.LogDebug("Resolved filename from Content-Disposition header: {fileName}", fileName);
                    return SanitizeFolderName(fileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "HEAD request for filename failed, falling back to URL parsing.");
        }

        _logger.LogDebug("Could not resolve filename from headers, parsing URL instead.");
        return GetFileNameFromUrl(downloadUrl, pluginKey, version);
    }

    private static string GetFileNameFromUrl(string downloadUrl, string? pluginKey, string? version)
    {
        try
        {
            var uri = new Uri(downloadUrl);
            var urlFileName = Path.GetFileName(uri.LocalPath);

            if (!string.IsNullOrWhiteSpace(urlFileName) && Path.HasExtension(urlFileName))
            {
                return urlFileName;
            }

            if (uri.Host == "marketplace.atlassian.com" && (uri.LocalPath.StartsWith("/files/") || uri.LocalPath.StartsWith("/download/")))
            {
                return $"{pluginKey ?? "plugin"}-{version ?? "unknown"}.jar";
            }

            return !string.IsNullOrWhiteSpace(urlFileName) ? urlFileName : $"{pluginKey ?? "plugin"}-{version ?? "unknown"}.jar";
        }
        catch
        {
            return $"{pluginKey ?? "plugin"}-{version ?? "unknown"}.jar";
        }
    }

    private async Task DownloadPluginFile(string downloadUrl, string outputFile, DownloaderSettings settings, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= settings.MaxRetries; attempt++)
        {
            try
            {
                if (File.Exists(outputFile) && !settings.SkipFileCheck)
                {
                    var localFileSize = new FileInfo(outputFile).Length;
                    using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                    var headResponse = await _client.SendAsync(headRequest, cancellationToken);

                    if (headResponse.IsSuccessStatusCode && headResponse.Content.Headers.ContentLength.HasValue)
                    {
                        var remoteFileSize = headResponse.Content.Headers.ContentLength.Value;
                        if (remoteFileSize == localFileSize)
                        {
                            _logger.LogInformation("File sizes match ({size} bytes). Skipping download of {outputFile}", remoteFileSize, outputFile);
                            return;
                        }
                        else
                        {
                            _logger.LogWarning("File sizes differ (remote: {remoteSize}, local: {localSize}). Re-downloading {outputFile}", remoteFileSize, localFileSize, outputFile);
                            File.Delete(outputFile);
                        }
                    }
                }

                using var outputStream = File.OpenWrite(outputFile);
                using var request = await _client.GetStreamAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("File successfully downloaded to {outputFile}", outputFile);
                return; // Success, exit the method
            }
            // MODIFIED: Added specific catch for 404/400 errors
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.NotFound || httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Download link for {file} is broken or unavailable (HTTP {statusCode}). Skipping this file.", outputFile, httpEx.StatusCode);

                // This is a final failure for this file. We should not retry. Exit the method.
                // First, ensure the failed partial file is deleted.
                try { if (File.Exists(outputFile)) File.Delete(outputFile); } catch { /* Ignore */ }
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Download of {outputFile} was canceled by the user.", outputFile);
                try { File.Delete(outputFile); } catch (Exception ex) { _logger.LogError(ex, "Failed to delete incomplete file {outputFile}", outputFile); }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {attempt} failed to download {url} to {outputFile}", attempt, downloadUrl, outputFile);
                if (attempt == settings.MaxRetries)
                {
                    try { File.Delete(outputFile); } catch (Exception removeEx) { _logger.LogError(removeEx, "Failed to remove incomplete file {outputFile}", outputFile); }
                    throw;
                }
                else
                {
                    await Task.Delay(settings.DelayBetweenRetries, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    #endregion

    #region Private Helpers - Products

    private async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetJson(string feedUrl, string? productVersion, CancellationToken cancellationToken)
    {
        var atlassianJson = await _client.GetStringAsync(feedUrl, cancellationToken).ConfigureAwait(false);
        const string dlPrefix = "downloads(";
        var json = atlassianJson.StartsWith(dlPrefix) ? atlassianJson.Trim()[dlPrefix.Length..^1] : atlassianJson;
        _logger.LogTrace("Downloaded json: {json}", json);
        var parsed = JsonSerializer.Deserialize<ResponseItem[]>(json, jsonOptions)!;
        _logger.LogDebug("Found {releaseCount} releases", parsed.Length);
        var versions = parsed
            .GroupBy(a => a.Version)
            .Where(a => productVersion is null || a.Key == productVersion)
            .ToDictionary(a => a.Key, a => a.ToArray());
        _logger.LogDebug("Found {releaseCount} releases", versions.Count);
        return (json, versions);
    }

    private IReadOnlyList<string> GetFeedUrls(Uri[]? customFeed) => customFeed != null
                ? customFeed.Select(a => a.ToString()).ToArray()
                : SourceInformation.AtlassianSources;

    private async Task DownloadFilesFromFeed(string feedUrl, IDictionary<string, ResponseItem[]> versions, DownloaderSettings settings, CancellationToken cancellationToken)
    {
        var feedDir = Path.Combine(settings.OutputDir, feedUrl.Split('/').Last().Replace(".json", ""));
        _logger.LogInformation("Download from JSON \"{feedUrl}\" started", feedUrl);
        foreach (var version in versions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.Combine(feedDir, version.Key);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            foreach (var file in version.Value)
            {
                if (file.ZipUrl is null) continue;
                var serverPath = file.ZipUrl.PathAndQuery;
                var outputFile = Path.Combine(directory, serverPath.Split('/').Last());

                if (File.Exists(outputFile) && settings.SkipFileCheck)
                {
                    _logger.LogWarning("File \"{outputFile}\" already exists. Download skipped.", outputFile);
                    continue;
                }
                if (settings.RandomizeDelay)
                {
                    var delay = Random.Shared.Next(settings.MinDelay, settings.MaxDelay);
                    _logger.LogDebug("Throttling: Waiting {delay}ms before next file...", delay);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                await DownloadFile(file, outputFile, settings, cancellationToken);
            }
        }
        _logger.LogInformation("All files from \"{feedUrl}\" successfully downloaded.", feedUrl);
    }

    private async Task DownloadFile(ResponseItem file, string outputFile, DownloaderSettings settings, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= settings.MaxRetries; attempt++)
        {
            try
            {
                if (File.Exists(outputFile) && !settings.SkipFileCheck)
                {
                    var localFileSize = new FileInfo(outputFile).Length;
                    using var headRequest = new HttpRequestMessage(HttpMethod.Head, file.ZipUrl);
                    var headResponse = await _client.SendAsync(headRequest, cancellationToken);

                    if (headResponse.IsSuccessStatusCode && headResponse.Content.Headers.ContentLength.HasValue)
                    {
                        var remoteFileSize = headResponse.Content.Headers.ContentLength.Value;
                        if (remoteFileSize == localFileSize)
                        {
                            _logger.LogInformation("Size of remote and local files are same for {outputFile}. Skipping.", outputFile);
                            return;
                        }
                        else
                        {
                            _logger.LogWarning("Size of remote and local files are not same for {outputFile}. Re-downloading.", outputFile);
                            File.Delete(outputFile);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(file.Md5))
                {
                    await File.WriteAllTextAsync(outputFile + ".md5", file.Md5, cancellationToken);
                }

                using var outputStream = File.OpenWrite(outputFile);
                using var request = await _client.GetStreamAsync(file.ZipUrl!, cancellationToken);
                await request.CopyToAsync(outputStream, cancellationToken);
                _logger.LogInformation("File \"{uri}\" successfully downloaded to \"{outputFile}\".", file.ZipUrl, outputFile);
                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Download of \"{uri}\" was canceled by the user.", file.ZipUrl);
                try { File.Delete(outputFile); } catch (Exception ex) { _logger.LogError(ex, "Failed to delete incomplete file {outputFile}", outputFile); }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attempt {attempt} failed to download file \"{uri}\".", attempt, file.ZipUrl);

                if (attempt == settings.MaxRetries)
                {
                    try { File.Delete(outputFile); } catch (Exception removeEx) { _logger.LogError(removeEx, "Failed to remove incomplete file \"{outputFile}\".", outputFile); }
                    throw;
                }
                else
                {
                    int delay;
                    if (settings.RandomizeDelay)
                    {
                        delay = Random.Shared.Next(settings.MinDelay, settings.MaxDelay);
                        _logger.LogDebug("Retry delay: Waiting random {delay}ms", delay);
                    }
                    else
                    {
                        delay = settings.DelayBetweenRetries;
                    }

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
    #endregion
}