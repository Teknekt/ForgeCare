@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0forge-release.ps1" -SkipInstaller
set EXITCODE=%ERRORLEVEL%
echo.
pause
exit /b %EXITCODE%
