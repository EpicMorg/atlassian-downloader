namespace EpicMorg.Atlassian.Downloader.Core;

using EpicMorg.Atlassian.Downloader.Models;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var feedUrls = this.GetFeedUrls();

            this.logger.LogInformation($"Task started");
            foreach (var feedUrl in feedUrls)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

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

        this.logger.LogInformation($"Complete");

        this.hostApplicationLifetime.StopApplication();
    }

    private async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetJson(string feedUrl, string? productVersion = null, CancellationToken cancellationToken = default)
    {
        var atlassianJson = await this.client.GetStringAsync(feedUrl, cancellationToken).ConfigureAwait(false);
        var json = atlassianJson.Trim()["downloads(".Length..^1];
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
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var directory = Path.Combine(feedDir, version.Key);
            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            foreach (var file in version.Value)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

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
                        try
                        {
                            var httpClient = new HttpClient();
                            httpClient.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
                            var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, file.ZipUrl), cancellationToken);
                            if (response.IsSuccessStatusCode)
                            {
                                if (response.Content.Headers.ContentLength.HasValue)
                                {
                                    var remoteFileSize = response.Content.Headers.ContentLength.Value;
                                    this.logger.LogInformation("Size of remote file is \"{remoteFileSize}\" bytes.", remoteFileSize);

                                    if (remoteFileSize == localFileSize)
                                    {
                                        this.logger.LogInformation(
                                            "Size of remote and local files and are same ({remoteFileSize} bytes and {localFileSize} bytes). Nothing to download. Operation skipped.",
                                            remoteFileSize,
                                            localFileSize);
                                    }
                                    else
                                    {
                                        this.logger.LogWarning(
                                            "Size of remote and local files and are not same ({remoteFileSize} bytes and {localFileSize} bytes). Download started.",
                                            remoteFileSize,
                                            localFileSize);
                                        File.Delete(outputFile);
                                        await this.DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    this.logger.LogWarning("Cant get size of remote file  \"{uri}\". May be server not support it feature. Sorry.", file.ZipUrl);
                                    continue;
                                }
                            }
                            else
                            {
                                this.logger.LogError("Request execution error for {uri}: \"{statusCode}\". Sorry.", file.ZipUrl, response.StatusCode);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            this.logger.LogError(ex, "HTTP request error for {uri}: \"{message}\", \"{statusCode}\". Sorry.", file.ZipUrl, ex.Message, ex.StatusCode);
                        }
                    }
                    else
                    {
                        logger.LogWarning("File \"{outputFile}\" already exists. Download from \"{uri}\" skipped.", outputFile, file.ZipUrl);
                        continue;
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
                File.WriteAllText(outputFile + ".md5", file.Md5);
            }

            try
            {
                using var outputStream = File.OpenWrite(outputFile);
                using var request = await this.client.GetStreamAsync(file.ZipUrl, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception downloadEx)
            {
                this.logger.LogError(downloadEx, "Attempt {attempt} failed to download file \"{uri}\" to \"{outputFile}\".", attempt, file.ZipUrl, outputFile);

                if (attempt == options.MaxRetries)
                {
                    this.logger.LogError(downloadEx, "Failed to download file \"{uri}\" to \"{outputFile}\".", file.ZipUrl, outputFile);
                    try
                    {
                        File.Delete(outputFile);
                    }
                    catch (Exception removeEx)
                    {
                        this.logger.LogError(removeEx, "Failed to remove incomplete file \"{outputFile}\".", outputFile);
                    }
                    throw; 
                }
                else
                {
                    await Task.Delay(options.DelayBetweenRetries, cancellationToken).ConfigureAwait(false);
                }
            }
            this.logger.LogInformation("File \"{uri}\" successfully downloaded to \"{outputFile}\".", file.ZipUrl, outputFile);
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task StopAsync(CancellationToken cancellationToken) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

}
