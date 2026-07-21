param(
    [string]$InstallerPath = "",
    [string]$ExpectedVersion = "3.2",
    [switch]$AllowCustomInstallLocation
)

$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $root "dist\DanteConfigEditorV3_2_Installer.exe"
}

$installer = (Resolve-Path -LiteralPath $InstallerPath -ErrorAction Stop).Path
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Ce test d'installation doit être lancé dans PowerShell en tant qu'administrateur."
}

$targetRegistryPaths = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{76E68F80-5C89-4415-A090-370CA60EB3AD}_is1",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{76E68F80-5C89-4415-A090-370CA60EB3AD}_is1"
)

$obsoleteBetaRegistryPaths = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{A2CC4547-2811-4EB5-B0BC-FBE4B7B847DF}_is1",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{A2CC4547-2811-4EB5-B0BC-FBE4B7B847DF}_is1"
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

function Get-ObsoleteBetaInstallRecords {
    return @(
        foreach ($registryPath in $obsoleteBetaRegistryPaths) {
            if (Test-Path -LiteralPath $registryPath) {
                Get-ItemProperty -LiteralPath $registryPath
            }
        }
    )
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
        throw "$Step : une seule entrée V3.2 était attendue, trouvé $($targetRecords.Count)."
    }

    $obsoleteBetaRecords = @(Get-ObsoleteBetaInstallRecords)
    if ($obsoleteBetaRecords.Count -ne 0) {
        throw "$Step : l'ancienne V3.2 Beta est encore installée."
    }

    $allRecords = @(Get-AllV3InstallRecords)
    if ($allRecords.Count -ne 1) {
        throw "$Step : une seule installation V3 était attendue, trouvé $($allRecords.Count)."
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

    $shortcut = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonPrograms)) "Dante Config Editor V3.2\Dante Config Editor V3.2.lnk"
    if (-not (Test-Path -LiteralPath $shortcut)) {
        throw "$Step : raccourci Menu Démarrer manquant : $shortcut"
    }

    $desktopShortcutCandidates = @(
        (Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)) "Dante Config Editor V3.2.lnk"),
        (Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonDesktopDirectory)) "Dante Config Editor V3.2.lnk")
    ) | Select-Object -Unique
    if (-not ($desktopShortcutCandidates | Where-Object { Test-Path -LiteralPath $_ })) {
        throw "$Step : raccourci Bureau manquant. Chemins vérifiés : $($desktopShortcutCandidates -join ', ')"
    }

    return $record
}

$script:V3InstallRecordsBefore = @(Get-AllV3InstallRecords).Count
$script:TargetInstallRecordsBefore = @(Get-TargetInstallRecords).Count

# La V3.2 remplace les anciennes installations ; le second passage valide sa mise à niveau.
Invoke-InstallerPass -Name "Installation V3.2"
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
    ObsoleteBetaInstallRecords = @(Get-ObsoleteBetaInstallRecords).Count
    V3InstallRecords = @(Get-AllV3InstallRecords).Count
} | ConvertTo-Json -Depth 3
