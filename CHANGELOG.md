# Atlassian Downloader - Changelog

## 2.x
* `2.0.0.0` - migrated to `dotnet8` and updated libs. 
    * code optimized by [@kasthack](https://github.com/kasthack). 
    * reworked build scripts via `cli` and `vs`.
    * added new dists - `osx-arm64`, `linux-bionic-x64`.
    * added support of custom useragent via flag
    * added suppor of skipping existing files via flag
## 1.x
* `1.1.0.0` - added automatic compare of local and remote file sizes. If they differ - the file will be re-downloaded.
* `1.0.1.1` - minor update: added `UserAgent` to HTTP headers and added mirrors of json files.
* `1.0.1.0` - added support of `Atlassian Bitbucket (Mesh)` product, updated deps, fixed `Chocolatey` support and start logic.
* `1.0.0.9` - updated deps.
* `1.0.0.8` - switched to `dontet6.0`, updated deps.
* `1.0.0.7` - added `unofficial support`  of `sourcetree` via automatic mirror [from github](https://github.com/EpicMorg/atlassian-json). fixed `logger` output, code improvments.
* `1.0.0.6` - added support of `clover`. fixed broken json parsing. added new `logger`.
* `1.0.0.5` - added support for `EAP` releases.
* `1.0.0.4` - bump version. rewrited build scripts. added support of `arm` and `arm64`.
* `1.0.0.3` - some cosmetics improvements.
* `1.0.0.2` - some cosmetics improvements.
* `1.0.0.1` - some improvements. added support of all available products.
* `1.0.0.0` - test script. internal use. not published.
