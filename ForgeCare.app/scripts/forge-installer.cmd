@echo off
setlocal

echo.
echo ==================================================
echo  FORGECARE INSTALLER / UPGRADE BUILD
echo  v1.0.0
echo ==================================================
echo.
echo This requires Inno Setup 6.
echo Existing ForgeCare per-user installations use a stable
echo AppId and are upgraded in place by newer setup builds.
echo.

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0forge-release.ps1"

set EXITCODE=%ERRORLEVEL%
echo.
pause
exit /b %EXITCODE%
