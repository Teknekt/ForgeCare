@echo off
setlocal EnableExtensions

cd /d "%~dp0"

echo.
echo ============================================================
echo   FORGECARE TECHNICIAN EDITION v1.0.0
echo   FINAL SETUP BUILDER
echo ============================================================
echo.

if not exist "ForgeCare.app.csproj" (
    echo ERROR: Put BUILD_SETUP_NOW.cmd in the ForgeCare.app project folder.
    echo It must sit beside ForgeCare.app.csproj.
    echo.
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: .NET SDK was not found.
    echo Install the .NET 10 SDK, then run this file again.
    echo.
    pause
    exit /b 1
)

set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"

if not defined ISCC (
    echo Inno Setup 6 was not found.
    echo.
    where winget >nul 2>nul
    if errorlevel 1 (
        echo Please install Inno Setup 6 manually and run this file again.
        echo.
        pause
        exit /b 1
    )

    choice /C YN /N /M "Install Inno Setup 6 with winget now? [Y/N] "
    if errorlevel 2 exit /b 1

    echo.
    echo Installing Inno Setup 6...
    winget install --id JRSoftware.InnoSetup -e --accept-source-agreements --accept-package-agreements

    if errorlevel 1 (
        echo.
        echo ERROR: Inno Setup installation failed.
        pause
        exit /b 1
    )

    if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
    if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
    if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
)

if not defined ISCC (
    echo ERROR: Inno Setup 6 is installed but ISCC.exe could not be located.
    pause
    exit /b 1
)

echo.
echo [1/4] Cleaning previous release output...
if exist "artifacts\publish\win-x64" rmdir /s /q "artifacts\publish\win-x64"
if exist "artifacts\installer" rmdir /s /q "artifacts\installer"
mkdir "artifacts\publish\win-x64" >nul 2>nul
mkdir "artifacts\installer" >nul 2>nul

echo [2/4] Publishing ForgeCare self-contained win-x64...
dotnet publish "ForgeCare.app.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o "artifacts\publish\win-x64"

if errorlevel 1 (
    echo.
    echo ERROR: dotnet publish failed.
    pause
    exit /b 1
)

echo [3/4] Building ForgeCare Setup.exe...
"%ISCC%" "installer\ForgeCare.iss"

if errorlevel 1 (
    echo.
    echo ERROR: Inno Setup build failed.
    pause
    exit /b 1
)

set "SETUP=artifacts\installer\ForgeCare-v1.0.0-Setup.exe"

if not exist "%SETUP%" (
    echo.
    echo ERROR: Setup build completed but expected file was not found:
    echo %SETUP%
    pause
    exit /b 1
)

echo [4/4] Calculating SHA-256...
powershell.exe -NoLogo -NoProfile -Command ^
  "$h=(Get-FileHash '%SETUP%' -Algorithm SHA256).Hash; Set-Content 'artifacts\installer\ForgeCare-v1.0.0-Setup.sha256.txt' $h; Write-Host ''; Write-Host 'SHA-256:' $h -ForegroundColor Green"

echo.
echo ============================================================
echo   SUCCESS
echo ============================================================
echo.
echo Setup:
echo   %CD%\%SETUP%
echo.
echo SHA-256:
echo   %CD%\artifacts\installer\ForgeCare-v1.0.0-Setup.sha256.txt
echo.
echo Opening installer output folder...
start "" "artifacts\installer"
echo.
pause
exit /b 0
