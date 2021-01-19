using System;

namespace EpicMorg.Atlassian.Downloader
{
    public record DownloaderOptions(string OutputDir, Uri[] CustomFeed, DownloadAction Action,bool Version) { }
}