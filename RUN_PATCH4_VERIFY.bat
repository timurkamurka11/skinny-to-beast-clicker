@echo off
setlocal
set "PATCH4_PROJECT_ROOT=%~dp0"
cd /d "%PATCH4_PROJECT_ROOT%"

echo GameWork Patch 4.0 one-command launcher
echo It will preserve unrelated local Unity files, synchronize Patch 4,
echo compile, generate assets, run tests and open the project.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$r=(Resolve-Path $env:PATCH4_PROJECT_ROOT).Path; git -C $r fetch origin patch-4.0; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; $s=Join-Path $env:TEMP 'RUN_PATCH4_VERIFY_LATEST.ps1'; git -C $r show origin/patch-4.0:RUN_PATCH4_VERIFY.ps1 ^| Set-Content -Encoding UTF8 $s; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; ^& $s -ProjectRoot $r"
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
