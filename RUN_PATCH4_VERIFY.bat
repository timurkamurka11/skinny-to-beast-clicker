@echo off
setlocal
set "PATCH4_PROJECT_ROOT=%~dp0"
cd /d "%PATCH4_PROJECT_ROOT%"

echo GameWork Patch 4.0 one-command launcher
echo It downloads the latest Patch 4 launcher directly from GitHub,
echo preserves unrelated Unity files, generates assets, runs tests and opens Unity.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$r=(Resolve-Path $env:PATCH4_PROJECT_ROOT).Path; $s=Join-Path $env:TEMP 'RUN_PATCH4_VERIFY_LATEST.ps1'; Invoke-WebRequest -UseBasicParsing -Uri 'https://raw.githubusercontent.com/timurkamurka11/skinny-to-beast-clicker/patch-4.0/RUN_PATCH4_VERIFY.ps1' -OutFile $s -TimeoutSec 120; ^& $s -ProjectRoot $r"
set EXIT_CODE=%ERRORLEVEL%

echo.
if "%EXIT_CODE%"=="0" (
    echo Patch 4 automatic verification completed successfully.
) else (
    echo Patch 4 finished with findings. Reports and Unity were opened automatically when possible.
)
echo.
pause
exit /b %EXIT_CODE%
