namespace DanteConfigEditor.Services;

public static class LocalizationService
{
    private static readonly Dictionary<string, string> French = new(StringComparer.Ordinal)
    {
        ["Language.French"] = "Français",
        ["Language.English"] = "Anglais",
        ["Language.Label"] = "Langue",

        ["Search.Hint"] = "Tapez au moins 2 caractères pour chercher",
        ["Search.NoFileLoaded"] = "Chargez un XML pour chercher",
        ["Search.NoResult"] = "Aucun résultat",

        ["Filter.AllSenders"] = "Tous les émetteurs",
        ["Filter.AllReceivers"] = "Tous les récepteurs",
        ["Filter.AllRx"] = "Tous les RX",
        ["Filter.ActivePatches"] = "Patchs actifs",
        ["Filter.FreeRx"] = "RX libres",
        ["Filter.LocalPatches"] = "Patchs locaux",
        ["Filter.MissingTxDevices"] = "Devices TX absents",
        ["Filter.MissingTxChannels"] = "Canaux TX introuvables",
        ["Filter.Warnings"] = "Warnings",
        ["Filter.HealthWarnings"] = "Avertissements",
        ["Filter.Conflicts"] = "Conflits",
        ["Filter.Modified"] = "Modifiés",
        ["Filter.All"] = "Tous",
        ["Filter.Info"] = "Infos",
        ["Filter.Errors"] = "Erreurs",
        ["Filter.Patches"] = "Patchs",
        ["Filter.Devices"] = "Devices",
        ["Filter.Clock"] = "Clock",
        ["Filter.Network"] = "Réseau",
        ["Filter.XmlCompatibility"] = "Compatibilité XML",
        ["Filter.WarningsConflicts"] = "Warnings / conflits",

        ["PatchView.Simple"] = "Simple",
        ["PatchView.Expert"] = "Expert",

        ["Status.Ready"] = "Prêt",
        ["Status.FileLoaded"] = "Fichier chargé. Les modifications seront enregistrées sous un nouveau nom.",
        ["Status.EditEnabled"] = "Mode édition activé.",
        ["Status.FileSaved"] = "Fichier sauvegardé.",
        ["Status.LastActionUndone"] = "Dernière action annulée.",
        ["Status.TopologyDisplayed"] = "Topologie simple affichée.",
        ["Status.TxtExported"] = "Rapport TXT exporté.",
        ["Status.PdfExported"] = "Rapport PDF exporté.",
        ["Status.PatchbookTxtExported"] = "Patchbook TXT exporté.",
        ["Status.PatchbookCsvExported"] = "Patchbook CSV exporté.",
        ["Status.EditMode"] = "Mode : Édition",
        ["Status.ReadOnlyMode"] = "Mode : Lecture seule",
        ["Status.NoFileLoaded"] = "Aucun fichier chargé.",
        ["Status.NoFileOpen"] = "Aucun fichier ouvert",
        ["Status.Unmodified"] = "Non modifié",
        ["Status.ModifiedUnsaved"] = "Modifié - non sauvegardé",
        ["Status.EditActiveButton"] = "Édition active",
        ["Status.ActivateEditButton"] = "Activer l'édition",
        ["Status.LoadXmlToStart"] = "Chargez un fichier XML pour commencer.",

        ["Action.DeviceRenamed"] = "Nom mis à jour.",
        ["Action.NetworkModeUpdated"] = "Mode réseau mis à jour.",
        ["Action.LatencyUpdated"] = "Latence mise à jour.",
        ["Action.SampleRateUpdated"] = "Sample rate mise à jour.",
        ["Action.EncodingUpdated"] = "Bits par échantillon mis à jour.",
        ["Action.IpAutoApplied"] = "IP automatique appliquée.",
        ["Action.IpStaticApplied"] = "IP fixe appliquée.",
        ["Action.DevicePatchesReset"] = "Patchs RX/TX de la machine réinitialisés.",
        ["Action.DeviceDetailsUpdated"] = "Détail machine mis à jour.",
        ["Action.PreferredMasterUpdated"] = "Preferred master mis à jour.",
        ["Action.ChannelsReset"] = "Canaux réinitialisés.",
        ["Action.ChannelRenamed"] = "Canal renommé.",
        ["Action.BatchRenameApplied"] = "Renommage en série appliqué.",
        ["Action.AllNetworkModesApplied"] = "Mode réseau appliqué à tous les devices.",
        ["Action.AllLatenciesApplied"] = "Latence appliquée à tous les devices.",
        ["Action.AllSampleRatesApplied"] = "Sample rate appliquée à tous les devices.",
        ["Action.AllEncodingsApplied"] = "Bits par échantillon appliqués à tous les devices.",
        ["Action.AllIpAutoApplied"] = "IP automatique appliquée à tous les devices.",
        ["Action.AllIpStaticApplied"] = "IP fixes appliquées en série.",
        ["Action.AllChannelsReset"] = "Tous les canaux ont été réinitialisés.",
        ["Action.PatchApplied"] = "Patch appliqué.",
        ["Action.PatchRemoved"] = "Patch supprimé.",
        ["Action.TxChannelRenamed"] = "Canal TX renommé et patchs mis à jour.",
        ["Action.RxChannelRenamed"] = "Canal RX renommé.",
        ["Action.DeviceDeleted"] = "Machine supprimée.",
        ["Action.XmlMerged"] = "XML ajouté au projet.",

        ["Dialog.ConfirmTitle"] = "Confirmation requise",
        ["Dialog.OpenXmlTitle"] = "Ouvrir une configuration Dante",
        ["Dialog.MergeXmlTitle"] = "Ajouter un XML au projet ouvert",
        ["Dialog.SaveXmlTitle"] = "Enregistrer une nouvelle configuration",
        ["Dialog.ExportTxtTitle"] = "Exporter le rapport TXT",
        ["Dialog.ExportPdfTitle"] = "Exporter le rapport PDF",
        ["Dialog.ExportPatchbookTxtTitle"] = "Exporter le patchbook TXT",
        ["Dialog.ExportPatchbookCsvTitle"] = "Exporter le patchbook CSV",
        ["Dialog.XmlFilter"] = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*",
        ["Dialog.TxtFilter"] = "Rapport texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
        ["Dialog.PdfFilter"] = "Rapport PDF (*.pdf)|*.pdf|Tous les fichiers (*.*)|*.*",
        ["Dialog.PatchbookTxtFilter"] = "Patchbook texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
        ["Dialog.PatchbookCsvFilter"] = "Patchbook CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
        ["Dialog.NoRecentFileTitle"] = "Aucun fichier récent",
        ["Dialog.NoRecentFileMessage"] = "Sélectionnez un fichier récent à ouvrir.",
        ["Dialog.FileMissingTitle"] = "Fichier introuvable",
        ["Dialog.FileMissingMessage"] = "Ce fichier récent n'existe plus.",
        ["Dialog.OpenFailedTitle"] = "Le fichier ne peut pas être ouvert.",
        ["Dialog.SaveImpossibleTitle"] = "Sauvegarde impossible",
        ["Dialog.ChooseAnotherNameTitle"] = "Choisissez un autre nom",
        ["Dialog.ChooseAnotherNameMessage"] = "Pour protéger le XML d'origine, l'application n'enregistre pas par-dessus le fichier ouvert. Choisissez un nouveau nom de fichier.",
        ["Dialog.OverwriteMessage"] = "Ce fichier existe déjà. Voulez-vous vraiment l'écraser ?",
        ["Dialog.SaveSummaryTitle"] = "Résumé avant sauvegarde",
        ["Dialog.OriginalBackupMessage"] = "Une sauvegarde du fichier original sera créée avant l'écriture. Continuer ?",
        ["Dialog.SaveErrorTitle"] = "Erreur pendant la sauvegarde",
        ["Dialog.RevertTitle"] = "Annuler les changements",
        ["Dialog.RevertMessage"] = "Les changements non sauvegardés seront perdus. Continuer ?",
        ["Dialog.ReloadErrorTitle"] = "Impossible de recharger le fichier original",
        ["Dialog.UndoErrorTitle"] = "Annulation impossible",
        ["Dialog.NoChannelTitle"] = "Aucun canal sélectionné",
        ["Dialog.NoChannelMessage"] = "Sélectionnez un canal TX ou RX à renommer.",
        ["Dialog.InvalidRangeTitle"] = "Plage invalide",
        ["Dialog.InvalidRangeMessage"] = "Sélectionnez un canal de début et un canal de fin.",
        ["Dialog.InvalidRangeOrderMessage"] = "Le canal de fin doit être placé après le canal de début.",
        ["Dialog.InvalidNumberTitle"] = "Numéro invalide",
        ["Dialog.InvalidNumberMessage"] = "Indiquez un numéro de départ valide.",
        ["Dialog.NoRxTitle"] = "Aucun canal RX sélectionné",
        ["Dialog.NoRxMessage"] = "Sélectionnez une ligne dans la table de patch.",
        ["Dialog.NoRxLineMessage"] = "Sélectionnez une ligne RX dans la table de patch.",
        ["Dialog.MissingTxTitle"] = "Canal TX manquant",
        ["Dialog.MissingTxMessage"] = "Sélectionnez un device TX et un canal TX dans la zone de patch.",
        ["Dialog.ExportImpossibleTitle"] = "Export impossible",
        ["Dialog.ExportPatchbookImpossibleTitle"] = "Export Patchbook impossible",
        ["Dialog.ExportPatchbookCsvImpossibleTitle"] = "Export Patchbook CSV impossible",
        ["Dialog.ActionImpossibleTitle"] = "Action impossible",
        ["Dialog.NoFileLoadedTitle"] = "Aucun fichier chargé",
        ["Dialog.NoFileLoadedMessage"] = "Ouvrez d'abord un fichier XML de configuration Dante.",
        ["Dialog.DeleteDeviceWarning"] = "La machine '{0}' sera supprimée du projet. Les subscriptions/patchs qui pointent vers cette machine seront aussi supprimés. Continuer ?",
        ["Dialog.ResetDevicePatchesWarning"] = "Les entrées RX de la machine '{0}' seront déconnectées, et tous les patchs qui utilisent ses TX seront supprimés. Continuer ?",
        ["Dialog.MergeXmlWarning"] = "Les machines du fichier XML sélectionné seront ajoutées au projet courant. Les noms de machines déjà présents seront refusés. Continuer ?",
        ["DuplicateDialog.Title"] = "Doublons de machines",
        ["DuplicateDialog.Intro"] = "Certaines machines du XML importé existent déjà dans le projet ouvert. Vous pouvez importer seulement les machines sans doublon, renommer automatiquement les doublons, ou choisir vous-même les nouveaux noms.",
        ["DuplicateDialog.OriginalName"] = "Nom dans le XML importé",
        ["DuplicateDialog.NewName"] = "Nouveau nom à importer",
        ["DuplicateDialog.UniqueOnly"] = "Importer uniques seulement",
        ["DuplicateDialog.AutoRename"] = "Renommage auto",
        ["DuplicateDialog.ManualRename"] = "Importer avec ces noms",
        ["DuplicateDialog.Cancel"] = "Annuler",
        ["DuplicateDialog.InvalidTitle"] = "Noms invalides",
        ["DuplicateDialog.EmptyName"] = "Chaque machine renommée doit avoir un nouveau nom.",
        ["DuplicateDialog.DuplicateNewName"] = "Deux machines importées ne peuvent pas recevoir le même nouveau nom.",
        ["Dialog.LatencyWarning"] = "Modifier la latence Dante peut provoquer une reconfiguration des flux lors de l'import/application dans les outils Dante. Vérifiez toujours le preset dans Dante Controller.",
        ["Dialog.LatencyWarningContinue"] = "Modifier la latence Dante peut provoquer une reconfiguration des flux lors de l'import/application dans les outils Dante. Continuer ?",
        ["Dialog.AudioFormatWarning"] = "Modifier la sample rate ou les bits par échantillon peut rendre certaines machines incompatibles si elles ne supportent pas cette valeur. Vérifiez toujours le preset dans Dante Controller.",
        ["Dialog.AudioFormatWarningContinue"] = "Modifier la sample rate ou les bits par échantillon peut rendre certaines machines incompatibles si elles ne supportent pas cette valeur. Continuer ?",
        ["Dialog.IpStaticWarning"] = "Modifier une IP en fixe peut couper la communication si l'adresse, le masque ou la passerelle sont mauvais. Vérifiez toujours le preset dans Dante Controller.",
        ["Dialog.IpStaticWarningContinue"] = "Modifier les IP en fixe peut couper la communication si la plage, le masque ou la passerelle sont mauvais. Continuer ?",
        ["Dialog.DeviceDetailsWarning"] = "Les changements de cette fiche peuvent modifier le nom de la machine, ses formats, son IP et ses canaux. Continuer ?",
        ["Dialog.ResetDeviceChannelsWarning"] = "Les noms des canaux du device sélectionné seront remplacés par 1, 2, 3...",
        ["Dialog.BatchRenameWarning"] = "Les noms des canaux {0} {1} à {2} seront remplacés en série. Continuer ?",
        ["Dialog.Continue"] = "Continuer ?",
        ["Dialog.RemovePatchWarning"] = "Le patch du canal RX sélectionné sera supprimé.",
        ["Dialog.ExternalPatchWarning"] = "Ce patch pointe vers un device qui n'est pas présent dans le preset. Cela peut être normal si le preset Dante est partiel. Ne le modifiez que si vous êtes certain de vouloir remplacer cette source. Continuer ?",
        ["Dialog.ExternalPatchStatus"] = "Ce patch pointe vers un device absent du preset. Cela peut être normal si le preset Dante est partiel.",

        ["Log.FileLoaded"] = "Fichier chargé : {0}",
        ["Log.EditEnabled"] = "Mode édition activé.",
        ["Log.EditEnabledAuto"] = "Mode édition activé automatiquement.",
        ["Log.OriginalBackupCreated"] = "Sauvegarde originale créée : {0}",
        ["Log.FileSaved"] = "Fichier enregistré : {0}",
        ["Log.ReloadOriginal"] = "Changements annulés. Rechargement du fichier original.",
        ["Log.ActionUndone"] = "Action annulée : {0}",
        ["Log.TxtExported"] = "Rapport TXT exporté : {0}",
        ["Log.PdfExported"] = "Rapport PDF exporté : {0}",
        ["Log.PatchbookTxtExported"] = "Patchbook TXT exporté : {0}",
        ["Log.PatchbookCsvExported"] = "Patchbook CSV exporté : {0}",
        ["Log.XmlMerged"] = "XML ajouté au projet : {0}",

        ["Summary.PatchRows"] = "{0} lignes - {1} actifs - {2} locaux - {3} warning(s) - {4} conflit(s)",
        ["Summary.Health"] = "Preset : {0}  |  Version : {1}  |  Mode : {2}  |  Fichier : {3}\nDevices : {4}  |  TX : {5}  |  RX : {6}  |  Patchs actifs : {7}  |  RX libres : {8}\nPatchs locaux : {9}  |  Devices TX absents : {10}  |  Canaux TX introuvables : {11}  |  Preferred masters : {12}\nSamplerates : {13}  |  Encodages : {14}  |  Latences : {15}\nRedondants : {16}  |  Daisychain : {17}  |  IP fixes détectées : {18}  |  Erreurs : {19}  |  Warnings : {20}",

        ["Blank"] = "(vide)"
    };

    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["Language.French"] = "French",
        ["Language.English"] = "English",
        ["Language.Label"] = "Language",

        ["Search.Hint"] = "Type at least 2 characters to search",
        ["Search.NoFileLoaded"] = "Load an XML file to search",
        ["Search.NoResult"] = "No result",

        ["Filter.AllSenders"] = "All transmitters",
        ["Filter.AllReceivers"] = "All receivers",
        ["Filter.AllRx"] = "All Rx",
        ["Filter.ActivePatches"] = "Active subscriptions",
        ["Filter.FreeRx"] = "Free Rx",
        ["Filter.LocalPatches"] = "Local subscriptions",
        ["Filter.MissingTxDevices"] = "Missing Tx devices",
        ["Filter.MissingTxChannels"] = "Missing Tx channels",
        ["Filter.Warnings"] = "Warnings",
        ["Filter.HealthWarnings"] = "Warnings",
        ["Filter.Conflicts"] = "Conflicts",
        ["Filter.Modified"] = "Modified",
        ["Filter.All"] = "All",
        ["Filter.Info"] = "Info",
        ["Filter.Errors"] = "Errors",
        ["Filter.Patches"] = "Subscriptions",
        ["Filter.Devices"] = "Devices",
        ["Filter.Clock"] = "Clock",
        ["Filter.Network"] = "Network",
        ["Filter.XmlCompatibility"] = "XML compatibility",
        ["Filter.WarningsConflicts"] = "Warnings / conflicts",

        ["PatchView.Simple"] = "Simple",
        ["PatchView.Expert"] = "Expert",

        ["Status.Ready"] = "Ready",
        ["Status.FileLoaded"] = "File loaded. Changes must be saved under a new name.",
        ["Status.EditEnabled"] = "Edit mode enabled.",
        ["Status.FileSaved"] = "File saved.",
        ["Status.LastActionUndone"] = "Last action undone.",
        ["Status.TopologyDisplayed"] = "Simple topology displayed.",
        ["Status.TxtExported"] = "TXT report exported.",
        ["Status.PdfExported"] = "PDF report exported.",
        ["Status.PatchbookTxtExported"] = "Patchbook TXT exported.",
        ["Status.PatchbookCsvExported"] = "Patchbook CSV exported.",
        ["Status.EditMode"] = "Mode: Edit",
        ["Status.ReadOnlyMode"] = "Mode: Read-only",
        ["Status.NoFileLoaded"] = "No file loaded.",
        ["Status.NoFileOpen"] = "No file open",
        ["Status.Unmodified"] = "Unmodified",
        ["Status.ModifiedUnsaved"] = "Modified - not saved",
        ["Status.EditActiveButton"] = "Edit active",
        ["Status.ActivateEditButton"] = "Enable editing",
        ["Status.LoadXmlToStart"] = "Load an XML file to begin.",

        ["Action.DeviceRenamed"] = "Device name updated.",
        ["Action.NetworkModeUpdated"] = "Network mode updated.",
        ["Action.LatencyUpdated"] = "Latency updated.",
        ["Action.SampleRateUpdated"] = "Sample rate updated.",
        ["Action.EncodingUpdated"] = "Bits per sample updated.",
        ["Action.IpAutoApplied"] = "Automatic IP applied.",
        ["Action.IpStaticApplied"] = "Static IP applied.",
        ["Action.DevicePatchesReset"] = "Device Rx/Tx subscriptions reset.",
        ["Action.DeviceDetailsUpdated"] = "Device details updated.",
        ["Action.PreferredMasterUpdated"] = "Preferred Master updated.",
        ["Action.ChannelsReset"] = "Channels reset.",
        ["Action.ChannelRenamed"] = "Channel renamed.",
        ["Action.BatchRenameApplied"] = "Batch rename applied.",
        ["Action.AllNetworkModesApplied"] = "Network mode applied to all devices.",
        ["Action.AllLatenciesApplied"] = "Latency applied to all devices.",
        ["Action.AllSampleRatesApplied"] = "Sample rate applied to all devices.",
        ["Action.AllEncodingsApplied"] = "Bits per sample applied to all devices.",
        ["Action.AllIpAutoApplied"] = "Automatic IP applied to all devices.",
        ["Action.AllIpStaticApplied"] = "Static IP range applied.",
        ["Action.AllChannelsReset"] = "All channels have been reset.",
        ["Action.PatchApplied"] = "Subscription applied.",
        ["Action.PatchRemoved"] = "Subscription removed.",
        ["Action.TxChannelRenamed"] = "Tx channel renamed and subscriptions updated.",
        ["Action.RxChannelRenamed"] = "Rx channel renamed.",
        ["Action.DeviceDeleted"] = "Device deleted.",
        ["Action.XmlMerged"] = "XML added to project.",

        ["Dialog.ConfirmTitle"] = "Confirmation required",
        ["Dialog.OpenXmlTitle"] = "Open a Dante configuration",
        ["Dialog.MergeXmlTitle"] = "Add XML to the open project",
        ["Dialog.SaveXmlTitle"] = "Save a new configuration",
        ["Dialog.ExportTxtTitle"] = "Export TXT report",
        ["Dialog.ExportPdfTitle"] = "Export PDF report",
        ["Dialog.ExportPatchbookTxtTitle"] = "Export patchbook TXT",
        ["Dialog.ExportPatchbookCsvTitle"] = "Export patchbook CSV",
        ["Dialog.XmlFilter"] = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
        ["Dialog.TxtFilter"] = "Text report (*.txt)|*.txt|All files (*.*)|*.*",
        ["Dialog.PdfFilter"] = "PDF report (*.pdf)|*.pdf|All files (*.*)|*.*",
        ["Dialog.PatchbookTxtFilter"] = "Patchbook text (*.txt)|*.txt|All files (*.*)|*.*",
        ["Dialog.PatchbookCsvFilter"] = "Patchbook CSV (*.csv)|*.csv|All files (*.*)|*.*",
        ["Dialog.NoRecentFileTitle"] = "No recent file",
        ["Dialog.NoRecentFileMessage"] = "Select a recent file to open.",
        ["Dialog.FileMissingTitle"] = "File not found",
        ["Dialog.FileMissingMessage"] = "This recent file no longer exists.",
        ["Dialog.OpenFailedTitle"] = "The file cannot be opened.",
        ["Dialog.SaveImpossibleTitle"] = "Save unavailable",
        ["Dialog.ChooseAnotherNameTitle"] = "Choose another name",
        ["Dialog.ChooseAnotherNameMessage"] = "To protect the original XML, the application does not save over the opened file. Choose a new file name.",
        ["Dialog.OverwriteMessage"] = "This file already exists. Do you really want to overwrite it?",
        ["Dialog.SaveSummaryTitle"] = "Summary before saving",
        ["Dialog.OriginalBackupMessage"] = "A backup of the original file will be created before writing. Continue?",
        ["Dialog.SaveErrorTitle"] = "Error while saving",
        ["Dialog.RevertTitle"] = "Revert changes",
        ["Dialog.RevertMessage"] = "Unsaved changes will be lost. Continue?",
        ["Dialog.ReloadErrorTitle"] = "Unable to reload the original file",
        ["Dialog.UndoErrorTitle"] = "Undo unavailable",
        ["Dialog.NoChannelTitle"] = "No channel selected",
        ["Dialog.NoChannelMessage"] = "Select a Tx or Rx channel to rename.",
        ["Dialog.InvalidRangeTitle"] = "Invalid range",
        ["Dialog.InvalidRangeMessage"] = "Select a start channel and an end channel.",
        ["Dialog.InvalidRangeOrderMessage"] = "The end channel must be after the start channel.",
        ["Dialog.InvalidNumberTitle"] = "Invalid number",
        ["Dialog.InvalidNumberMessage"] = "Enter a valid starting number.",
        ["Dialog.NoRxTitle"] = "No Rx channel selected",
        ["Dialog.NoRxMessage"] = "Select a row in the patch table.",
        ["Dialog.NoRxLineMessage"] = "Select an Rx row in the patch table.",
        ["Dialog.MissingTxTitle"] = "Missing Tx channel",
        ["Dialog.MissingTxMessage"] = "Select a Tx device and a Tx channel in the patch area.",
        ["Dialog.ExportImpossibleTitle"] = "Export unavailable",
        ["Dialog.ExportPatchbookImpossibleTitle"] = "Patchbook export unavailable",
        ["Dialog.ExportPatchbookCsvImpossibleTitle"] = "Patchbook CSV export unavailable",
        ["Dialog.ActionImpossibleTitle"] = "Action unavailable",
        ["Dialog.NoFileLoadedTitle"] = "No file loaded",
        ["Dialog.NoFileLoadedMessage"] = "Open a Dante configuration XML file first.",
        ["Dialog.DeleteDeviceWarning"] = "Device '{0}' will be deleted from the project. Subscriptions/patches pointing to this device will also be removed. Continue?",
        ["Dialog.ResetDevicePatchesWarning"] = "The Rx inputs of device '{0}' will be disconnected, and all subscriptions using its Tx channels will be removed. Continue?",
        ["Dialog.MergeXmlWarning"] = "Devices from the selected XML file will be added to the current project. Device names that already exist will be rejected. Continue?",
        ["DuplicateDialog.Title"] = "Duplicate devices",
        ["DuplicateDialog.Intro"] = "Some devices from the imported XML already exist in the open project. You can import only non-duplicate devices, automatically rename duplicates, or choose the new names manually.",
        ["DuplicateDialog.OriginalName"] = "Name in imported XML",
        ["DuplicateDialog.NewName"] = "New name to import",
        ["DuplicateDialog.UniqueOnly"] = "Import unique only",
        ["DuplicateDialog.AutoRename"] = "Auto rename",
        ["DuplicateDialog.ManualRename"] = "Import with these names",
        ["DuplicateDialog.Cancel"] = "Cancel",
        ["DuplicateDialog.InvalidTitle"] = "Invalid names",
        ["DuplicateDialog.EmptyName"] = "Each renamed device must have a new name.",
        ["DuplicateDialog.DuplicateNewName"] = "Two imported devices cannot receive the same new name.",
        ["Dialog.LatencyWarning"] = "Changing Dante latency may reconfigure flows when the preset is imported/applied in Dante tools. Always verify the preset in Dante Controller.",
        ["Dialog.LatencyWarningContinue"] = "Changing Dante latency may reconfigure flows when the preset is imported/applied in Dante tools. Continue?",
        ["Dialog.AudioFormatWarning"] = "Changing sample rate or bits per sample may make some devices incompatible if they do not support that value. Always verify the preset in Dante Controller.",
        ["Dialog.AudioFormatWarningContinue"] = "Changing sample rate or bits per sample may make some devices incompatible if they do not support that value. Continue?",
        ["Dialog.IpStaticWarning"] = "Setting a static IP can break communication if the address, netmask, or gateway is wrong. Always verify the preset in Dante Controller.",
        ["Dialog.IpStaticWarningContinue"] = "Setting static IPs can break communication if the range, netmask, or gateway is wrong. Continue?",
        ["Dialog.DeviceDetailsWarning"] = "This device sheet can change the device name, formats, IP address, and channels. Continue?",
        ["Dialog.ResetDeviceChannelsWarning"] = "The selected device channel names will be replaced by 1, 2, 3...",
        ["Dialog.BatchRenameWarning"] = "Channel names {0} {1} to {2} will be replaced in a batch rename. Continue?",
        ["Dialog.Continue"] = "Continue?",
        ["Dialog.RemovePatchWarning"] = "The selected Rx channel subscription will be removed.",
        ["Dialog.ExternalPatchWarning"] = "This subscription points to a device that is not present in the preset. This may be normal if the Dante preset is partial. Only modify it if you are sure you want to replace this source. Continue?",
        ["Dialog.ExternalPatchStatus"] = "This subscription points to a device that is missing from the preset. This may be normal if the Dante preset is partial.",

        ["Log.FileLoaded"] = "File loaded: {0}",
        ["Log.EditEnabled"] = "Edit mode enabled.",
        ["Log.EditEnabledAuto"] = "Edit mode enabled automatically.",
        ["Log.OriginalBackupCreated"] = "Original backup created: {0}",
        ["Log.FileSaved"] = "File saved: {0}",
        ["Log.ReloadOriginal"] = "Changes reverted. Original file reloaded.",
        ["Log.ActionUndone"] = "Action undone: {0}",
        ["Log.TxtExported"] = "TXT report exported: {0}",
        ["Log.PdfExported"] = "PDF report exported: {0}",
        ["Log.PatchbookTxtExported"] = "Patchbook TXT exported: {0}",
        ["Log.PatchbookCsvExported"] = "Patchbook CSV exported: {0}",
        ["Log.XmlMerged"] = "XML added to project: {0}",

        ["Summary.PatchRows"] = "{0} rows - {1} active - {2} local - {3} warning(s) - {4} conflict(s)",
        ["Summary.Health"] = "Preset: {0}  |  Version: {1}  |  Mode: {2}  |  File: {3}\nDevices: {4}  |  TX: {5}  |  RX: {6}  |  Active subscriptions: {7}  |  Free RX: {8}\nLocal subscriptions: {9}  |  Missing TX devices: {10}  |  Missing TX channels: {11}  |  Preferred Masters: {12}\nSample rates: {13}  |  Encoding: {14}  |  Latencies: {15}\nRedundant: {16}  |  Daisy-chain: {17}  |  Static IPs detected: {18}  |  Errors: {19}  |  Warnings: {20}",

        ["Blank"] = "(empty)"
    };

    private static readonly Dictionary<string, string> LiteralFrenchToEnglish = BuildLiteralMap();
    private static readonly Dictionary<string, string> LiteralEnglishToFrench = BuildInverseLiteralMap();

    public static string Text(UiLanguage language, string key)
    {
        Dictionary<string, string> dictionary = language == UiLanguage.English ? English : French;
        return dictionary.TryGetValue(key, out string? value) ? value : key;
    }

    public static string Format(UiLanguage language, string key, params object[] args)
    {
        return string.Format(Text(language, key), args);
    }

    public static string TranslateLiteral(UiLanguage language, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (language == UiLanguage.English && LiteralFrenchToEnglish.TryGetValue(value, out string? english))
        {
            return english;
        }

        if (language == UiLanguage.French && LiteralEnglishToFrench.TryGetValue(value, out string? french))
        {
            return french;
        }

        return value;
    }

    private static Dictionary<string, string> BuildLiteralMap()
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> pair in English)
        {
            if (French.TryGetValue(pair.Key, out string? frenchText) && !string.Equals(frenchText, pair.Value, StringComparison.Ordinal))
            {
                map[frenchText] = pair.Value;
            }
        }

        Add(map, "Ouvrir XML", "Open XML");
        Add(map, "Ajouter XML au projet", "Add XML to project");
        Add(map, "Enregistrer sous", "Save as");
        Add(map, "Annuler action", "Undo action");
        Add(map, "Annuler les changements", "Revert changes");
        Add(map, "Ouvrir récent", "Open recent");
        Add(map, "Thème clair", "Light theme");
        Add(map, "Thème sombre", "Dark theme");
        Add(map, "Projet", "Project");
        Add(map, "Recherche", "Search");
        Add(map, "Mode hors ligne : l'application modifie uniquement les fichiers XML chargés. Elle ne se connecte pas au réseau Dante.", "Offline mode: the application only modifies loaded XML files. It does not connect to the Dante network.");
        Add(map, "Configuration", "Configuration");
        Add(map, "POINTS À VÉRIFIER", "ITEMS TO CHECK");
        Add(map, "Machine sélectionnée", "Selected device");
        Add(map, "Machine", "Device");
        Add(map, "Nouveau nom", "New name");
        Add(map, "Changer le nom", "Change name");
        Add(map, "Supprimer la machine", "Delete device");
        Add(map, "Mode réseau", "Network mode");
        Add(map, "Redondant", "Redundant");
        Add(map, "Changer le mode", "Change mode");
        Add(map, "Latence unicast", "Unicast latency");
        Add(map, "Changer la latence", "Change latency");
        Add(map, "Sample rate", "Sample rate");
        Add(map, "Changer la fréquence", "Change sample rate");
        Add(map, "Bits par échantillon", "Bits per sample");
        Add(map, "Changer les bits", "Change bits");
        Add(map, "Adresse IP fixe", "Static IP address");
        Add(map, "Adresse IP", "IP address");
        Add(map, "Masque", "Netmask");
        Add(map, "Passerelle", "Gateway");
        Add(map, "Fixer l'IP", "Set static IP");
        Add(map, "Mettre l'IP en automatique", "Set IP to automatic");
        Add(map, "Reset patch RX/TX machine", "Reset device Rx/Tx subscriptions");
        Add(map, "Horloge", "Clock");
        Add(map, "Changer preferred master", "Change Preferred Master");
        Add(map, "Canaux de la machine", "Device channels");
        Add(map, "Canal à renommer", "Channel to rename");
        Add(map, "Nouveau nom de canal", "New channel name");
        Add(map, "Renommer le canal", "Rename channel");
        Add(map, "Réinitialiser les canaux de la machine", "Reset device channels");
        Add(map, "Renommage en série", "Batch rename");
        Add(map, "Canal début", "Start channel");
        Add(map, "Canal fin", "End channel");
        Add(map, "Préfixe", "Prefix");
        Add(map, "Numéro", "Number");
        Add(map, "Renommer la série", "Rename range");
        Add(map, "Actions globales", "Global actions");
        Add(map, "Réseau / audio", "Network / audio");
        Add(map, "Appliquer le mode à tous", "Apply mode to all");
        Add(map, "Latence globale", "Global latency");
        Add(map, "Appliquer la latence à tous", "Apply latency to all");
        Add(map, "Sample rate globale", "Global sample rate");
        Add(map, "Appliquer la fréquence à tous", "Apply sample rate to all");
        Add(map, "Bits par échantillon globaux", "Global bits per sample");
        Add(map, "Appliquer les bits à tous", "Apply bits to all");
        Add(map, "Préfixe IP", "IP prefix");
        Add(map, "Premier numéro", "First number");
        Add(map, "Fixer les IP en série", "Set static IP range");
        Add(map, "Mettre toutes les IP en automatique", "Set all IPs to automatic");
        Add(map, "Réinitialiser tous les canaux", "Reset all channels");
        Add(map, "Listes rapides", "Quick lists");
        Add(map, "Redondants", "Redundant");
        Add(map, "Latences", "Latencies");
        Add(map, "Sample rates", "Sample rates");
        Add(map, "Bits", "Bits");
        Add(map, "IP fixes", "Static IPs");
        Add(map, "Détail machine", "Device details");
        Add(map, "Identité et formats", "Identity and formats");
        Add(map, "Nom machine", "Device name");
        Add(map, "Adresse IP", "IP address");
        Add(map, "Mode IP", "IP mode");
        Add(map, "Automatique", "Automatic");
        Add(map, "Fixe", "Static");
        Add(map, "Canaux", "Channels");
        Add(map, "Nom", "Name");
        Add(map, "Annuler", "Cancel");
        Add(map, "Appliquer", "Apply");
        Add(map, "Les changements seront appliqués au XML après validation.", "Changes will be applied to the XML after confirmation.");
        Add(map, "Friendly name", "Friendly name");
        Add(map, "Latence", "Latency");
        Add(map, "IP", "IP");
        Add(map, "Preferred", "Preferred");
        Add(map, "Patch", "Patch");
        Add(map, "Filtre émetteur TX", "Tx transmitter filter");
        Add(map, "Filtre récepteur RX", "Rx receiver filter");
        Add(map, "Recherche device ou canal", "Search device or channel");
        Add(map, "Filtre état", "State filter");
        Add(map, "Source TX à appliquer", "Tx source to apply");
        Add(map, "Canal TX à appliquer", "Tx channel to apply");
        Add(map, "Affichage", "View");
        Add(map, "Appliquer", "Apply");
        Add(map, "Supprimer", "Remove");
        Add(map, "RX device", "Rx device");
        Add(map, "RX Dante Id", "Rx Dante ID");
        Add(map, "RX canal", "Rx channel");
        Add(map, "Source complète", "Full source");
        Add(map, "TX affiché", "Displayed Tx");
        Add(map, "TX brut XML", "Raw XML Tx");
        Add(map, "TX résolu", "Resolved Tx");
        Add(map, "TX canal", "Tx channel");
        Add(map, "Actif", "Active");
        Add(map, "État", "Status");
        Add(map, "Afficher seulement les conflits", "Show conflicts only");
        Add(map, "Renommer le RX sélectionné", "Rename selected Rx");
        Add(map, "Renommer le TX source", "Rename source Tx");
        Add(map, "Renommer RX", "Rename Rx");
        Add(map, "Renommer TX", "Rename Tx");
        Add(map, "Santé du fichier", "File health");
        Add(map, "Filtre santé", "Health filter");
        Add(map, "Gravité", "Severity");
        Add(map, "Catégorie", "Category");
        Add(map, "Canal", "Channel");
        Add(map, "Message", "Message");
        Add(map, "Sécurité et journal", "Safety and log");
        Add(map, "Vérifier le fichier", "Validate file");
        Add(map, "Rapport compatibilité Dante Controller", "Dante Controller compatibility report");
        Add(map, "Actualiser le résumé", "Refresh summary");
        Add(map, "Exporter TXT", "Export TXT");
        Add(map, "Exporter PDF", "Export PDF");
        Add(map, "Patchbook TXT", "Patchbook TXT");
        Add(map, "Patchbook CSV", "Patchbook CSV");
        Add(map, "Topologie simple", "Simple topology");
        Add(map, "Comparer XML", "Compare XML");
        Add(map, "Journal", "Log");
        Add(map, "Le XML conserve la valeur brute Dante.", "The XML keeps the raw Dante value.");
        Add(map, "La valeur affichée est en ms. Le XML conserve la valeur brute Dante.", "Displayed value is in ms. The XML keeps the raw Dante value.");

        return map;
    }

    private static Dictionary<string, string> BuildInverseLiteralMap()
    {
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in LiteralFrenchToEnglish)
        {
            map.TryAdd(pair.Value, pair.Key);
        }

        return map;
    }

    private static void Add(Dictionary<string, string> map, string french, string english)
    {
        map[french] = english;
    }
}
