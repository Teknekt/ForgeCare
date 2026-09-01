; ForgeCare Sprint 13C — Installer & Update Foundation
; Mindforge Studio
; Stable per-user installer identity. Newer builds with the same AppId upgrade in place.

#define MyAppName "ForgeCare"
#ifndef MyAppVersion
  #error MyAppVersion must be supplied by the ForgeCare release pipeline.
#endif
#ifndef MyNumericVersion
  #error MyNumericVersion must be supplied by the ForgeCare release pipeline.
#endif
#ifndef MyOutputBaseFilename
  #error MyOutputBaseFilename must be supplied by the ForgeCare release pipeline.
#endif
#define MyAppPublisher "Mindforge Studio"
#define MyAppExeName "ForgeCare.exe"
#define MyAppId "{{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyNumericVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=ForgeCare Technician Edition Setup
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyNumericVersion}

DefaultDirName={localappdata}\Programs\ForgeCare
DefaultGroupName=ForgeCare
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

OutputDir=..\artifacts\installer
OutputBaseFilename={#MyOutputBaseFilename}

SetupIconFile=..\Assets\Icons\ForgeCare.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName=ForgeCare Technician Edition

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DisableProgramGroupPage=yes
DirExistsWarning=no
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes
CloseApplications=yes
RestartApplications=yes
SetupLogging=yes
UsedUserAreasWarning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\ForgeCare"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\ForgeCare"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ForgeCare"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Deliberately remove only installer-owned files.
; ForgeCare user data lives separately in %LOCALAPPDATA%\ForgeCare and is preserved.
Type: filesandordirs; Name: "{app}"
