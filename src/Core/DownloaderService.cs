namespace EpicMorg.Atlassian.Downloader.Core;

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
        BellsAndWhistles.ShowVersionInfo(this.logger);

        if (!this.options.Version && !string.IsNullOrWhiteSpace(this.options.OutputDir))
        {
            if (this.options.Action == DownloadAction.Plugin)
            {
                await this.HandlePluginAction(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var feedUrls = this.GetFeedUrls();

                this.logger.LogInformation($"Task started");
                foreach (var feedUrl in feedUrls)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var (json, versions) = await this.GetJson(feedUrl, this.options.ProductVersion, cancellationToken).ConfigureAwait(false);

                    switch (this.options.Action)
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

        this.logger.LogInformation($"Complete");
        this.hostApplicationLifetime.StopApplication();
    }

    // This new method will intelligently determine the filename
    private async Task<string> GetActualFileNameAsync(string downloadUrl, string? pluginKey, string? version, CancellationToken cancellationToken)
    {
        // Try to get filename from Content-Disposition header first
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
            var headResponse = await this.client.SendAsync(headRequest, cancellationToken);
            headResponse.EnsureSuccessStatusCode();

            if (headResponse.Content.Headers.ContentDisposition?.FileName != null)
            {
                // The header gives us the best possible name (e.g., "my-plugin.obr")
                var fileName = headResponse.Content.Headers.ContentDisposition.FileName.Trim('\"');
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    this.logger.LogDebug("Resolved filename from Content-Disposition header: {fileName}", fileName);
                    return SanitizeFolderName(fileName);
                }
            }
        }
        catch (Exception ex)
        {
            // This can fail (e.g., 404, timeout, or server doesn't support HEAD), which is okay.
            // We will just fall back to the old logic.
            this.logger.LogDebug(ex, "HEAD request for filename failed, falling back to URL parsing.");
        }

        // Fallback to our old logic if the header is missing or the request fails
        this.logger.LogDebug("Could not resolve filename from headers, parsing URL instead.");
        return GetFileNameFromUrl(downloadUrl, pluginKey, version);
    }

    // --- NEW LOGIC FOR PLUGIN DOWNLOADING ---

    private async Task HandlePluginAction(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this.options.PluginId))
        {
            this.logger.LogError("Plugin ID is required for Plugin action. Use --plugin-id parameter.");
            return;
        }

        this.logger.LogInformation("Starting plugin archival for {pluginId}", this.options.PluginId);

        try
        {
            var allVersions = await GetAllPluginVersions(this.options.PluginId, cancellationToken);
            if (!allVersions.Any())
            {
                this.logger.LogWarning("No versions found for plugin {pluginId}", this.options.PluginId);
                return;
            }

            this.logger.LogInformation("Found a total of {count} versions. Processing...", allVersions.Count);

            var pluginInfo = await GetPluginInfo(this.options.PluginId, cancellationToken);
            if (pluginInfo is null)
            {
                this.logger.LogError("Could not retrieve basic info for plugin {pluginId}", this.options.PluginId);
                return;
            }

            await DownloadPluginVersions(pluginInfo!, allVersions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // This catch is for graceful shutdown if cancellation is requested during non-download async operations.
            this.logger.LogWarning("Plugin download process was canceled by the user.");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred during the plugin download process for {pluginId}", this.options.PluginId);
        }
    }

    private async Task<MarketplacePlugin?> GetPluginInfo(string pluginId, CancellationToken cancellationToken)
    {
        var pluginInfoUrl = $"https://marketplace.atlassian.com/rest/2/addons/{pluginId}";
        this.logger.LogDebug("Getting plugin info from: {url}", pluginInfoUrl);
        var pluginInfoJson = await this.client.GetStringAsync(pluginInfoUrl, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<MarketplacePlugin>(pluginInfoJson, jsonOptions);
    }

    private async Task<List<AddonVersionSummary>> GetAllPluginVersions(string pluginId, CancellationToken cancellationToken)
    {
        var allVersions = new List<AddonVersionSummary>();
        var nextUrl = $"https://marketplace.atlassian.com/rest/2/addons/{pluginId}/versions";

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.logger.LogDebug("Fetching versions from: {url}", nextUrl);
            var responseJson = await this.client.GetStringAsync(nextUrl, cancellationToken).ConfigureAwait(false);
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
                this.logger.LogDebug("Skipping version {versionName} (not for Server/DC)", version.Name);
                continue;
            }

            var versionDetail = await GetDetailedVersionInfo(plugin, version, cancellationToken);
            if (versionDetail is null || !versionDetail.Value.CompatibleProducts.Any())
            {
                continue;
            }

            this.logger.LogInformation("Plugin {pluginKey} v{version} is compatible with: {products}", plugin.Key, version.Name, string.Join(", ", versionDetail.Value.CompatibleProducts));

            foreach (var product in versionDetail.Value.CompatibleProducts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sanitizedPluginName = SanitizeFolderName(plugin.Name ?? plugin.Key ?? this.options.PluginId!);
                var versionDir = Path.Combine(this.options.OutputDir, "plugins", product, sanitizedPluginName, SanitizeFolderName(version.Name!));

                if (!Directory.Exists(versionDir)) Directory.CreateDirectory(versionDir);

                var readmePath = Path.Combine(versionDir, "readme.md");
                await File.WriteAllTextAsync(readmePath, versionDetail.Value.ReadmeContent ?? "No release notes.", cancellationToken);

                if (string.IsNullOrWhiteSpace(versionDetail.Value.DownloadUrl))
                {
                    this.logger.LogWarning("No download URL found for version {version}", version.Name);
                    continue;
                }

                var fileName = await GetActualFileNameAsync(versionDetail.Value.DownloadUrl, plugin.Key, version.Name, cancellationToken);
                var outputFile = Path.Combine(versionDir, fileName);

                if (File.Exists(outputFile) && this.options.SkipFileCheck)
                {
                    this.logger.LogInformation("File {outputFile} already exists and skip check is enabled. Skipping.", outputFile);
                    continue;
                }

                await this.DownloadPluginFile(versionDetail.Value.DownloadUrl, outputFile, cancellationToken);
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

            var detailJson = await this.client.GetStringAsync(selfUrl, cancellationToken);
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

            var converter = new Converter();
            var htmlNotes = detailVersion.Text?.ReleaseNotes ?? "No release notes provided for this version.";
            var markdownNotes = converter.Convert(htmlNotes);

            var readmeContent = $"""
        # Release Notes for {plugin.Name} v{detailVersion.Name}

        ## Key Information
        * **Plugin Key**: `{plugin.Key}`
        * **Version**: `{detailVersion.Name}`
        * **Release Date**: `{detailVersion.Release?.Date ?? "N/A"}`
        * **Compatible Products**: `{string.Join(", ", compatibleProducts)}`

        ## Release Notes
        {markdownNotes}
        """;

            return (downloadUrl, compatibleProducts, readmeContent);
        }
        // MODIFIED: Added specific catch for cancellation
        catch (OperationCanceledException)
        {
            // This is not an error. It's an intentional stop request from the user.
            // We just let the exception bubble up to the higher-level handler in HandlePluginAction,
            // which will log a clean warning and stop the main loop.
            throw;
        }
        catch (Exception ex)
        {
            // This will now only catch REAL errors (network issues, bad JSON, etc.)
            this.logger.LogError(ex, "Error getting detailed info for version {version}", version.Name);
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

    private async Task DownloadPluginFile(string downloadUrl, string outputFile, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= options.MaxRetries; attempt++)
        {
            try
            {
                if (File.Exists(outputFile) && !this.options.SkipFileCheck)
                {
                    var localFileSize = new FileInfo(outputFile).Length;
                    using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                    var headResponse = await this.client.SendAsync(headRequest, cancellationToken);

                    if (headResponse.IsSuccessStatusCode && headResponse.Content.Headers.ContentLength.HasValue)
                    {
                        var remoteFileSize = headResponse.Content.Headers.ContentLength.Value;
                        if (remoteFileSize == localFileSize)
                        {
                            this.logger.LogInformation("File sizes match ({size} bytes). Skipping download of {outputFile}", remoteFileSize, outputFile);
                            return;
                        }
                        else
                        {
                            this.logger.LogWarning("File sizes differ (remote: {remoteSize}, local: {localSize}). Re-downloading {outputFile}", remoteFileSize, localFileSize, outputFile);
                            File.Delete(outputFile);
                        }
                    }
                }

                using var outputStream = File.OpenWrite(outputFile);
                using var request = await this.client.GetStreamAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);

                this.logger.LogInformation("File successfully downloaded to {outputFile}", outputFile);
                return;
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning("Download of {outputFile} was canceled by the user.", outputFile);
                try { File.Delete(outputFile); } catch (Exception ex) { this.logger.LogError(ex, "Failed to delete incomplete file {outputFile}", outputFile); }
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Attempt {attempt} failed to download {url} to {outputFile}", attempt, downloadUrl, outputFile);
                if (attempt == options.MaxRetries)
                {
                    try { File.Delete(outputFile); } catch (Exception removeEx) { this.logger.LogError(removeEx, "Failed to remove incomplete file {outputFile}", outputFile); }
                    throw;
                }
                else
                {
                    await Task.Delay(options.DelayBetweenRetries, cancellationToken).ConfigureAwait(false);
                }
            }
        }
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

    // --- ORIGINAL METHODS FOR PRODUCT DOWNLOADING ---

    private async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetJson(string feedUrl, string? productVersion = null, CancellationToken cancellationToken = default)
    {
        var atlassianJson = await this.client.GetStringAsync(feedUrl, cancellationToken).ConfigureAwait(false);
        const string dlPrefix = "downloads(";

        var json = atlassianJson.StartsWith(dlPrefix) ? atlassianJson.Trim()[dlPrefix.Length..^1] : atlassianJson;
        this.logger.LogTrace("Downloaded json: {json}", json);
        var parsed = JsonSerializer.Deserialize<ResponseItem[]>(json, jsonOptions)!;
        this.logger.LogDebug("Found {releaseCount} releases", parsed.Length);
        var versions = parsed
            .GroupBy(a => a.Version)
            .Where(a => productVersion is null || a.Key == productVersion)
            .ToDictionary(a => a.Key, a => a.ToArray());
        this.logger.LogDebug("Found {releaseCount} releases", versions.Count);
        return (json, versions);
    }

    private IReadOnlyList<string> GetFeedUrls() => this.options.CustomFeed != null
                ? this.options.CustomFeed.Select(a => a.ToString()).ToArray()
                : SourceInformation.AtlassianSources;

    private async Task DownloadFilesFromFeed(string feedUrl, IDictionary<string, ResponseItem[]> versions, CancellationToken cancellationToken)
    {
        var feedDir = Path.Combine(this.options.OutputDir, feedUrl[(feedUrl.LastIndexOf('/') + 1)..feedUrl.LastIndexOf('.')]);
        this.logger.LogInformation("Download from JSON \"{feedUrl}\" started", feedUrl);
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
                    this.logger.LogWarning("Empty ZipUrl found for version '{version}' in {feedUrl}", version.Key, feedUrl);
                    continue;
                }

                var serverPath = file.ZipUrl.PathAndQuery;
                var outputFile = Path.Combine(directory, serverPath[(serverPath.LastIndexOf('/') + 1)..]);
                if (!File.Exists(outputFile))
                {
                    await this.DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (this.options.SkipFileCheck == false)
                    {
                        this.logger.LogWarning("File \"{outputFile}\" already exists. File sizes will be compared.", outputFile);
                        var localFileSize = new FileInfo(outputFile).Length;
                        this.logger.LogInformation("Size of local file is {localFileSize} bytes.", localFileSize);

                        // NOTE: This part creates a new HttpClient, which is not ideal, but it's from the original code.
                        // For consistency, we leave it as is. The new plugin downloader uses the shared client.
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
                        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, file.ZipUrl), cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            if (response.Content.Headers.ContentLength.HasValue)
                            {
                                var remoteFileSize = response.Content.Headers.ContentLength.Value;
                                this.logger.LogInformation("Size of remote file is \"{remoteFileSize}\" bytes.", remoteFileSize);

                                if (remoteFileSize != localFileSize)
                                {
                                    this.logger.LogWarning("Size of remote and local files and are not same. Download started.");
                                    File.Delete(outputFile);
                                    await this.DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    this.logger.LogInformation("Size of remote and local files are same. Nothing to download. Operation skipped.");
                                }
                            }
                            else
                            {
                                this.logger.LogWarning("Cant get size of remote file \"{uri}\".", file.ZipUrl);
                            }
                        }
                        else
                        {
                            this.logger.LogError("Request execution error for {uri}: \"{statusCode}\".", file.ZipUrl, response.StatusCode);
                        }
                    }
                    else
                    {
                        logger.LogWarning("File \"{outputFile}\" already exists. Download from \"{uri}\" skipped.", outputFile, file.ZipUrl);
                    }
                }
            }
        }
        this.logger.LogInformation("All files from \"{feedUrl}\" successfully downloaded.", feedUrl);
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
                using var request = await this.client.GetStreamAsync(file.ZipUrl!, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);

                this.logger.LogInformation("File \"{uri}\" successfully downloaded to \"{outputFile}\".", file.ZipUrl, outputFile);
                return;
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning("Download of \"{uri}\" was canceled by the user.", file.ZipUrl);
                try { File.Delete(outputFile); } catch (Exception ex) { this.logger.LogError(ex, "Failed to delete incomplete file {outputFile}", outputFile); }
                throw;
            }
            catch (Exception downloadEx)
            {
                this.logger.LogError(downloadEx, "Attempt {attempt} failed to download file \"{uri}\".", attempt, file.ZipUrl);

                if (attempt == options.MaxRetries)
                {
                    try { File.Delete(outputFile); } catch (Exception removeEx) { this.logger.LogError(removeEx, "Failed to remove incomplete file \"{outputFile}\".", outputFile); }
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