param(
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $ProjectRoot "ForgeCare.app.csproj"
$ReleasePropsPath = Join-Path $ProjectRoot "ForgeCare.Release.props"
$IdentityCheck = Join-Path $PSScriptRoot "Test-ReleaseIdentity.ps1"

& $IdentityCheck -ProjectRoot $ProjectRoot

[xml]$ReleaseProps = Get-Content $ReleasePropsPath
$ReleaseIdentity = $ReleaseProps.Project.PropertyGroup
$Version = [string]$ReleaseIdentity.ForgeCareVersion
$NumericVersion = [string]$ReleaseIdentity.ForgeCareNumericVersion
$Channel = [string]$ReleaseIdentity.ForgeCareReleaseChannel
$PortableFileName = [string]$ReleaseIdentity.ForgeCarePortableFileName
$InstallerBaseName = [string]$ReleaseIdentity.ForgeCareInstallerBaseName

$Artifacts = Join-Path $ProjectRoot "artifacts"
$PublishDir = Join-Path $Artifacts "publish\win-x64"
$InstallerDir = Join-Path $Artifacts "installer"
$PortableZip = Join-Path $Artifacts $PortableFileName
$InstallerOutput = Join-Path $InstallerDir "$InstallerBaseName.exe"
$InstallerScript = Join-Path $ProjectRoot "installer\ForgeCare.iss"
$ManifestPath = Join-Path $Artifacts "release-manifest.json"

Write-Host ""
Write-Host "==================================================" -ForegroundColor DarkYellow
Write-Host " FORGECARE RELEASE FORGE - v$Version" -ForegroundColor Yellow
Write-Host " Mindforge Studio · Technician Edition" -ForegroundColor DarkGray
Write-Host "==================================================" -ForegroundColor DarkYellow
Write-Host ""

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI was not found. Install the .NET 10 SDK, then retry."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "git was not found. Exact source commit metadata is required for beta packaging."
}

$SourceCommit = (& git -C $ProjectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $SourceCommit -notmatch '^[0-9a-fA-F]{40}$') {
    throw "Could not resolve the exact Git source commit for beta packaging."
}

Write-Host "dotnet SDK: $(dotnet --version)" -ForegroundColor DarkGray

if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null

Write-Host ""
Write-Host "[1/6] Restore" -ForegroundColor Cyan
dotnet restore $Project

Write-Host "[2/6] Release build" -ForegroundColor Cyan
dotnet build $Project -c Release --no-restore

Write-Host "[3/6] Self-contained Windows x64 publish" -ForegroundColor Cyan
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $PublishDir

$Readme = @"
ForgeCare v$Version
Mindforge Studio
Technician Edition

Windows x64 beta build.

RUN
  ForgeCare.exe

DISTRIBUTION
- Portable ZIP: run from an extracted user-writable folder.
- Installer: per-user installation under LocalAppData\Programs\ForgeCare.
- A newer installer with the same ForgeCare AppId upgrades the installed build in place.
- ForgeCare operational/user data is kept under LocalAppData\ForgeCare and is not removed by app uninstall.
- This controlled beta is unsigned. Verify artifact SHA-256 values before use.

IMPORTANT
- ForgeCare runs as the current Windows user by default.
- It does not silently elevate permissions.
- This is ForgeCare Technician Edition v$Version. Use technician judgement before system-changing actions.
- Update discovery, secure download and hash verification are available when configured; installer handoff remains technician-controlled.
"@

Set-Content `
    -Path (Join-Path $PublishDir "README.txt") `
    -Value $Readme `
    -Encoding UTF8

$ReleaseInfo = [ordered]@{
    product = "ForgeCare"
    edition = "Technician Edition"
    publisher = "Mindforge Studio"
    version = $Version
    numericVersion = $NumericVersion
    channel = $Channel
    runtime = "win-x64"
    target = "windows-11-x64"
    selfContained = $true
    signed = $false
    sourceCommit = $SourceCommit
    installScope = "per-user"
    updateMode = "technician-controlled-beta"
    stableInstallerAppId = "{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}"
    localDataRoot = "%LOCALAPPDATA%\ForgeCare"
    generatedAt = (Get-Date).ToString("o")
}

$ReleaseInfo |
    ConvertTo-Json -Depth 4 |
    Set-Content `
        -Path (Join-Path $PublishDir "release-info.json") `
        -Encoding UTF8

foreach ($file in @("BETA_TEST_CHECKLIST.md", "BETA_TESTER_README.txt")) {
    $source = Join-Path $ProjectRoot $file
    if (Test-Path $source) {
        Copy-Item $source (Join-Path $PublishDir $file) -Force
    }
}

Write-Host "[4/6] Portable ZIP" -ForegroundColor Cyan
if (Test-Path $PortableZip) {
    Remove-Item $PortableZip -Force
}

Compress-Archive `
    -Path (Join-Path $PublishDir "*") `
    -DestinationPath $PortableZip

$InstallerBuilt = $false

if ($SkipInstaller) {
    Write-Host "[5/6] Installer skipped by request" -ForegroundColor DarkGray
}
else {
    Write-Host "[5/6] Upgrade-safe per-user installer" -ForegroundColor Cyan

    $InnoCandidates = @(
    @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }
)

    if ($InnoCandidates.Count -eq 0) {
    Write-Host ""
    Write-Host "Inno Setup 6 was not found." -ForegroundColor Yellow
    Write-Host "Portable beta release is READY." -ForegroundColor Green
    Write-Host "Install Inno Setup 6 and rerun this script to create the installer." -ForegroundColor Yellow
}
else {
    $ISCC = $InnoCandidates[0]
    Write-Host "Inno Setup: $ISCC" -ForegroundColor DarkGray
    & $ISCC `
        "/DMyAppVersion=$Version" `
        "/DMyNumericVersion=$NumericVersion" `
        "/DMyOutputBaseFilename=$InstallerBaseName" `
        $InstallerScript

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup returned exit code $LASTEXITCODE."
    }

    $InstallerBuilt = Test-Path $InstallerOutput
}
}

Write-Host "[6/6] Release manifest + SHA-256 fingerprints" -ForegroundColor Cyan

$PortableHash = (Get-FileHash $PortableZip -Algorithm SHA256).Hash

$InstallerHash = $null
if ($InstallerBuilt) {
    $InstallerHash = (Get-FileHash $InstallerOutput -Algorithm SHA256).Hash
}

$Manifest = [ordered]@{
    schemaVersion = 1
    product = "ForgeCare"
    edition = "Technician Edition"
    publisher = "Mindforge Studio"
    version = $Version
    numericVersion = $NumericVersion
    channel = $Channel
    target = "windows-11-x64"
    signed = $false
    sourceCommit = $SourceCommit
    publishedAt = (Get-Date).ToString("o")
    updateMode = "technician-controlled-beta"
    appId = "{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}"
    portable = [ordered]@{
        file = [IO.Path]::GetFileName($PortableZip)
        sha256 = $PortableHash
        runtime = "win-x64"
        selfContained = $true
    }
    installer = if ($InstallerBuilt) {
        [ordered]@{
            file = [IO.Path]::GetFileName($InstallerOutput)
            sha256 = $InstallerHash
            scope = "per-user"
            upgradeInPlace = $true
        }
    } else {
        $null
    }
    remoteUpdate = [ordered]@{
        enabled = $true
        discoveryOnly = $false
        secureDownload = $true
        installerExecution = $false
        manifestUrl = $null
        note = "ForgeCare supports HTTPS discovery, secure download, SHA-256 verification and explicit technician-controlled installer handoff when configured."
    }
}

$Manifest |
    ConvertTo-Json -Depth 8 |
    Set-Content `
        -Path $ManifestPath `
        -Encoding UTF8

Write-Host ""
Write-Host "==================================================" -ForegroundColor DarkYellow
Write-Host " RELEASE OUTPUT" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor DarkYellow
Write-Host "Publish:   $PublishDir"
Write-Host "Portable:  $PortableZip"
Write-Host "Manifest:  $ManifestPath"

if ($InstallerBuilt) {
    Write-Host "Installer: $InstallerOutput" -ForegroundColor Green
}
else {
    Write-Host "Installer: not built (portable release still valid)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Forge complete." -ForegroundColor Green
