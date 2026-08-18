param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet CLI was not found in PATH." -ForegroundColor Red
    Write-Host "Install the .NET SDK used by ForgeCare, then run this script again."
    exit 2
}

Write-Host "dotnet: $((dotnet --version))" -ForegroundColor DarkGray
$Project = Join-Path $ProjectRoot "ForgeCare.app.csproj"
$Artifacts = Join-Path $ProjectRoot "artifacts"
$PublishDir = Join-Path $Artifacts "ForgeCare-win-x64"
$ZipPath = Join-Path $Artifacts "ForgeCare-v0.0.24-alpha-win-x64.zip"

Write-Host ""
Write-Host "==========================================" -ForegroundColor DarkYellow
Write-Host " FORGECARE RELEASE FORGE - v0.0.24-alpha" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor DarkYellow
Write-Host ""

if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

Write-Host "[1/4] Restoring..." -ForegroundColor Cyan
dotnet restore $Project

Write-Host "[2/4] Building Release..." -ForegroundColor Cyan
dotnet build $Project -c $Configuration --no-restore

Write-Host "[3/4] Publishing self-contained win-x64..." -ForegroundColor Cyan
dotnet publish $Project `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $PublishDir

Write-Host "[4/4] Packaging portable release..." -ForegroundColor Cyan

$Readme = @"
ForgeCare v0.0.24-alpha
Mindforge Studio

Portable Windows x64 alpha build.

RUN:
  ForgeCare.exe

NOTES:
- ForgeCare intentionally runs as the current user.
- Operations that require additional Windows permissions must request them explicitly.
- This is an alpha build. Test on non-critical systems before production use.
- Report/session data is stored locally by ForgeCare.
"@

Set-Content -Path (Join-Path $PublishDir "README.txt") -Value $Readme -Encoding UTF8

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $ZipPath

Write-Host ""
Write-Host "RELEASE READY" -ForegroundColor Green
Write-Host "Folder: $PublishDir"
Write-Host "ZIP:    $ZipPath"
Write-Host ""
