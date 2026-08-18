@echo off
setlocal

echo.
echo ==================================================
echo  FORGECARE FIRST EXTERNAL MACHINE TEST KIT
echo  v1.0.0
echo ==================================================
echo.
echo Builds the portable self-contained beta package.
echo PowerShell policy bypass applies to this process only.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0forge-release.ps1" -SkipInstaller

set EXITCODE=%ERRORLEVEL%

echo.
if "%EXITCODE%"=="0" (
    echo Beta test kit build completed.
    echo.
    echo Send/copy the portable ZIP from the artifacts folder
    echo to a NON-CRITICAL Windows x64 test machine.
) else (
    echo Beta kit build FAILED with exit code %EXITCODE%.
)
echo.
pause
exit /b %EXITCODE%
