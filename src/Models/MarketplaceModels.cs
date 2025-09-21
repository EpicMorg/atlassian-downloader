namespace EpicMorg.Atlassian.Downloader.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

// THIS CLASS WAS MISSING. It's for the basic info from /rest/2/addons/{addonKey}
public class MarketplacePlugin
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

// Represents the top-level response from the /versions endpoint
public class AddonVersionCollection
{
    [JsonPropertyName("_links")]
    public CollectionLinks? Links { get; set; }

    [JsonPropertyName("_embedded")]
    public CollectionEmbedded? Embedded { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class CollectionLinks
{
    [JsonPropertyName("next")]
    public Link? Next { get; set; }
}

public class CollectionEmbedded
{
    [JsonPropertyName("versions")]
    public AddonVersionSummary[]? Versions { get; set; }
}

// Represents a single version summary in the list from the /versions endpoint
public class AddonVersionSummary
{
    [JsonPropertyName("_links")]
    public VersionSummaryLinks? Links { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("deployment")]
    public DeploymentInfo? Deployment { get; set; }
}

public class VersionSummaryLinks
{
    [JsonPropertyName("self")]
    public Link? Self { get; set; }
}

public class DeploymentInfo
{
    [JsonPropertyName("server")]
    public bool Server { get; set; }

    [JsonPropertyName("dataCenter")]
    public bool DataCenter { get; set; }
}

// Represents the detailed information for a single version
public class AddonVersionDetail
{
    [JsonPropertyName("_embedded")]
    public VersionDetailEmbedded? Embedded { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("release")]
    public ReleaseInfo? Release { get; set; }

    [JsonPropertyName("compatibilities")]
    public CompatibilityInfo[]? Compatibilities { get; set; }

    [JsonPropertyName("text")]
    public TextInfo? Text { get; set; }
}

public class VersionDetailEmbedded
{
    [JsonPropertyName("artifact")]
    public ArtifactInfo? Artifact { get; set; }
}

public class ArtifactInfo
{
    [JsonPropertyName("_links")]
    public ArtifactLinks? Links { get; set; }
}

public class ArtifactLinks
{
    [JsonPropertyName("binary")]
    public Link? Binary { get; set; }
}

public class ReleaseInfo
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public class CompatibilityInfo
{
    [JsonPropertyName("application")]
    public string? Application { get; set; }

    [JsonPropertyName("hosting")]
    public HostingInfo? Hosting { get; set; }
}

public class HostingInfo
{
    [JsonPropertyName("dataCenter")]
    public object? DataCenter { get; set; }

    [JsonPropertyName("server")]
    public object? Server { get; set; }
}

public class TextInfo
{
    [JsonPropertyName("releaseNotes")]
    public string? ReleaseNotes { get; set; }
}

// Common helper class
public class Link
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }
}