# build.ps1 - Builds the Core library and packages the Console application.

[CmdletBinding()]
param (
    # Add a switch to optionally create an additional Native AOT build for the console app
    [switch]$Aot
)

# --- Configuration ---
# All settings are in one place for easy updates.
$CoreProjectFolder = "Atlassian.Downloader.Core"
$CoreProjectFile = Join-Path $CoreProjectFolder "Atlassian.Downloader.Core.csproj"

$ConsoleProjectName = "atlassian-downloader"
$ConsoleProjectFolder = "Atlassian.Downloader.Console"
$ConsoleProjectFile = Join-Path $ConsoleProjectFolder "$ConsoleProjectName.csproj"

$Configuration = "Release"
$Framework = "net9.0"

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

# The 'dotnet pack' command will automatically trigger a 'Build'.
# The signing target inside the .csproj will run after the build and before packing.
Write-Host "Building, signing, and packing Core library..."
Invoke-Expression "dotnet pack $CoreProjectFile -c $Configuration"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet pack failed for Core library. Aborting." -ForegroundColor Red
    return # Stop the script if the core library fails
}

Write-Host "Core library processed successfully." -ForegroundColor Green


# ==================================================
# STAGE 2: Publish and Package the Console Application
# ==================================================
Write-Host "=================================================="
Write-Host "Processing Atlassian.Downloader.Console application..." -ForegroundColor Magenta

# Loop through each runtime and perform all actions for the console app
foreach ($rid in $runtimes) {
    Write-Host "--------------------------------------------------"
    Write-Host "Processing Runtime: $rid" -ForegroundColor Yellow

    # --- Standard Self-Contained Build (Default Archive) ---
    Write-Host "Starting Standard Self-Contained build..." -ForegroundColor Cyan
    $publishDirDefault = Join-Path $ConsoleProjectFolder "bin\$Configuration\$Framework\$rid\publish-self-contained"
    $archiveNameDefault = Join-Path $ConsoleProjectFolder "bin\$ConsoleProjectName-$Framework-$rid.zip"
    
    Invoke-Expression "dotnet publish $ConsoleProjectFile -c $Configuration --runtime $rid -o $publishDirDefault --force"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish (Standard) failed for $rid." -ForegroundColor Red; continue }
    
    # Cleanup and Archiving
    Remove-Item (Join-Path $publishDirDefault "*.pdb") -ErrorAction SilentlyContinue
    Push-Location $publishDirDefault
    7z a -tzip -mx5 -r -aoa $archiveNameDefault * | Out-Null
    Pop-Location
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip (Standard) failed for $rid." -ForegroundColor Red }
    
    Write-Host "Successfully processed Standard build for $rid." -ForegroundColor Green

    # --- Native AOT Build (Optional) ---
    if ($Aot) {
        Write-Host "--------------------------------------------------"
        Write-Host "Starting Native AOT build..." -ForegroundColor Cyan
        $publishDirAot = Join-Path $ConsoleProjectFolder "bin\$Configuration\$Framework\$rid\publish-aot"
        $archiveNameAot = Join-Path $ConsoleProjectFolder "bin\$ConsoleProjectName-$Framework-$rid-aot.zip"
        
        Invoke-Expression "dotnet publish $ConsoleProjectFile -c $Configuration --runtime $rid -p:PublishAot=true -o $publishDirAot --force"
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