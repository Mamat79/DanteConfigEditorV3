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

        ["DeviceFilter.All"] = "Toutes",
        ["DeviceFilter.Locked"] = "Verrouillées",
        ["DeviceFilter.StaticIp"] = "IP fixes",
        ["DeviceFilter.PreferredMaster"] = "Preferred masters",
        ["DeviceFilter.Redundant"] = "Redondantes",
        ["DeviceFilter.Daisychain"] = "Daisychain",
        ["DeviceFilter.NoTx"] = "Sans TX",
        ["DeviceFilter.NoRx"] = "Sans RX",
        ["DeviceFilter.Modified"] = "Modifiées uniquement",
        ["DeviceFilter.WarningSelection"] = "Alerte sélectionnée",
        ["DeviceFilter.SampleRateDifferent"] = "Sample rate différente",
        ["DeviceFilter.EncodingDifferent"] = "Bits différents",
        ["Target.AllUnlocked"] = "Toutes non verrouillées",
        ["Target.SelectedUnlocked"] = "Sélection non verrouillée",
        ["Target.FilteredUnlocked"] = "Filtre affiché non verrouillé",

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
        ["Status.NoDeviceSettingsChanged"] = "Aucun paramètre de la machine à appliquer.",
        ["Status.RecoveryRestored"] = "Session automatique récupérée - modifications non sauvegardées.",
        ["Status.NoImportantWarning"] = "Aucun point important à vérifier.",
        ["Status.WarningDevicesDisplayed"] = "{0} machine(s) concernée(s) affichée(s).",
        ["Status.ProfileAlreadyApplied"] = "Le profil est déjà appliqué à toute la cible.",
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
        ["Action.DeviceRxPatchesReset"] = "Patchs RX de la machine réinitialisés.",
        ["Action.DeviceTxPatchesReset"] = "Patchs TX de la machine réinitialisés.",
        ["Action.DeviceDetailsUpdated"] = "Détail machine mis à jour.",
        ["Action.DeviceSettingsUpdated"] = "Paramètres de la machine mis à jour.",
        ["Action.QuickProfileApplied"] = "Profil rapide appliqué.",
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
        ["Action.VisualPatchesApplied"] = "{0} changement(s) de patch visuel appliqué(s).",
        ["Action.TxChannelRenamed"] = "Canal TX renommé et patchs mis à jour.",
        ["Action.RxChannelRenamed"] = "Canal RX renommé.",
        ["Action.DeviceDeleted"] = "Machine supprimée.",
        ["Action.XmlMerged"] = "XML ajouté au projet.",
        ["Action.AtomicChaosApplied"] = "Exercice atomique généré - non sauvegardé.",

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
        ["Dialog.AtomicChaosTitle"] = "Atomic Bomb - exercice de dépannage",
        ["Dialog.AtomicChaosFirst"] = "Premier verrou : cette fonction désorganise volontairement la copie XML chargée pour créer un exercice de dépannage. Elle mélange les noms, modes réseau, horloges, latences, formats audio, IP principales et patchs. Continuer ?",
        ["Dialog.AtomicChaosSecond"] = "Deuxième verrou : le résultat sera volontairement incohérent, mais seules des valeurs Dante reconnues seront écrites. Les identifiants techniques, DNS, passerelles et interfaces secondaires seront conservés. Préparer cet exercice ?",
        ["Dialog.AtomicChaosThird"] = "DERNIÈRE CONFIRMATION : atomiser la configuration en mémoire ? Le fichier original restera intact et Enregistrer sous sera obligatoire pour conserver l'exercice.",
        ["Dialog.AtomicChaosCompleted"] = "Scénario atomique créé (graine {0}). {1} machine(s), {2} TX, {3} RX patché(s), {4} RX libre(s), {5} IP fixe(s), {6} IP automatique(s). Le XML original n'a pas été modifié. Utilisez Enregistrer sous pour conserver l'exercice.",
        ["Dialog.RecoveryTitle"] = "Récupération de session",
        ["Dialog.RecoveryFound"] = "Une copie automatique non enregistrée datant du {0:g} a été trouvée. Voulez-vous la récupérer ?\n\nNon supprimera cette copie temporaire et ouvrira le XML original.",
        ["Dialog.RecoverySourceChanged"] = "Attention : le fichier XML original a changé depuis cette récupération. Vérifiez attentivement les différences avant de sauvegarder.",
        ["Dialog.NoDeviceChanges"] = "Aucune modification de machine, canal ou patch n'est détectée depuis l'ouverture du XML.",
        ["Dialog.DeviceChangesTitle"] = "Modifications avant / après",
        ["Dialog.SelectProfile"] = "Sélectionnez un profil rapide.",
        ["Dialog.ProfileWarningContinue"] = "Ce profil peut modifier plusieurs paramètres audio et réseau en une seule action. Vérifiez la prévisualisation et contrôlez le XML final dans Dante Controller. Continuer ?",
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
        ["Log.RecoveryRestored"] = "Session automatique récupérée.",
        ["Log.RecoveryUnavailable"] = "Récupération automatique indisponible : {0}",
        ["Log.TxtExported"] = "Rapport TXT exporté : {0}",
        ["Log.PdfExported"] = "Rapport PDF exporté : {0}",
        ["Log.PatchbookTxtExported"] = "Patchbook TXT exporté : {0}",
        ["Log.PatchbookCsvExported"] = "Patchbook CSV exporté : {0}",
        ["Log.XmlMerged"] = "XML ajouté au projet : {0}",

        ["Summary.PatchRows"] = "{0} lignes - {1} actifs - {2} locaux - {3} warning(s) - {4} conflit(s)",
        ["Summary.Health"] = "Preset : {0}  |  Version : {1}  |  Mode : {2}  |  Fichier : {3}\nDevices : {4}  |  TX : {5}  |  RX : {6}  |  Patchs actifs : {7}  |  RX libres : {8}\nPatchs locaux : {9}  |  Devices TX absents : {10}  |  Canaux TX introuvables : {11}  |  Preferred masters : {12}\nSamplerates : {13}  |  Encodages : {14}  |  Latences : {15}\nRedondants : {16}  |  Daisychain : {17}  |  IP fixes détectées : {18}  |  Erreurs : {19}  |  Warnings : {20}",
        ["Profile.48k24b1msAuto"] = "48 kHz / 24 bit / 1 ms / IP auto",
        ["Profile.48k24b2msAuto"] = "48 kHz / 24 bit / 2 ms / IP auto",
        ["Profile.96k24b1msAuto"] = "96 kHz / 24 bit / 1 ms / IP auto",
        ["Profile.96k24b2msAuto"] = "96 kHz / 24 bit / 2 ms / IP auto",
        ["Profile.48k24b1msRedundant"] = "48 kHz / 24 bit / 1 ms / Redondant / IP auto",
        ["Profile.48k24b1msDaisychain"] = "48 kHz / 24 bit / 1 ms / Daisychain / IP auto",

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

        ["DeviceFilter.All"] = "All",
        ["DeviceFilter.Locked"] = "Locked",
        ["DeviceFilter.StaticIp"] = "Static IPs",
        ["DeviceFilter.PreferredMaster"] = "Preferred Masters",
        ["DeviceFilter.Redundant"] = "Redundant",
        ["DeviceFilter.Daisychain"] = "Daisychain",
        ["DeviceFilter.NoTx"] = "No Tx",
        ["DeviceFilter.NoRx"] = "No Rx",
        ["DeviceFilter.Modified"] = "Modified only",
        ["DeviceFilter.WarningSelection"] = "Selected warning",
        ["DeviceFilter.SampleRateDifferent"] = "Different sample rate",
        ["DeviceFilter.EncodingDifferent"] = "Different bits",
        ["Target.AllUnlocked"] = "All unlocked",
        ["Target.SelectedUnlocked"] = "Selected unlocked",
        ["Target.FilteredUnlocked"] = "Visible filter unlocked",

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
        ["Status.NoDeviceSettingsChanged"] = "No device settings to apply.",
        ["Status.RecoveryRestored"] = "Automatic session recovered - unsaved changes.",
        ["Status.NoImportantWarning"] = "No important item to check.",
        ["Status.WarningDevicesDisplayed"] = "{0} affected device(s) displayed.",
        ["Status.ProfileAlreadyApplied"] = "The profile is already applied to the whole target.",
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
        ["Action.DeviceRxPatchesReset"] = "Device Rx subscriptions reset.",
        ["Action.DeviceTxPatchesReset"] = "Device Tx subscriptions reset.",
        ["Action.DeviceDetailsUpdated"] = "Device details updated.",
        ["Action.DeviceSettingsUpdated"] = "Device settings updated.",
        ["Action.QuickProfileApplied"] = "Quick profile applied.",
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
        ["Action.VisualPatchesApplied"] = "{0} visual subscription change(s) applied.",
        ["Action.TxChannelRenamed"] = "Tx channel renamed and subscriptions updated.",
        ["Action.RxChannelRenamed"] = "Rx channel renamed.",
        ["Action.DeviceDeleted"] = "Device deleted.",
        ["Action.XmlMerged"] = "XML added to project.",
        ["Action.AtomicChaosApplied"] = "Atomic exercise generated - not saved.",

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
        ["Dialog.AtomicChaosTitle"] = "Atomic Bomb - troubleshooting exercise",
        ["Dialog.AtomicChaosFirst"] = "First lock: this function deliberately scrambles the loaded XML copy to create a troubleshooting exercise. It mixes names, network modes, clocks, latencies, audio formats, primary IP settings, and subscriptions. Continue?",
        ["Dialog.AtomicChaosSecond"] = "Second lock: the result will be deliberately inconsistent, but only recognized Dante values will be written. Technical identifiers, DNS, gateways, and secondary interfaces will be preserved. Prepare this exercise?",
        ["Dialog.AtomicChaosThird"] = "FINAL CONFIRMATION: atomize the configuration in memory? The original file will remain intact and Save As will be required to keep the exercise.",
        ["Dialog.AtomicChaosCompleted"] = "Atomic scenario created (seed {0}). {1} device(s), {2} Tx channels, {3} patched Rx, {4} free Rx, {5} static IP(s), {6} automatic IP(s). The original XML was not modified. Use Save As to keep the exercise.",
        ["Dialog.RecoveryTitle"] = "Session recovery",
        ["Dialog.RecoveryFound"] = "An unsaved automatic copy from {0:g} was found. Do you want to recover it?\n\nNo will delete this temporary copy and open the original XML.",
        ["Dialog.RecoverySourceChanged"] = "Warning: the original XML file has changed since this recovery. Carefully review the differences before saving.",
        ["Dialog.NoDeviceChanges"] = "No device, channel, or subscription change is detected since the XML was opened.",
        ["Dialog.DeviceChangesTitle"] = "Before / after changes",
        ["Dialog.SelectProfile"] = "Select a quick profile.",
        ["Dialog.ProfileWarningContinue"] = "This profile can change several audio and network settings in one operation. Review the preview and validate the final XML in Dante Controller. Continue?",
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
        ["Log.RecoveryRestored"] = "Automatic session recovered.",
        ["Log.RecoveryUnavailable"] = "Automatic recovery unavailable: {0}",
        ["Log.TxtExported"] = "TXT report exported: {0}",
        ["Log.PdfExported"] = "PDF report exported: {0}",
        ["Log.PatchbookTxtExported"] = "Patchbook TXT exported: {0}",
        ["Log.PatchbookCsvExported"] = "Patchbook CSV exported: {0}",
        ["Log.XmlMerged"] = "XML added to project: {0}",

        ["Summary.PatchRows"] = "{0} rows - {1} active - {2} local - {3} warning(s) - {4} conflict(s)",
        ["Summary.Health"] = "Preset: {0}  |  Version: {1}  |  Mode: {2}  |  File: {3}\nDevices: {4}  |  TX: {5}  |  RX: {6}  |  Active subscriptions: {7}  |  Free RX: {8}\nLocal subscriptions: {9}  |  Missing TX devices: {10}  |  Missing TX channels: {11}  |  Preferred Masters: {12}\nSample rates: {13}  |  Encoding: {14}  |  Latencies: {15}\nRedundant: {16}  |  Daisy-chain: {17}  |  Static IPs detected: {18}  |  Errors: {19}  |  Warnings: {20}",
        ["Profile.48k24b1msAuto"] = "48 kHz / 24 bit / 1 ms / automatic IP",
        ["Profile.48k24b2msAuto"] = "48 kHz / 24 bit / 2 ms / automatic IP",
        ["Profile.96k24b1msAuto"] = "96 kHz / 24 bit / 1 ms / automatic IP",
        ["Profile.96k24b2msAuto"] = "96 kHz / 24 bit / 2 ms / automatic IP",
        ["Profile.48k24b1msRedundant"] = "48 kHz / 24 bit / 1 ms / Redundant / automatic IP",
        ["Profile.48k24b1msDaisychain"] = "48 kHz / 24 bit / 1 ms / Daisy-chain / automatic IP",

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
        Add(map, "Voir les machines", "Show devices");
        Add(map, "Filtre machines", "Device filter");
        Add(map, "Cible actions", "Action target");
        Add(map, "Sélectionner visibles", "Select visible");
        Add(map, "Effacer sélection", "Clear selection");
        Add(map, "Verrouiller sélection", "Lock selection");
        Add(map, "Déverrouiller sélection", "Unlock selection");
        Add(map, "Avant / après", "Before / after");
        Add(map, "Lock", "Lock");
        Add(map, "Machine sélectionnée", "Selected device");
        Add(map, "Machine", "Device");
        Add(map, "Nouveau nom", "New name");
        Add(map, "Changer le nom", "Change name");
        Add(map, "Appliquer les paramètres", "Apply settings");
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
        Add(map, "Reset patch RX/TX", "Clear Rx/Tx");
        Add(map, "Reset patch RX", "Clear Rx");
        Add(map, "Reset patch TX", "Clear Tx");
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
        Add(map, "Échange de labels", "Label exchange");
        Add(map, "Importer des labels", "Import labels");
        Add(map, "Exporter des labels", "Export labels");
        Add(map, "Importe des labels JSON, CSV ou XLSX DMT vers une ou plusieurs machines, avec prévisualisation.", "Imports JSON, CSV, or DMT XLSX labels into one or more devices with a preview.");
        Add(map, "Exporte les labels TX/RX en JSON, CSV ou dans une copie d'un modèle XLSX DMT.", "Exports Tx/Rx labels as JSON, CSV, or a copy of a DMT XLSX template.");
        Add(map, "Labels / DMT", "Labels / DMT");
        Add(map, "Échange de labels avec dLive MIDI Tools (DMT)", "Label exchange with dLive MIDI Tools (DMT)");
        Add(map, "Échange hors ligne par fichiers. Choisissez le sens du transfert.", "Offline file exchange. Choose the transfer direction.");
        Add(map, "DMT → Dante Config Editor", "DMT → Dante Config Editor");
        Add(map, "Importez les labels de la feuille Channels d'un fichier XLSX DMT, ou depuis un fichier JSON/CSV.", "Import labels from the Channels sheet of a DMT XLSX file, or from a JSON/CSV file.");
        Add(map, "Importer depuis DMT / JSON / CSV", "Import from DMT / JSON / CSV");
        Add(map, "Lit un fichier DMT, JSON ou CSV, puis prévisualise les labels avant de les appliquer aux machines Dante.", "Reads a DMT, JSON, or CSV file, then previews labels before applying them to Dante devices.");
        Add(map, "Dante Config Editor → DMT", "Dante Config Editor → DMT");
        Add(map, "Exportez les labels Dante en JSON/CSV, ou créez une copie d'un modèle XLSX DMT.", "Export Dante labels as JSON/CSV, or create a copy of a DMT XLSX template.");
        Add(map, "Exporter vers DMT / JSON / CSV", "Export to DMT / JSON / CSV");
        Add(map, "Exporte les labels TX/RX. Pour DMT, une copie du modèle XLSX choisi est créée et l'original reste intact.", "Exports Tx/Rx labels. For DMT, a copy of the selected XLSX template is created and the original remains unchanged.");
        Add(map, "Ouvrir le projet dLive MIDI Tools (DMT)", "Open the dLive MIDI Tools (DMT) project");
        Add(map, "Ouvre la page GitHub officielle de dLive MIDI Tools par togrupe.", "Opens the official dLive MIDI Tools GitHub page by togrupe.");
        Add(map, "Ouverture du projet DMT impossible", "Cannot open the DMT project");
        Add(map, "Actions globales", "Global actions");
        Add(map, "Réseau / audio", "Network / audio");
        Add(map, "Profils", "Profiles");
        Add(map, "Profil rapide", "Quick profile");
        Add(map, "Appliquer le profil à la cible", "Apply profile to target");
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
        Add(map, "Détails", "Details");
        Add(map, "Réduire les réglages", "Hide settings");
        Add(map, "Afficher les réglages", "Show settings");
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
        Add(map, "Patch visuel / grille", "Visual patch / matrix");
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
        Add(map, "Rapport final avant Dante", "Final Dante check");
        Add(map, "Rapport compatibilité Dante Controller", "Dante Controller compatibility report");
        Add(map, "Actualiser le résumé", "Refresh summary");
        Add(map, "Historique actions", "Action history");
        Add(map, "Exporter TXT", "Export TXT");
        Add(map, "Exporter PDF", "Export PDF");
        Add(map, "Patchbook TXT", "Patchbook TXT");
        Add(map, "Patchbook CSV", "Patchbook CSV");
        Add(map, "Topologie simple", "Simple topology");
        Add(map, "Comparer XML", "Compare XML");
        Add(map, "Quick start", "Quick start");
        Add(map, "Notice complète", "Full guide");
        Add(map, "Journal", "Log");
        Add(map, "GÉNÉRATEUR D'EXERCICE DE DÉPANNAGE", "TROUBLESHOOTING EXERCISE GENERATOR");
        Add(map, "Désorganise volontairement la copie XML chargée pour créer un réseau d'entraînement à réparer. Le fichier original et les identifiants techniques restent protégés.", "Deliberately scrambles the loaded XML copy to create a training network to repair. The original file and technical identifiers remain protected.");
        Add(map, "ATOMIC BOMB", "ATOMIC BOMB");
        Add(map, "Crée un exercice hors ligne en mélangeant les noms, patchs, modes réseau, formats audio, horloges et IP principales. Trois confirmations sont requises.", "Creates an offline exercise by mixing names, subscriptions, network modes, audio formats, clocks, and primary IP settings. Three confirmations are required.");
        Add(map, "Charge un export XML Dante Controller.", "Loads a Dante Controller XML export.");
        Add(map, "Ajoute les machines d'un autre XML au projet ouvert. Les doublons peuvent être renommés.", "Adds devices from another XML to the open project. Duplicates can be renamed.");
        Add(map, "Enregistre un nouveau XML et crée un backup de sécurité.", "Saves a new XML file and creates a safety backup.");
        Add(map, "Autorise les modifications dans l'interface. La sauvegarde reste faite sous un nouveau nom.", "Allows changes in the interface. Saving still uses a new file name.");
        Add(map, "Annule la dernière action réalisée dans cette session.", "Undoes the last action made in this session.");
        Add(map, "Recharge le fichier XML d'origine et abandonne les modifications non sauvegardées.", "Reloads the original XML file and discards unsaved changes.");
        Add(map, "Liste les derniers XML ouverts.", "Lists the most recently opened XML files.");
        Add(map, "Ouvre le fichier sélectionné dans la liste récente.", "Opens the selected file from the recent list.");
        Add(map, "Déconnecte les RX de la machine et supprime les patchs qui utilisent ses TX.", "Disconnects the device Rx channels and removes subscriptions using its Tx channels.");
        Add(map, "Déconnecte seulement les entrées RX de la machine sélectionnée.", "Disconnects only the selected device Rx inputs.");
        Add(map, "Supprime seulement les patchs qui utilisent les TX de la machine sélectionnée.", "Removes only subscriptions using the selected device Tx channels.");
        Add(map, "Préfixe simple : HF donnera HF 01, HF 02. Modèle avancé : HF {00}, IN-{device}-{000}, ou {n} sans zéro.", "Simple prefix: HF gives HF 01, HF 02. Advanced pattern: HF {00}, IN-{device}-{000}, or {n} without leading zeros.");
        Add(map, "Applique le mode réseau à la cible choisie, en ignorant les machines verrouillées.", "Applies the network mode to the chosen target, ignoring locked devices.");
        Add(map, "Applique la latence à la cible choisie après prévisualisation.", "Applies latency to the chosen target after preview.");
        Add(map, "Applique la sample rate à la cible choisie. À vérifier avant import Dante.", "Applies the sample rate to the chosen target. Verify before Dante import.");
        Add(map, "Applique les bits par échantillon à la cible choisie.", "Applies bits per sample to the chosen target.");
        Add(map, "Réinitialise les noms de canaux TX/RX de la cible choisie, en respectant les verrous.", "Resets Tx/Rx channel names for the chosen target, respecting locks.");
        Add(map, "Attribue des IP fixes en série aux machines de la cible qui ont une interface IPv4 modifiable.", "Assigns static IPs in sequence to target devices with an editable IPv4 interface.");
        Add(map, "Repasse les IP reconnues en automatique pour la cible choisie.", "Sets recognized IP fields back to automatic for the chosen target.");
        Add(map, "Filtre seulement le tableau des machines, sans modifier le XML.", "Filters only the device table without modifying the XML.");
        Add(map, "Affiche rapidement les machines en IP fixe, preferred master, redondantes, daisychain, sans TX/RX ou avec formats différents.", "Quickly shows static IP, Preferred Master, redundant, daisychain, no Tx/Rx, or different-format devices.");
        Add(map, "Détermine quelles machines seront touchées par les actions globales.", "Defines which devices global actions will affect.");
        Add(map, "Choisissez si les actions globales s'appliquent à toutes les machines non verrouillées, à la sélection ou au filtre affiché.", "Choose whether global actions apply to all unlocked devices, the selection, or the visible filter.");
        Add(map, "Sélectionne toutes les machines actuellement visibles dans le tableau.", "Selects all devices currently visible in the table.");
        Add(map, "Vide la sélection multiple du tableau.", "Clears the table multi-selection.");
        Add(map, "Les machines verrouillées sont ignorées par les actions globales.", "Locked devices are ignored by global actions.");
        Add(map, "Retire le verrou des machines sélectionnées.", "Unlocks the selected devices.");
        Add(map, "Verrouille cette machine pour que les actions globales ne la modifient pas.", "Locks this device so global actions do not modify it.");
        Add(map, "Affiche un résumé OK / points à vérifier avant d'importer le XML dans Dante Controller.", "Shows an OK / items-to-check summary before importing the XML into Dante Controller.");
        Add(map, "Affiche les dernières actions réalisées dans l'application.", "Shows the latest actions made in the application.");
        Add(map, "Ouvre la notice rapide PDF.", "Opens the quick start PDF.");
        Add(map, "Ouvre la notice complète PDF.", "Opens the full guide PDF.");
        Add(map, "Le XML conserve la valeur brute Dante.", "The XML keeps the raw Dante value.");
        Add(map, "La valeur affichée est en ms. Le XML conserve la valeur brute Dante.", "Displayed value is in ms. The XML keeps the raw Dante value.");
        Add(map, "Applique en une seule fois le nom, le mode réseau, la latence et le statut Preferred master.", "Applies the name, network mode, latency, and Preferred Master status in one operation.");
        Add(map, "Ouvre tous les réglages de la machine : IP automatique ou fixe, formats audio et noms des canaux.", "Opens all device settings: automatic or static IP, audio formats, and channel names.");
        Add(map, "Supprime la machine et nettoie les patchs qui lui sont associés.", "Deletes the device and removes its associated subscriptions.");
        Add(map, "Masque les panneaux de réglage pour agrandir le tableau des machines.", "Hides the settings panels to enlarge the device table.");
        Add(map, "Affiche les panneaux de réglage de la configuration.", "Shows the configuration settings panels.");
        Add(map, "Affiche toutes les différences de machines, canaux et patchs depuis l'ouverture du XML.", "Shows all device, channel, and subscription differences since the XML was opened.");
        Add(map, "Applique en une seule action les formats audio, la latence, l'IP automatique et éventuellement le mode réseau du profil à la cible choisie.", "Applies the profile audio formats, latency, automatic IP, and optional network mode to the selected target in one operation.");
        Add(map, "Ouvre une vue TX/RX avec glisser-déposer, affectation en série et grille de patch.", "Opens a Tx/Rx view with drag and drop, sequential assignment, and a patch matrix.");

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
