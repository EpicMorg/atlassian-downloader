namespace EpicMorg.Atlassian.Downloader.ConsoleApp;

using atlassian_downloader;
using EpicMorg.Atlassian.Downloader.Core;
using EpicMorg.Atlassian.Downloader.Core.Models;
using EpicMorg.Atlassian.Downloader.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq; // ADDED
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient<AtlassianClient>();
                services.Configure<DownloaderOptions>(hostContext.Configuration);
                services.AddHostedService<Worker>();
            })
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration))
            .RunConsoleAsync();
    }
}

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
        IOptions<DownloaderOptions> options)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _atlassianClient = atlassianClient;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        BellsAndWhistles.ShowVersionInfo(_logger);

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
                    // MODIFIED: Calling the specific download method
                    await _atlassianClient.DownloadProductsAsync(settings, cancellationToken);
                    break;

                // Logic for listing actions is now correctly handled here in the UI layer
                case DownloadAction.ListURLs:
                case DownloadAction.ListVersions:
                case DownloadAction.ShowRawJson:
                    var feedUrls = _atlassianClient.GetProductFeedUrls(settings);
                    foreach (var feedUrl in feedUrls)
                    {
                        var (json, versions) = await _atlassianClient.GetProductDataAsync(feedUrl, settings, cancellationToken);
                        if (_options.Action == DownloadAction.ShowRawJson) Console.Out.WriteLine(json);
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
                _logger.LogCritical(ex, "An unhandled exception occurred during execution.");
            }
        }
        finally
        {
            _logger.LogInformation("Execution finished. Application will now shut down.");
            _appLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}