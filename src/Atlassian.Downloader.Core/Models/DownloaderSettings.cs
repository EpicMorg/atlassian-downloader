// Atlassian.Downloader.Core/Models/DownloaderSettings.cs
namespace EpicMorg.Atlassian.Downloader.Core.Models;

public class DownloaderSettings
{
    public required string OutputDir { get; set; }
    public Uri[]? CustomFeed { get; set; }
    public string? ProductVersion { get; set; }
    public bool SkipFileCheck { get; set; }
    public string UserAgent { get; set; } = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:101.0) Gecko/20100101 Firefox/101.0";
    public int MaxRetries { get; set; } = 5;
    public int DelayBetweenRetries { get; set; } = 2500;
    public bool RandomizeDelay { get; set; } = false;
    public int MinDelay { get; set; } = 300;
    public int MaxDelay { get; set; } = 10000;
}