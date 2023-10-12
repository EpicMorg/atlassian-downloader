namespace EpicMorg.Atlassian.Downloader;

using EpicMorg.Atlassian.Downloader.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.Threading.Tasks;

public class Program
{
    /// <summary>
    /// Atlassian archive downloader. See https://github.com/EpicMorg/atlassian-downloader for more info
    /// </summary>
    /// <param name="action">Action to perform</param>
    /// <param name="outputDir">Override output directory to download</param>
    /// <param name="customFeed">Override URIs to import</param>
    /// <param name="about">Show credits banner</param>
    /// <param name="productVersion">Override target version to download some product. Advice: Use it with "customFeed".</param>
    static async Task Main(string outputDir, Uri[] customFeed = null, DownloadAction action = DownloadAction.Download, bool about = false, string productVersion = null) => await
        Host
            .CreateDefaultBuilder()
            .ConfigureHostConfiguration(configHost => configHost.AddEnvironmentVariables())
            .ConfigureAppConfiguration((ctx, configuration) =>
                configuration
                    .SetBasePath(System.AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables())
            .ConfigureServices((ctx, services) => services
                   .AddOptions()
                   .AddLogging(builder =>
                   {
                       Log.Logger = new LoggerConfiguration()
                              .ReadFrom.Configuration(ctx.Configuration)
                              .CreateLogger();
                       _ = builder
                            .ClearProviders()
                            .AddSerilog(dispose: true);
                   })
                   .AddHostedService<DownloaderService>()
                   .AddSingleton(new DownloaderOptions(outputDir, customFeed, action, about, productVersion))
                   .AddHttpClient())
            .RunConsoleAsync()
            .ConfigureAwait(false);
}