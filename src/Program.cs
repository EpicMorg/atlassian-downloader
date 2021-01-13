using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

var output = "output";
var feedUrl = "https://my.atlassian.com/download/feeds/archived/jira.json";
    //https://my.atlassian.com/download/feeds/current/jira-software.json

var client = new HttpClient();
var atlassianJson = await client.GetStringAsync(feedUrl);
var callString = "downloads(";
var json = atlassianJson[callString.Length..^1];
var parsed = JsonSerializer.Deserialize<ResponseArray[]>(json, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
});
var versions = parsed.GroupBy(a => a.Version).ToDictionary(a => a.Key, a => a.Select(b => b.ZipUrl).ToArray());

foreach (var version in versions)
{
    var directory = Path.Combine(output, version.Key);
    if (!Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }
    foreach (var link in version.Value)
    {
        var q = link.PathAndQuery;
        var outputFile = Path.Combine(directory, q[(q.LastIndexOf("/") + 1)..]);
        if (!File.Exists(outputFile))
        {
            using var file = File.OpenWrite(outputFile);
            using var request = await client.GetStreamAsync(link).ConfigureAwait(false);
            await request.CopyToAsync(file).ConfigureAwait(false);
            Console.WriteLine($"Downloaded {link}");
        }
        else
        {
            Console.WriteLine($"File for {link} already exists");
        }
    }
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