using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpicMorg.Atlassian.Downloader
{
    class DonloaderService : IHostedService
    {
        private readonly ILogger<DonloaderService> logger;
        private readonly DownloaderOptions options;
        private readonly HttpClient client;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        private readonly string assemblyEnvironment = string.Format("[{1}, {0}]",
    System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
    System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        private readonly string assemblyVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        private readonly string assemblyName = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;
        const string assemblyBuildType =
#if DEBUG
                "[Debug]"
#else
            
                "[Release]"
#endif
            ;

        public DonloaderService(IHostApplicationLifetime hostApplicationLifetime, ILogger<DonloaderService> logger, HttpClient client, DownloaderOptions options)
        {
            this.logger = logger;
            this.client = client;
            this.options = options;
            this.hostApplicationLifetime = hostApplicationLifetime;
        }
        public const ConsoleColor DEFAULT = ConsoleColor.Blue;

        public static void WriteColorLine(string text, params object[] args)
        {
            Dictionary<char, ConsoleColor> colors = new()
            {
                { '!', ConsoleColor.Red },
                { '@', ConsoleColor.Green },
                { '#', ConsoleColor.Blue },
                { '$', ConsoleColor.Magenta },
                { '&', ConsoleColor.Yellow },
                { '%', ConsoleColor.Cyan }
            };
            // TODO: word wrap, backslash escapes
            text = string.Format(text, args);
            string chunk = "";
            bool paren = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (colors.ContainsKey(c) && StringNext(text, i) != ' ')
                {
                    Console.Write(chunk);
                    chunk = "";
                    if (StringNext(text, i) == '(')
                    {
                        i++; // skip past the paren
                        paren = true;
                    }
                    Console.ForegroundColor = colors[c];
                }
                else if (paren && c == ')')
                {
                    paren = false;
                    Console.ForegroundColor = DEFAULT;
                }
                else if (Console.ForegroundColor != DEFAULT)
                {
                    Console.Write(c);
                    if (c == ' ' && !paren)
                        Console.ForegroundColor = DEFAULT;
                }
                else
                    chunk += c;
            }
            Console.WriteLine(chunk);
            Console.ForegroundColor = DEFAULT;
        }

        public static char StringPrev(string text, int index)
        {
            return (index == 0 || text.Length == 0) ? '\0' : text[index - 1];
        }

        public static char StringNext(string text, int index)
        {
            return (index < text.Length) ? text[index + 1] : '\0';
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            SetConsoleTitle();
            if (options.Version)
            {
                logger.LogInformation($"{assemblyName} {assemblyVersion} {assemblyEnvironment} {assemblyBuildType}");
                Console.BackgroundColor = ConsoleColor.Black;
                WriteColorLine("%╔═╦═══════════════════════════════════════════════════════════════════════════════════════╦═╗");
                WriteColorLine("%╠═╝                  .''.                                                                 %╚═%╣");
                WriteColorLine("%║                 .:cc;.                                                                    %║");
                WriteColorLine("%║                .;cccc;.                                                                   %║");
                WriteColorLine("%║               .;cccccc;.             !╔══════════════════════════════════════════════╗     %║");
                WriteColorLine("%║               .:ccccccc;.            !║    " + assemblyName + "                      !║     %║");
                WriteColorLine("%║               'ccccccccc;.           !╠══════════════════════════════════════════════╣     %║");
                WriteColorLine("%║               ,cccccccccc;.          !║    &Code:    @kastkack                         !║     %║");
                WriteColorLine("%║               ,ccccccccccc;.         !║    &GFX:     @stam                             !║     %║");
                WriteColorLine("%║          .... .:ccccccccccc;.        !╠══════════════════════════════════════════════╣     %║");
                WriteColorLine("%║         .',,'..;cccccccccccc;.       !║    &Version: " + assemblyVersion + "                          !║     %║");
                WriteColorLine("%║        .,,,,,'.';cccccccccccc;.      !║    &GitHub:  $EpicMorg/atlassian-downloader    !║     %║");
                WriteColorLine("%║       .,;;;;;,'.':cccccccccccc;.     !╚══════════════════════════════════════════════╝     %║");
                WriteColorLine("%║      .;:;;;;;;,...:cccccccccccc;.                                                         %║");
                WriteColorLine("%║     .;:::::;;;;'. .;:ccccccccccc;.                                                        %║");
                WriteColorLine("%║    .:cc::::::::,.  ..:ccccccccccc;.                                                       %║");
                WriteColorLine("%║   .:cccccc:::::'     .:ccccccccccc;.                                                      %║");
                WriteColorLine("%║  .;:::::::::::,.      .;:::::::::::,.                                                     %║");
                WriteColorLine("%╠═╗ ............          ............                                                    %╔═╣");
                WriteColorLine("%╚═╩═══════════════════════════════════════════════════════════════════════════════════════╩═╝");
                Console.ResetColor();
            }
            else
            {
                var feedUrls = this.GetFeedUrls();

                logger.LogInformation($"Task started");
                foreach (var feedUrl in feedUrls)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    var (json, versions) = await this.GetJson(feedUrl, cancellationToken).ConfigureAwait(false);

                    switch (options.Action)
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
            logger.LogInformation($"Complete");

            this.hostApplicationLifetime.StopApplication();
        }

        private async Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetJson(string feedUrl, CancellationToken cancellationToken)
        {
            var atlassianJson = await client.GetStringAsync(feedUrl, cancellationToken).ConfigureAwait(false);
            var json = atlassianJson.Trim()["downloads(".Length..^1];
            logger.LogTrace("Downloaded json: {0}", json);
            var parsed = JsonSerializer.Deserialize<ResponseItem[]>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            logger.LogDebug("Found {0} releases", parsed.Length);
            var versions = parsed.GroupBy(a => a.Version).ToDictionary(a => a.Key, a => a.ToArray());
            logger.LogDebug("Found {0} releases", versions.Count);
            return (json, versions);
        }

        private string[] GetFeedUrls() => options.CustomFeed != null
            ? options.CustomFeed.Select(a => a.ToString()).ToArray()
            : new[] {
                "https://my.atlassian.com/download/feeds/archived/bamboo.json",
                "https://my.atlassian.com/download/feeds/archived/clover.json",
                "https://my.atlassian.com/download/feeds/archived/confluence.json",
                "https://my.atlassian.com/download/feeds/archived/crowd.json",
                "https://my.atlassian.com/download/feeds/archived/crucible.json",
                "https://my.atlassian.com/download/feeds/archived/fisheye.json",
                "https://my.atlassian.com/download/feeds/archived/jira-core.json",
                "https://my.atlassian.com/download/feeds/archived/jira-servicedesk.json",
                "https://my.atlassian.com/download/feeds/archived/jira-software.json",
                "https://my.atlassian.com/download/feeds/archived/jira.json",
                "https://my.atlassian.com/download/feeds/archived/stash.json",

                "https://my.atlassian.com/download/feeds/current/bamboo.json",
                "https://my.atlassian.com/download/feeds/current/clover.json",
                "https://my.atlassian.com/download/feeds/current/confluence.json",
                "https://my.atlassian.com/download/feeds/current/crowd.json",
                "https://my.atlassian.com/download/feeds/current/crucible.json",
                "https://my.atlassian.com/download/feeds/current/fisheye.json",
                "https://my.atlassian.com/download/feeds/current/jira-core.json",
                "https://my.atlassian.com/download/feeds/current/jira-servicedesk.json",
                "https://my.atlassian.com/download/feeds/current/jira-software.json",
                "https://my.atlassian.com/download/feeds/current/stash.json",

                "https://my.atlassian.com/download/feeds/eap/bamboo.json",
                "https://my.atlassian.com/download/feeds/eap/confluence.json",
                "https://my.atlassian.com/download/feeds/eap/jira.json",
                "https://my.atlassian.com/download/feeds/eap/jira-servicedesk.json",
                "https://my.atlassian.com/download/feeds/eap/stash.json",
				
				//https://raw.githubusercontent.com/EpicMorg/atlassian-json/master/json-backups/archived/sourcetree.json
				"https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/archived/sourcetree.json",
				
				//https://raw.githubusercontent.com/EpicMorg/atlassian-json/master/json-backups/current/sourcetree.json
				"https://raw.githack.com/EpicMorg/atlassian-json/master/json-backups/current/sourcetree.json"

            };

        private void SetConsoleTitle()
        {
            Console.Title = $@"{assemblyName} {assemblyVersion} {assemblyEnvironment} - {assemblyBuildType}";
        }

        private async Task DownloadFilesFromFeed(string feedUrl, IDictionary<string, ResponseItem[]> versions, CancellationToken cancellationToken)
        {

            var feedDir = Path.Combine(options.OutputDir, feedUrl[(feedUrl.LastIndexOf('/') + 1)..(feedUrl.LastIndexOf('.'))]);
            logger.LogInformation($"Download from JSON \"{feedUrl}\" started");
            foreach (var version in versions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var directory = Path.Combine(feedDir, version.Key);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                foreach (var file in version.Value)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (file.ZipUrl == null)
                    {
                        logger.LogWarning($"Empty ZipUrl found for version '{version.Key}' in {feedUrl}");
                        continue;
                    }
                    var serverPath = file.ZipUrl.PathAndQuery;
                    var outputFile = Path.Combine(directory, serverPath[(serverPath.LastIndexOf("/") + 1)..]);
                    if (!File.Exists(outputFile))
                    {
                        await DownloadFile(file, outputFile, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogWarning($"File \"{outputFile}\" already exists. Download from \"{file.ZipUrl}\" skipped.");
                    }
                }
            }
            logger.LogInformation($"All files from \"{feedUrl}\" successfully downloaded.");

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
                using var request = await client.GetStreamAsync(file.ZipUrl, cancellationToken).ConfigureAwait(false);
                await request.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception downloadEx)
            {
                logger.LogError(downloadEx, $"Failed to download file {file.ZipUrl} to {outputFile}.");
                try
                {
                    File.Delete(outputFile);
                }
                catch (Exception removeEx)
                {
                    logger.LogError(removeEx, $"Failed to remove incomplete file {outputFile}.");
                }
            }
            logger.LogInformation($"File \"{file.ZipUrl}\" successfully downloaded to \"{outputFile}\".");
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task StopAsync(CancellationToken cancellationToken) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}