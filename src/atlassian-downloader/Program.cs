using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/*
 -o, --output-folder "path" --> set output folder
-v, --version --> show version
-h, --help --> show help
-l, --list, --links --> show all links to output
-f, -feed, --feed-url, -u, --url --> use json link
*/

namespace EpicMorg.Atlassian.Downloader {
    class Program {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputDir">Override output directory to download</param>
        /// <param name="list">Show all download links from feed without downloading</param>
        /// <param name="customFeed">Override URIs to import.</param>
        /// <returns></returns>
        static async Task Main(string outputDir = "atlassian", bool list = false, Uri[] customFeed=null) {
            
            var appTitle = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var appStartupDate = DateTime.Now;
            var appBuildType = "[Release]";
#if DEBUG
            appBuildType = "[Debug]";
#endif
             
            var feedUrls = customFeed != null
                ? customFeed.Select(a=>a.ToString()).ToArray()
                : new[] {
        "https://my.atlassian.com/download/feeds/archived/bamboo.json",
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
        "https://my.atlassian.com/download/feeds/eap/jira-servicedesk.json"
                };

            Console.Title = $"{appTitle} {appVersion} {appBuildType}";
            Console.WriteLine($"Task started at {appStartupDate}.");

            var client = new HttpClient();

            foreach (var feedUrl in feedUrls) {
                var feedDir = Path.Combine(outputDir, feedUrl[(feedUrl.LastIndexOf('/') + 1)..(feedUrl.LastIndexOf('.'))]);
                var atlassianJson = await client.GetStringAsync(feedUrl);
                var callString = "downloads(";
                var json = atlassianJson[callString.Length..^1];
                var parsed = JsonSerializer.Deserialize<ResponseArray[]>(json, new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
                var versionsProg = parsed.GroupBy(a => a.Version).ToDictionary(a => a.Key, a => a.ToArray());
                if (list) {
                    foreach (var versionProg in versionsProg) {
                        foreach (var file in versionProg.Value) {
                            Console.WriteLine(file.ZipUrl);
                        }
                    }
                } else {
                    Console.WriteLine($"[INFO] Download from JSON \"{feedUrl}\" started at {appStartupDate}.");
                    foreach (var versionProg in versionsProg) {
                        var directory = Path.Combine(feedDir, versionProg.Key);
                        if (!Directory.Exists(directory)) {
                            Directory.CreateDirectory(directory);
                        }
                        foreach (var file in versionProg.Value) {
                            if (file.ZipUrl == null) { continue; }
                            var serverPath = file.ZipUrl.PathAndQuery;
                            var outputFile = Path.Combine(directory, serverPath[(serverPath.LastIndexOf("/") + 1)..]);
                            if (!File.Exists(outputFile)) {
                                if (!string.IsNullOrEmpty(file.Md5)) {
                                    File.WriteAllText(outputFile + ".md5", file.Md5);
                                }
                                using var outputStream = File.OpenWrite(outputFile);
                                using var request = await client.GetStreamAsync(file.ZipUrl).ConfigureAwait(false);
                                await request.CopyToAsync(outputStream).ConfigureAwait(false);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"[INFO] File \"{file.ZipUrl}\" successfully downloaded to \"{outputFile}\".");
                                Console.ResetColor();
                            } else {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[WARN] File \"{outputFile}\" already exists. Download from \"{file.ZipUrl}\" skipped.");
                                Console.ResetColor();
                            }
                        }
                    }
                    Console.WriteLine($"[SUCCESS] All files from \"{feedUrl}\" successfully downloaded.");
                }
            }

            Console.WriteLine($"Download complete at {appStartupDate}.");

        }
    }

    public partial class ResponseArray {
        public string Description { get; set; }
        public string Edition { get; set; }
        public Uri ZipUrl { get; set; }
        public object TarUrl { get; set; }
        public string Md5 { get; set; }
        public string Size { get; set; }
        public string Released { get; set; }
        public string Type { get; set; }
        public string Platform { get; set; }
        public string Version { get; set; }
        public Uri ReleaseNotes { get; set; }
        public Uri UpgradeNotes { get; set; }
    }

}