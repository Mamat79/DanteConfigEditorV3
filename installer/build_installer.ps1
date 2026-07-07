$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "DanteConfigEditorV3.csproj"
$dist = Join-Path $root "dist"
$payload = Join-Path $dist "installer_payload"
$installer = Join-Path $dist "DanteConfigEditorV3_Installer.exe"
$script = Join-Path $PSScriptRoot "DanteConfigEditorV3.iss"
$isccCandidates = @(
    $env:ISCC_PATH,
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetCandidates = @((Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"))
if ($dotnetCommand) {
    $dotnetCandidates += $dotnetCommand.Source
}
$dotnetCandidates = $dotnetCandidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

$dotnet = $null
foreach ($candidate in $dotnetCandidates) {
    if ((Test-Path $candidate) -and ((& $candidate --list-sdks) -match "^8\."))
    {
        $dotnet = $candidate
        break
    }
}

if (-not $dotnet) {
    throw "SDK .NET 8 introuvable. Installez le SDK .NET 8 avant de construire l'installateur."
}

if (-not $iscc) {
    throw "Inno Setup est introuvable. Installez Inno Setup 6 ou définissez ISCC_PATH vers ISCC.exe avant de construire l'installateur."
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

foreach ($temporaryPath in @($payload, (Join-Path $dist "self-contained-win-x64"), (Join-Path $dist "portable"), (Join-Path $dist "setup_payload"))) {
    if (Test-Path $temporaryPath) {
        Remove-Item -LiteralPath $temporaryPath -Recurse -Force
    }
}

foreach ($obsoleteFile in @(
    (Join-Path $dist "DanteConfigEditorV3_Setup.exe"),
    (Join-Path $dist "DanteConfigEditorV3_SetupPayload.7z"),
    (Join-Path $dist "DanteConfigEditorV3_SetupPayload.zip"),
    (Join-Path $dist "DanteConfigEditorV3_SfxConfig.txt")
)) {
    if (Test-Path $obsoleteFile) {
        Remove-Item -LiteralPath $obsoleteFile -Force
    }
}

& $dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $payload

if ($LASTEXITCODE -ne 0) {
    throw "La publication .NET a échoué."
}

& $iscc $script

if ($LASTEXITCODE -ne 0) {
    throw "La création de l'installateur a échoué."
}

if (-not (Test-Path $installer)) {
    throw "L'installateur final est introuvable : $installer"
}

Remove-Item -LiteralPath $payload -Recurse -Force

if (Test-Path (Join-Path $root "bin")) {
    Remove-Item -LiteralPath (Join-Path $root "bin") -Recurse -Force
}

if (Test-Path (Join-Path $root "obj")) {
    Remove-Item -LiteralPath (Join-Path $root "obj") -Recurse -Force
}

Write-Host "Installateur professionnel créé : $installer"
