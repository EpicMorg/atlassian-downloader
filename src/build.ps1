# build.ps1 - Builds and packages the console application.

[CmdletBinding()]
param (
    # Add a switch to optionally create an additional Native AOT build
    [switch]$Aot
)

# --- Configuration ---
# All settings are in one place for easy updates.
# Note: We now point to the Console project. The Core library will be built automatically as a dependency.
$ProjectName = "atlassian-downloader"
$ConsoleProjectFolder = "Atlassian.Downloader.Console"
$ProjectFile = Join-Path $ConsoleProjectFolder "$ProjectName.csproj"

$Configuration = "Release"
$Framework = "net9.0" 

# Define all target runtimes in one array.
$runtimes = @(
    "win-x64", "win-x86", "win-arm64",
    "osx-x64", "osx-arm64",
    "linux-x64", "linux-musl-x64", "linux-arm", "linux-arm64", "linux-bionic-x64"
)

# --- Build Logic ---
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'

# Loop through each runtime and perform all actions
foreach ($rid in $runtimes) {
    Write-Host "=================================================="
    Write-Host "Processing Runtime: $rid" -ForegroundColor Yellow
    Write-Host "--------------------------------------------------"

    # --- STAGE 1: STANDARD SELF-CONTAINED BUILD (Default Archive) ---
    
    Write-Host "Starting Standard Self-Contained build..." -ForegroundColor Cyan
    $publishDirDefault = Join-Path $ConsoleProjectFolder "bin\$Configuration\$Framework\$rid\publish-self-contained"
    $archiveNameDefault = Join-Path $ConsoleProjectFolder "bin\$ProjectName-$Framework-$rid.zip"
    
    Invoke-Expression "dotnet publish $ProjectFile -c $Configuration --runtime $rid -o $publishDirDefault --force"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish (Standard) failed for $rid." -ForegroundColor Red; continue }
    
    Remove-Item (Join-Path $publishDirDefault "*.pdb") -ErrorAction SilentlyContinue
    New-Item -Path (Join-Path $publishDirDefault "createdump.exe.ignore") -ItemType File -Force | Out-Null
    
    Write-Host "Creating archive: $archiveNameDefault"
    Push-Location $publishDirDefault
    7z a -tzip -mx5 -r -aoa $archiveNameDefault * | Out-Null
    Pop-Location
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip (Standard) failed for $rid." -ForegroundColor Red }
    
    Write-Host "Successfully processed Standard build for $rid." -ForegroundColor Green


    # --- STAGE 2: NATIVE AOT BUILD (Optional, with -aot suffix) ---

    if ($Aot) {
        Write-Host "--------------------------------------------------"
        Write-Host "Starting Native AOT build..." -ForegroundColor Cyan
        $publishDirAot = Join-Path $ConsoleProjectFolder "bin\$Configuration\$Framework\$rid\publish-aot"
        $archiveNameAot = Join-Path $ConsoleProjectFolder "bin\$ProjectName-$Framework-$rid-aot.zip"
        
        Invoke-Expression "dotnet publish $ProjectFile -c $Configuration --runtime $rid -p:PublishAot=true -o $publishDirAot --force"
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish (AOT) failed for $rid." -ForegroundColor Red; continue }

        Remove-Item (Join-Path $publishDirAot "*.pdb") -ErrorAction SilentlyContinue
        New-Item -Path (Join-Path $publishDirAot "createdump.exe.ignore") -ItemType File -Force | Out-Null
        
        Write-Host "Creating AOT archive: $archiveNameAot"
        Push-Location $publishDirAot
        7z a -tzip -mx5 -r -aoa $archiveNameAot * | Out-Null
        Pop-Location
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip (AOT) failed for $rid." -ForegroundColor Red }
        
        Write-Host "Successfully processed AOT build for $rid." -ForegroundColor Green
    }
}

Write-Host "=================================================="
Write-Host "All builds are complete."