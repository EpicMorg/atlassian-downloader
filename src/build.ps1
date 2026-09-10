# build.ps1 - Builds the Core library and packages the Console application.

[CmdletBinding()]
param (
    # Add a switch to optionally create an additional Native AOT build for the console app
    [switch]$Aot
)

# --- Configuration ---
# All settings are in one place for easy updates.

# $Configuration has to be set before anything is built out of it. It used to sit below, so
# $CoreBinReleaseFolder was assembled from an empty value and pointed at the bin root rather than
# bin\Release; only the -Recurse on the lookup further down kept that from being noticed.
$Configuration = "Release"

# The label that goes into the published archive names, NOT a target framework moniker. Every
# release so far ships as atlassian-downloader-dotnet10.0-<rid>.zip, so it stays as it is; the real
# moniker is $TargetFramework below.
$ReleaseLabel = "dotnet10.0"
$TargetFramework = "net10.0"

$CoreProjectFolder = "Atlassian.Downloader.Core"
$CoreProjectFile = Join-Path $CoreProjectFolder "Atlassian.Downloader.Core.csproj"
$CoreBinReleaseFolder = Join-Path $CoreProjectFolder "bin" $Configuration
$CoreBinReleaseNugetFile = Join-Path $CoreBinReleaseFolder "*.nupkg"

$ConsoleProjectName = "atlassian-downloader"
$ConsoleProjectFolder = "Atlassian.Downloader.Console"
$ConsoleProjectFile = Join-Path $ConsoleProjectFolder "$ConsoleProjectName.csproj"

$sha1Thumbprint = "3BAA227AD0DBA8DB55D0EFA14B74AA56B689601D"  
$sha256Fingerprint = "678456D26F89DF46A2AE8522825C157A6F9B937E890BBB5E6D51D1A2CBBD8702"
$TimeStampServer = "http://timestamp.digicert.com"

$runtimes = @(
    "win-x64", "win-x86", "win-arm64",
    "osx-x64", "osx-arm64",
    "linux-x64", "linux-musl-x64", "linux-arm", "linux-arm64", "linux-bionic-x64"
)

# --- Build Logic ---
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'


# ==================================================
# STAGE 1: Build and Pack the Core Library
# ==================================================
Write-Host "=================================================="
Write-Host "Processing Atlassian.Downloader.Core library..." -ForegroundColor Magenta

# Step 1: Build the project. This will trigger signing the DLL and creating the .nupkg file.
Write-Host "Building, signing DLL, and creating NuGet package..."
Invoke-Expression "dotnet build $CoreProjectFile -c $Configuration"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet build failed for Core library. Aborting." -ForegroundColor Red
    return
}

# Step 2: Find the most recently created .nupkg file in the output directory.
Write-Host "Searching for the created NuGet package..."
$nupkgFile = Get-ChildItem -Path $CoreBinReleaseFolder -Recurse -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $nupkgFile) {
    Write-Host "ERROR: Could not find any .nupkg file after the build." -ForegroundColor Red
    return
}

$nupkgPath = $nupkgFile.FullName
Write-Host "Found package: $nupkgPath" -ForegroundColor Cyan

# Step 3: Sign the package using the exact path we found.
Write-Host "(SKIPPING) Signing NuGet package..."
#dotnet nuget sign "$nupkgPath" --certificate-fingerprint $sha256Fingerprint --timestamper $TimeStampServer --overwrite
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet nuget sign failed for Core library. Aborting." -ForegroundColor Red
    return
}

Write-Host "Core library processed successfully." -ForegroundColor Green

# ==================================================
# STAGE 2: Publish and Package the Console Application
# ==================================================
Write-Host "=================================================="
Write-Host "Processing Atlassian.Downloader.Console application..." -ForegroundColor Magenta

foreach ($rid in $runtimes) {
    Write-Host "--------------------------------------------------"
    Write-Host "Processing Runtime: $rid" -ForegroundColor Yellow

    # --- Standard Self-Contained Build ---
    Write-Host "Starting Standard Self-Contained build..." -ForegroundColor Cyan
    
    # MODIFIED: Paths are now absolute, constructed from the script's root location
    $publishDir = Join-Path $PSScriptRoot $ConsoleProjectFolder "bin\$Configuration\$TargetFramework\$rid\publish"
    $archiveName = Join-Path $PSScriptRoot $ConsoleProjectFolder "bin\$ConsoleProjectName-$ReleaseLabel-$rid.zip"
    
    Invoke-Expression "dotnet publish $ConsoleProjectFile -c $Configuration --runtime $rid -p:SelfContained=true -p:PublishTrimmed=false -p:PublishAot=false -p:PublishSingleFile=false -o $publishDir --force"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish failed for $rid." -ForegroundColor Red; continue }
    
    Remove-Item (Join-Path $publishDir "*.pdb") -ErrorAction SilentlyContinue
    New-Item -Path (Join-Path $publishDir "createdump.exe.ignore") -ItemType File -Force | Out-Null
		
    Write-Host "Creating archive: $archiveName"
    Push-Location $publishDir # Temporarily enter the publish directory
    7z a -tzip -mx5 -r -aoa $archiveName * | Out-Null # Archive its contents (*)
    Pop-Location # Go back
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip failed for $rid." -ForegroundColor Red }
    
    Write-Host "Successfully processed build for $rid." -ForegroundColor Green
    
    # --- Native AOT Build (Optional) ---
    if ($Aot) {
        Write-Host "--------------------------------------------------"
        Write-Host "Starting Native AOT build..." -ForegroundColor Cyan
        $publishDirAot = Join-Path $ConsoleProjectFolder "bin\$Configuration\$TargetFramework\$rid\publish-aot"
        $archiveNameAot = Join-Path $ConsoleProjectFolder "bin\$ConsoleProjectName-$ReleaseLabel-$rid-aot.zip"
        
        Invoke-Expression "dotnet publish $ConsoleProjectFile -c $Configuration --runtime $rid -p:SelfContained=false -p:PublishTrimmed=false -p:PublishAot=true -p:PublishSingleFile=false -o $publishDirAot --force"
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish (AOT) failed for $rid." -ForegroundColor Red; continue }

        Remove-Item (Join-Path $publishDirAot "*.pdb") -ErrorAction SilentlyContinue
        
        Push-Location $publishDirAot
        7z a -tzip -mx5 -r -aoa $archiveNameAot * | Out-Null
        Pop-Location
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip (AOT) failed for $rid." -ForegroundColor Red }
        
        Write-Host "Successfully processed AOT build for $rid." -ForegroundColor Green
    }
}

Write-Host "=================================================="
Write-Host "All builds are complete."