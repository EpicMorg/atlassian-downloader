using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader
{
    public class Program
    {
        /// <summary>
        /// Atlassian archive downloader. See https://github.com/EpicMorg/atlassian-downloader for more info
        /// </summary>
        /// <param name="Action">Action to perform</param>
        /// <param name="OutputDir">Override output directory to download.</param>
        /// <param name="customFeed">Override URIs to import.</param>
        static async Task Main(string OutputDir = "atlassian", Uri[] customFeed = null, DownloadAction Action = DownloadAction.Download) => await
            Host
                .CreateDefaultBuilder()
                .ConfigureHostConfiguration(configHost => configHost.AddEnvironmentVariables())
                .ConfigureAppConfiguration((ctx, configuration) =>
                    configuration
                        .SetBasePath(Environment.CurrentDirectory)
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
                            builder
                                .ClearProviders()
                                .AddSerilog(dispose: true);
                       })
                       .AddHostedService<DonloaderService>()
                       .AddSingleton(new DownloaderOptions(OutputDir, customFeed, Action))
                       .AddHttpClient())
                .RunConsoleAsync()
                .ConfigureAwait(false);
    }
}