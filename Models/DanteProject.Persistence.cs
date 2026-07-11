using System.IO;
using System.Xml.Linq;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Models;

public sealed partial class DanteProject
{
    public string SaveAs(string destinationPath)
    {
        return SaveAs(destinationPath, null);
    }

    internal string SaveAs(string destinationPath, Action<string>? saveStageObserver)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Le chemin de destination doit être renseigné.", nameof(destinationPath));
        }

        DanteValidationResult validation = Validate();
        if (validation.HasErrors)
        {
            throw new InvalidOperationException("Sauvegarde impossible tant que des erreurs bloquantes existent." + Environment.NewLine + validation.ToDisplayText());
        }

        DanteValidationResult guard = ValidateXmlChangeGuard();
        if (guard.HasErrors)
        {
            throw new InvalidOperationException("Sauvegarde refusée : une modification interdite du XML Dante a été détectée." + Environment.NewLine + guard.ToDisplayText());
        }

        string fullDestinationPath = Path.GetFullPath(destinationPath);
        string destinationDirectory = Path.GetDirectoryName(fullDestinationPath)
            ?? throw new InvalidOperationException("Le dossier de destination est introuvable.");
        if (!Directory.Exists(destinationDirectory))
        {
            throw new DirectoryNotFoundException($"Le dossier de destination n'existe pas : {destinationDirectory}");
        }

        string temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.tmp");
        string backupPath = string.Empty;

        try
        {
            // On sauvegarde d'abord dans un fichier temporaire, puis on le relit.
            // Cela évite de remplacer le fichier final par un XML illisible.
            Document.Save(temporaryPath, SaveOptions.DisableFormatting);
            saveStageObserver?.Invoke("AfterTemporaryFileCreated");
            XDocument temporaryDocument = XDocument.Load(temporaryPath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            DanteValidationResult compatibility = DanteXmlCompatibilityService.ValidateCompatibility(temporaryDocument, _originalCompatibilityProfile);
            if (compatibility.HasErrors)
            {
                throw new InvalidOperationException("Sauvegarde refusée : le XML temporaire casse la compatibilité Dante Controller." + Environment.NewLine + compatibility.ToDisplayText());
            }

            DanteValidationResult temporaryValidation = Load(temporaryPath).Validate();
            if (temporaryValidation.HasErrors)
            {
                throw new InvalidOperationException("Sauvegarde refusée : le XML temporaire contient des erreurs bloquantes." + Environment.NewLine + temporaryValidation.ToDisplayText());
            }

            DanteValidationResult temporaryGuard = DanteXmlChangeGuardService.ValidateChanges(_originalDocument, temporaryDocument);
            if (temporaryGuard.HasErrors)
            {
                throw new InvalidOperationException("Sauvegarde refusée : une modification interdite du XML Dante a été détectée dans le fichier temporaire." + Environment.NewLine + temporaryGuard.ToDisplayText());
            }

            string previousFilePath = OriginalFilePath;
            backupPath = SafeFileService.CreateOriginalBackup(previousFilePath);
            saveStageObserver?.Invoke("BeforeDestinationCommit");

            if (File.Exists(fullDestinationPath))
            {
                string destinationBackupPath = SafeFileService.BuildDestinationBackupPath(fullDestinationPath);
                File.Replace(temporaryPath, fullDestinationPath, destinationBackupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, fullDestinationPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        OriginalFilePath = fullDestinationPath;
        LastSavedPath = fullDestinationPath;
        _originalDocument = new XDocument(Document);
        _originalCompatibilityProfile = DanteXmlCompatibilityService.CaptureProfile(_originalDocument);
        RegisterChange("Sauvegarde", $"Fichier sauvegardé sous {fullDestinationPath}");
        IsModified = false;
        _undoSnapshots.Clear();
        return backupPath;
    }
}
