param(
    [string]$ProjectRoot = 'D:\Game2\skinny-to-beast-clicker'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RequiredUnityVersion = '6000.3.19f1'
$RequiredUnityChangeset = '7689f4515d75'
$VerifierBaseUrl = 'https://raw.githubusercontent.com/timurkamurka11/skinny-to-beast-clicker/patch-4.0/RUN_PATCH4_VERIFY.ps1'
$HubInstallerUrl = 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-x64.exe'

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
        if (Test-Path $Candidate) { return (Resolve-Path $Candidate).Path }
    }

    foreach ($Root in @(
        'C:\Program Files\Unity\Hub\Editor',
        'C:\Program Files\Unity Hub\Editor',
        'D:\Program Files\Unity\Hub\Editor',
        'D:\Program Files\Unity Hub\Editor',
        'D:\Unity\Hub\Editor',
        'E:\Program Files\Unity\Hub\Editor',
        'E:\Program Files\Unity Hub\Editor',
        'E:\Unity\Hub\Editor')) {
        if (-not (Test-Path $Root)) { continue }
        $Found = Get-ChildItem -Path $Root -Filter Unity.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*$RequiredUnityVersion*" } |
            Select-Object -First 1
        if ($Found) { return $Found.FullName }
    }

    return $null
}

function Find-UnityHub {
    $Candidates = @(
        'C:\Program Files\Unity Hub\Unity Hub.exe',
        'C:\Program Files\Unity\Hub\Unity Hub.exe',
        (Join-Path $env:LOCALAPPDATA 'Programs\Unity Hub\Unity Hub.exe'),
        (Join-Path $env:LOCALAPPDATA 'UnityHub\Unity Hub.exe'),
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

    foreach ($RegistryPath in @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Unity Hub.exe',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Unity Hub.exe')) {
        try {
            $Properties = Get-ItemProperty -Path $RegistryPath -ErrorAction Stop
            $Value = $Properties.'(default)'
            if ($Value -and (Test-Path $Value)) { return (Resolve-Path $Value).Path }
        } catch {}
    }

    foreach ($Root in @(
        'C:\Program Files',
        (Join-Path $env:LOCALAPPDATA 'Programs'),
        'D:\Program Files',
        'E:\Program Files')) {
        if (-not $Root -or -not (Test-Path $Root)) { continue }
        $Found = Get-ChildItem -Path $Root -Filter 'Unity Hub.exe' -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($Found) { return $Found.FullName }
    }

    return $null
}

function Wait-ForUnityHub([int]$Seconds = 180) {
    $Deadline = (Get-Date).AddSeconds($Seconds)
    do {
        $Hub = Find-UnityHub
        if ($Hub) { return $Hub }
        Start-Sleep -Seconds 5
    } while ((Get-Date) -lt $Deadline)
    return $null
}

function Install-UnityHub {
    Write-Step 'Installing Unity Hub automatically'
    Write-Host 'Windows may display one administrator confirmation. Press Yes.' -ForegroundColor Yellow

    $Installed = $false
    $Winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($Winget) {
        try {
            $WingetArgs = @(
                'install', '--id', 'Unity.UnityHub', '--exact', '--source', 'winget',
                '--silent', '--accept-package-agreements', '--accept-source-agreements',
                '--disable-interactivity'
            )
            $WingetProcess = Start-Process -FilePath $Winget.Source -ArgumentList $WingetArgs -Wait -PassThru
            if ($WingetProcess.ExitCode -eq 0 -or $WingetProcess.ExitCode -eq -1978335189) {
                $Installed = $true
            }
        } catch {
            Write-Warning "winget installation attempt failed: $($_.Exception.Message)"
        }
    }

    $Hub = Wait-ForUnityHub 30
    if ($Hub) { return $Hub }

    Write-Host 'Using direct official Unity Hub installer fallback...' -ForegroundColor Yellow
    $InstallerPath = Join-Path $env:TEMP 'UnityHubSetup-x64.exe'
    Invoke-WebRequest -UseBasicParsing -Uri ($HubInstallerUrl + '?t=' + [DateTime]::UtcNow.Ticks) -OutFile $InstallerPath -TimeoutSec 300
    if (-not (Test-Path $InstallerPath) -or (Get-Item $InstallerPath).Length -lt 1000000) {
        throw 'Unity Hub installer download was incomplete.'
    }

    $Installer = Start-Process -FilePath $InstallerPath -ArgumentList '/S' -Verb RunAs -Wait -PassThru
    Write-Host "Unity Hub installer exit code: $($Installer.ExitCode)"

    $Hub = Wait-ForUnityHub 180
    if (-not $Hub) {
        throw 'Unity Hub installation finished, but Unity Hub.exe could not be found.'
    }

    return $Hub
}

function Wait-ForUnityEditor([int]$Seconds = 7200) {
    $Deadline = (Get-Date).AddSeconds($Seconds)
    do {
        $Editor = Find-UnityEditor
        if ($Editor) { return $Editor }
        Start-Sleep -Seconds 10
    } while ((Get-Date) -lt $Deadline)
    return $null
}

function Install-RequiredUnityEditor([string]$Hub) {
    Write-Step "Installing Unity $RequiredUnityVersion"
    Write-Host 'This is a large download. Keep this window open.' -ForegroundColor Yellow
    Write-Host "Unity Hub: $Hub"

    $HubArguments = "-- --headless --errors install --version $RequiredUnityVersion --changeset $RequiredUnityChangeset"
    $Process = Start-Process -FilePath $Hub -ArgumentList $HubArguments -Wait -PassThru
    Write-Host "Unity Hub editor install exit code: $($Process.ExitCode)"

    $Editor = Wait-ForUnityEditor 7200
    if (-not $Editor) {
        throw "Unity $RequiredUnityVersion was not found after installation. Unity Hub may require a one-time sign-in or license activation."
    }
    return $Editor
}

try {
    Write-Step 'Preparing GameWork Patch 4.0 V2'

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
        $Hub = Find-UnityHub
        if (-not $Hub) { $Hub = Install-UnityHub }
        Write-Host "Unity Hub ready: $Hub" -ForegroundColor Green
        $UnityExe = Install-RequiredUnityEditor $Hub
    }

    Write-Host "Unity ready: $UnityExe" -ForegroundColor Green

    Write-Step 'Downloading latest Patch 4 verifier'
    $VerifierPath = Join-Path $env:TEMP 'RUN_PATCH4_VERIFY_V2.ps1'
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
