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
    private readonly string UserAgentString = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:101.0) Gecko/20100101 Firefox/101.0";

    private readonly ILogger<DownloaderService> logger;
    private readonly DownloaderOptions options;
    private readonly HttpClient client;
    private readonly IHostApplicationLifetime hostApplicationLifetime;

    public DownloaderService(IHostApplicationLifetime hostApplicationLifetime, ILogger<DownloaderService> logger, HttpClient client, DownloaderOptions options)
    {
        this.logger = logger;
        this.client = client;
        client.DefaultRequestHeaders.Add("User-Agent", this.UserAgentString);
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

    private async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetJson(string feedUrl, string productVersion = null, CancellationToken cancellationToken = default)
    {
        var atlassianJson = await this.client.GetStringAsync(feedUrl, cancellationToken).ConfigureAwait(false);
        var json = atlassianJson.Trim()["downloads(".Length..^1];
        this.logger.LogTrace("Downloaded json: {0}", json);
        var parsed = JsonSerializer.Deserialize<ResponseItem[]>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
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
                    this.logger.LogWarning($"Empty ZipUrl found for version '{version.Key}' in {feedUrl}");
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
                    this.logger.LogWarning($"File \"{outputFile}\" already exists. File sizes will be compared.");
                    var localFileSize = new FileInfo(outputFile).Length;
                    this.logger.LogInformation($"Size of local file is {localFileSize} bytes.");
                    try
                    {
                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", this.UserAgentString);
                        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, file.ZipUrl));
                        if (response.IsSuccessStatusCode)
                        {
                            if (response.Content.Headers.ContentLength.HasValue)
                            {
                                var remoteFileSize = response.Content.Headers.ContentLength.Value;
                                this.logger.LogInformation($"Size of remote file is \"{remoteFileSize}\" bytes.");

                                if (remoteFileSize == localFileSize)
                                {
                                    this.logger.LogInformation($"Size of remote and local files and are same ({remoteFileSize} bytes and {localFileSize} bytes). Nothing to download. Operation skipped.");
                                }
                                else
                                {
                                    this.logger.LogWarning($"Size of remote and local files and are not same ({remoteFileSize} bytes and {localFileSize} bytes). Download started.");
                                    File.Delete(outputFile);
                                    await this.DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                this.logger.LogWarning($"Cant get size of remote file  \"{file.ZipUrl}\". May be server not support it feature. Sorry.");
                                continue;
                            }
                        }
                        else
                        {
                            this.logger.LogCritical($"Request execution error: \"{response.StatusCode}\". Sorry.");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        this.logger.LogCritical($"HTTP request error: \"{ex.Message}\", \"{ex.StackTrace}\", \"{ex.StatusCode}\". Sorry.");
                    }
                }
            }
        }

        this.logger.LogInformation($"All files from \"{feedUrl}\" successfully downloaded.");

    }

    private async Task DownloadFile(ResponseItem file, string outputFile, CancellationToken cancellationToken)
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
            this.logger.LogError(downloadEx, "Failed to download file \"{uri}\" to \"{outputFile}\".", file.ZipUrl, outputFile);
            try
            {
                File.Delete(outputFile);
            }
            catch (Exception removeEx)
            {
                this.logger.LogError(removeEx, $"Failed to remove incomplete file \"{outputFile}\".");
            }
        }

        this.logger.LogInformation($"File \"{file.ZipUrl}\" successfully downloaded to \"{outputFile}\".");
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task StopAsync(CancellationToken cancellationToken) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
