namespace EpicMorg.Atlassian.Downloader;
using System;

public record DownloaderOptions(string OutputDir, Uri[] CustomFeed, DownloadAction Action, bool Version, string ProductVersion, bool SkipFileCheck) { }