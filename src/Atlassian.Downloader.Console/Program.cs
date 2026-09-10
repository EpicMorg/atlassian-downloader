// Atlassian.Downloader.Console/Program.cs
using EpicMorg.Atlassian.Downloader.ConsoleApp;
using EpicMorg.Atlassian.Downloader.Core;
using EpicMorg.Atlassian.Downloader.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader;

public class Program
{
    /// <summary>
    /// Atlassian archive downloader. See https://github.com/EpicMorg/atlassian-downloader for more info.
    /// </summary>
    /// <param name="action">Action to perform.</param>
    /// <param name="outputDir">Directory to download into. Defaults to the current directory.</param>
    /// <param name="pluginId">Marketplace plugin key. Required by "--action Plugin".</param>
    /// <param name="productVersion">Download only this version. Advice: use it together with "--custom-feed".</param>
    /// <param name="skipFileCheck">Do not compare sizes of files that already exist locally; keep them as they are.</param>
    /// <param name="userAgent">User agent to send. Ignored when "--random-user-agent" is set.</param>
    /// <param name="maxRetries">Attempts per file before giving up.</param>
    /// <param name="delayBetweenRetries">Pause between attempts, in milliseconds.</param>
    /// <param name="customFeed">Feed URIs to use instead of the built-in list. Repeat the option to pass several.</param>
    /// <param name="about">Show credits banner.</param>
    /// <param name="randomUserAgent">Pick a user agent at random instead of using "--user-agent". One is drawn per run.</param>
    /// <param name="randomDelay">Randomize the pause between downloads, between "--min-delay" and "--max-delay".</param>
    /// <param name="minDelay">Lower bound for "--random-delay", in milliseconds.</param>
    /// <param name="maxDelay">Upper bound for "--random-delay", in milliseconds.</param>
    static async Task Main(
        DownloadAction action = DownloadAction.Download,
        string? outputDir = null,
        string? pluginId = null,
        string? productVersion = null,
        bool skipFileCheck = false,
        string userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:101.0) Gecko/20100101 Firefox/101.0",
        int maxRetries = 5,
        int delayBetweenRetries = 2500,
        Uri[]? customFeed = null,
        bool about = false,
        bool randomUserAgent = false,
        bool randomDelay = false,
        int minDelay = 300,
        int maxDelay = 10000
        )
    {
        // Manually create the options object from the parsed parameters
        var options = new DownloaderOptions(
            outputDir ?? Environment.CurrentDirectory,
            customFeed,
            action,
            about,
            productVersion,
            skipFileCheck,
            userAgent,
            maxRetries,
            delayBetweenRetries,
            pluginId,
            randomUserAgent,
            randomDelay,
            minDelay,
            maxDelay
        );

        await Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton(options);
                services.AddHttpClient<AtlassianClient>();
                services.AddHostedService<Worker>();
            })
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration))
            .RunConsoleAsync();
    }
}