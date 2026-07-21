#define MyAppName "Dante Config Editor V3.1"
#define MyAppVersion "3.1"
#define MyAppPublisher "Mamat"
#define MyAppExeName "DanteConfigEditorV3.exe"
#define MyAppShortcutName "Dante Config Editor V3.1"
#define SourceRoot ".."

[Setup]
AppId={{76E68F80-5C89-4415-A090-370CA60EB3AD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Dante Config Editor V3.1
DefaultGroupName=Dante Config Editor V3.1
DisableProgramGroupPage=no
AllowNoIcons=yes
OutputDir={#SourceRoot}\dist
OutputBaseFilename=DanteConfigEditorV3_1_Installer
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
VersionInfoDescription=Dante Config Editor V3.1 installer
VersionInfoProductName={#MyAppName}
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousGroup=yes

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceRoot}\dist\installer_payload\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\DanteEdit.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\README_EN.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\CHANGELOG_V3.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\RELEASE_NOTES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\RELEASE_NOTES_EN.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\docs\QuickStart_DanteConfigEditorV3_FR.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\docs\QuickStart_DanteConfigEditorV3_EN.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\docs\Notice_DanteConfigEditorV3_FR.pdf"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}\docs\Notice_DanteConfigEditorV3_EN.pdf"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
Type: files; Name: "{app}\QuickStart_DanteConfigEditorV3.pdf"
Type: files; Name: "{app}\Notice_DanteConfigEditorV3.pdf"
Type: files; Name: "{group}\Quick start PDF.lnk"
Type: files; Name: "{group}\Notice PDF.lnk"

[Icons]
Name: "{group}\{code:GetShortcutAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"
Name: "{group}\Documentation - Français"; Filename: "{app}\README.md"
Name: "{group}\Documentation - English"; Filename: "{app}\README_EN.md"
Name: "{group}\Démarrage rapide - Français"; Filename: "{app}\QuickStart_DanteConfigEditorV3_FR.pdf"
Name: "{group}\Quick start - English"; Filename: "{app}\QuickStart_DanteConfigEditorV3_EN.pdf"
Name: "{group}\Notice complète - Français"; Filename: "{app}\Notice_DanteConfigEditorV3_FR.pdf"
Name: "{group}\Full user guide - English"; Filename: "{app}\Notice_DanteConfigEditorV3_EN.pdf"
Name: "{group}\Notes de version - Français"; Filename: "{app}\RELEASE_NOTES.md"
Name: "{group}\Release notes - English"; Filename: "{app}\RELEASE_NOTES_EN.md"
Name: "{group}\Désinstaller {code:GetShortcutAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{code:GetShortcutAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\DanteEdit.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Dante Config Editor V3.1}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\RELEASE_NOTES.md"; Description: "Ouvrir les notes de version"; Flags: postinstall shellexec unchecked skipifsilent; Check: IsFrenchLanguage
Filename: "{app}\RELEASE_NOTES_EN.md"; Description: "Open the release notes"; Flags: postinstall shellexec unchecked skipifsilent; Check: IsEnglishLanguage
Filename: "{app}\QuickStart_DanteConfigEditorV3_FR.pdf"; Description: "Ouvrir le démarrage rapide en français"; Flags: postinstall shellexec unchecked skipifsilent; Check: IsFrenchLanguage
Filename: "{app}\Notice_DanteConfigEditorV3_FR.pdf"; Description: "Ouvrir la notice complète en français"; Flags: postinstall shellexec unchecked skipifsilent; Check: IsFrenchLanguage
Filename: "{app}\QuickStart_DanteConfigEditorV3_EN.pdf"; Description: "Open the English quick start"; Flags: postinstall shellexec unchecked skipifsilent; Check: IsEnglishLanguage
Filename: "{app}\Notice_DanteConfigEditorV3_EN.pdf"; Description: "Open the full English user guide"; Flags: postinstall shellexec unchecked skipifsilent; Check: IsEnglishLanguage

[Code]
var
  SignatureLabel: TNewStaticText;
  SignatureAgentsLabel: TNewStaticText;
  GithubLabel: TNewStaticText;
  ExistingInstallDir: String;
  ExistingInstallVersion: String;
  ExistingInstallIsCurrent: Boolean;
  LegacyV309Uninstaller: String;
  LegacyV308Uninstaller: String;
  LegacyV307Uninstaller: String;

function GetShortcutAppName(Param: String): String;
begin
  Result := '{#MyAppShortcutName}';
end;

function IsFrenchLanguage(): Boolean;
begin
  Result := ActiveLanguage = 'french';
end;

function IsEnglishLanguage(): Boolean;
begin
  Result := ActiveLanguage = 'english';
end;

procedure OpenGithub(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://github.com/Mamat79/DanteConfigEditorV3', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

function QueryInstallValue(AppId: String; ValueName: String; var Value: String): Boolean;
var
  RegistryKey: String;
begin
  RegistryKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{' + AppId + '}_is1';
  Result := RegQueryStringValue(HKLM, RegistryKey, ValueName, Value);
  if not Result then
  begin
    Result := RegQueryStringValue(HKCU, RegistryKey, ValueName, Value);
  end;
end;

function DetectExistingInstall(): Boolean;
var
  LegacyInstallDir: String;
begin
  ExistingInstallDir := '';
  ExistingInstallVersion := '';
  ExistingInstallIsCurrent := False;
  LegacyV309Uninstaller := '';
  LegacyV308Uninstaller := '';
  LegacyV307Uninstaller := '';

  QueryInstallValue('C72399DF-AC3B-4FFA-A503-D79A4D6D9380', 'UninstallString', LegacyV309Uninstaller);
  QueryInstallValue('23FF6543-561B-4C55-B733-817C9F92F5AA', 'UninstallString', LegacyV308Uninstaller);
  QueryInstallValue('D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1', 'UninstallString', LegacyV307Uninstaller);

  Result := QueryInstallValue('76E68F80-5C89-4415-A090-370CA60EB3AD', 'InstallLocation', ExistingInstallDir);
  ExistingInstallIsCurrent := Result;

  if not Result then
  begin
    LegacyInstallDir := '';
    if QueryInstallValue('C72399DF-AC3B-4FFA-A503-D79A4D6D9380', 'InstallLocation', LegacyInstallDir) then
    begin
      ExistingInstallDir := LegacyInstallDir;
      Result := True;
    end
    else if QueryInstallValue('23FF6543-561B-4C55-B733-817C9F92F5AA', 'InstallLocation', LegacyInstallDir) then
    begin
      ExistingInstallDir := LegacyInstallDir;
      Result := True;
    end
    else if QueryInstallValue('D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1', 'InstallLocation', LegacyInstallDir) then
    begin
      ExistingInstallDir := LegacyInstallDir;
      Result := True;
    end;
  end;

  if Result then
  begin
    if ExistingInstallIsCurrent then
      QueryInstallValue('76E68F80-5C89-4415-A090-370CA60EB3AD', 'DisplayVersion', ExistingInstallVersion)
    else if LegacyV309Uninstaller <> '' then
      QueryInstallValue('C72399DF-AC3B-4FFA-A503-D79A4D6D9380', 'DisplayVersion', ExistingInstallVersion)
    else if LegacyV308Uninstaller <> '' then
      QueryInstallValue('23FF6543-561B-4C55-B733-817C9F92F5AA', 'DisplayVersion', ExistingInstallVersion)
    else
      QueryInstallValue('D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1', 'DisplayVersion', ExistingInstallVersion);

    if ExistingInstallVersion = '' then
    begin
      ExistingInstallVersion := 'version inconnue';
    end;
  end;
end;

function ExistingInstallPromptText(): String;
begin
  if ActiveLanguage = 'english' then
  begin
    Result :=
      'A previous installation of Dante Config Editor was found.' + #13#10#13#10 +
      'Detected version: ' + ExistingInstallVersion + #13#10 +
      'Folder: ' + ExistingInstallDir + #13#10#13#10 +
      'Yes = replace/update this installation.' + #13#10 +
      'No = close the installer without changing the installed version.';
  end
  else
  begin
    Result :=
      'Une version précédente de Dante Config Editor est déjà installée.' + #13#10#13#10 +
      'Version détectée : ' + ExistingInstallVersion + #13#10 +
      'Dossier : ' + ExistingInstallDir + #13#10#13#10 +
      'Oui = remplacer / mettre à jour cette installation.' + #13#10 +
      'Non = quitter sans modifier la version installée.';
  end;
end;

procedure InitializeWizard();
begin
  if ExistingInstallIsCurrent and (ExistingInstallDir <> '') then
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
  SignatureLabel.Left := WizardForm.ClientWidth - ScaleX(82);
  SignatureLabel.Top := WizardForm.ClientHeight - ScaleY(36);
  SignatureLabel.Font.Color := clGray;

  SignatureAgentsLabel := TNewStaticText.Create(WizardForm);
  SignatureAgentsLabel.Parent := WizardForm;
  SignatureAgentsLabel.Caption := 'et ses agents';
  SignatureAgentsLabel.Left := WizardForm.ClientWidth - ScaleX(82);
  SignatureAgentsLabel.Top := WizardForm.ClientHeight - ScaleY(22);
  SignatureAgentsLabel.Font.Color := clGray;
  SignatureAgentsLabel.Font.Size := 7;
end;

function RunLegacyUninstaller(UninstallCommand: String): Boolean;
var
  ResultCode: Integer;
  UninstallerPath: String;
begin
  Result := True;
  if UninstallCommand = '' then
    Exit;

  UninstallerPath := RemoveQuotes(UninstallCommand);
  Result := Exec(
    UninstallerPath,
    '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) and (ResultCode = 0);
end;

procedure DeleteLegacyShortcutVersion(VersionName: String);
begin
  { Les anciennes versions ont parfois créé leurs raccourcis hors du groupe attendu. }
  DeleteFile(ExpandConstant('{userprograms}\Dante Config Editor ' + VersionName + '.lnk'));
  DeleteFile(ExpandConstant('{commonprograms}\Dante Config Editor ' + VersionName + '.lnk'));
  DeleteFile(ExpandConstant('{userdesktop}\Dante Config Editor ' + VersionName + '.lnk'));
  DeleteFile(ExpandConstant('{commondesktop}\Dante Config Editor ' + VersionName + '.lnk'));
  DelTree(ExpandConstant('{userprograms}\Dante Config Editor ' + VersionName), True, True, True);
  DelTree(ExpandConstant('{commonprograms}\Dante Config Editor ' + VersionName), True, True, True);
end;

procedure DeleteLegacyShortcuts();
begin
  DeleteLegacyShortcutVersion('V3.07');
  DeleteLegacyShortcutVersion('V3.08');
  DeleteLegacyShortcutVersion('V3.09');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';

  if not RunLegacyUninstaller(LegacyV309Uninstaller) then
  begin
    Result := 'Impossible de remplacer automatiquement Dante Config Editor V3.09. Fermez l''application puis réessayez.';
    Exit;
  end;

  if not RunLegacyUninstaller(LegacyV308Uninstaller) then
  begin
    Result := 'Impossible de remplacer automatiquement Dante Config Editor V3.08. Fermez l''application puis réessayez.';
    Exit;
  end;

  if not RunLegacyUninstaller(LegacyV307Uninstaller) then
  begin
    Result := 'Impossible de remplacer automatiquement Dante Config Editor V3.07. Fermez l''application puis réessayez.';
    Exit;
  end;

  DeleteLegacyShortcuts();
end;

function InitializeSetup(): Boolean;
var
  Choice: Integer;
begin
  Result := True;

  DetectExistingInstall();

  if WizardSilent then
  begin
    Exit;
  end;

  if ExistingInstallDir <> '' then
  begin
    Choice := MsgBox(ExistingInstallPromptText(), mbConfirmation, MB_YESNO);
    if Choice <> IDYES then
    begin
      Result := False;
      Exit;
    end;
  end;
end;
