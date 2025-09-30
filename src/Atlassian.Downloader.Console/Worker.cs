using atlassian_downloader;
using EpicMorg.Atlassian.Downloader.Core;
using EpicMorg.Atlassian.Downloader.Core.Models;
using EpicMorg.Atlassian.Downloader.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader.ConsoleApp;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly AtlassianClient _atlassianClient;
    private readonly DownloaderOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IHostApplicationLifetime appLifetime,
        AtlassianClient atlassianClient,
        DownloaderOptions options)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _atlassianClient = atlassianClient;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // First, always show the banner
        BellsAndWhistles.ShowVersionInfo(_logger);

        // --- THIS IS THE FIX ---
        // If the --about flag was used, our only job is to show the banner.
        // We stop the application and immediately exit the method.
        if (_options.About)
        {
            _appLifetime.StopApplication();
            return;
        }
        try
        {
            var settings = new DownloaderSettings
            {
                OutputDir = _options.OutputDir,
                SkipFileCheck = _options.SkipFileCheck,
                UserAgent = _options.UserAgent,
                MaxRetries = _options.MaxRetries,
                DelayBetweenRetries = _options.DelayBetweenRetries,
                CustomFeed = _options.CustomFeed,
                ProductVersion = _options.ProductVersion
            };

            switch (_options.Action)
            {
                case DownloadAction.Plugin:
                    if (string.IsNullOrWhiteSpace(_options.PluginId))
                    {
                        _logger.LogError("Action 'Plugin' requires a --plugin-id argument.");
                    }
                    else
                    {
                        await _atlassianClient.DownloadPluginAsync(_options.PluginId, settings, cancellationToken);
                    }
                    break;

                case DownloadAction.Download:
                    await _atlassianClient.DownloadProductsAsync(settings, cancellationToken);
                    break;

                case DownloadAction.ListURLs:
                case DownloadAction.ListVersions:
                case DownloadAction.ShowRawJson:
                    var feedUrls = _atlassianClient.GetProductFeedUrls(settings);
                    foreach (var feedUrl in feedUrls)
                    {
                        var (json, versions) = await _atlassianClient.GetProductDataAsync(feedUrl, settings, cancellationToken);

                        if (_options.Action == DownloadAction.ShowRawJson)
                        {
                            Console.Out.WriteLine(json);
                        }
                        else if (_options.Action == DownloadAction.ListVersions)
                        {
                            foreach (var v in versions.Keys) Console.Out.WriteLine(v);
                        }
                        else if (_options.Action == DownloadAction.ListURLs)
                        {
                            foreach (var url in versions.SelectMany(v => v.Value).Select(f => f.ZipUrl))
                            {
                                if (url != null) Console.Out.WriteLine(url);
                            }
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                _logger.LogCritical(ex, "An unhandled exception occurred.");
            }
        }
        finally
        {
            _logger.LogInformation("Execution finished. Application will now shut down.");
            _appLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}