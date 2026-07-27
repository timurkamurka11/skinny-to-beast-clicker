@echo off
setlocal
cd /d "%~dp0"

echo GameWork Patch 4.0 automatic verification
echo Close Unity Editor before continuing.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0RUN_PATCH4_VERIFY.ps1"
set EXIT_CODE=%ERRORLEVEL%

echo.
if "%EXIT_CODE%"=="0" (
    echo Patch 4 automatic verification completed successfully.
) else (
    echo Patch 4 verification finished with errors. Open Patch4VerificationResults for reports.
)
echo.
pause
exit /b %EXIT_CODE%
