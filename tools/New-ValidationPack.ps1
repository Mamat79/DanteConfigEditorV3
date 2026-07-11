param(
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$SourceXml,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $PSScriptRoot 'DanteConfigEditor.ValidationPack\DanteConfigEditor.ValidationPack.csproj'
$resolvedSource = (Resolve-Path -LiteralPath $SourceXml).Path
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)

dotnet run --project $projectPath -c Release -- $resolvedSource $resolvedOutput
if ($LASTEXITCODE -ne 0) {
    throw "La création du pack de validation a échoué (code $LASTEXITCODE)."
}

Write-Host "Pack de validation créé dans : $resolvedOutput"
