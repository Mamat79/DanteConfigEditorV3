$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $PSScriptRoot "DanteConfigEditorV3.Tests\DanteConfigEditorV3.Tests.csproj"

dotnet test $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Les tests Dante Config Editor ont échoué."
}

Write-Host "Tous les tests Dante Config Editor ont réussi."
