$ErrorActionPreference = "Stop"

$installDir = Join-Path $env:LOCALAPPDATA "DanteConfigEditorV3"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Dante Config Editor V3"
$desktopShortcut = Join-Path ([Environment]::GetFolderPath("Desktop")) "Dante Config Editor V3.lnk"

if (Test-Path $desktopShortcut) {
    Remove-Item -LiteralPath $desktopShortcut -Force
}

if (Test-Path $startMenuDir) {
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}

if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

Write-Host "Dante Config Editor V3 a été désinstallé."
