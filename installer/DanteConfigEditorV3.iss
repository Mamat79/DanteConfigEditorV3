#define MyAppName "Dante Config Editor V3.01"
#define MyAppVersion "3.01-dev"
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
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion=3.1.0
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
Source: "{#SourceRoot}\RELEASE_NOTES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\docs\Notice_DanteConfigEditorV3.pdf"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Dante Config Editor V3"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"
Name: "{group}\Documentation"; Filename: "{app}\README.md"
Name: "{group}\Notice PDF"; Filename: "{app}\Notice_DanteConfigEditorV3.pdf"
Name: "{group}\Release notes"; Filename: "{app}\RELEASE_NOTES.md"
Name: "{group}\Désinstaller Dante Config Editor V3"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Dante Config Editor V3"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Dante Config Editor V3}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\RELEASE_NOTES.md"; Description: "Ouvrir les release notes"; Flags: postinstall shellexec unchecked skipifsilent
Filename: "{app}\Notice_DanteConfigEditorV3.pdf"; Description: "Ouvrir la notice d'utilisation PDF"; Flags: postinstall shellexec unchecked skipifsilent

[Code]
var
  SignatureLabel: TNewStaticText;
  GithubLabel: TNewStaticText;

procedure OpenGithub(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://github.com/Mamat79/DanteConfigEditorV3', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure InitializeWizard();
begin
  GithubLabel := TNewStaticText.Create(WizardForm);
  GithubLabel.Parent := WizardForm;
  GithubLabel.Caption := 'GitHub public';
  GithubLabel.Left := ScaleX(12);
  GithubLabel.Top := WizardForm.ClientHeight - ScaleY(28);
  GithubLabel.Font.Color := clBlue;
  GithubLabel.Font.Style := [fsUnderline];
  GithubLabel.Cursor := crHand;
  GithubLabel.OnClick := @OpenGithub;

  SignatureLabel := TNewStaticText.Create(WizardForm);
  SignatureLabel.Parent := WizardForm;
  SignatureLabel.Caption := 'By Mamat';
  SignatureLabel.Left := WizardForm.ClientWidth - ScaleX(75);
  SignatureLabel.Top := WizardForm.ClientHeight - ScaleY(28);
  SignatureLabel.Font.Color := clGray;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;
