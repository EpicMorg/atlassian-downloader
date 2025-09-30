// Atlassian.Downloader.Console/Program.cs
using EpicMorg.Atlassian.Downloader.ConsoleApp;
using EpicMorg.Atlassian.Downloader.Core;
using EpicMorg.Atlassian.Downloader.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader; // Keeping your original namespace

public class Program
{
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
        bool about = false 
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
            pluginId
        );

        await Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton(options); // Register the created options object
                services.AddHttpClient<AtlassianClient>();
                services.AddHostedService<Worker>();
            })
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration))
            .RunConsoleAsync();
    }
}