using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader
{
    public class Program
    {
        /// <summary>
        /// Atlassian archive downloader. See https://github.com/EpicMorg/atlassian-downloader for more info
        /// </summary>
        /// <param name="Action">Action to perform</param>
        /// <param name="OutputDir">Override output directory to download</param>
        /// <param name="customFeed">Override URIs to import</param>
        /// <param name="Version">Show credits banner</param>
        /// <param name="productVersion">Override target version to download some product. Advice: Use it with "customFeed".</param>
        static async Task Main(string OutputDir, Uri[] customFeed = null, DownloadAction Action = DownloadAction.Download, bool Version = false, string productVersion = null) => await
            Host
                .CreateDefaultBuilder()
                .ConfigureHostConfiguration(configHost => configHost.AddEnvironmentVariables())
                .ConfigureAppConfiguration((ctx, configuration) =>
                    configuration
                        .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
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
                       .AddSingleton(new DownloaderOptions(OutputDir, customFeed, Action, Version, productVersion))
                       .AddHttpClient())
                .RunConsoleAsync()
                .ConfigureAwait(false);
    }
}