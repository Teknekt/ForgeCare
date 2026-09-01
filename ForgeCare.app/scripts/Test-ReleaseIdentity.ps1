param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw "Release identity validation failed: $Message"
    }
}

$PropsPath = Join-Path $ProjectRoot "ForgeCare.Release.props"
$ProjectPath = Join-Path $ProjectRoot "ForgeCare.app.csproj"
$ManifestPath = Join-Path $ProjectRoot "app.manifest"
$InstallerPath = Join-Path $ProjectRoot "installer\ForgeCare.iss"
$TemplatePath = Join-Path $ProjectRoot "release-manifest.template.json"
$CurrentReleaseSurfaces = @(
    "BETA_TESTER_README.txt",
    "BETA_TEST_CHECKLIST.md",
    "BUILD_SETUP_NOW.cmd",
    "scripts\forge-beta-kit.cmd",
    "scripts\forge-installer.cmd",
    "scripts\forge-release.cmd",
    "scripts\forge-release.ps1",
    "scripts\publish-win-x64.cmd",
    "scripts\publish-win-x64.ps1",
    "..\docs\V1.1_BETA_QUICK_START.md",
    "..\docs\V1.1_BETA_KNOWN_LIMITATIONS.md",
    "..\docs\V1.1_BETA_PRIVACY_SUPPORT.md",
    "..\docs\V1.1_BETA_ISSUE_REPORTING.md",
    "..\docs\V1.1_BETA_INSTALL_UPGRADE_ACCEPTANCE.md"
)

Require (Test-Path $PropsPath) "ForgeCare.Release.props is missing."
[xml]$Props = Get-Content $PropsPath
$Identity = $Props.Project.PropertyGroup

$Version = [string]$Identity.ForgeCareVersion
$NumericVersion = [string]$Identity.ForgeCareNumericVersion
$Channel = [string]$Identity.ForgeCareReleaseChannel
$PortableFileName = [string]$Identity.ForgeCarePortableFileName
$InstallerBaseName = [string]$Identity.ForgeCareInstallerBaseName

Require ($Version -match '^1\.1\.0-beta\.\d+$') "beta version '$Version' is not a v1.1 beta identity."
Require ($NumericVersion -eq '1.1.0.0') "numeric version '$NumericVersion' must be 1.1.0.0."
Require ($Channel -eq 'beta') "channel '$Channel' must be beta."
Require ($PortableFileName -eq "ForgeCare-v$Version-win-x64-portable.zip") "portable filename is inconsistent."
Require ($InstallerBaseName -eq "ForgeCare-v$Version-Setup") "installer filename is inconsistent."

[xml]$Project = Get-Content $ProjectPath
$PropertyGroup = @($Project.Project.PropertyGroup) | Where-Object { $_.Version } | Select-Object -First 1
Require ($Project.Project.Import.Project -eq 'ForgeCare.Release.props') "application project does not import the authoritative release identity."
Require ($PropertyGroup.Version -eq '$(ForgeCareVersion)') "project Version does not consume ForgeCareVersion."
Require ($PropertyGroup.AssemblyVersion -eq '$(ForgeCareNumericVersion)') "project AssemblyVersion does not consume ForgeCareNumericVersion."
Require ($PropertyGroup.FileVersion -eq '$(ForgeCareNumericVersion)') "project FileVersion does not consume ForgeCareNumericVersion."
Require ($PropertyGroup.InformationalVersion -eq '$(ForgeCareVersion)') "project InformationalVersion does not consume ForgeCareVersion."

[xml]$Manifest = Get-Content $ManifestPath
$ManifestNamespace = New-Object System.Xml.XmlNamespaceManager($Manifest.NameTable)
$ManifestNamespace.AddNamespace('asm', 'urn:schemas-microsoft-com:asm.v1')
$AssemblyIdentity = $Manifest.SelectSingleNode('/asm:assembly/asm:assemblyIdentity', $ManifestNamespace)
Require ($AssemblyIdentity.version -eq $NumericVersion) "application manifest version is inconsistent."

$Installer = Get-Content $InstallerPath -Raw
Require ($Installer -match '#ifndef MyAppVersion') "installer does not require the release version input."
Require ($Installer -match '#ifndef MyNumericVersion') "installer does not require the numeric version input."
Require ($Installer -match '#ifndef MyOutputBaseFilename') "installer does not require the output filename input."
Require ($Installer -match 'AppId=\{#MyAppId\}') "stable installer AppId contract is missing."
Require ($Installer -match '\{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7\}') "stable installer AppId changed."

$Template = Get-Content $TemplatePath -Raw | ConvertFrom-Json
Require ($Template.version -eq $Version) "release manifest template version is inconsistent."
Require ($Template.numericVersion -eq $NumericVersion) "release manifest template numeric version is inconsistent."
Require ($Template.channel -eq $Channel) "release manifest template channel is inconsistent."
Require ($Template.portable.file -eq $PortableFileName) "release manifest template portable filename is inconsistent."
Require ($Template.installer.file -eq "$InstallerBaseName.exe") "release manifest template installer filename is inconsistent."
Require ($Template.target -eq 'windows-11-x64') "release manifest template target is inconsistent."
Require ($Template.signed -eq $false) "controlled beta must declare its unsigned state."

foreach ($RelativePath in $CurrentReleaseSurfaces) {
    $SurfacePath = Join-Path $ProjectRoot $RelativePath
    Require (Test-Path $SurfacePath) "current release surface '$RelativePath' is missing."
    $Surface = Get-Content $SurfacePath -Raw
    Require ($Surface -notmatch '(?i)(v?1\.0\.0|v0\.0\.\d+-(alpha|beta))') "current release surface '$RelativePath' contains a stale release identity."
}

foreach ($RelativePath in $CurrentReleaseSurfaces | Where-Object { $_ -like '..\docs\*' -or $_ -like 'BETA_*' }) {
    $Surface = Get-Content (Join-Path $ProjectRoot $RelativePath) -Raw
    Require ($Surface -match '1\.1\.0-beta\.1') "beta document '$RelativePath' does not identify v1.1.0-beta.1."
    Require ($Surface -match '(?i)Windows 11 x64') "beta document '$RelativePath' does not state the Windows 11 x64 target."
    Require ($Surface -match '(?i)unsigned') "beta document '$RelativePath' does not state the unsigned beta policy."
}

Write-Host "Release identity PASS: ForgeCare v$Version ($NumericVersion), channel $Channel." -ForegroundColor Green
