$ErrorActionPreference = "Stop"

$exe = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\DanteConfigEditorV3.exe"
if (-not (Test-Path $exe)) {
    Write-Host "L'application n'est pas encore compilée. Lancement d'une compilation Release..."
    $dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
    if (-not (Test-Path $dotnet)) {
        $dotnet = "dotnet"
    }

    & $dotnet build "$PSScriptRoot\DanteConfigEditorV3.csproj" -c Release
}

Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
