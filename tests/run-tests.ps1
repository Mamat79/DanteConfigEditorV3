$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$projects = @(
    @{
        Name = "Tests Core et sécurité XML"
        Path = Join-Path $PSScriptRoot "DanteConfigEditorV3.Tests\DanteConfigEditorV3.Tests.csproj"
    },
    @{
        Name = "Tests interface Mac headless"
        Path = Join-Path $PSScriptRoot "DanteConfigEditor.Mac.Tests\DanteConfigEditor.Mac.Tests.csproj"
    }
)

foreach ($project in $projects) {
    Write-Host "Restauration - $($project.Name)"
    dotnet restore $project.Path
    if ($LASTEXITCODE -ne 0) {
        throw "La restauration a échoué pour : $($project.Name)."
    }

    Write-Host "Exécution - $($project.Name)"
    dotnet test $project.Path -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Les tests ont échoué pour : $($project.Name)."
    }
}

Write-Host "Toutes les suites Dante Config Editor ont réussi."
