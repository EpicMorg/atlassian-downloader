// Atlassian.Downloader.Console/Models/DownloaderOptions.cs
using System;

namespace EpicMorg.Atlassian.Downloader.Models;

public record DownloaderOptions(
    string OutputDir,
    Uri[]? CustomFeed,
    DownloadAction Action,
    bool About, 
    string? ProductVersion,
    bool SkipFileCheck,
    string UserAgent,
    int MaxRetries,
    int DelayBetweenRetries,
    string? PluginId
);