$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root "dist"
$zipPath = Join-Path $dist "DanteConfigEditorV3_source.zip"

New-Item -ItemType Directory -Force -Path $dist | Out-Null
if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$excludedDirectories = @(".git", "bin", "obj", "publish", "dist", "tmp", ".vs", ".vscode")
$excludedExtensions = @(".log", ".tmp", ".bak")

$files = Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $relative = $_.FullName.Substring($root.Length).TrimStart("\", "/")
    $parts = $relative -split "[\\/]"
    -not ($parts | Where-Object { $excludedDirectories -contains $_ }) `
        -and $excludedExtensions -notcontains $_.Extension.ToLowerInvariant() `
        -and $_.Name -notlike "*.exe"
}

$staging = Join-Path $env:TEMP ("DanteConfigEditorV3_source_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $staging | Out-Null

try {
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($root.Length).TrimStart("\", "/")
        $target = Join-Path $staging $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }

    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -Force
}
finally {
    if (Test-Path $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}

Write-Host "Archive source créée : $zipPath"
