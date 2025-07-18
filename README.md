# [![Activity](https://img.shields.io/github/commit-activity/m/EpicMorg/atlassian-downloader?label=commits&style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/commits) [![GitHub issues](https://img.shields.io/github/issues/EpicMorg/atlassian-downloader.svg?style=popout-square)](https://github.com/EpicMorg/atlassian-downloader/issues) [![GitHub forks](https://img.shields.io/github/forks/EpicMorg/atlassian-downloader.svg?style=popout-square)](https://github.com/EpicMorg/atlassian-downloader/network) [![GitHub stars](https://img.shields.io/github/stars/EpicMorg/atlassian-downloader.svg?style=popout-square)](https://github.com/EpicMorg/atlassian-downloader/stargazers)  [![Size](https://img.shields.io/github/repo-size/EpicMorg/atlassian-downloader?label=size&style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/archive/master.zip) [![Release](https://img.shields.io/github/v/release/EpicMorg/atlassian-downloader?style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/releases) [![Downloads](https://img.shields.io/github/downloads/EpicMorg/atlassian-downloader/total.svg?style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/releases) [![GitHub license](https://img.shields.io/github/license/EpicMorg/atlassian-downloader.svg?style=popout-square)](LICENSE.md) [![Changelog](https://img.shields.io/badge/Changelog-yellow.svg?style=popout-square)](CHANGELOG.md)

# Atlassian Downloader

Console app written with `c#` and `dotnet9` for downloading all avalible products from `Atlassian`. Why not?

![Atlassian Downloader](https://rawcdn.githack.com/EpicMorg/atlassian-downloader/28d17af55fbd4944d75f70d6bcb702e409820f64/.github/media/screenshot-01.png)
![Atlassian Downloader](https://rawcdn.githack.com/EpicMorg/atlassian-downloader/28d17af55fbd4944d75f70d6bcb702e409820f64/.github/media/screenshot-03.png)

# Supported OS: 
`win-x86`, `win-x64`, `win-arm64`, `linux-x86`, `linux-x64`, `linux-musl-x64`, `linux-arm`, `linux-arm64`, `linux-bionic-x64`, `osx-x64`, `osx-arm64`

-------------------

# How to...

## ..develop
1. preinstall `dotnet9`. Download [here](https://dotnet.microsoft.com/download/dotnet/8.0).
2. preinstall `VS2022`.  Download [here](https://visualstudio.microsoft.com/vs/).
3. `git clone` this repo.
4. `cd` to `<repo>/src`.
5. open `*.sln` file
6. ...
7. profit!

## ..build from scratch
1. `git clone` this repo.
2. `cd` to `<repo>/src`.
3. execute `build.bat(sh)` in `src` folder.
4. by default all data will be downloaded to `src/Atlassian` folder and subfolders.

## ..use binary versions
1. just download latest [![Downloads](https://img.shields.io/github/downloads/EpicMorg/atlassian-downloader/total.svg?style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/releases) [![Release](https://img.shields.io/github/v/release/EpicMorg/atlassian-downloader?style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/releases)
2. ...
3. profit!

## ..intall via Chocolatey
| CLI | Version   | Downloads
| ------  | ------ | ------ 
| :computer: `choco install atlassian-downloader` |  [![Version](https://img.shields.io/chocolatey/v/atlassian-downloader?label=version&style=for-the-badge)](https://chocolatey.org/packages/atlassian-downloader/) | [![Version](https://img.shields.io/chocolatey/dt/atlassian-downloader?style=for-the-badge)](https://chocolatey.org/packages/atlassian-downloader/) 

-------------------

# Usage and settings
## CLI args

![Atlassian Downloader](https://rawcdn.githack.com/EpicMorg/atlassian-downloader/28d17af55fbd4944d75f70d6bcb702e409820f64/.github/media/screenshot-02.png)

```
atlassian-downloader:
  Atlassian archive downloader. See https://github.com/EpicMorg/atlassian-downloader for more info

Usage:
  atlassian-downloader [options]

Options:
  --output-dir <output-dir>                              Override output directory to download
  --custom-feed <custom-feed>                            Override URIs to import []
  --action <Download|ListURLs|ListVersions|ShowRawJson>  Action to perform [default: Download]
  --about                                                Show credits banner [default: False]
  --product-version <product-version>                    Override target version to download some product. Advice: Use
                                                         it with "customFeed". []
  --skip-file-check                                      Skip compare of file sizes if a local file already exists.
                                                         Existing file will be skipped to check and redownload.
                                                         [default: False]
  --user-agent <user-agent>                              Set custom user agent via this feature flag. [default:
                                                         Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:101.0)
                                                         Gecko/20100101 Firefox/101.0]
  --version                                              Show version information
  -?, -h, --help                                         Show help and usage information
```

## Example of usage:

### How to download it all at first time, or get update of local archive
```
PS> .\atlassian-downloader.exe --output-dir "P:\Atlassian"
or
bash# ./atlassian-downloader --output-dir "/mnt/nfs/atlassian"
```
If you already have some folders at output path - they will be ignored and not be downloaded again and skipped. Downloader will be download only new versions of files which not be present locally yet.

### Set only some url feed and dowload it:
```
PS> .\atlassian-downloader.exe --output-dir "P:\Atlassian" --custom-feed https://my.atlassian.com/download/feeds/current/bamboo.json
or
bash# ./atlassian-downloader --output-dir "/mnt/nfs/atlassian" --custom-feed https://my.atlassian.com/download/feeds/current/bamboo.json
```

### cron or crontab example
``` 
0 0 * 1 0 /opt/epicmorg/atlassian-downloader/atlassian-downloader  --output-dir "/mnt/nfs/atlassian"
```
### Show only urls from jsons
```
PS> .\atlassian-downloader.exe --action ListURLs
or
bash# ./atlassian-downloader --action ListURLs
```

## Additional settings
File `src/appSettings.json` contains additional settings, like [loglevel](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-5.0#fields) and [console output theme](https://github.com/serilog/serilog-sinks-console). You can set it up via editing this file.

### Supported log levels
| Level | Enum | Description
|-------------|:-------------:|-------------|
| `Critical` | `5` | Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires immediate attention.
| `Debug`	| `1` | Logs that are used for interactive investigation during development. These logs should primarily contain information useful for debugging and have no long-term value.
| `Error` | `4` | Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a failure in the current activity, not an application-wide failure.
| `Information` | `2` | Logs that track the general flow of the application. These logs should have long-term value.
| `None` | `6` | Not used for writing log messages. Specifies that a logging category should not write any messages.
| `Trace`	| `0` | Logs that contain the most detailed messages. These messages may contain sensitive application data. These messages are disabled by default and should never be enabled in a production environment.
| `Warning` | `3` | Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application execution to stop.

### Supported console themes
The following built-in themes are available, provided by `Serilog.Sinks.Console` package:

 * `ConsoleTheme.None` - no styling
 * `SystemConsoleTheme.Literate` - styled to replicate _Serilog.Sinks.Literate_, using the `System.Console` coloring modes supported on all Windows/.NET targets; **this is the default when no theme is specified**
 * `SystemConsoleTheme.Grayscale` - a theme using only shades of gray, white, and black
 * `AnsiConsoleTheme.Literate` - an ANSI 16-color version of the "literate" theme; we expect to update this to use 256-colors for a more refined look in future
 * `AnsiConsoleTheme.Grayscale` - an ANSI 256-color version of the "grayscale" theme
 * `AnsiConsoleTheme.Code` - an ANSI 256-color Visual Studio Code-inspired theme

-------------------

# Supported products:

| Product | Current | Archive | EAP  |
|-------------|:-------------:|:-------------:|:-------------:|
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Bamboo&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/bamboo) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Bitbucket%20(Stash)&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/bitbucket) | :white_check_mark: | :white_check_mark: | :interrobang: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Bitbucket%20(Mesh)&color=bright%20green&style=for-the-badge)](https://confluence.atlassian.com/bitbucketserver/bitbucket-mesh-compatibility-matrix-1127254859.html) | :white_check_mark: | :white_check_mark: | :interrobang: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Clover&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/clover) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Confluence&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/confluence) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Crowd&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/crowd) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Crucible&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/crucible) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=FishEye&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/fisheye) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Jira%20Core&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/jira/core) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Jira%20Software&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/jira) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Jira%20Servicedesk&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/jira/service-management) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=SourceTree&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/sourcetree) | :white_check_mark: | :white_check_mark: | :x: |

* Archive of `Atlassian` jsons available [here](https://github.com/EpicMorg/atlassian-json).

-------------------

## Authors
* [@kasthack](https://github.com/kasthack) - code
* [@stam](https://github.com/stamepicmorg) - code, repo
