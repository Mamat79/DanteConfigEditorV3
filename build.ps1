$ErrorActionPreference = "Stop"

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetCandidates = @((Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"))
if ($dotnetCommand) {
    $dotnetCandidates += $dotnetCommand.Source
}

$dotnet = $null
foreach ($candidate in ($dotnetCandidates | Select-Object -Unique)) {
    if (Test-Path $candidate) {
        $sdkList = & $candidate --list-sdks
        if ($LASTEXITCODE -eq 0 -and ($sdkList -match "^8\.")) {
            $dotnet = $candidate
            break
        }
    }
}

if (-not $dotnet) {
    Write-Host "Le SDK .NET 8 est nécessaire pour compiler Dante Config Editor V3."
    Write-Host "Téléchargement : https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

function Invoke-DotNetStep {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host $Name
    & $dotnet @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "$Name a échoué avec le code retour $exitCode."
    }
}

$project = "$PSScriptRoot\DanteConfigEditorV3.csproj"
Invoke-DotNetStep -Name "Restauration des dépendances" -Arguments @("restore", $project)
Invoke-DotNetStep -Name "Compilation Release" -Arguments @("build", $project, "-c", "Release", "--no-restore")
Invoke-DotNetStep -Name "Publication Windows x64" -Arguments @("publish", $project, "-c", "Release", "-r", "win-x64", "--self-contained", "false", "-o", "$PSScriptRoot\publish")

Write-Host ""
Write-Host "Compilation terminée."
Write-Host "Application publiée dans : $PSScriptRoot\publish"
