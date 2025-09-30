// Atlassian.Downloader.Console/Models/DownloaderOptions.cs
using System;

namespace EpicMorg.Atlassian.Downloader.Models;

public class DownloaderOptions
{
    public string OutputDir { get; set; } = Environment.CurrentDirectory;
    public Uri[]? CustomFeed { get; set; }
    public DownloadAction Action { get; set; } = DownloadAction.Download;
    public bool Version { get; set; }
    public string? ProductVersion { get; set; }
    public bool SkipFileCheck { get; set; }
    public string UserAgent { get; set; } = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:101.0) Gecko/20100101 Firefox/101.0";
    public int MaxRetries { get; set; } = 5;
    public int DelayBetweenRetries { get; set; } = 2500;
    public string? PluginId { get; set; }
}