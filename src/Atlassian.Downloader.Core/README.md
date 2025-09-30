# EpicMorg.Atlassian.Downloader

[![NuGet Version](https://img.shields.io/nuget/v/EpicMorg.Atlassian.Downloader.svg)](https://www.nuget.org/packages/EpicMorg.Atlassian.Downloader/)


`EpicMorg.Atlassian.Downloader` is a modern .NET library for programmatically downloading Atlassian Server/Data Center products and Marketplace plugins. It provides a simple, asynchronous API to handle interactions with Atlassian's download services, including paginated endpoints and artifact resolution.

This library is the core engine for the [Atlassian Downloader](https://github.com/EpicMorg/atlassian-downloader) console utility.

---

## Installation

Install the library via the NuGet package manager.

```powershell
dotnet add package EpicMorg.Atlassian.Downloader
```

## Usage
The primary entry point for the library is the AtlassianClient class. It's designed to be used with dependency injection and IHttpClientFactory for proper HttpClient management.

### 1. Setup (Dependency Injection)
In your Program.cs or startup configuration, register the AtlassianClient.

```
using EpicMorg.Atlassian.Downloader.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register the AtlassianClient and configure HttpClient for it
        services.AddHttpClient<AtlassianClient>();

        // Register your other application services
        // services.AddHostedService<MyWorker>();
    })
    .Build();
```

### 2. Creating Settings
All download operations require a DownloaderSettings object to configure their behavior.

```
using EpicMorg.Atlassian.Downloader.Core.Models;

var settings = new DownloaderSettings
{
    OutputDir = "C:\\atlassian-archive",
    SkipFileCheck = false,
    MaxRetries = 5,
    DelayBetweenRetries = 3000,
    UserAgent = "My-Awesome-App/1.0"
};
```

### 3. API and Examples
Once you have an instance of AtlassianClient (injected by your DI container), you can call its public methods.

#### DownloadPluginAsync
Downloads all available Server/Data Center versions of a specific Marketplace plugin. It automatically handles pagination, determines compatibility, creates an organized folder structure, and generates a readme.md for each version.

#### Signature:

`Task DownloadPluginAsync(string pluginId, DownloaderSettings settings, CancellationToken cancellationToken = default)`

#### Example:

```
using EpicMorg.Atlassian.Downloader.Core;
using Microsoft.Extensions.DependencyInjection; // For GetRequiredService

// Get the client from your DI container
var atlassianClient = host.Services.GetRequiredService<AtlassianClient>();

try
{
    string pluginId = "com.onresolve.jira.groovy.groovyrunner"; // Example: ScriptRunner for Jira
    await atlassianClient.DownloadPluginAsync(pluginId, settings);
    Console.WriteLine($"Successfully archived all versions of {pluginId}.");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
```

#### DownloadProductsAsync
Downloads Atlassian products (like Jira, Confluence) from the official JSON data feeds.

#### Signature:

```
Task DownloadProductsAsync(DownloaderSettings settings, CancellationToken cancellationToken = default)
```

Example:
You can specify ProductVersion and CustomFeed in the settings object to customize the download.

```
// Download a specific version of Confluence
var confluenceSettings = new DownloaderSettings
{
    OutputDir = "C:\\atlassian-archive",
    ProductVersion = "8.5.3",
    CustomFeed = new Uri[] { new Uri("[https://my.atlassian.com/download/feeds/current/confluence.json](https://my.atlassian.com/download/feeds/current/confluence.json)") }
};

try
{
    await atlassianClient.DownloadProductsAsync(confluenceSettings);
    Console.WriteLine("Confluence 8.5.3 download process completed.");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
```

#### GetProductDataAsync & GetProductFeedUrls
These are lower-level methods for more granular control, allowing you to fetch product data without immediately downloading the files.

#### Signatures:

```
IReadOnlyList<string> GetProductFeedUrls(DownloaderSettings settings);

Task<(string json, IDictionary<string, ResponseItem[]> versions)> GetProductDataAsync(string feedUrl, DownloaderSettings settings, CancellationToken cancellationToken);
```
Example: Listing all available Jira versions without downloading.

```
var jiraSettings = new DownloaderSettings 
{ 
    OutputDir = "C:\\atlassian-archive",
    CustomFeed = new Uri[] { new Uri("[https://my.atlassian.com/download/feeds/current/jira-software.json](https://my.atlassian.com/download/feeds/current/jira-software.json)") }
};

var jiraFeed = atlassianClient.GetProductFeedUrls(jiraSettings).First();

var (_, versions) = await atlassianClient.GetProductDataAsync(jiraFeed, jiraSettings, CancellationToken.None);

Console.WriteLine($"--- Available Jira Software Versions ---");
foreach (var versionKey in versions.Keys)
{
    Console.WriteLine(versionKey);
}
```