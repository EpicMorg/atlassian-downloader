namespace EpicMorg.Atlassian.Downloader;

public enum DownloadAction
{
    /// <summary>
    /// Download application files
    /// </summary>
    Download,
    /// <summary>
    /// Print download URLs and exit
    /// </summary>
    ListURLs,
    /// <summary>
    /// Print available application versions and exit
    /// </summary>
    ListVersions,
    /// <summary>
    /// Print feed JSONs to stdout and exit
    /// </summary>
    ShowRawJson,
    /// <summary>
    /// Download plugin files from Atlassian Marketplace
    /// </summary>
    Plugin,
}