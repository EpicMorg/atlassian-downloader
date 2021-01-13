using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

var outputDir = "output";
var feedUrls =
    new[] {
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
        "https://my.atlassian.com/download/feeds/current/stash.json"
    };



var client = new HttpClient();
foreach (var feedUrl in feedUrls)
{
    var feedDir = Path.Combine(outputDir, feedUrl[(feedUrl.LastIndexOf('/') + 1)..(feedUrl.LastIndexOf('.'))]);
    var atlassianJson = await client.GetStringAsync(feedUrl);
    var callString = "downloads(";
    var json = atlassianJson[callString.Length..^1];
    var parsed = JsonSerializer.Deserialize<ResponseArray[]>(json, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    });
    var versions = parsed.GroupBy(a => a.Version).ToDictionary(a => a.Key, a => a.ToArray());

    foreach (var version in versions)
    {
        var directory = Path.Combine(feedDir, version.Key);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        foreach (var file in version.Value)
        {
            var serverPath = file.ZipUrl.PathAndQuery;
            var outputFile = Path.Combine(directory, serverPath[(serverPath.LastIndexOf("/") + 1)..]);
            if (!File.Exists(outputFile))
            {
                if (!string.IsNullOrEmpty(file.Md5))
                {
                    File.WriteAllText(outputFile + ".md5", file.Md5);
                }
                using var outputStream = File.OpenWrite(outputFile);
                using var request = await client.GetStreamAsync(file.ZipUrl).ConfigureAwait(false);
                await request.CopyToAsync(outputStream).ConfigureAwait(false);
                Console.WriteLine($"Downloaded {outputFile}");
            }
            else
            {
                Console.WriteLine($"File for {file.ZipUrl} already exists");
            }
        }
    }
    Console.WriteLine($"Downloaded all files from " +
        $"{feedUrl}");
}
Console.WriteLine("Download complete");


public partial class ResponseArray
{
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