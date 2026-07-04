$ErrorActionPreference = "Stop"

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetCandidates = @((Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"))
if ($dotnetCommand) {
    $dotnetCandidates += $dotnetCommand.Source
}

$dotnet = $null
foreach ($candidate in ($dotnetCandidates | Select-Object -Unique)) {
    if ((Test-Path $candidate) -and ((& $candidate --list-sdks) -match "^8\."))
    {
        $dotnet = $candidate
        break
    }
}

if (-not $dotnet) {
    Write-Host "Le SDK .NET 8 est nécessaire pour compiler Dante Config Editor V3."
    Write-Host "Téléchargement : https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

& $dotnet build "$PSScriptRoot\DanteConfigEditorV3.csproj" -c Release
& $dotnet publish "$PSScriptRoot\DanteConfigEditorV3.csproj" -c Release -r win-x64 --self-contained false -o "$PSScriptRoot\publish"

Write-Host ""
Write-Host "Compilation terminée."
Write-Host "Application publiée dans : $PSScriptRoot\publish"
