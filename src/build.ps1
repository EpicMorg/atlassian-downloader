# build.ps1

[CmdletBinding()]
param (
    # Add a switch to optionally create an additional Native AOT build
    [switch]$Aot,

    # Add a switch to enable the code signing step
    [switch]$Sign,

    # Path to your PFX code signing certificate file. Required if -Sign is used.
    [string]$PfxPath,

    # The password for your PFX file. Required if -Sign is used.
    [string]$PfxPassword
)

# --- Helper Function to find signtool.exe ---
function Find-SignTool {
    # Path is usually in Windows Kits. Let's find the latest version.
    $winKitsDir = "C:\Program Files (x86)\Windows Kits\10\bin"
    if (-not (Test-Path $winKitsDir)) {
        return $null
    }
    
    $latestVersion = Get-ChildItem -Path $winKitsDir | Sort-Object Name -Descending | Select-Object -First 1
    $signToolPath = Join-Path $latestVersion.FullName "x64\signtool.exe"

    if (Test-Path $signToolPath) {
        return $signToolPath
    } else {
        return $null # Not found
    }
}


# --- Configuration ---
$ProjectName = "atlassian-downloader"
$Configuration = "Release"
$Framework = "net9.0"
$ProjectFile = "$ProjectName.csproj"
$TimestampServer = "http://timestamp.digicert.com"

# Define all target runtimes in one array.
$runtimes = @(
    "win-x64", "win-x86", "win-arm64",
    "osx-x64", "osx-arm64",
    "linux-x64", "linux-musl-x64", "linux-arm", "linux-arm64", "linux-bionic-x64"
)

# --- Build Logic ---
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'

# Find signtool.exe at the beginning if needed
$signTool = $null
if ($Sign) {
    if (-not ($PfxPath) -or -not ($PfxPassword)) {
        Write-Host "ERROR: -PfxPath and -PfxPassword are required when using the -Sign switch." -ForegroundColor Red
        return
    }
    $signTool = Find-SignTool
    if (-not $signTool) {
        Write-Host "WARNING: signtool.exe not found. Signing will be skipped." -ForegroundColor Yellow
    }
}

# Loop through each runtime
foreach ($rid in $runtimes) {
    Write-Host "=================================================="
    Write-Host "Processing Runtime: $rid" -ForegroundColor Yellow
    Write-Host "--------------------------------------------------"

    # --- STAGE 1: STANDARD SELF-CONTAINED BUILD (Default Archive) ---
    
    Write-Host "Starting Standard Self-Contained build..." -ForegroundColor Cyan
    $publishDirDefault = Join-Path $PSScriptRoot "bin\$Configuration\$Framework\$rid\publish-self-contained"
    $archiveNameDefault = Join-Path $PSScriptRoot "bin\$ProjectName-$Framework-$rid.zip"
    
    Invoke-Expression "dotnet publish $ProjectFile -c $Configuration --runtime $rid -o $publishDirDefault --force"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish (Standard) failed for $rid." -ForegroundColor Red; continue }
    
    $exeFileDefault = Join-Path $publishDirDefault "$ProjectName.exe"
    if ($Sign -and $rid.StartsWith("win-") -and (Test-Path $exeFileDefault) -and $signTool) {
        Write-Host "Signing Self-Contained executable: $exeFileDefault"
        $signCommand = "& `"$signTool`" sign /f `"$PfxPath`" /p `"$PfxPassword`" /tr $TimestampServer /td sha256 /fd sha256 `"$exeFileDefault`""
        Invoke-Expression $signCommand
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Signing (Self-Contained) failed for $rid." -ForegroundColor Red }
    }

    Remove-Item (Join-Path $publishDirDefault "*.pdb") -ErrorAction SilentlyContinue
    New-Item -Path (Join-Path $publishDirDefault "createdump.exe.ignore") -ItemType File -Force | Out-Null
    
    # MODIFIED: Archive block for Self-Contained
    Write-Host "Creating archive: $archiveNameDefault"
    Push-Location $publishDirDefault  # Temporarily change directory to the publish folder
    7z a -tzip -mx5 -r -aoa $archiveNameDefault * | Out-Null # Archive its contents (*)
    Pop-Location # Go back to the original directory
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip (Standard) failed for $rid." -ForegroundColor Red }
    
    Write-Host "Successfully processed Standard build for $rid." -ForegroundColor Green


    # --- STAGE 2: NATIVE AOT BUILD (Optional, with -aot suffix) ---

    if ($Aot) {
        Write-Host "--------------------------------------------------"
        Write-Host "Starting Native AOT build..." -ForegroundColor Cyan
        $publishDirAot = Join-Path $PSScriptRoot "bin\$Configuration\$Framework\$rid\publish-aot"
        $archiveNameAot = Join-Path $PSScriptRoot "bin\$ProjectName-$Framework-$rid-aot.zip"
        
        Invoke-Expression "dotnet publish $ProjectFile -c $Configuration --runtime $rid -p:PublishAot=true -o $publishDirAot --force"
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: dotnet publish (AOT) failed for $rid." -ForegroundColor Red; continue }

        $exeFileAot = Join-Path $publishDirAot "$ProjectName.exe"
        if ($Sign -and $rid.StartsWith("win-") -and (Test-Path $exeFileAot) -and $signTool) {
            Write-Host "Signing AOT executable: $exeFileAot"
            $signCommand = "& `"$signTool`" sign /f `"$PfxPath`" /p `"$PfxPassword`" /tr $TimestampServer /td sha256 /fd sha256 `"$exeFileAot`""
            Invoke-Expression $signCommand
            if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Signing (AOT) failed for $rid." -ForegroundColor Red }
        }
        
        Remove-Item (Join-Path $publishDirAot "*.pdb") -ErrorAction SilentlyContinue
        New-Item -Path (Join-Path $publishDirAot "createdump.exe.ignore") -ItemType File -Force | Out-Null
        
        # MODIFIED: Archive block for AOT
        Write-Host "Creating AOT archive: $archiveNameAot"
        Push-Location $publishDirAot # Temporarily change directory
        7z a -tzip -mx5 -r -aoa $archiveNameAot * | Out-Null # Archive its contents (*)
        Pop-Location # Go back
        if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: 7-Zip (AOT) failed for $rid." -ForegroundColor Red }
        
        Write-Host "Successfully processed AOT build for $rid." -ForegroundColor Green
    }
}

Write-Host "=================================================="
Write-Host "All builds are complete."