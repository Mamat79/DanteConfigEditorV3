#define MyAppName "Dante Config Editor V3.05"
#define MyAppVersion "3.05-dev"
#define MyAppPublisher "Mamat"
#define MyAppExeName "DanteConfigEditorV3.exe"
#define MyAppShortcutName "Dante Config Editor V3"
#define MyAppSideBySideName "Dante Config Editor V3 3.05-dev"
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
VersionInfoVersion=3.5.0
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
Source: "{#SourceRoot}\docs\QuickStart_DanteConfigEditorV3.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\docs\Notice_DanteConfigEditorV3.pdf"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{code:GetShortcutAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"
Name: "{group}\Documentation"; Filename: "{app}\README.md"
Name: "{group}\Quick start PDF"; Filename: "{app}\QuickStart_DanteConfigEditorV3.pdf"
Name: "{group}\Notice PDF"; Filename: "{app}\Notice_DanteConfigEditorV3.pdf"
Name: "{group}\Release notes"; Filename: "{app}\RELEASE_NOTES.md"
Name: "{group}\Désinstaller {code:GetShortcutAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{code:GetShortcutAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Dante Config Editor V3}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\RELEASE_NOTES.md"; Description: "Ouvrir les release notes"; Flags: postinstall shellexec unchecked skipifsilent
Filename: "{app}\QuickStart_DanteConfigEditorV3.pdf"; Description: "Ouvrir le quick start PDF"; Flags: postinstall shellexec unchecked skipifsilent
Filename: "{app}\Notice_DanteConfigEditorV3.pdf"; Description: "Ouvrir la notice d'utilisation PDF"; Flags: postinstall shellexec unchecked skipifsilent

[Code]
var
  SignatureLabel: TNewStaticText;
  GithubLabel: TNewStaticText;
  ExistingInstallDir: String;
  ExistingInstallVersion: String;
  InstallAlongside: Boolean;

function GetShortcutAppName(Param: String): String;
begin
  if InstallAlongside then
  begin
    Result := '{#MyAppSideBySideName}';
  end
  else
  begin
    Result := '{#MyAppShortcutName}';
  end;
end;

procedure OpenGithub(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://github.com/Mamat79/DanteConfigEditorV3', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

function QueryExistingInstallValue(ValueName: String; var Value: String): Boolean;
begin
  Result := RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}_is1', ValueName, Value);
  if not Result then
  begin
    Result := RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}_is1', ValueName, Value);
  end;
end;

function DetectExistingInstall(): Boolean;
begin
  ExistingInstallDir := '';
  ExistingInstallVersion := '';
  Result := QueryExistingInstallValue('InstallLocation', ExistingInstallDir);

  if Result then
  begin
    QueryExistingInstallValue('DisplayVersion', ExistingInstallVersion);
    if ExistingInstallVersion = '' then
    begin
      ExistingInstallVersion := 'version inconnue';
    end;
  end;
end;

function NormalizeDir(Value: String): String;
begin
  Result := Lowercase(RemoveBackslashUnlessRoot(Value));
end;

function ExistingInstallPromptText(): String;
begin
  if ActiveLanguage = 'english' then
  begin
    Result :=
      'A previous installation of Dante Config Editor was found.' + #13#10#13#10 +
      'Detected version: ' + ExistingInstallVersion + #13#10 +
      'Folder: ' + ExistingInstallDir + #13#10#13#10 +
      'Yes = replace/update this installation (recommended).' + #13#10 +
      'No = install an additional copy in another folder.' + #13#10 +
      'Cancel = close the installer.';
  end
  else
  begin
    Result :=
      'Une version précédente de Dante Config Editor est déjà installée.' + #13#10#13#10 +
      'Version détectée : ' + ExistingInstallVersion + #13#10 +
      'Dossier : ' + ExistingInstallDir + #13#10#13#10 +
      'Oui = remplacer / mettre à jour cette installation (recommandé).' + #13#10 +
      'Non = installer en plus dans un autre dossier.' + #13#10 +
      'Annuler = quitter l''installateur.';
  end;
end;

procedure InitializeWizard();
begin
  if InstallAlongside then
  begin
    WizardForm.DirEdit.Text := ExpandConstant('{autopf}\{#MyAppSideBySideName}');
    WizardForm.GroupEdit.Text := '{#MyAppSideBySideName}';
  end
  else if ExistingInstallDir <> '' then
  begin
    WizardForm.DirEdit.Text := ExistingInstallDir;
  end;

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
var
  Choice: Integer;
begin
  Result := True;
  InstallAlongside := False;

  if WizardSilent then
  begin
    Exit;
  end;

  if DetectExistingInstall() then
  begin
    Choice := MsgBox(ExistingInstallPromptText(), mbConfirmation, MB_YESNOCANCEL);
    if Choice = IDCANCEL then
    begin
      Result := False;
      Exit;
    end;

    InstallAlongside := Choice = IDNO;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if InstallAlongside and (CurPageID = wpSelectDir) and (ExistingInstallDir <> '') then
  begin
    if CompareText(NormalizeDir(WizardDirValue), NormalizeDir(ExistingInstallDir)) = 0 then
    begin
      if ActiveLanguage = 'english' then
      begin
        MsgBox('For an additional installation, choose a different destination folder.', mbError, MB_OK);
      end
      else
      begin
        MsgBox('Pour installer en plus, choisissez un dossier de destination différent.', mbError, MB_OK);
      end;

      Result := False;
    end;
  end;
end;
