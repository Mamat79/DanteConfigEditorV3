$ErrorActionPreference = "Stop"

$installDir = Join-Path $env:LOCALAPPDATA "DanteConfigEditorV3"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Dante Config Editor V3"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Dante Config Editor V3.lnk"
$startMenuShortcut = Join-Path $startMenuDir "Dante Config Editor V3.lnk"

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null

$files = @(
    "DanteConfigEditorV3.exe",
    "DanteEdit.ico",
    "README_V3.md",
    "CHANGELOG_V3.md",
    "QuickStart_DanteConfigEditorV3_FR.pdf",
    "QuickStart_DanteConfigEditorV3_EN.pdf",
    "Notice_DanteConfigEditorV3_FR.pdf",
    "Notice_DanteConfigEditorV3_EN.pdf",
    "uninstall.ps1"
)

foreach ($file in $files) {
    $source = Join-Path $PSScriptRoot $file
    if (Test-Path $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $installDir $file) -Force
    }
}

$exePath = Join-Path $installDir "DanteConfigEditorV3.exe"
if (-not (Test-Path $exePath)) {
    throw "Installation impossible : DanteConfigEditorV3.exe est introuvable dans le paquet."
}

$shell = New-Object -ComObject WScript.Shell

$desktop = $shell.CreateShortcut($desktopShortcut)
$desktop.TargetPath = $exePath
$desktop.WorkingDirectory = $installDir
$desktop.IconLocation = $exePath
$desktop.Save()

$startMenu = $shell.CreateShortcut($startMenuShortcut)
$startMenu.TargetPath = $exePath
$startMenu.WorkingDirectory = $installDir
$startMenu.IconLocation = $exePath
$startMenu.Save()

Write-Host ""
Write-Host "Dante Config Editor V3 est installé."
Write-Host "Dossier : $installDir"
Write-Host "Raccourci Bureau : $desktopShortcut"
Write-Host "Raccourci Menu Démarrer : $startMenuShortcut"
Write-Host ""
Write-Host "Aucune installation .NET supplémentaire n'est nécessaire pour utiliser cette version autonome."
