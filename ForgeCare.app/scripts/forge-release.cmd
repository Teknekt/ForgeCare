@echo off
setlocal

echo.
echo ===============================================
echo  FORGECARE RELEASE FORGE - v1.0.0
echo ===============================================
echo.
echo Builds portable + installer when Inno Setup 6 is available.
echo PowerShell policy bypass applies to THIS PROCESS ONLY.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0forge-release.ps1"

set EXITCODE=%ERRORLEVEL%

echo.
if "%EXITCODE%"=="0" (
    echo ForgeCare release pipeline finished.
) else (
    echo ForgeCare release pipeline FAILED with exit code %EXITCODE%.
)
echo.
pause
exit /b %EXITCODE%
