param(
    [string]$ProjectRoot = 'D:\Game2\skinny-to-beast-clicker'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RequiredUnityVersion = '6000.3.19f1'
$RequiredUnityChangeset = '7689f4515d75'
$VerifierUrl = 'https://raw.githubusercontent.com/timurkamurka11/skinny-to-beast-clicker/patch-4.0/RUN_PATCH4_VERIFY.ps1'

function Write-Step([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Find-UnityEditor {
    $Candidates = @(
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

    $Roots = @(
        'C:\Program Files\Unity\Hub\Editor',
        'C:\Program Files\Unity Hub\Editor',
        'D:\Program Files\Unity\Hub\Editor',
        'D:\Program Files\Unity Hub\Editor',
        'D:\Unity\Hub\Editor',
        'E:\Program Files\Unity\Hub\Editor',
        'E:\Program Files\Unity Hub\Editor',
        'E:\Unity\Hub\Editor'
    )

    foreach ($Root in $Roots) {
        if (-not (Test-Path $Root)) { continue }
        $Found = Get-ChildItem -Path $Root -Filter Unity.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*$RequiredUnityVersion*" } |
            Select-Object -First 1
        if ($Found) {
            return $Found.FullName
        }
    }

    return $null
}

function Find-UnityHub {
    $Candidates = @(
        'C:\Program Files\Unity Hub\Unity Hub.exe',
        'C:\Program Files\Unity\Hub\Unity Hub.exe',
        (Join-Path $env:LOCALAPPDATA 'Programs\Unity Hub\Unity Hub.exe'),
        'D:\Program Files\Unity Hub\Unity Hub.exe',
        'D:\Program Files\Unity\Hub\Unity Hub.exe',
        'E:\Program Files\Unity Hub\Unity Hub.exe',
        'E:\Program Files\Unity\Hub\Unity Hub.exe'
    )

    foreach ($Candidate in $Candidates) {
        if ($Candidate -and (Test-Path $Candidate)) {
            return (Resolve-Path $Candidate).Path
        }
    }

    return $null
}

function Install-RequiredUnityEditor {
    $Hub = Find-UnityHub
    if (-not $Hub) {
        throw 'Unity Hub was not found. Install Unity Hub once, then run the same command again.'
    }

    Write-Step "Installing Unity $RequiredUnityVersion automatically"
    Write-Host 'This is a large download and can take considerable time. Do not close this window.' -ForegroundColor Yellow
    Write-Host "Unity Hub: $Hub"

    $ArgumentLine = "-- --headless install --version $RequiredUnityVersion --changeset $RequiredUnityChangeset"
    $Process = Start-Process -FilePath $Hub `
        -ArgumentList $ArgumentLine `
        -Wait `
        -PassThru

    if ($Process.ExitCode -ne 0) {
        throw "Unity Hub installation failed with exit code $($Process.ExitCode)."
    }

    Start-Sleep -Seconds 5
    $Editor = Find-UnityEditor
    if (-not $Editor) {
        throw "Unity Hub completed, but Unity $RequiredUnityVersion was not found in the standard install locations."
    }

    return $Editor
}

try {
    Write-Step 'Preparing GameWork Patch 4.0'

    if (-not (Test-Path $ProjectRoot)) {
        throw "Project folder was not found: $ProjectRoot"
    }

    if (-not (Test-Path (Join-Path $ProjectRoot 'Assets')) -or
        -not (Test-Path (Join-Path $ProjectRoot 'ProjectSettings'))) {
        throw "The selected folder is not a Unity project root: $ProjectRoot"
    }

    if (Get-Process Unity -ErrorAction SilentlyContinue) {
        throw 'Close the currently running Unity Editor once, then run the same one-command launcher again.'
    }

    $UnityExe = Find-UnityEditor
    if (-not $UnityExe) {
        $UnityExe = Install-RequiredUnityEditor
    }

    Write-Host "Unity ready: $UnityExe" -ForegroundColor Green

    Write-Step 'Downloading latest Patch 4 verifier'
    $VerifierPath = Join-Path $env:TEMP 'RUN_PATCH4_VERIFY_LATEST.ps1'
    Invoke-WebRequest -UseBasicParsing -Uri $VerifierUrl -OutFile $VerifierPath -TimeoutSec 120

    Write-Step 'Running full Patch 4 synchronization, build and tests'
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $VerifierPath -ProjectRoot $ProjectRoot -UnityExe $UnityExe
    exit $LASTEXITCODE
}
catch {
    Write-Host "`nFAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
