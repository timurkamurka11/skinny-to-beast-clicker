param(
    [string]$ProjectRoot = 'D:\Game2\skinny-to-beast-clicker'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RequiredUnityVersion = '6000.3.19f1'
$RequiredUnityChangeset = '7689f4515d75'
$EditorInstallerUrl = "https://download.unity3d.com/download_unity/$RequiredUnityChangeset/Windows64EditorInstaller/UnitySetup64-$RequiredUnityVersion.exe"
$VerifierBaseUrl = 'https://raw.githubusercontent.com/timurkamurka11/skinny-to-beast-clicker/patch-4.0/RUN_PATCH4_VERIFY.ps1'

function Write-Step([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Find-UnityEditor {
    $ProjectDrive = [System.IO.Path]::GetPathRoot($ProjectRoot).TrimEnd('\')
    $Candidates = @(
        "$ProjectDrive\UnityEditors\$RequiredUnityVersion\Editor\Unity.exe",
        "$ProjectDrive\Unity\$RequiredUnityVersion\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "C:\Program Files\Unity Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "D:\Program Files\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "D:\Program Files\Unity Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "D:\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "E:\Program Files\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "E:\Program Files\Unity Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "E:\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe"
    )

    foreach ($Candidate in $Candidates) {
        if (Test-Path $Candidate) {
            return (Resolve-Path $Candidate).Path
        }
    }

    return $null
}

function Download-LargeFile([string]$Url, [string]$Destination) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null

    if (Test-Path $Destination) {
        if ((Get-Item $Destination).Length -gt 100000000) {
            Write-Host "Using existing Unity installer: $Destination" -ForegroundColor Green
            return
        }
        Remove-Item $Destination -Force -ErrorAction SilentlyContinue
    }

    Write-Host 'Downloading Unity Editor installer. This is a large file; keep this window open.' -ForegroundColor Yellow

    $Bits = Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue
    if ($Bits) {
        Start-BitsTransfer -Source $Url -Destination $Destination -DisplayName "Unity $RequiredUnityVersion" -Description 'Downloading Unity Editor'
    } else {
        Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $Destination -TimeoutSec 7200
    }

    if (-not (Test-Path $Destination) -or (Get-Item $Destination).Length -lt 100000000) {
        throw 'Unity Editor installer download was incomplete.'
    }
}

function Install-UnityEditorDirect {
    $ProjectDrive = [System.IO.Path]::GetPathRoot($ProjectRoot).TrimEnd('\')
    if ([string]::IsNullOrWhiteSpace($ProjectDrive)) {
        $ProjectDrive = 'D:'
    }

    $InstallRoot = "$ProjectDrive\UnityEditors\$RequiredUnityVersion"
    $UnityExe = Join-Path $InstallRoot 'Editor\Unity.exe'
    if (Test-Path $UnityExe) {
        return (Resolve-Path $UnityExe).Path
    }

    $DriveInfo = Get-PSDrive -Name $ProjectDrive.TrimEnd(':') -ErrorAction SilentlyContinue
    if ($DriveInfo -and $DriveInfo.Free -lt 15000000000) {
        throw "Not enough free space on $ProjectDrive. At least 15 GB is required for Unity installation."
    }

    $CacheRoot = "$ProjectDrive\UnityInstallCache"
    $InstallerPath = Join-Path $CacheRoot "UnitySetup64-$RequiredUnityVersion.exe"
    Download-LargeFile $EditorInstallerUrl $InstallerPath

    Write-Step "Installing Unity $RequiredUnityVersion directly"
    Write-Host "Install folder: $InstallRoot" -ForegroundColor Yellow
    Write-Host 'Windows may show one administrator confirmation. Press Yes.' -ForegroundColor Yellow

    New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null

    # Unity's Windows installer requires /D to be the final argument and without quotes.
    $ArgumentLine = "/S /D=$InstallRoot"
    $Process = Start-Process -FilePath $InstallerPath -ArgumentList $ArgumentLine -Verb RunAs -Wait -PassThru
    Write-Host "Unity Editor installer exit code: $($Process.ExitCode)"

    $Deadline = (Get-Date).AddMinutes(30)
    do {
        if (Test-Path $UnityExe) {
            return (Resolve-Path $UnityExe).Path
        }
        Start-Sleep -Seconds 10
    } while ((Get-Date) -lt $Deadline)

    throw "Unity installation finished, but Unity.exe was not found at $UnityExe."
}

try {
    Write-Step 'Preparing GameWork Patch 4.0 V3'

    if (-not (Test-Path $ProjectRoot)) {
        throw "Project folder was not found: $ProjectRoot"
    }
    if (-not (Test-Path (Join-Path $ProjectRoot 'Assets')) -or
        -not (Test-Path (Join-Path $ProjectRoot 'ProjectSettings'))) {
        throw "The selected folder is not a Unity project root: $ProjectRoot"
    }
    if (Get-Process Unity -ErrorAction SilentlyContinue) {
        throw 'Close the currently running Unity Editor once, then run this same command again.'
    }

    $UnityExe = Find-UnityEditor
    if (-not $UnityExe) {
        $UnityExe = Install-UnityEditorDirect
    }

    Write-Host "Unity ready: $UnityExe" -ForegroundColor Green

    Write-Step 'Downloading latest Patch 4 verifier'
    $VerifierPath = Join-Path $env:TEMP 'RUN_PATCH4_VERIFY_V3.ps1'
    $VerifierUrl = $VerifierBaseUrl + '?t=' + [DateTime]::UtcNow.Ticks
    Invoke-WebRequest -UseBasicParsing -Uri $VerifierUrl -OutFile $VerifierPath -TimeoutSec 180

    Write-Step 'Running full Patch 4 synchronization, build and tests'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $VerifierPath -ProjectRoot $ProjectRoot -UnityExe $UnityExe
    exit $LASTEXITCODE
}
catch {
    Write-Host "`nFAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
