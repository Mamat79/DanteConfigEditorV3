param(
    [string]$InstallerPath = "",
    [string]$ExpectedVersion = "3.08",
    [switch]$AllowCustomInstallLocation
)

$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $root "dist\DanteConfigEditorV3_08_Installer.exe"
}

$installer = (Resolve-Path -LiteralPath $InstallerPath -ErrorAction Stop).Path
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Ce test d'installation doit être lancé dans PowerShell en tant qu'administrateur."
}

$targetRegistryPaths = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{23FF6543-561B-4C55-B733-817C9F92F5AA}_is1",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{23FF6543-561B-4C55-B733-817C9F92F5AA}_is1"
)

$stableRegistryPaths = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}_is1",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D9A22EA8-8370-4C6D-9E7C-DBC5A59F53A1}_is1"
)

function Get-TargetInstallRecords {
    return @(
        foreach ($registryPath in $targetRegistryPaths) {
            if (Test-Path -LiteralPath $registryPath) {
                Get-ItemProperty -LiteralPath $registryPath
            }
        }
    )
}

function Get-StableInstallRecords {
    return @(
        foreach ($registryPath in $stableRegistryPaths) {
            if (Test-Path -LiteralPath $registryPath) {
                Get-ItemProperty -LiteralPath $registryPath
            }
        }
    )
}

$stableRecordsBefore = @(Get-StableInstallRecords)
if ($stableRecordsBefore.Count -gt 1) {
    throw "Plusieurs installations V3.07 stables ont été trouvées avant le test."
}

$stableSnapshot = @(
    foreach ($record in $stableRecordsBefore) {
        [pscustomobject]@{
            DisplayVersion = [string]$record.DisplayVersion
            InstallLocation = [string]$record.InstallLocation
        }
    }
)

function Assert-StableInstallUnchanged {
    param([Parameter(Mandatory = $true)][string]$Step)

    $current = @(Get-StableInstallRecords)
    if ($current.Count -ne $stableSnapshot.Count) {
        throw "$Step : l'installation V3.07 stable a été ajoutée ou supprimée par erreur."
    }

    for ($index = 0; $index -lt $stableSnapshot.Count; $index++) {
        if ($current[$index].DisplayVersion -ne $stableSnapshot[$index].DisplayVersion -or
            -not [string]::Equals([string]$current[$index].InstallLocation, [string]$stableSnapshot[$index].InstallLocation, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "$Step : l'installation V3.07 stable a été modifiée par erreur."
        }
    }
}

function Get-AllV3InstallRecords {
    $roots = @(
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall"
    )

    return @(
        foreach ($registryRoot in $roots) {
            Get-ChildItem -LiteralPath $registryRoot -ErrorAction SilentlyContinue |
                ForEach-Object { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction SilentlyContinue } |
                Where-Object { $_.DisplayName -like "Dante Config Editor V3*" }
        }
    )
}

function Invoke-InstallerPass {
    param([Parameter(Mandatory = $true)][string]$Name)

    Write-Host "$Name : $installer"
    $process = Start-Process -FilePath $installer `
        -ArgumentList @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/SP-", "/LANG=french") `
        -WindowStyle Hidden `
        -Wait `
        -PassThru

    if ($process.ExitCode -ne 0) {
        throw "$Name a échoué avec le code retour $($process.ExitCode)."
    }
}

function Assert-InstalledState {
    param([Parameter(Mandatory = $true)][string]$Step)

    $targetRecords = @(Get-TargetInstallRecords)
    if ($targetRecords.Count -ne 1) {
        throw "$Step : une seule entrée V3.08 était attendue, trouvé $($targetRecords.Count)."
    }

    Assert-StableInstallUnchanged -Step $Step

    $allRecords = @(Get-AllV3InstallRecords)
    $expectedV3Count = $stableSnapshot.Count + 1
    if ($allRecords.Count -ne $expectedV3Count) {
        throw "$Step : $expectedV3Count installation(s) Dante Config Editor V3 étaient attendues, trouvé $($allRecords.Count)."
    }

    $record = $targetRecords[0]
    if ($record.DisplayVersion -ne $ExpectedVersion) {
        throw "$Step : version installée '$($record.DisplayVersion)' au lieu de '$ExpectedVersion'."
    }

    $installLocation = [System.IO.Path]::GetFullPath([string]$record.InstallLocation)
    if (-not $AllowCustomInstallLocation) {
        $programFilesRoot = [System.IO.Path]::GetFullPath($env:ProgramFiles).TrimEnd('\') + '\'
        if (-not $installLocation.StartsWith($programFilesRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "$Step : installation hors de Program Files : $installLocation"
        }
    }

    $requiredFiles = @(
        "DanteConfigEditorV3.exe",
        "QuickStart_DanteConfigEditorV3_FR.pdf",
        "QuickStart_DanteConfigEditorV3_EN.pdf",
        "Notice_DanteConfigEditorV3_FR.pdf",
        "Notice_DanteConfigEditorV3_EN.pdf",
        "README.md",
        "README_EN.md",
        "RELEASE_NOTES.md",
        "RELEASE_NOTES_EN.md",
        "unins000.exe"
    )

    foreach ($requiredFile in $requiredFiles) {
        $path = Join-Path $installLocation $requiredFile
        if (-not (Test-Path -LiteralPath $path)) {
            throw "$Step : fichier installé manquant : $path"
        }
    }

    $shortcut = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonPrograms)) "Dante Config Editor V3.08\Dante Config Editor V3.08.lnk"
    if (-not (Test-Path -LiteralPath $shortcut)) {
        throw "$Step : raccourci Menu Démarrer manquant : $shortcut"
    }

    return $record
}

# Deux passages prouvent que la V3.08 se met à niveau elle-même sans toucher à la V3.07.
Invoke-InstallerPass -Name "Installation ou remplacement initial"
$firstRecord = Assert-InstalledState -Step "Après le premier passage"

Invoke-InstallerPass -Name "Mise à niveau de contrôle"
$secondRecord = Assert-InstalledState -Step "Après le second passage"

if (-not [string]::Equals([string]$firstRecord.InstallLocation, [string]$secondRecord.InstallLocation, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Le chemin d'installation a changé pendant la mise à niveau."
}

$exePath = Join-Path ([string]$secondRecord.InstallLocation) "DanteConfigEditorV3.exe"
$fileVersion = (Get-Item -LiteralPath $exePath).VersionInfo.FileVersion

[pscustomobject]@{
    Result = "Success"
    Version = [string]$secondRecord.DisplayVersion
    FileVersion = $fileVersion
    InstallLocation = [string]$secondRecord.InstallLocation
    Installer = $installer
    InstallerSha256 = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
    UpgradePasses = 2
    TargetInstallRecords = @(Get-TargetInstallRecords).Count
    StableInstallRecords = @(Get-StableInstallRecords).Count
    V3InstallRecords = @(Get-AllV3InstallRecords).Count
} | ConvertTo-Json -Depth 3
