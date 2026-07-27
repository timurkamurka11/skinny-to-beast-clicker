param(
    [string]$UnityExe = $env:UNITY_EXE,
    [switch]$SkipPull
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RequiredBranch = 'patch-4.0'
$RequiredUnityVersion = '6000.3.19f1'
$ExpectedMasterSha = '5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c'
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ResultsRoot = Join-Path $ProjectRoot "Patch4VerificationResults\$Timestamp"
New-Item -ItemType Directory -Path $ResultsRoot -Force | Out-Null

function Write-Step([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Stop-WithReport([string]$Message, [int]$Code = 1) {
    Write-Host "`nFAILED: $Message" -ForegroundColor Red
    Set-Content -Path (Join-Path $ResultsRoot 'FAILED.txt') -Value $Message -Encoding UTF8
    try { Start-Process explorer.exe $ResultsRoot | Out-Null } catch {}
    exit $Code
}

function Invoke-Git([string[]]$Arguments) {
    & git -C $ProjectRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Find-UnityEditor {
    if ($UnityExe -and (Test-Path $UnityExe)) {
        return (Resolve-Path $UnityExe).Path
    }

    $Candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "D:\Program Files\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "D:\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "E:\Program Files\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe",
        "E:\Unity\Hub\Editor\$RequiredUnityVersion\Editor\Unity.exe"
    )

    foreach ($Candidate in $Candidates) {
        if (Test-Path $Candidate) {
            return $Candidate
        }
    }

    $HubRoots = @(
        'C:\Program Files\Unity\Hub\Editor',
        'D:\Program Files\Unity\Hub\Editor',
        'D:\Unity\Hub\Editor',
        'E:\Program Files\Unity\Hub\Editor',
        'E:\Unity\Hub\Editor'
    )

    foreach ($Root in $HubRoots) {
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

function Download-File([string]$Url, [string]$Destination, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Url)) {
        throw "URL is empty for $Label"
    }

    $Directory = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    Write-Host "Downloading $Label..."
    Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $Destination -TimeoutSec 90
    if (-not (Test-Path $Destination) -or (Get-Item $Destination).Length -lt 64) {
        throw "Downloaded file is missing or too small: $Destination"
    }
}

function Run-Unity([string]$Label, [string[]]$Arguments, [string]$LogName) {
    Write-Step $Label
    $LogPath = Join-Path $ResultsRoot $LogName
    $AllArguments = @(
        '-batchmode',
        '-nographics',
        '-accept-apiupdate',
        '-projectPath', $ProjectRoot,
        '-logFile', $LogPath
    ) + $Arguments

    $Process = Start-Process -FilePath $script:ResolvedUnityExe `
        -ArgumentList $AllArguments `
        -Wait `
        -PassThru `
        -NoNewWindow

    Write-Host "$Label exit code: $($Process.ExitCode)"
    return $Process.ExitCode
}

try {
    Write-Step 'Checking repository'
    if (-not (Test-Path (Join-Path $ProjectRoot '.git'))) {
        Stop-WithReport 'RUN_PATCH4_VERIFY.ps1 must be located in the Git repository root.'
    }

    $Dirty = (& git -C $ProjectRoot status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        Stop-WithReport 'Could not read Git status.'
    }
    if ($Dirty) {
        Stop-WithReport "The working tree contains local changes. Commit or stash them first.`n$($Dirty -join "`n")"
    }

    if (-not $SkipPull) {
        Write-Step 'Updating patch-4.0 from GitHub'
        Invoke-Git @('fetch', 'origin', $RequiredBranch)
        $CurrentBranch = (& git -C $ProjectRoot branch --show-current).Trim()
        if ($CurrentBranch -ne $RequiredBranch) {
            $LocalBranchExists = (& git -C $ProjectRoot branch --list $RequiredBranch).Trim()
            if ($LocalBranchExists) {
                Invoke-Git @('switch', $RequiredBranch)
            } else {
                Invoke-Git @('switch', '--track', "origin/$RequiredBranch")
            }
        }
        Invoke-Git @('pull', '--ff-only', 'origin', $RequiredBranch)
    }

    $ProjectVersionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $ProjectVersionFile)) {
        Stop-WithReport 'ProjectSettings/ProjectVersion.txt was not found.'
    }
    $ProjectVersionText = Get-Content $ProjectVersionFile -Raw
    if ($ProjectVersionText -notmatch [regex]::Escape($RequiredUnityVersion)) {
        Stop-WithReport "Project requires Unity $RequiredUnityVersion. ProjectVersion.txt contains another version."
    }

    if (Get-Process Unity -ErrorAction SilentlyContinue) {
        Stop-WithReport 'Close all running Unity Editor windows before starting the automatic verification.'
    }

    $script:ResolvedUnityExe = Find-UnityEditor
    if (-not $script:ResolvedUnityExe) {
        Stop-WithReport "Unity $RequiredUnityVersion was not found. Install it in Unity Hub or set UNITY_EXE to the full Unity.exe path."
    }
    Write-Host "Unity: $script:ResolvedUnityExe"

    Write-Step 'Downloading approved Adobe sources'
    $ManifestPath = Join-Path $ProjectRoot 'Assets\GameWorkPatch4\Art\Character\FatMan\Masks\adobe-mask-manifest.json'
    if (-not (Test-Path $ManifestPath)) {
        Stop-WithReport 'Adobe mask manifest is missing.'
    }
    $Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

    $ArtRoot = Join-Path $ProjectRoot 'Assets\GameWorkPatch4\Art\Character\FatMan'
    $SourcePath = Join-Path $ArtRoot ("Source\" + $Manifest.source.fileName)
    Download-File $Manifest.source.url $SourcePath 'approved neutral master'

    $ActualSha = (Get-FileHash -Algorithm SHA256 -Path $SourcePath).Hash.ToLowerInvariant()
    if ($ActualSha -ne $ExpectedMasterSha) {
        Stop-WithReport "Approved master SHA mismatch. Expected $ExpectedMasterSha, received $ActualSha."
    }

    if ($Manifest.riggingReference) {
        $ReferencePath = Join-Path $ArtRoot ("References\" + $Manifest.riggingReference.fileName)
        Download-File $Manifest.riggingReference.url $ReferencePath 'Adobe rigging reference'
    }

    foreach ($Mask in $Manifest.masks) {
        if (-not $Mask.valid) { continue }
        $MaskPath = Join-Path $ArtRoot ("Masks\Downloaded\" + $Mask.fileName)
        Download-File $Mask.url $MaskPath ("mask " + $Mask.id)
    }

    $PrepareExit = Run-Unity `
        'Compiling, baking layers and rebuilding locked Patch 4 assets' `
        @('-executeMethod', 'SkinnyToBeast.Gameplay.Patch4.Editor.Patch4BatchAutomation.PrepareAndValidate') `
        '01-prepare-and-smoke.log'

    $EditExit = Run-Unity `
        'Running Patch 4 EditMode tests' `
        @('-runTests', '-testPlatform', 'EditMode', '-testFilter', 'SkinnyToBeast.GameWorkPatch4.EditModeTests', '-testResults', (Join-Path $ResultsRoot 'editmode-results.xml')) `
        '02-editmode.log'

    $PlayExit = Run-Unity `
        'Running Patch 4 PlayMode tests' `
        @('-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'SkinnyToBeast.GameWorkPatch4.PlayModeTests', '-testResults', (Join-Path $ResultsRoot 'playmode-results.xml')) `
        '03-playmode.log'

    $LibraryReports = Join-Path $ProjectRoot 'Library\GameWorkPatch4Reports'
    if (Test-Path $LibraryReports) {
        Copy-Item -Path (Join-Path $LibraryReports '*') -Destination $ResultsRoot -Recurse -Force
    }

    $Summary = @"
GameWork Patch 4.0 automatic verification
Generated: $(Get-Date -Format o)
Branch: $RequiredBranch
Unity: $RequiredUnityVersion
Prepare/smoke exit: $PrepareExit
EditMode exit: $EditExit
PlayMode exit: $PlayExit

Exit code 0 means the automated step passed.
Draft pixel/joint checks may remain blocked until hidden joints and final facial poses are manually painted.
Production art approval remains locked by design.
"@
    Set-Content -Path (Join-Path $ResultsRoot 'SUMMARY.txt') -Value $Summary -Encoding UTF8

    $ZipPath = "$ResultsRoot.zip"
    Compress-Archive -Path (Join-Path $ResultsRoot '*') -DestinationPath $ZipPath -Force

    Write-Step 'Finished'
    Write-Host $Summary
    Write-Host "Reports: $ResultsRoot"
    Write-Host "ZIP: $ZipPath"
    Start-Process explorer.exe $ResultsRoot | Out-Null

    if ($PrepareExit -ne 0 -or $EditExit -ne 0 -or $PlayExit -ne 0) {
        exit 2
    }
    exit 0
}
catch {
    Stop-WithReport $_.Exception.Message
}
