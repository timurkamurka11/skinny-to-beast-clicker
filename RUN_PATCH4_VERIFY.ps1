param(
    [string]$ProjectRoot = "",
    [string]$UnityExe = $env:UNITY_EXE,
    [switch]$SkipRemoteSync,
    [switch]$KeepUnityClosed
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$ProjectRoot = (Resolve-Path $ProjectRoot).Path

$RequiredBranch = 'patch-4.0'
$RequiredUnityVersion = '6000.3.19f1'
$ExpectedMasterSha = '5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c'
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ResultsRoot = Join-Path $ProjectRoot "Patch4VerificationResults\$Timestamp"
$ResultsInitialized = $false

$ManagedPaths = @(
    'Assets/GameWorkPatch4',
    'Docs/Patch4',
    'RUN_PATCH4_VERIFY.ps1',
    'RUN_PATCH4_VERIFY.bat',
    '.github/workflows/patch4-static-guard.yml'
)

function Initialize-Results {
    if (-not $script:ResultsInitialized) {
        New-Item -ItemType Directory -Path $script:ResultsRoot -Force | Out-Null
        $script:ResultsInitialized = $true
    }
}

function Write-Step([string]$Message) {
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Stop-WithReport([string]$Message, [int]$Code = 1) {
    Initialize-Results
    Write-Host "`nFAILED: $Message" -ForegroundColor Red
    Set-Content -Path (Join-Path $script:ResultsRoot 'FAILED.txt') -Value $Message -Encoding UTF8
    try { Start-Process explorer.exe $script:ResultsRoot | Out-Null } catch {}
    exit $Code
}

function Invoke-Git([string[]]$Arguments) {
    & git -C $ProjectRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Normalize-StatusPath([string]$StatusLine) {
    if ($StatusLine.Length -lt 4) { return '' }
    $Path = $StatusLine.Substring(3).Trim()
    if ($Path.Contains(' -> ')) {
        $Path = ($Path -split ' -> ')[-1]
    }
    return $Path.Trim('"')
}

function Is-ManagedPath([string]$Path) {
    $Normalized = $Path.Replace('\', '/')
    foreach ($Managed in $ManagedPaths) {
        if ($Normalized -eq $Managed -or $Normalized.StartsWith($Managed + '/', [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Backup-LocalState {
    Initialize-Results
    $Status = @(& git -C $ProjectRoot status --porcelain=v1)
    if ($LASTEXITCODE -ne 0) {
        Stop-WithReport 'Could not read Git status.'
    }

    if ($Status.Count -eq 0) {
        Set-Content -Path (Join-Path $ResultsRoot 'LOCAL_CHANGES_PRESERVED.txt') -Value 'Working tree was clean.' -Encoding UTF8
        return
    }

    Set-Content -Path (Join-Path $ResultsRoot 'LOCAL_CHANGES_PRESERVED.txt') -Value $Status -Encoding UTF8
    $BackupRoot = Join-Path $ResultsRoot 'ManagedBackup'

    foreach ($Line in $Status) {
        $Code = $Line.Substring(0, 2)
        $Path = Normalize-StatusPath $Line
        if (-not (Is-ManagedPath $Path)) { continue }
        if ($Code -eq '??') { continue }

        $Source = Join-Path $ProjectRoot $Path
        if (-not (Test-Path $Source)) { continue }
        $Destination = Join-Path $BackupRoot $Path
        New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
        Copy-Item -Path $Source -Destination $Destination -Recurse -Force
    }
}

function Copy-SyncedPath([string]$SyncRoot, [string]$RelativePath) {
    $Source = Join-Path $SyncRoot ($RelativePath.Replace('/', '\'))
    $Destination = Join-Path $ProjectRoot ($RelativePath.Replace('/', '\'))
    if (-not (Test-Path $Source)) { return }

    if ((Get-Item $Source).PSIsContainer) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
        Get-ChildItem -Path $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
    } else {
        New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
        Copy-Item -Path $Source -Destination $Destination -Force
    }
}

function Sync-Patch4FromRemote {
    Write-Step 'Synchronizing only Patch 4 files from GitHub'
    Backup-LocalState
    Invoke-Git @('fetch', 'origin', $RequiredBranch)

    $SyncId = [Guid]::NewGuid().ToString('N')
    $ArchivePath = Join-Path $env:TEMP "patch4-sync-$SyncId.zip"
    $SyncRoot = Join-Path $env:TEMP "patch4-sync-$SyncId"
    New-Item -ItemType Directory -Path $SyncRoot -Force | Out-Null

    try {
        $ArchiveArguments = @(
            'archive',
            '--format=zip',
            "--output=$ArchivePath",
            "origin/$RequiredBranch",
            '--'
        ) + $ManagedPaths
        Invoke-Git $ArchiveArguments
        Expand-Archive -Path $ArchivePath -DestinationPath $SyncRoot -Force

        foreach ($Path in $ManagedPaths) {
            Copy-SyncedPath $SyncRoot $Path
        }
    }
    finally {
        Remove-Item -Path $ArchivePath -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $SyncRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host 'Patch 4 files synchronized. Unrelated local Unity files were preserved.' -ForegroundColor Green
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
        if (Test-Path $Candidate) { return $Candidate }
    }

    foreach ($Root in @(
        'C:\Program Files\Unity\Hub\Editor',
        'D:\Program Files\Unity\Hub\Editor',
        'D:\Unity\Hub\Editor',
        'E:\Program Files\Unity\Hub\Editor',
        'E:\Unity\Hub\Editor')) {
        if (-not (Test-Path $Root)) { continue }
        $Found = Get-ChildItem -Path $Root -Filter Unity.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*$RequiredUnityVersion*" } |
            Select-Object -First 1
        if ($Found) { return $Found.FullName }
    }

    return $null
}

function Download-File([string]$Url, [string]$Destination, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Url)) {
        throw "URL is empty for $Label"
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
    Write-Host "Downloading $Label..."
    Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $Destination -TimeoutSec 90
    if (-not (Test-Path $Destination) -or (Get-Item $Destination).Length -lt 64) {
        throw "Downloaded file is missing or too small: $Destination"
    }
}

function Quote-NativeArgument([string]$Value) {
    if ($Value -notmatch '[\s"]') { return $Value }
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Run-Unity([string]$Label, [string[]]$Arguments, [string]$LogName) {
    Write-Step $Label
    Initialize-Results
    $LogPath = Join-Path $script:ResultsRoot $LogName
    $AllArguments = @(
        '-batchmode',
        '-nographics',
        '-accept-apiupdate',
        '-projectPath', $ProjectRoot,
        '-logFile', $LogPath
    ) + $Arguments

    $ArgumentLine = ($AllArguments | ForEach-Object { Quote-NativeArgument $_ }) -join ' '
    $Process = Start-Process -FilePath $script:ResolvedUnityExe `
        -ArgumentList $ArgumentLine `
        -Wait `
        -PassThru `
        -NoNewWindow

    Write-Host "$Label exit code: $($Process.ExitCode)"
    return $Process.ExitCode
}

function Open-UnityProject {
    if ($KeepUnityClosed) { return }
    try {
        $Arguments = '-projectPath ' + (Quote-NativeArgument $ProjectRoot)
        Start-Process -FilePath $script:ResolvedUnityExe -ArgumentList $Arguments | Out-Null
        Write-Host 'Unity project opened.' -ForegroundColor Green
    } catch {
        Write-Warning "Could not open Unity automatically: $($_.Exception.Message)"
    }
}

try {
    Write-Step 'Checking project'
    if (-not (Test-Path (Join-Path $ProjectRoot '.git'))) {
        Stop-WithReport 'The selected project root is not a Git repository.'
    }

    if (-not $SkipRemoteSync) {
        Sync-Patch4FromRemote
    } else {
        Backup-LocalState
    }

    Initialize-Results

    $ProjectVersionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $ProjectVersionFile)) {
        Stop-WithReport 'ProjectSettings/ProjectVersion.txt was not found.'
    }
    if ((Get-Content $ProjectVersionFile -Raw) -notmatch [regex]::Escape($RequiredUnityVersion)) {
        Stop-WithReport "The project requires Unity $RequiredUnityVersion."
    }

    if (Get-Process Unity -ErrorAction SilentlyContinue) {
        Stop-WithReport 'Close the currently running Unity Editor once, then run the same one-command launcher again. Unsaved Unity work was not force-closed.'
    }

    $script:ResolvedUnityExe = Find-UnityEditor
    if (-not $script:ResolvedUnityExe) {
        Stop-WithReport "Unity $RequiredUnityVersion was not found. Install it in Unity Hub or set UNITY_EXE to Unity.exe."
    }
    Write-Host "Unity: $script:ResolvedUnityExe"

    Write-Step 'Downloading approved Adobe sources'
    $ManifestPath = Join-Path $ProjectRoot 'Assets\GameWorkPatch4\Art\Character\FatMan\Masks\adobe-mask-manifest.json'
    if (-not (Test-Path $ManifestPath)) { Stop-WithReport 'Adobe mask manifest is missing.' }
    $Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

    $ArtRoot = Join-Path $ProjectRoot 'Assets\GameWorkPatch4\Art\Character\FatMan'
    $SourcePath = Join-Path $ArtRoot ("Source\" + $Manifest.source.fileName)
    Download-File $Manifest.source.url $SourcePath 'approved neutral master'

    $ActualSha = (Get-FileHash -Algorithm SHA256 -Path $SourcePath).Hash.ToLowerInvariant()
    if ($ActualSha -ne $ExpectedMasterSha) {
        Stop-WithReport "Approved master SHA mismatch. Expected $ExpectedMasterSha, received $ActualSha."
    }

    if ($Manifest.riggingReference) {
        Download-File `
            $Manifest.riggingReference.url `
            (Join-Path $ArtRoot ("References\" + $Manifest.riggingReference.fileName)) `
            'Adobe rigging reference'
    }

    foreach ($Mask in $Manifest.masks) {
        if (-not $Mask.valid) { continue }
        Download-File `
            $Mask.url `
            (Join-Path $ArtRoot ("Masks\Downloaded\" + $Mask.fileName)) `
            ("mask " + $Mask.id)
    }

    $PrepareExit = Run-Unity `
        'Compiling, baking layers and rebuilding locked Patch 4 assets' `
        @('-executeMethod', 'SkinnyToBeast.Gameplay.Patch4.Editor.Patch4BatchAutomation.PrepareAndValidate') `
        '01-prepare-and-smoke.log'

    $EditExit = Run-Unity `
        'Running Patch 4 EditMode tests' `
        @('-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'SkinnyToBeast.GameWorkPatch4.EditModeTests', '-testResults', (Join-Path $ResultsRoot 'editmode-results.xml')) `
        '02-editmode.log'

    $PlayExit = Run-Unity `
        'Running Patch 4 PlayMode tests' `
        @('-runTests', '-testPlatform', 'PlayMode', '-assemblyNames', 'SkinnyToBeast.GameWorkPatch4.PlayModeTests', '-testResults', (Join-Path $ResultsRoot 'playmode-results.xml')) `
        '03-playmode.log'

    $LibraryReports = Join-Path $ProjectRoot 'Library\GameWorkPatch4Reports'
    if (Test-Path $LibraryReports) {
        Copy-Item -Path (Join-Path $LibraryReports '*') -Destination $ResultsRoot -Recurse -Force
    }

    $Summary = @"
GameWork Patch 4.0 automatic verification
Generated: $(Get-Date -Format o)
Project: $ProjectRoot
Unity: $RequiredUnityVersion
Prepare/smoke exit: $PrepareExit
EditMode exit: $EditExit
PlayMode exit: $PlayExit

Patch 4 source files were synchronized without switching branches, stashing, resetting or deleting unrelated local Unity files.
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
    Open-UnityProject

    if ($PrepareExit -ne 0 -or $EditExit -ne 0 -or $PlayExit -ne 0) { exit 2 }
    exit 0
}
catch {
    Stop-WithReport $_.Exception.Message
}
