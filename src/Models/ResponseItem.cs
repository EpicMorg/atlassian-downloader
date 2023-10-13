namespace EpicMorg.Atlassian.Downloader;

using System;

public partial class ResponseItem
{
    public string? Description { get; set; }
    public string? Edition { get; set; }
    public Uri? ZipUrl { get; set; }
    public object? TarUrl { get; set; }
    public string? Md5 { get; set; }
    public string? Size { get; set; }
    public string? Released { get; set; }
    public string? Type { get; set; }
    public string? Platform { get; set; }
    public required string Version { get; set; }
    public Uri? ReleaseNotes { get; set; }
    public Uri? UpgradeNotes { get; set; }
}