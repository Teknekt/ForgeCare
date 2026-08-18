@echo off
setlocal

echo.
echo ==========================================
echo  FORGECARE RELEASE FORGE - v0.0.24-alpha
echo ==========================================
echo.
echo This launcher runs the LOCAL ForgeCare release script with
echo ExecutionPolicy Bypass for this PowerShell process only.
echo It does NOT change your system or user execution policy.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-win-x64.ps1"

set EXITCODE=%ERRORLEVEL%

echo.
if not "%EXITCODE%"=="0" (
    echo ForgeCare publish failed with exit code %EXITCODE%.
) else (
    echo ForgeCare publish completed successfully.
)

echo.
pause
exit /b %EXITCODE%
