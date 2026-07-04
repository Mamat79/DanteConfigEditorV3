#define MyAppName "Dante Config Editor V3"
#define MyAppVersion "0.3.0-dev"
#define MyAppPublisher "Mamat"
#define MyAppExeName "DanteConfigEditorV3.exe"
#define SourceRoot ".."

[Setup]
AppId={{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Dante Config Editor V3
DefaultGroupName=Dante Config Editor V3
DisableProgramGroupPage=no
AllowNoIcons=yes
OutputDir={#SourceRoot}\dist
OutputBaseFilename=DanteConfigEditorV3_Installer
SetupIconFile={#SourceRoot}\DanteEdit.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion=0.3.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Dante Config Editor V3 installer
VersionInfoProductName={#MyAppName}
SetupLogging=yes

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceRoot}\dist\installer_payload\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\DanteEdit.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README_V3.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\CHANGELOG_V3.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Dante Config Editor V3"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"
Name: "{group}\Documentation"; Filename: "{app}\README.md"
Name: "{group}\Désinstaller Dante Config Editor V3"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Dante Config Editor V3"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Dante Config Editor V3}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
