# [![Activity](https://img.shields.io/github/commit-activity/m/EpicMorg/atlassian-downloader?label=commits&style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/commits) [![GitHub issues](https://img.shields.io/github/issues/EpicMorg/atlassian-downloader.svg?style=popout-square)](https://github.com/EpicMorg/atlassian-downloader/issues) [![GitHub forks](https://img.shields.io/github/forks/EpicMorg/atlassian-downloader.svg?style=popout-square)](https://github.com/EpicMorg/atlassian-downloader/network) [![GitHub stars](https://img.shields.io/github/stars/EpicMorg/atlassian-downloader.svg?style=popout-square)](https://github.com/EpicMorg/atlassian-downloader/stargazers)  [![Size](https://img.shields.io/github/repo-size/EpicMorg/atlassian-downloader?label=size&style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/archive/master.zip) [![Release](https://img.shields.io/github/v/release/EpicMorg/atlassian-downloader?style=flat-square)](https://github.com/EpicMorg/atlassian-downloader/releases)  [![GitHub license](https://img.shields.io/github/license/EpicMorg/atlassian-downloader.svg?style=popout-square)](LICENSE.md) [![Changelog](https://img.shields.io/badge/Changelog-yellow.svg?style=popout-square)](CHANGELOG.md)

# Atlassian Downloader

Console app written with `c#` and `dotnet5` for downloading all avalible products from `Atlassian`. Why not?

![Atlassian Downloader](https://rawcdn.githack.com/EpicMorg/atlassian-downloader/8fd59dfb0514aeff8556761c2f9862185d3489ea/.github/screenshot-1.png)

## Requerments
1. Preinstalled `dotnet5`. Download [here](https://dotnet.microsoft.com/download/dotnet/5.0).
2. Supported OS: `win32/win64`, `linux`, `macosx`, `arm/arm64`

## How to
1. `git clone` this repo.
2. `cd` to `<repo>/src`.
3. execute `donten run` in `src` folder.
4. by default all data will be downloaded to `src/atlassian` folder and subfolders.

## Usage

```
Usage:
  atlassian-downloader [options]

Options:
  --output-dir <output-dir>      Override output directory to download [default: atlassian]
  --list                         Show all download links from feed without downloading [default: False]
  --custom-feed <custom-feed>    Override URIs to import. [default: ]
  --version                      Show version information
  -?, -h, --help                 Show help and usage information
```

## Supported products:

| Product | Current | Archive | EAP  |
|-------------|:-------------:|:-------------:|:-------------:|
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Bamboo&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/bamboo) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Bitbucket%20(Stash)&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/bitbucket) | :white_check_mark: | :white_check_mark: | :interrobang: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Clover&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/clover) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Confluence&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/confluence) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Crowd&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/crowd) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Crucible&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/crucible) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=FishEye&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/fisheye) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Jira%20Core&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/jira/core) | :white_check_mark: | :white_check_mark: | :x: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Jira%20Software&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/jira) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=Jira%20Servicedesk&color=bright%20green&style=for-the-badge)](https://www.atlassian.com/software/jira/service-management) | :white_check_mark: | :white_check_mark: | :white_check_mark: |
| [![Product](https://img.shields.io/static/v1?label=Atlassian&message=SourceTree&color=yellow&style=for-the-badge)](https://www.atlassian.com/software/sourcetree) | :x: | :x: | :x: |

* Archive of `Atlassian` jsons available [here](https://github.com/EpicMorg/atlassian-json).

------

## Authors
* [@kasthack](https://github.com/kasthack) - code
* [@stam](https://github.com/stamepicmorg) - code, repo
