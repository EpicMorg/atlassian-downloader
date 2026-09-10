# Atlassian Downloader - Changelog

## Library `EpicMorg.Atlassian.Downloader`

Versioned on its own since `2.0.0.5`, when the core logic was split out of the console app. The
`1.x` section further down is the old history of the app itself, not of this package.

* `1.0.0.4` - fixes:
    * a feed that cannot be reached or parsed is now logged and skipped instead of ending the run. Previously the first bad feed aborted everything after it, so which products got mirrored depended on their position in the list.
    * restored `raw.githubusercontent.com` alongside `raw.githack.com` for the `SourceTree` mirrors. `SourceTree` is the one product with no official feed, and the two hosts have been seen serving different content for days at a time.
    * code signing is skipped off Windows instead of failing the build. `signtool.exe` ships with the Windows SDK, so a `Release` build on Linux or macOS died with exit code 127 after everything had already compiled.
    * `--random-user-agent` works. The flag and the pool behind it both existed since `2.0.0.7`, but the value never left `Program.Main` and nothing ever read the pool.
    * the user agent header no longer accumulates. `ParseAdd` appends, and every entry point set it on the same `HttpClient`, so more than one call left several user agents in a single header.

## 2.x

* `2.0.0.9` - fixes:
    * `--random-user-agent` does something now. It was accepted, listed in `--help` and announced in `2.0.0.7`, but the value never left `Program.Main`.
    * `--help` describes the options again. `DragonFruit` builds it from the XML doc comments on `Main`, and with no documentation file generated every option was labelled with its own parameter name, like `outputDir []`.
    * a feed that cannot be reached or parsed no longer ends the run.
    * `Release` builds work off Windows: code signing is skipped there instead of failing.
    * requires library `1.0.0.4`.
* `2.0.0.8` - technical update:
	* updated libs
* `2.0.0.7` - technical update:
    * added delays
	* added randomazin useragents
	* switched to `dotnet10`
* `2.0.0.6` - technical update:
    * backported shim-fix for choco runs
    * updated libs
* `2.0.0.5` - technical update:
    * Splitted core logic to standalone library,
	* reworked build scripts,
	* added code signing,
    * cleanup code.
* `2.0.0.4` - update:
    * Added support for downloading marketplace plugins
    * updated httpClient code
    * Updated dependencies.
* `2.0.0.3` - minor update:
    * Updated dependencies.
    * `dotnet9`
    * updated to new JSON format from atlassian
* `2.0.0.2` - minor update:
    * Added `maxRetries (default: 5)` and `delayBetweenRetries (default: 2500, milliseconds)` args, to redownload file if connection will be reset.
    * Updated dependencies.
* `2.0.0.1` - minor update:
    * Fix default output dir, enable nullables, fix compiler warnings #43
    * Remove redundant parameters from publish profiles #42
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
