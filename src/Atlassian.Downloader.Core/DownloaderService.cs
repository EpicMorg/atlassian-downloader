namespace Atlassian.Downloader.Core;
using EpicMorg.Atlassian.Downloader;

using EpicMorg.Atlassian.Downloader.Models;
using Microsoft.Extensions.Hosting;
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

internal class DownloaderService : IHostedService
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly ILogger<DownloaderService> logger;
    private readonly DownloaderOptions options;
    private readonly HttpClient client;
    private readonly IHostApplicationLifetime hostApplicationLifetime;

    public DownloaderService(IHostApplicationLifetime hostApplicationLifetime, ILogger<DownloaderService> logger, HttpClient client, DownloaderOptions options)
    {
        this.logger = logger;
        this.client = client;
        client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        this.options = options;
        this.hostApplicationLifetime = hostApplicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        BellsAndWhistles.SetConsoleTitle();
        BellsAndWhistles.ShowVersionInfo(logger);

        if (!options.Version && !string.IsNullOrWhiteSpace(options.OutputDir))
        {
            if (options.Action == DownloadAction.Plugin)
            {
                await HandlePluginAction(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var feedUrls = GetFeedUrls();

                logger.LogInformation($"Task started");
                foreach (var feedUrl in feedUrls)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var (json, versions) = await this.GetJson(feedUrl, options.ProductVersion, cancellationToken).ConfigureAwait(false);

                    switch (options.Action)
                    {
                        case DownloadAction.ShowRawJson:
                            Console.Out.WriteLine(json);
                            break;
                        case DownloadAction.Download:
                            await this.DownloadFilesFromFeed(feedUrl, versions, cancellationToken).ConfigureAwait(false);
                            break;
                        case DownloadAction.ListURLs:
                            foreach (var versionProg in versions)
                            {
                                foreach (var file in versionProg.Value)
                                {
                                    Console.Out.WriteLine(file.ZipUrl);
                                }
                            }
                            break;
                        case DownloadAction.ListVersions:
                            foreach (var versionProg in versions)
                            {
                                foreach (var file in versionProg.Value)
                                {
                                    Console.Out.WriteLine(file.Version);
                                }
                            }
                            break;
                    }
                }
            }
        }

        logger.LogInformation($"Complete");
        hostApplicationLifetime.StopApplication();
    }

    // --- NEW LOGIC FOR PLUGIN DOWNLOADING ---

    private async Task HandlePluginAction(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.PluginId))
        {
            logger.LogError("Plugin ID is required for Plugin action. Use --plugin-id parameter.");
            return;
        }

        logger.LogInformation("Starting plugin archival for {pluginId}", options.PluginId);

        try
        {
            var allVersions = await GetAllPluginVersions(options.PluginId, cancellationToken);
            if (!allVersions.Any())
            {
                logger.LogWarning("No versions found for plugin {pluginId}", options.PluginId);
                return;
            }

            logger.LogInformation("Found a total of {count} versions. Processing...", allVersions.Count);

            var pluginInfo = await GetPluginInfo(options.PluginId, cancellationToken);
            if (pluginInfo is null)
            {
                logger.LogError("Could not retrieve basic info for plugin {pluginId}", options.PluginId);
                return;
            }

            await DownloadPluginVersions(pluginInfo!, allVersions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // This catch is for graceful shutdown if cancellation is requested during non-download async operations.
            logger.LogWarning("Plugin download process was canceled by the user.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the plugin download process for {pluginId}", options.PluginId);
        }
    }

    private async Task<MarketplacePlugin?> GetPluginInfo(string pluginId, CancellationToken cancellationToken)
    {
        var pluginInfoUrl = $"https://marketplace.atlassian.com/rest/2/addons/{pluginId}";
        logger.LogDebug("Getting plugin info from: {url}", pluginInfoUrl);
        var pluginInfoJson = await client.GetStringAsync(pluginInfoUrl, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<MarketplacePlugin>(pluginInfoJson, jsonOptions);
    }

    private async Task<List<AddonVersionSummary>> GetAllPluginVersions(string pluginId, CancellationToken cancellationToken)
    {
        var allVersions = new List<AddonVersionSummary>();
        var nextUrl = $"https://marketplace.atlassian.com/rest/2/addons/{pluginId}/versions";

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("Fetching versions from: {url}", nextUrl);
            var responseJson = await client.GetStringAsync(nextUrl, cancellationToken).ConfigureAwait(false);
            var page = JsonSerializer.Deserialize<AddonVersionCollection>(responseJson, jsonOptions);

            if (page?.Embedded?.Versions != null)
            {
                allVersions.AddRange(page.Embedded.Versions);
            }

            nextUrl = page?.Links?.Next?.Href;
            if (nextUrl != null && !nextUrl.StartsWith("http"))
            {
                nextUrl = $"https://marketplace.atlassian.com{nextUrl}";
            }

        } while (!string.IsNullOrWhiteSpace(nextUrl));

        return allVersions;
    }

    private async Task DownloadPluginVersions(MarketplacePlugin plugin, IEnumerable<AddonVersionSummary> versions, CancellationToken cancellationToken)
    {
        foreach (var version in versions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (version.Deployment?.Server != true && version.Deployment?.DataCenter != true)
            {
                logger.LogDebug("Skipping version {versionName} (not for Server/DC)", version.Name);
                continue;
            }

            var versionDetail = await GetDetailedVersionInfo(plugin, version, cancellationToken);
            if (versionDetail is null || !versionDetail.Value.CompatibleProducts.Any())
            {
                continue;
            }

            logger.LogInformation("Plugin {pluginKey} v{version} is compatible with: {products}", plugin.Key, version.Name, string.Join(", ", versionDetail.Value.CompatibleProducts));

            foreach (var product in versionDetail.Value.CompatibleProducts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sanitizedPluginName = SanitizeFolderName(plugin.Name ?? plugin.Key ?? options.PluginId!);
                var versionDir = Path.Combine(options.OutputDir, "plugins", product, sanitizedPluginName, SanitizeFolderName(version.Name!));

                if (!Directory.Exists(versionDir)) Directory.CreateDirectory(versionDir);

                var readmePath = Path.Combine(versionDir, "readme.md");
                await File.WriteAllTextAsync(readmePath, versionDetail.Value.ReadmeContent ?? "No release notes.", cancellationToken);

                if (string.IsNullOrWhiteSpace(versionDetail.Value.DownloadUrl))
                {
                    logger.LogWarning("No download URL found for version {version}", version.Name);
                    continue;
                }

                var fileName = await GetActualFileNameAsync(versionDetail.Value.DownloadUrl, plugin.Key, version.Name, cancellationToken);
                var outputFile = Path.Combine(versionDir, fileName);

                if (File.Exists(outputFile) && options.SkipFileCheck)
                {
                    logger.LogInformation("File {outputFile} already exists and skip check is enabled. Skipping.", outputFile);
                    continue;
                }

                try
                {
                    await DownloadPluginFile(versionDetail.Value.DownloadUrl, outputFile, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                    {
                        logger.LogError("Failed to download {pluginName} v{version} for {product}. Reason: {message}", plugin.Name, version.Name, product, ex.Message);
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

            var detailJson = await client.GetStringAsync(selfUrl, cancellationToken);
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
                    return $"* **{c.Application.ToUpperInvariant()}**: {min} - {max}";
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
            logger.LogWarning("Could not get details for version {versionName} (HTTP {statusCode}). Skipping.", version.Name, httpEx.StatusCode);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting detailed info for version {version}", version.Name);
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
            var headResponse = await client.SendAsync(headRequest, cancellationToken);
            headResponse.EnsureSuccessStatusCode();

            if (headResponse.Content.Headers.ContentDisposition?.FileName != null)
            {
                var fileName = headResponse.Content.Headers.ContentDisposition.FileName.Trim('\"');
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    logger.LogDebug("Resolved filename from Content-Disposition header: {fileName}", fileName);
                    return SanitizeFolderName(fileName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "HEAD request for filename failed, falling back to URL parsing.");
        }

        logger.LogDebug("Could not resolve filename from headers, parsing URL instead.");
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

    private async Task DownloadPluginFile(string downloadUrl, string outputFile, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= options.MaxRetries; attempt++)
        {
            try
            {
                if (File.Exists(outputFile) && !options.SkipFileCheck)
                {
                    var localFileSize = new FileInfo(outputFile).Length;
                    using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                    var headResponse = await client.SendAsync(headRequest, cancellationToken);

                    if (headResponse.IsSuccessStatusCode && headResponse.Content.Headers.ContentLength.HasValue)
                    {
                        var remoteFileSize = headResponse.Content.Headers.ContentLength.Value;
                        if (remoteFileSize == localFileSize)
                        {
                            logger.LogInformation("File sizes match ({size} bytes). Skipping download of {outputFile}", remoteFileSize, outputFile);
                            return;
                        }
                        else
                        {
                            logger.LogWarning("File sizes differ (remote: {remoteSize}, local: {localSize}). Re-downloading {outputFile}", remoteFileSize, localFileSize, outputFile);
                            File.Delete(outputFile);
                        }
                    }
                }

                using var outputStream = File.OpenWrite(outputFile);
                using var request = await client.GetStreamAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("File successfully downloaded to {outputFile}", outputFile);
                return;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Download of {outputFile} was canceled by the user.", outputFile);
                try { File.Delete(outputFile); } catch (Exception ex) { logger.LogError(ex, "Failed to delete incomplete file {outputFile}", outputFile); }
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attempt {attempt} failed to download {url} to {outputFile}", attempt, downloadUrl, outputFile);
                if (attempt == options.MaxRetries)
                {
                    try { File.Delete(outputFile); } catch (Exception removeEx) { logger.LogError(removeEx, "Failed to remove incomplete file {outputFile}", outputFile); }
                    throw;
                }
                else
                {
                    await Task.Delay(options.DelayBetweenRetries, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    // --- ORIGINAL METHODS FOR PRODUCT DOWNLOADING ---

    private async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetJson(string feedUrl, string? productVersion = null, CancellationToken cancellationToken = default)
    {
        var atlassianJson = await client.GetStringAsync(feedUrl, cancellationToken).ConfigureAwait(false);
        const string dlPrefix = "downloads(";

        var json = atlassianJson.StartsWith(dlPrefix) ? atlassianJson.Trim()[dlPrefix.Length..^1] : atlassianJson;
        logger.LogTrace("Downloaded json: {json}", json);
        var parsed = JsonSerializer.Deserialize<ResponseItem[]>(json, jsonOptions)!;
        logger.LogDebug("Found {releaseCount} releases", parsed.Length);
        var versions = parsed
            .GroupBy(a => a.Version)
            .Where(a => productVersion is null || a.Key == productVersion)
            .ToDictionary(a => a.Key, a => a.ToArray());
        logger.LogDebug("Found {releaseCount} releases", versions.Count);
        return (json, versions);
    }

    private IReadOnlyList<string> GetFeedUrls() => options.CustomFeed != null
                ? options.CustomFeed.Select(a => a.ToString()).ToArray()
                : SourceInformation.AtlassianSources;

    private async Task DownloadFilesFromFeed(string feedUrl, IDictionary<string, ResponseItem[]> versions, CancellationToken cancellationToken)
    {
        var feedDir = Path.Combine(options.OutputDir, feedUrl[(feedUrl.LastIndexOf('/') + 1)..feedUrl.LastIndexOf('.')]);
        logger.LogInformation("Download from JSON \"{feedUrl}\" started", feedUrl);
        foreach (var version in versions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.Combine(feedDir, version.Key);
            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            foreach (var file in version.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (file.ZipUrl == null)
                {
                    logger.LogWarning("Empty ZipUrl found for version '{version}' in {feedUrl}", version.Key, feedUrl);
                    continue;
                }

                var serverPath = file.ZipUrl.PathAndQuery;
                var outputFile = Path.Combine(directory, serverPath[(serverPath.LastIndexOf('/') + 1)..]);
                if (!File.Exists(outputFile))
                {
                    await DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (options.SkipFileCheck == false)
                    {
                        logger.LogWarning("File \"{outputFile}\" already exists. File sizes will be compared.", outputFile);
                        var localFileSize = new FileInfo(outputFile).Length;
                        logger.LogInformation("Size of local file is {localFileSize} bytes.", localFileSize);

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
                        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, file.ZipUrl), cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            if (response.Content.Headers.ContentLength.HasValue)
                            {
                                var remoteFileSize = response.Content.Headers.ContentLength.Value;
                                logger.LogInformation("Size of remote file is \"{remoteFileSize}\" bytes.", remoteFileSize);

                                if (remoteFileSize != localFileSize)
                                {
                                    logger.LogWarning("Size of remote and local files and are not same. Download started.");
                                    File.Delete(outputFile);
                                    await DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    logger.LogInformation("Size of remote and local files are same. Nothing to download. Operation skipped.");
                                }
                            }
                            else
                            {
                                logger.LogWarning("Cant get size of remote file \"{uri}\".", file.ZipUrl);
                            }
                        }
                        else
                        {
                            logger.LogError("Request execution error for {uri}: \"{statusCode}\".", file.ZipUrl, response.StatusCode);
                        }
                    }
                    else
                    {
                        logger.LogWarning("File \"{outputFile}\" already exists. Download from \"{uri}\" skipped.", outputFile, file.ZipUrl);
                    }
                }
            }
        }
        logger.LogInformation("All files from \"{feedUrl}\" successfully downloaded.", feedUrl);
    }

    private async Task DownloadFile(ResponseItem file, string outputFile, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= options.MaxRetries; attempt++)
        {
            if (!string.IsNullOrEmpty(file.Md5))
            {
                await File.WriteAllTextAsync(outputFile + ".md5", file.Md5, cancellationToken);
            }

            try
            {
                using var outputStream = File.OpenWrite(outputFile);
                using var request = await client.GetStreamAsync(file.ZipUrl!, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("File \"{uri}\" successfully downloaded to \"{outputFile}\".", file.ZipUrl, outputFile);
                return;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Download of \"{uri}\" was canceled by the user.", file.ZipUrl);
                try { File.Delete(outputFile); } catch (Exception ex) { logger.LogError(ex, "Failed to delete incomplete file {outputFile}", outputFile); }
                throw;
            }
            catch (Exception downloadEx)
            {
                logger.LogError(downloadEx, "Attempt {attempt} failed to download file \"{uri}\".", attempt, file.ZipUrl);

                if (attempt == options.MaxRetries)
                {
                    try { File.Delete(outputFile); } catch (Exception removeEx) { logger.LogError(removeEx, "Failed to remove incomplete file \"{outputFile}\".", outputFile); }
                    throw;
                }
                else
                {
                    await Task.Delay(options.DelayBetweenRetries, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task StopAsync(CancellationToken cancellationToken) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}