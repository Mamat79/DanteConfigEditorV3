using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;
using Microsoft.Win32;

namespace DanteConfigEditor;

public partial class MainWindow : Window
{
    private const string AllSendersItem = "Tous les émetteurs";
    private const string AllReceiversItem = "Tous les récepteurs";

    // Collections liées directement aux listes WPF. Quand on les modifie,
    // l'interface se met à jour sans recréer toute la fenêtre.
    private readonly ObservableCollection<DanteSubscription> _patchRows = [];
    private readonly ObservableCollection<string> _logs = [];
    private readonly ObservableCollection<GlobalSearchResult> _searchResults = [];
    private readonly ObservableCollection<DanteValidationIssue> _healthIssues = [];
    private readonly LatencyChoice[] _latencies =
    [
        new("250", "0,25 ms"),
        new("1000", "1 ms"),
        new("2000", "2 ms"),
        new("5000", "5 ms")
    ];
    private readonly string[] _patchViewModes = ["Simple", "Expert"];
    private readonly string[] _patchStateFilters =
    [
        "Tous les RX",
        "Patchs actifs",
        "RX libres",
        "Patchs locaux",
        "Devices TX absents",
        "Canaux TX introuvables",
        "Warnings",
        "Conflits",
        "Modifiés"
    ];
    private readonly string[] _healthFilters =
    [
        "Tous",
        "Infos",
        "Avertissements",
        "Erreurs",
        "Patchs",
        "Devices",
        "Clock",
        "Réseau",
        "Compatibilité XML"
    ];
    private readonly string[] _patchbookScopes = ["Tous les RX", "Patchs actifs", "Warnings / conflits"];
    private DanteProject? _project;
    private bool _editModeEnabled;

    // Évite que les changements de sélection déclenchés par RefreshAll relancent
    // eux-mêmes des actions utilisateur.
    private bool _refreshingUi;

    private sealed record ChannelChoice(DanteChannelKind Kind, int Index, string Name)
    {
        public override string ToString()
        {
            return $"{Index} - {Name}";
        }
    }

    private sealed record TxChannelChoice(string DeviceName, int DanteId, string ChannelName)
    {
        public override string ToString()
        {
            return $"{DanteId:000} - {ChannelName}";
        }
    }

    private sealed record LatencyChoice(string XmlValue, string Display)
    {
        public override string ToString()
        {
            return Display;
        }
    }

    private sealed record GlobalSearchResult(
        string Kind,
        string Label,
        string? DeviceName = null,
        DanteChannelKind? ChannelKind = null,
        int? ChannelIndex = null,
        string? RxDevice = null,
        int? RxIndex = null)
    {
        public override string ToString()
        {
            return $"{Kind} - {Label}";
        }
    }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialisation des sources de données utilisées par les contrôles.
        LatencyComboBox.ItemsSource = _latencies;
        GlobalLatencyComboBox.ItemsSource = _latencies;
        ChannelKindComboBox.ItemsSource = new[] { "TX", "RX" };
        ChannelKindComboBox.SelectedItem = "TX";
        PatchStateFilterComboBox.ItemsSource = _patchStateFilters;
        PatchStateFilterComboBox.SelectedItem = _patchStateFilters[0];
        PatchViewModeComboBox.ItemsSource = _patchViewModes;
        PatchViewModeComboBox.SelectedItem = _patchViewModes[0];
        HealthFilterComboBox.ItemsSource = _healthFilters;
        HealthFilterComboBox.SelectedItem = _healthFilters[0];
        PatchbookScopeComboBox.ItemsSource = _patchbookScopes;
        PatchbookScopeComboBox.SelectedItem = _patchbookScopes[0];
        PatchGrid.ItemsSource = _patchRows;
        LogListBox.ItemsSource = _logs;
        GlobalSearchListBox.ItemsSource = _searchResults;
        HealthIssuesGrid.ItemsSource = _healthIssues;
        GlobalDaisychainRadioButton.IsChecked = true;
        DaisychainRadioButton.IsChecked = true;
        SetTheme(useLightTheme: false);
        RefreshRecentFiles();
        RefreshAll();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*",
            Title = "Ouvrir une configuration Dante"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LoadProjectFromPath(dialog.FileName);
    }

    private void OpenRecentButton_Click(object sender, RoutedEventArgs e)
    {
        if (RecentFilesComboBox.SelectedItem is not string path || string.IsNullOrWhiteSpace(path))
        {
            ShowError("Aucun fichier récent", "Sélectionnez un fichier récent à ouvrir.");
            return;
        }

        if (!File.Exists(path))
        {
            ShowError("Fichier introuvable", "Ce fichier récent n'existe plus.");
            RefreshRecentFiles();
            return;
        }

        LoadProjectFromPath(path);
    }

    public void LoadProjectFromPath(string path)
    {
        try
        {
            // DanteProject contient toute la logique XML. La fenêtre ne garde que
            // l'état d'affichage et les actions utilisateur.
            _project = DanteProject.Load(path);
            _editModeEnabled = false;
            _logs.Clear();
            RecentFilesService.Add(path);
            RefreshRecentFiles();
            AddLog("Fichier chargé : " + path);
            RefreshAll();
            SetStatus("Fichier chargé.");
        }
        catch (Exception ex)
        {
            ShowError("Le fichier ne peut pas être ouvert.", ex.Message);
        }
    }

    private void ActivateEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
            this,
            "Vous allez activer l'édition du fichier XML hors ligne. Travaillez toujours sur une copie et vérifiez le fichier final dans Dante Controller avant production.",
            "Activer l'édition",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        _editModeEnabled = true;
        AddLog("Mode édition activé.");
        RefreshAll();
        SetStatus("Mode édition activé.");
    }

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        if (!EnsureEditMode())
        {
            return;
        }

        // La validation est relancée juste avant la sauvegarde pour éviter de
        // créer un fichier final manifestement invalide.
        DanteValidationResult validation = _project!.Validate();
        if (validation.HasErrors)
        {
            ShowError("Sauvegarde impossible", validation.ToDisplayText());
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*",
            Title = "Enregistrer une nouvelle configuration",
            FileName = Path.GetFileName(SafeFileService.BuildDefaultSavePath(_project.OriginalFilePath)),
            InitialDirectory = Path.GetDirectoryName(_project.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (File.Exists(dialog.FileName))
        {
            MessageBoxResult overwrite = MessageBox.Show(
                this,
                "Ce fichier existe déjà. Voulez-vous vraiment l'écraser ?",
                "Confirmation requise",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (overwrite != MessageBoxResult.Yes)
            {
                return;
            }
        }

        string summary = _project.BuildSaveSummary();
        MessageBoxResult confirm = MessageBox.Show(
            this,
            summary + Environment.NewLine + Environment.NewLine + "Une sauvegarde du fichier original sera créée avant l'écriture. Continuer ?",
            "Résumé avant sauvegarde",
            MessageBoxButton.YesNo,
            validation.HasWarnings ? MessageBoxImage.Warning : MessageBoxImage.Information);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            string backupPath = _project.SaveAs(dialog.FileName);
            AddLog("Sauvegarde originale créée : " + backupPath);
            AddLog("Fichier enregistré : " + dialog.FileName);
            RefreshAll();
            SetStatus("Fichier sauvegardé.");
        }
        catch (Exception ex)
        {
            ShowError("Erreur pendant la sauvegarde", ex.Message);
        }
    }

    private void RevertButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        if (_project!.IsModified)
        {
            MessageBoxResult confirm = MessageBox.Show(
                this,
                "Les changements non sauvegardés seront perdus. Continuer ?",
                "Annuler les changements",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            string path = _project.OriginalFilePath;
            _project = DanteProject.Load(path);
            AddLog("Changements annulés. Rechargement du fichier original.");
            RefreshAll();
            SetStatus("Fichier original rechargé.");
        }
        catch (Exception ex)
        {
            ShowError("Impossible de recharger le fichier original", ex.Message);
        }
    }

    private void UndoLastButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        try
        {
            string label = _project!.UndoLastChange();
            AddLog("Action annulée : " + label);
            RefreshAll();
            SetStatus("Dernière action annulée.");
        }
        catch (Exception ex)
        {
            ShowError("Annulation impossible", ex.Message);
        }
    }

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Tag?.ToString(), out int index))
        {
            MainTabs.SelectedIndex = index;
        }
    }

    private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || _project is null)
        {
            return;
        }

        DanteDevice? device = _project.FindDevice(DeviceComboBox.SelectedItem as string);
        if (device is null)
        {
            return;
        }

        NewNameTextBox.Text = device.Name;
        RedundantRadioButton.IsChecked = device.IsRedundant;
        DaisychainRadioButton.IsChecked = !device.IsRedundant;
        SelectLatency(LatencyComboBox, device.Latency);
        PreferredMasterCheckBox.IsChecked = device.PreferredMaster;
        RefreshChannelSelector();
    }

    private void ApplyRenameButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction("Nom mis à jour.", () =>
        {
            string oldName = SelectedDeviceName();
            string newName = NewNameTextBox.Text;
            _project!.RenameDevice(oldName, newName);
        });
    }

    private void ApplyNetworkButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction("Mode réseau mis à jour.", () =>
        {
            _project!.SetNetworkMode(SelectedDeviceName(), RedundantRadioButton.IsChecked == true);
        });
    }

    private void ApplyLatencyButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction("Latence mise à jour.", () =>
        {
            string latency = SelectedLatencyXmlValue(LatencyComboBox);
            _project!.SetLatency(SelectedDeviceName(), latency);
        },
        "Modifier la latence Dante peut provoquer une reconfiguration des flux lors de l'import/application dans les outils Dante. Vérifiez toujours le preset dans Dante Controller.");
    }

    private void ApplyPreferredMasterButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction("Preferred master mis à jour.", () =>
        {
            _project!.SetPreferredMaster(SelectedDeviceName(), PreferredMasterCheckBox.IsChecked == true);
        });
    }

    private void ResetDeviceChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            "Canaux réinitialisés.",
            () => _project!.ResetChannels(SelectedDeviceName()),
            "Les noms des canaux du device sélectionné seront remplacés par 1, 2, 3...");
    }

    private void RenameChannelButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChannelComboBox.SelectedItem is not ChannelChoice channel)
        {
            ShowError("Aucun canal sélectionné", "Sélectionnez un canal TX ou RX à renommer.");
            return;
        }

        RunProjectAction("Canal renommé.", () =>
        {
            _project!.RenameChannel(SelectedDeviceName(), channel.Kind, channel.Index, NewChannelNameTextBox.Text);
        });
    }

    private void BatchRenameButton_Click(object sender, RoutedEventArgs e)
    {
        // Le type TX/RX choisi dans l'écran sert aussi aux listes de plage.
        DanteChannelKind kind = string.Equals(ChannelKindComboBox.SelectedItem as string, "RX", StringComparison.OrdinalIgnoreCase)
            ? DanteChannelKind.Rx
            : DanteChannelKind.Tx;

        if (BatchRenameStartChannelComboBox.SelectedItem is not ChannelChoice startChannel
            || BatchRenameEndChannelComboBox.SelectedItem is not ChannelChoice endChannel)
        {
            ShowError("Plage invalide", "Sélectionnez un canal de début et un canal de fin.");
            return;
        }

        if (startChannel.Kind != kind || endChannel.Kind != kind || startChannel.Index > endChannel.Index)
        {
            ShowError("Plage invalide", "Le canal de fin doit être placé après le canal de début.");
            return;
        }

        if (!int.TryParse(BatchRenameStartTextBox.Text.Trim(), out int firstNumber))
        {
            ShowError("Numéro invalide", "Indiquez un numéro de départ valide.");
            return;
        }

        RunProjectAction(
            "Renommage en série appliqué.",
            () => _project!.BatchRenameChannels(SelectedDeviceName(), kind, BatchRenamePrefixTextBox.Text, firstNumber, startChannel.Index, endChannel.Index),
            $"Les noms des canaux {kind} {startChannel.Index} à {endChannel.Index} seront remplacés en série. Continuer ?");
    }

    private void ApplyAllNetworkButton_Click(object sender, RoutedEventArgs e)
    {
        bool redundant = GlobalRedundantRadioButton.IsChecked == true;
        RunProjectAction(
            "Mode réseau appliqué à tous les devices.",
            () => _project!.SetAllNetworkModes(redundant),
            _project?.BuildAllNetworkModePreview(redundant) + Environment.NewLine + "Continuer ?");
    }

    private void ApplyAllLatencyButton_Click(object sender, RoutedEventArgs e)
    {
        string latency = SelectedLatencyXmlValue(GlobalLatencyComboBox);
        RunProjectAction(
            "Latence appliquée à tous les devices.",
            () =>
            {
                _project!.SetAllLatencies(latency);
            },
            _project?.BuildAllLatencyPreview(latency)
                + Environment.NewLine
                + "Modifier la latence Dante peut provoquer une reconfiguration des flux lors de l'import/application dans les outils Dante. Continuer ?");
    }

    private void ApplyAllPreferredMasterButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        if (GlobalPreferredMasterCheckBox.IsChecked == true)
        {
            string deviceName = SelectedDeviceName();
            RunProjectAction(
                "Preferred master unique appliqué.",
                () => _project!.SetSolePreferredMaster(deviceName),
                _project?.BuildSolePreferredMasterPreview(deviceName) + Environment.NewLine + "Continuer ?");
            return;
        }

        RunProjectAction(
            "Tous les preferred masters ont été retirés.",
            () => _project!.SetAllPreferredMasters(false),
            _project?.BuildClearPreferredMastersPreview() + Environment.NewLine + "Continuer ?");
    }

    private void ResetAllChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            "Tous les canaux ont été réinitialisés.",
            () => _project!.ResetAllChannels(),
            _project?.BuildResetAllChannelsPreview() + Environment.NewLine + "Continuer ?");
    }

    private void ListRedundantButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("Machines redondantes", project => project.ListRedundantDevices());
    }

    private void ListDaisychainButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("Machines en daisychain", project => project.ListDaisychainDevices());
    }

    private void ListLatenciesButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("Latences", project => project.ListLatencies());
    }

    private void ListPreferredMastersButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("Preferred masters", project => project.ListPreferredMasters());
    }

    private void SenderDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || SenderDeviceList.SelectedItem is not string deviceName)
        {
            return;
        }

        if (!string.Equals(deviceName, AllSendersItem, StringComparison.OrdinalIgnoreCase))
        {
            SourceDeviceComboBox.SelectedItem = deviceName;
        }

        RefreshPatchRows();
    }

    private void ReceiverDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || ReceiverDeviceList.SelectedItem is not string deviceName)
        {
            return;
        }

        RefreshPatchRows();
    }

    private void PatchFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_refreshingUi)
        {
            return;
        }

        RefreshPatchRows();
    }

    private void PatchFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi)
        {
            return;
        }

        RefreshPatchRows();
    }

    private void PatchViewModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyPatchViewMode();
    }

    private void HealthFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi)
        {
            return;
        }

        RefreshHealthPage();
    }

    private void GlobalSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshGlobalSearchResults();
    }

    private void GlobalSearchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || GlobalSearchListBox.SelectedItem is not GlobalSearchResult result)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(result.RxDevice) && result.RxIndex.HasValue)
        {
            MainTabs.SelectedIndex = 1;
            SenderDeviceList.SelectedItem = AllSendersItem;
            ReceiverDeviceList.SelectedItem = AllReceiversItem;
            RefreshPatchRows();
            DanteSubscription? subscription = _patchRows.FirstOrDefault(row =>
                string.Equals(row.RxDevice, result.RxDevice, StringComparison.OrdinalIgnoreCase)
                && row.RxIndex == result.RxIndex.Value);
            if (subscription is not null)
            {
                PatchGrid.SelectedItem = subscription;
                PatchGrid.ScrollIntoView(subscription);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(result.DeviceName))
        {
            MainTabs.SelectedIndex = 0;
            DeviceComboBox.SelectedItem = result.DeviceName;

            if (result.ChannelKind.HasValue && result.ChannelIndex.HasValue)
            {
                ChannelKindComboBox.SelectedItem = result.ChannelKind.Value == DanteChannelKind.Rx ? "RX" : "TX";
                RefreshChannelSelector();
                ChannelChoice? channel = (ChannelComboBox.ItemsSource as IEnumerable<ChannelChoice>)
                    ?.FirstOrDefault(choice => choice.Kind == result.ChannelKind.Value && choice.Index == result.ChannelIndex.Value);
                if (channel is not null)
                {
                    ChannelComboBox.SelectedItem = channel;
                }
            }
        }
    }

    private void ChannelSelector_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi)
        {
            return;
        }

        RefreshChannelSelector();
    }

    private void ChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi)
        {
            return;
        }

        if (ChannelComboBox.SelectedItem is ChannelChoice channel)
        {
            NewChannelNameTextBox.Text = channel.Name;
        }
    }

    private void SourceDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSourceChannels();
    }

    private void SourceChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi)
        {
            return;
        }

        PatchTxRenameChannelTextBox.Text = SelectedSourceChannelName();
    }

    private void PatchGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(subscription.ResolvedTxDeviceName))
        {
            SourceDeviceComboBox.SelectedItem = subscription.ResolvedTxDeviceName;
            RefreshSourceChannels();
            SelectSourceChannel(subscription.TxChannelName);
        }

        PatchRxRenameChannelTextBox.Text = subscription.RxChannelName;
        PatchTxRenameChannelTextBox.Text = subscription.TxChannelName;

        if (subscription.IsExternalMissingDevice)
        {
            SetStatus("Ce patch pointe vers un device absent du preset. Cela peut être normal si le preset Dante est partiel.");
        }
    }

    private void ApplyPatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            ShowError("Aucun canal RX sélectionné", "Sélectionnez une ligne dans la table de patch.");
            return;
        }

        RunProjectAction(
            "Patch appliqué.",
            () =>
            {
                string txDevice = SourceDeviceComboBox.SelectedItem as string ?? string.Empty;
                string txChannel = SelectedSourceChannelName();
                _project!.ApplyPatch(subscription.RxDevice, subscription.RxIndex, txDevice, txChannel);
            },
            subscription.IsExternalMissingDevice
                ? "Ce patch pointe vers un device qui n'est pas présent dans le preset. Cela peut être normal si le preset Dante est partiel. Ne le modifiez que si vous êtes certain de vouloir remplacer cette source. Continuer ?"
                : null);
    }

    private void RemovePatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            ShowError("Aucun canal RX sélectionné", "Sélectionnez une ligne dans la table de patch.");
            return;
        }

        RunProjectAction(
            "Patch supprimé.",
            () => _project!.RemovePatch(subscription.RxDevice, subscription.RxIndex),
            "Le patch du canal RX sélectionné sera supprimé.");
    }

    private void RenamePatchTxChannelButton_Click(object sender, RoutedEventArgs e)
    {
        // Renommer un TX depuis la page Patch passe par la même logique que la
        // page Configuration, pour garder la mise à jour des abonnements.
        string sourceDeviceName = SourceDeviceComboBox.SelectedItem as string ?? string.Empty;
        string sourceChannelName = SelectedSourceChannelName();
        string newName = PatchTxRenameChannelTextBox.Text;

        if (string.IsNullOrWhiteSpace(sourceDeviceName) || string.IsNullOrWhiteSpace(sourceChannelName))
        {
            ShowError("Canal TX manquant", "Sélectionnez un device TX et un canal TX dans la zone de patch.");
            return;
        }

        RunProjectAction("Canal TX renommé et patchs mis à jour.", () =>
        {
            DanteDevice txDevice = _project!.FindDevice(sourceDeviceName)
                ?? throw new InvalidOperationException("Device TX introuvable.");
            DanteChannel txChannel = txDevice.TxChannels.FirstOrDefault(channel =>
                    SourceChannelComboBox.SelectedItem is TxChannelChoice choice && channel.DanteId == choice.DanteId
                    || string.Equals(channel.DisplayName, sourceChannelName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(channel.Index.ToString(), sourceChannelName, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Canal TX introuvable.");

            _project.RenameChannel(sourceDeviceName, DanteChannelKind.Tx, txChannel.Index, newName);
        });
    }

    private void RenamePatchRxChannelButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            ShowError("Aucun canal RX sélectionné", "Sélectionnez une ligne RX dans la table de patch.");
            return;
        }

        RunProjectAction("Canal RX renommé.", () =>
        {
            _project!.RenameChannel(subscription.RxDevice, DanteChannelKind.Rx, subscription.RxIndex, PatchRxRenameChannelTextBox.Text);
        });
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        DanteValidationResult validation = _project!.Validate();
        SaveSummaryTextBox.Text = _project.BuildCompatibilityReport();
        MessageBox.Show(this, validation.ToDisplayText(), "Vérification", MessageBoxButton.OK, validation.HasErrors ? MessageBoxImage.Error : MessageBoxImage.Information);
    }

    private void CompatibilityReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveSummaryTextBox.Text = _project!.BuildCompatibilityReport();
        MainTabs.SelectedIndex = 3;
        SetStatus("Rapport compatibilité Dante Controller affiché.");
    }

    private void RefreshSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSummaryTextBox.Text = _project?.BuildSaveSummary() ?? "Aucun fichier chargé.";
    }

    private void ExportTxtButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = "Rapport texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            Title = "Exporter le rapport TXT",
            FileName = BuildDefaultReportFileName(".txt"),
            InitialDirectory = Path.GetDirectoryName(_project!.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            ReportExportService.ExportText(dialog.FileName, _project!.BuildReportText());
            AddLog("Rapport TXT exporté : " + dialog.FileName);
            SetStatus("Rapport TXT exporté.");
        }
        catch (Exception ex)
        {
            ShowError("Export impossible", ex.Message);
        }
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = "Rapport PDF (*.pdf)|*.pdf|Tous les fichiers (*.*)|*.*",
            Title = "Exporter le rapport PDF",
            FileName = BuildDefaultReportFileName(".pdf"),
            InitialDirectory = Path.GetDirectoryName(_project!.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            ReportExportService.ExportPdf(dialog.FileName, "Dante Config Editor", _project!.BuildReportText());
            AddLog("Rapport PDF exporté : " + dialog.FileName);
            SetStatus("Rapport PDF exporté.");
        }
        catch (Exception ex)
        {
            ShowError("Export impossible", ex.Message);
        }
    }

    private void ExportPatchbookTxtButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = "Patchbook texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
            Title = "Exporter le patchbook TXT",
            FileName = BuildDefaultReportFileName("_patchbook.txt"),
            InitialDirectory = Path.GetDirectoryName(_project!.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            string scope = PatchbookScopeComboBox.SelectedItem as string ?? _patchbookScopes[0];
            ReportExportService.ExportText(dialog.FileName, _project!.BuildPatchbookText(scope));
            AddLog("Patchbook TXT exporté : " + dialog.FileName);
            SetStatus("Patchbook TXT exporté.");
        }
        catch (Exception ex)
        {
            ShowError("Export Patchbook impossible", ex.Message);
        }
    }

    private void ExportPatchbookCsvButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = "Patchbook CSV (*.csv)|*.csv|Tous les fichiers (*.*)|*.*",
            Title = "Exporter le patchbook CSV",
            FileName = BuildDefaultReportFileName("_patchbook.csv"),
            InitialDirectory = Path.GetDirectoryName(_project!.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            string scope = PatchbookScopeComboBox.SelectedItem as string ?? _patchbookScopes[0];
            ReportExportService.ExportText(dialog.FileName, _project!.BuildPatchbookCsv(scope), includeSignature: false);
            AddLog("Patchbook CSV exporté : " + dialog.FileName);
            SetStatus("Patchbook CSV exporté.");
        }
        catch (Exception ex)
        {
            ShowError("Export Patchbook CSV impossible", ex.Message);
        }
    }

    private void TopologyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveSummaryTextBox.Text = _project!.BuildTopologyText();
        MainTabs.SelectedIndex = 3;
        SetStatus("Topologie simple affichée.");
    }

    private void CompareXmlButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        OpenFileDialog dialog = new()
        {
            Filter = "Fichiers XML (*.xml)|*.xml|Tous les fichiers (*.*)|*.*",
            Title = "Comparer avec un autre XML"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            DanteProject otherProject = DanteProject.Load(dialog.FileName);
            SaveSummaryTextBox.Text = _project!.CompareWith(otherProject);
            MainTabs.SelectedIndex = 3;
            AddLog("Comparaison XML effectuée : " + dialog.FileName);
            SetStatus("Comparaison XML affichée.");
        }
        catch (Exception ex)
        {
            ShowError("Comparaison impossible", ex.Message);
        }
    }

    private void ThemeToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        SetTheme(useLightTheme: true);
        ThemeToggleButton.Content = "Thème sombre";
    }

    private void ThemeToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        SetTheme(useLightTheme: false);
        ThemeToggleButton.Content = "Thème clair";
    }

    private void RefreshAll()
    {
        // Point central de rafraîchissement : après une modification XML, on
        // reconstruit les listes et on conserve autant que possible la sélection.
        _refreshingUi = true;
        try
        {
            if (_project is null)
            {
                FilePathTextBlock.Text = "Aucun fichier ouvert";
                ProjectSummaryTextBlock.Text = "Chargez un fichier XML pour commencer.";
                ConfigSummaryTextBlock.Text = "Aucun fichier chargé.";
                ImportantWarningsBorder.Visibility = Visibility.Collapsed;
                ImportantWarningsTextBlock.Text = string.Empty;
                DirtyStateTextBlock.Text = "Non modifié";
                ModeTextBlock.Text = "Mode : Lecture seule";
                CountsTextBlock.Text = "0 device - 0 TX - 0 RX";
                DeviceGrid.ItemsSource = null;
                DeviceComboBox.ItemsSource = null;
                SenderDeviceList.ItemsSource = new[] { AllSendersItem };
                SenderDeviceList.SelectedItem = AllSendersItem;
                ReceiverDeviceList.ItemsSource = new[] { AllReceiversItem };
                ReceiverDeviceList.SelectedItem = AllReceiversItem;
                SourceDeviceComboBox.ItemsSource = null;
                SaveSummaryTextBox.Text = "Aucun fichier chargé.";
                HealthSummaryTextBlock.Text = "Aucun fichier chargé.";
                _searchResults.Clear();
                _patchRows.Clear();
                _healthIssues.Clear();
                UpdateCommandState();
                return;
            }

            IReadOnlyList<DanteDevice> devices = _project.Devices;
            string[] deviceNames = devices.Select(device => device.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            string selectedDevice = DeviceComboBox.SelectedItem as string ?? deviceNames.FirstOrDefault() ?? string.Empty;
            string selectedSenderFilter = SenderDeviceList.SelectedItem as string ?? AllSendersItem;
            string selectedReceiverFilter = ReceiverDeviceList.SelectedItem as string ?? AllReceiversItem;
            string selectedSourceDevice = SourceDeviceComboBox.SelectedItem as string ?? deviceNames.FirstOrDefault() ?? string.Empty;

            FilePathTextBlock.Text = _project.OriginalFilePath;
            DirtyStateTextBlock.Text = _project.IsModified ? "Modifié - non sauvegardé" : "Non modifié";
            DirtyStateTextBlock.Foreground = _project.IsModified ? Resources["DangerBrush"] as Brush : Resources["MutedTextBrush"] as Brush;
            ModeTextBlock.Text = _editModeEnabled ? "Mode : Édition" : "Mode : Lecture seule";

            int txCount = devices.Sum(device => device.TxCount);
            int rxCount = devices.Sum(device => device.RxCount);
            ProjectSummaryTextBlock.Text = $"{devices.Count} devices\n{txCount} canaux TX\n{rxCount} canaux RX\n{_project.PatchMatrix.ActivePatchCount} patchs actifs";
            ConfigSummaryTextBlock.Text = $"{Path.GetFileName(_project.OriginalFilePath)} - {devices.Count} devices, {txCount} TX, {rxCount} RX.";
            CountsTextBlock.Text = $"{devices.Count} devices - {txCount} TX - {rxCount} RX";

            string importantWarnings = string.Join(Environment.NewLine, _project.BuildImportantWarnings());
            ImportantWarningsTextBlock.Text = importantWarnings;
            ImportantWarningsBorder.Visibility = string.IsNullOrWhiteSpace(importantWarnings) ? Visibility.Collapsed : Visibility.Visible;

            DeviceGrid.ItemsSource = devices;
            DeviceComboBox.ItemsSource = deviceNames;
            DeviceComboBox.SelectedItem = deviceNames.Contains(selectedDevice) ? selectedDevice : deviceNames.FirstOrDefault();
            SelectLatency(GlobalLatencyComboBox, _latencies.First().XmlValue);

            string[] senderFilterItems = new[] { AllSendersItem }.Concat(deviceNames).ToArray();
            string[] receiverFilterItems = new[] { AllReceiversItem }.Concat(deviceNames).ToArray();
            SenderDeviceList.ItemsSource = senderFilterItems;
            SenderDeviceList.SelectedItem = deviceNames.Contains(selectedSenderFilter) ? selectedSenderFilter : AllSendersItem;
            ReceiverDeviceList.ItemsSource = receiverFilterItems;
            ReceiverDeviceList.SelectedItem = deviceNames.Contains(selectedReceiverFilter) ? selectedReceiverFilter : AllReceiversItem;
            SourceDeviceComboBox.ItemsSource = deviceNames;
            SourceDeviceComboBox.SelectedItem = deviceNames.Contains(selectedSourceDevice) ? selectedSourceDevice : deviceNames.FirstOrDefault();

            SaveSummaryTextBox.Text = _project.BuildSaveSummary();
            RefreshGlobalSearchResults();
            RefreshHealthPage();
            UpdateCommandState();
        }
        finally
        {
            _refreshingUi = false;
        }

        DeviceComboBox_SelectionChanged(DeviceComboBox, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new List<object>(), new List<object>()));
        RefreshSourceChannels();
        RefreshChannelSelector();
        RefreshPatchRows();
        ApplyPatchViewMode();
        UpdateCommandState();
    }

    private void RefreshChannelSelector()
    {
        // Les trois listes de canaux partagent les mêmes choix : le canal à
        // renommer, le début de plage et la fin de plage.
        if (_project is null)
        {
            ChannelComboBox.ItemsSource = null;
            BatchRenameStartChannelComboBox.ItemsSource = null;
            BatchRenameEndChannelComboBox.ItemsSource = null;
            NewChannelNameTextBox.Text = string.Empty;
            return;
        }

        DanteDevice? device = _project.FindDevice(DeviceComboBox.SelectedItem as string);
        if (device is null)
        {
            ChannelComboBox.ItemsSource = null;
            BatchRenameStartChannelComboBox.ItemsSource = null;
            BatchRenameEndChannelComboBox.ItemsSource = null;
            NewChannelNameTextBox.Text = string.Empty;
            return;
        }

        DanteChannelKind kind = string.Equals(ChannelKindComboBox.SelectedItem as string, "RX", StringComparison.OrdinalIgnoreCase)
            ? DanteChannelKind.Rx
            : DanteChannelKind.Tx;

        string previousKey = ChannelComboBox.SelectedItem is ChannelChoice previous
            ? $"{previous.Kind}:{previous.Index}"
            : string.Empty;
        string previousStartKey = BatchRenameStartChannelComboBox.SelectedItem is ChannelChoice previousStart
            ? $"{previousStart.Kind}:{previousStart.Index}"
            : string.Empty;
        string previousEndKey = BatchRenameEndChannelComboBox.SelectedItem is ChannelChoice previousEnd
            ? $"{previousEnd.Kind}:{previousEnd.Index}"
            : string.Empty;

        ChannelChoice[] choices = (kind == DanteChannelKind.Tx ? device.TxChannels : device.RxChannels)
            .Select(channel => new ChannelChoice(kind, channel.Index, channel.DisplayName))
            .ToArray();

        ChannelComboBox.ItemsSource = choices;
        ChannelChoice? selected = choices.FirstOrDefault(choice => $"{choice.Kind}:{choice.Index}" == previousKey) ?? choices.FirstOrDefault();
        ChannelComboBox.SelectedItem = selected;
        BatchRenameStartChannelComboBox.ItemsSource = choices;
        BatchRenameEndChannelComboBox.ItemsSource = choices;
        BatchRenameStartChannelComboBox.SelectedItem = choices.FirstOrDefault(choice => $"{choice.Kind}:{choice.Index}" == previousStartKey) ?? choices.FirstOrDefault();
        BatchRenameEndChannelComboBox.SelectedItem = choices.FirstOrDefault(choice => $"{choice.Kind}:{choice.Index}" == previousEndKey) ?? choices.LastOrDefault();
        NewChannelNameTextBox.Text = selected?.Name ?? string.Empty;
    }

    private void RefreshPatchRows()
    {
        // La table Patch est filtrée côté interface, sans modifier le projet.
        _patchRows.Clear();

        if (_project is null)
        {
            PatchSummaryTextBlock.Text = "Aucun fichier chargé.";
            return;
        }

        string search = PatchSearchTextBox.Text.Trim();
        string sender = SenderDeviceList.SelectedItem as string ?? AllSendersItem;
        string receiver = ReceiverDeviceList.SelectedItem as string ?? AllReceiversItem;
        string stateFilter = PatchStateFilterComboBox.SelectedItem as string ?? _patchStateFilters[0];
        bool conflictsOnly = ConflictsOnlyCheckBox.IsChecked == true;

        IEnumerable<DanteSubscription> subscriptions = _project.PatchMatrix.Subscriptions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            subscriptions = subscriptions.Where(subscription =>
                Contains(subscription.RxDevice, search)
                || Contains(subscription.RxChannelName, search)
                || Contains(subscription.DisplayTxDeviceName, search)
                || Contains(subscription.RawTxDeviceName, search)
                || Contains(subscription.ResolvedTxDeviceName, search)
                || Contains(subscription.TxChannelName, search));
        }

        if (!string.Equals(sender, AllSendersItem, StringComparison.OrdinalIgnoreCase))
        {
            subscriptions = subscriptions.Where(subscription => string.Equals(subscription.ResolvedTxDeviceName, sender, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(receiver, AllReceiversItem, StringComparison.OrdinalIgnoreCase))
        {
            subscriptions = subscriptions.Where(subscription => string.Equals(subscription.RxDevice, receiver, StringComparison.OrdinalIgnoreCase));
        }

        if (conflictsOnly)
        {
            subscriptions = subscriptions.Where(subscription => subscription.IsConflict);
        }

        subscriptions = stateFilter switch
        {
            "Patchs actifs" => subscriptions.Where(subscription => subscription.IsActive),
            "RX libres" => subscriptions.Where(subscription => !subscription.IsActive),
            "Patchs locaux" => subscriptions.Where(subscription => subscription.IsLocalSubscription),
            "Devices TX absents" => subscriptions.Where(subscription => subscription.IsExternalMissingDevice),
            "Canaux TX introuvables" => subscriptions.Where(subscription => subscription.IsTxChannelMissing),
            "Warnings" => subscriptions.Where(subscription => subscription.IsWarning),
            "Conflits" => subscriptions.Where(subscription => subscription.IsConflict),
            "Modifiés" => subscriptions.Where(subscription => subscription.IsModified),
            _ => subscriptions
        };

        foreach (DanteSubscription subscription in subscriptions)
        {
            _patchRows.Add(subscription);
        }

        PatchSummaryTextBlock.Text = $"{_patchRows.Count} lignes - {_project.PatchMatrix.ActivePatchCount} actifs - {_project.PatchMatrix.LocalPatchCount} locaux - {_project.PatchMatrix.WarningCount} warning(s) - {_project.PatchMatrix.ConflictCount} conflit(s)";
    }

    private void RefreshHealthPage()
    {
        _healthIssues.Clear();

        if (_project is null)
        {
            HealthSummaryTextBlock.Text = "Aucun fichier chargé.";
            return;
        }

        DanteValidationResult validation = _project.Validate();
        string filter = HealthFilterComboBox.SelectedItem as string ?? _healthFilters[0];
        IEnumerable<DanteValidationIssue> issues = validation.Issues;
        issues = filter switch
        {
            "Infos" => issues.Where(issue => issue.Severity == DanteIssueSeverity.Info),
            "Avertissements" => issues.Where(issue => issue.Severity == DanteIssueSeverity.Warning),
            "Erreurs" => issues.Where(issue => issue.Severity == DanteIssueSeverity.Error),
            "Patchs" => issues.Where(issue => issue.Category == DanteIssueCategory.Patch),
            "Devices" => issues.Where(issue => issue.Category == DanteIssueCategory.Device),
            "Clock" => issues.Where(issue => issue.Category == DanteIssueCategory.Clock),
            "Réseau" => issues.Where(issue => issue.Category == DanteIssueCategory.Network),
            "Compatibilité XML" => issues.Where(issue => issue.Category == DanteIssueCategory.XmlCompatibility),
            _ => issues
        };

        foreach (DanteValidationIssue issue in issues.OrderByDescending(issue => issue.Severity).ThenBy(issue => issue.CategoryLabel).Take(500))
        {
            _healthIssues.Add(issue);
        }

        HealthSummaryTextBlock.Text =
            $"Preset : {_project.PresetName}  |  Version : {Blank(_project.PresetVersion)}  |  Mode : {(_editModeEnabled ? "Édition" : "Lecture seule")}  |  Fichier : {_project.OriginalFilePath}\n"
            + $"Devices : {_project.Devices.Count}  |  TX : {_project.Devices.Sum(device => device.TxCount)}  |  RX : {_project.Devices.Sum(device => device.RxCount)}  |  Patchs actifs : {_project.PatchMatrix.ActivePatchCount}  |  RX libres : {_project.PatchMatrix.FreeRxCount}\n"
            + $"Patchs locaux : {_project.PatchMatrix.LocalPatchCount}  |  Devices TX absents : {_project.PatchMatrix.ExternalMissingDeviceCount}  |  Canaux TX introuvables : {_project.PatchMatrix.MissingTxChannelCount}  |  Preferred masters : {_project.Devices.Count(device => device.PreferredMaster)}\n"
            + $"Samplerates : {DistinctDeviceValues("samplerate")}  |  Encodages : {DistinctDeviceValues("encoding")}  |  Latences : {DistinctLatencies()}\n"
            + $"Redondants : {_project.Devices.Count(device => device.IsRedundant)}  |  Daisychain : {_project.Devices.Count(device => !device.IsRedundant)}  |  IP fixes détectées : {_project.Devices.Count(device => device.UsesStaticIp)}  |  Erreurs : {validation.Errors.Count}  |  Warnings : {validation.Warnings.Count}";
    }

    private void RefreshSourceChannels()
    {
        if (_project is null)
        {
            SourceChannelComboBox.ItemsSource = null;
            return;
        }

        DanteDevice? sourceDevice = _project.FindDevice(SourceDeviceComboBox.SelectedItem as string);
        object[] channels = sourceDevice?.TxChannels
            .Select(channel => (object)new TxChannelChoice(sourceDevice.Name, channel.DanteId, channel.DisplayName))
            .Prepend(string.Empty)
            .ToArray() ?? new object[] { string.Empty };
        string previous = SelectedSourceChannelName();
        SourceChannelComboBox.ItemsSource = channels;
        SourceChannelComboBox.SelectedItem = channels.OfType<TxChannelChoice>().FirstOrDefault(choice => string.Equals(choice.ChannelName, previous, StringComparison.OrdinalIgnoreCase))
            ?? channels.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(SelectedSourceChannelName()))
        {
            PatchTxRenameChannelTextBox.Text = SelectedSourceChannelName();
        }
    }

    private void RefreshGlobalSearchResults()
    {
        // Recherche simple dans les champs déjà interprétés par l'application.
        string search = GlobalSearchTextBox.Text.Trim();
        _searchResults.Clear();

        if (_project is null || search.Length < 2)
        {
            return;
        }

        foreach (DanteDevice device in _project.Devices)
        {
            if (Contains(device.Name, search) || Contains(device.FriendlyName, search))
            {
                _searchResults.Add(new GlobalSearchResult("Machine", device.Name, DeviceName: device.Name));
            }

            foreach (DanteChannel channel in device.TxChannels)
            {
                if (Contains(channel.DisplayName, search))
                {
                    _searchResults.Add(new GlobalSearchResult("Canal TX", $"{device.Name} / {channel.Index} - {channel.DisplayName}", device.Name, DanteChannelKind.Tx, channel.Index));
                }
            }

            foreach (DanteChannel channel in device.RxChannels)
            {
                if (Contains(channel.DisplayName, search))
                {
                    _searchResults.Add(new GlobalSearchResult("Canal RX", $"{device.Name} / {channel.Index} - {channel.DisplayName}", device.Name, DanteChannelKind.Rx, channel.Index));
                }
            }
        }

        foreach (DanteSubscription subscription in _project.PatchMatrix.Subscriptions)
        {
            if (Contains(subscription.RxDevice, search)
                || Contains(subscription.RxChannelName, search)
                || Contains(subscription.DisplayTxDeviceName, search)
                || Contains(subscription.RawTxDeviceName, search)
                || Contains(subscription.ResolvedTxDeviceName, search)
                || Contains(subscription.TxChannelName, search)
                || Contains(subscription.Status, search))
            {
                _searchResults.Add(new GlobalSearchResult(
                    "Patch",
                    $"{subscription.RxDevice} / {subscription.RxChannelName} -> {Blank(subscription.DisplayTxDeviceName)} / {Blank(subscription.TxChannelName)}",
                    RxDevice: subscription.RxDevice,
                    RxIndex: subscription.RxIndex));
            }
        }

        while (_searchResults.Count > 80)
        {
            _searchResults.RemoveAt(_searchResults.Count - 1);
        }
    }

    private void RefreshRecentFiles()
    {
        IReadOnlyList<string> recentFiles = RecentFilesService.Load();
        RecentFilesComboBox.ItemsSource = recentFiles;
        RecentFilesComboBox.SelectedItem = recentFiles.FirstOrDefault();
    }

    private void UpdateCommandState()
    {
        bool hasProject = _project is not null;
        bool canEdit = hasProject && _editModeEnabled;

        ActivateEditButton.IsEnabled = hasProject && !_editModeEnabled;
        SaveAsButton.IsEnabled = canEdit;
        RevertButton.IsEnabled = canEdit;
        UndoLastButton.IsEnabled = canEdit && _project?.CanUndo == true;
        UndoLastButton.Content = _project?.CanUndo == true ? "Annuler action" : "Annuler action";

        foreach (Control control in EditableControls())
        {
            control.IsEnabled = canEdit;
        }
    }

    private IEnumerable<Control> EditableControls()
    {
        yield return ApplyRenameButton;
        yield return ApplyNetworkButton;
        yield return ApplyLatencyButton;
        yield return ApplyPreferredMasterButton;
        yield return RenameChannelButton;
        yield return ResetDeviceChannelsButton;
        yield return BatchRenameButton;
        yield return ApplyAllNetworkButton;
        yield return ApplyAllLatencyButton;
        yield return ApplyAllPreferredMasterButton;
        yield return ResetAllChannelsButton;
        yield return ApplyPatchButton;
        yield return RemovePatchButton;
        yield return RenamePatchRxChannelButton;
        yield return RenamePatchTxChannelButton;
    }

    private string BuildDefaultReportFileName(string extension)
    {
        string source = _project?.OriginalFilePath ?? "rapport";
        string name = Path.GetFileNameWithoutExtension(source);
        return $"{name}_rapport_DanteConfigEditor{extension}";
    }

    private void RunProjectAction(string successMessage, Action action, string? confirmationMessage = null)
    {
        // Toutes les actions qui modifient le XML passent ici : confirmation,
        // copie d'annulation, exécution, rafraîchissement et retour arrière si erreur.
        if (!EnsureProjectLoaded())
        {
            return;
        }

        if (!EnsureEditMode())
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(confirmationMessage))
        {
            MessageBoxResult confirm = MessageBox.Show(this, confirmationMessage, "Confirmation requise", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        try
        {
            _project!.PushUndoSnapshot(successMessage);
            action();
            AddLog(successMessage);
            RefreshAll();
            SetStatus(successMessage);
        }
        catch (Exception ex)
        {
            _project?.RestoreLastUndoSnapshot();
            RefreshAll();
            ShowError("Action impossible", ex.Message);
        }
    }

    private string SelectedDeviceName()
    {
        return DeviceComboBox.SelectedItem as string ?? throw new InvalidOperationException("Aucun device sélectionné.");
    }

    private bool EnsureProjectLoaded()
    {
        if (_project is not null)
        {
            return true;
        }

        ShowError("Aucun fichier chargé", "Ouvrez d'abord un fichier XML de configuration Dante.");
        return false;
    }

    private bool EnsureEditMode()
    {
        if (_editModeEnabled)
        {
            return true;
        }

        ShowError("Mode lecture seule", "Activez l'édition avant de modifier ou sauvegarder le XML.");
        return false;
    }

    private string SelectedLatencyXmlValue(ComboBox comboBox)
    {
        return comboBox.SelectedItem is LatencyChoice latencyChoice
            ? latencyChoice.XmlValue
            : comboBox.SelectedItem as string ?? string.Empty;
    }

    private void SelectLatency(ComboBox comboBox, string xmlValue)
    {
        comboBox.SelectedItem = _latencies.FirstOrDefault(latency => string.Equals(latency.XmlValue, xmlValue, StringComparison.OrdinalIgnoreCase));
    }

    private string SelectedSourceChannelName()
    {
        return SourceChannelComboBox.SelectedItem switch
        {
            TxChannelChoice choice => choice.ChannelName,
            string value => value,
            _ => string.Empty
        };
    }

    private void SelectSourceChannel(string channelName)
    {
        if (SourceChannelComboBox.ItemsSource is IEnumerable<object> items)
        {
            SourceChannelComboBox.SelectedItem = (object?)items.OfType<TxChannelChoice>().FirstOrDefault(choice => string.Equals(choice.ChannelName, channelName, StringComparison.OrdinalIgnoreCase))
                ?? items.OfType<string>().FirstOrDefault(value => string.Equals(value, channelName, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void ApplyPatchViewMode()
    {
        bool expert = string.Equals(PatchViewModeComboBox.SelectedItem as string, "Expert", StringComparison.OrdinalIgnoreCase);
        Visibility expertVisibility = expert ? Visibility.Visible : Visibility.Collapsed;

        PatchDisplayTxColumn.Visibility = expertVisibility;
        PatchRawTxColumn.Visibility = expertVisibility;
        PatchResolvedTxColumn.Visibility = expertVisibility;
        PatchTxChannelColumn.Visibility = expertVisibility;
        PatchTypeColumn.Visibility = expertVisibility;
        PatchActiveColumn.Visibility = expertVisibility;
        PatchModifiedColumn.Visibility = expertVisibility;
        PatchSourceFullColumn.Visibility = Visibility.Visible;
    }

    private string DistinctDeviceValues(string elementName)
    {
        string[] values = _project?.Devices
            .Select(device => device.Element.Element(elementName)?.Value.Trim() ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        return values.Length == 0 ? "-" : string.Join(", ", values);
    }

    private string DistinctLatencies()
    {
        string[] values = _project?.Devices
            .Select(device => device.Latency)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(DanteLatencyFormatter.FormatLatencyDisplay)
            .ToArray() ?? [];

        return values.Length == 0 ? "-" : string.Join(", ", values);
    }

    private void ShowProjectList(string title, Func<DanteProject, string> contentFactory)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        MessageBox.Show(this, contentFactory(_project!), title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddLog(string message)
    {
        _logs.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void ShowError(string title, string message)
    {
        AddLog(title + " - " + message);
        SetStatus(title);
        MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void SetTheme(bool useLightTheme)
    {
        if (useLightTheme)
        {
            SetBrush("WindowBackgroundBrush", "#F3F6FA");
            SetBrush("SurfaceBrush", "#FFFFFF");
            SetBrush("SurfaceAltBrush", "#EAF0F7");
            SetBrush("TextBrush", "#111827");
            SetBrush("MutedTextBrush", "#4B5563");
            SetBrush("BorderLineBrush", "#CBD5E1");
            SetBrush("AccentBrush", "#1D4ED8");
            SetBrush("AccentDarkBrush", "#1E40AF");
            SetBrush("DangerBrush", "#B91C1C");
            SetBrush("SuccessBrush", "#047857");
        }
        else
        {
            SetBrush("WindowBackgroundBrush", "#10141F");
            SetBrush("SurfaceBrush", "#171D2B");
            SetBrush("SurfaceAltBrush", "#202838");
            SetBrush("TextBrush", "#F6F8FB");
            SetBrush("MutedTextBrush", "#AAB4C5");
            SetBrush("BorderLineBrush", "#334057");
            SetBrush("AccentBrush", "#2F80ED");
            SetBrush("AccentDarkBrush", "#1D5FB8");
            SetBrush("DangerBrush", "#D64545");
            SetBrush("SuccessBrush", "#2E9D62");
        }
    }

    private void SetBrush(string key, string hex)
    {
        Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    private static bool Contains(string value, string search)
    {
        return value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string Blank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(vide)" : value;
    }
}
