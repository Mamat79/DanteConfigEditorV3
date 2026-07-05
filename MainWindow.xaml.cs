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
    private readonly string[] _latencies = ["250", "1000", "2000", "5000"];
    private DanteProject? _project;

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
        PatchGrid.ItemsSource = _patchRows;
        LogListBox.ItemsSource = _logs;
        GlobalSearchListBox.ItemsSource = _searchResults;
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

    private void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
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
        LatencyComboBox.SelectedItem = string.IsNullOrWhiteSpace(device.Latency) ? null : device.Latency;
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
            string latency = LatencyComboBox.SelectedItem as string ?? string.Empty;
            _project!.SetLatency(SelectedDeviceName(), latency);
        });
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
        RunProjectAction(
            "Mode réseau appliqué à tous les devices.",
            () => _project!.SetAllNetworkModes(GlobalRedundantRadioButton.IsChecked == true),
            "Le mode réseau sera modifié pour tous les devices du fichier.");
    }

    private void ApplyAllLatencyButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            "Latence appliquée à tous les devices.",
            () =>
            {
                string latency = GlobalLatencyComboBox.SelectedItem as string ?? string.Empty;
                _project!.SetAllLatencies(latency);
            },
            "La latence unicast sera modifiée pour tous les devices du fichier.");
    }

    private void ApplyAllPreferredMasterButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            "Preferred master appliqué à tous les devices.",
            () => _project!.SetAllPreferredMasters(GlobalPreferredMasterCheckBox.IsChecked == true),
            "Le réglage preferred master sera modifié pour tous les devices du fichier.");
    }

    private void ResetAllChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            "Tous les canaux ont été réinitialisés.",
            () => _project!.ResetAllChannels(),
            "Tous les noms de canaux TX/RX seront remplacés par 1, 2, 3...");
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

        PatchTxRenameChannelTextBox.Text = SourceChannelComboBox.SelectedItem as string ?? string.Empty;
    }

    private void PatchGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(subscription.TxDevice))
        {
            SourceDeviceComboBox.SelectedItem = subscription.TxDevice;
            RefreshSourceChannels();
            SourceChannelComboBox.SelectedItem = subscription.TxChannelName;
        }

        PatchRxRenameChannelTextBox.Text = subscription.RxChannelName;
        PatchTxRenameChannelTextBox.Text = subscription.TxChannelName;
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
                string txChannel = SourceChannelComboBox.SelectedItem as string ?? string.Empty;
                _project!.ApplyPatch(subscription.RxDevice, subscription.RxIndex, txDevice, txChannel);
            });
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
        string sourceChannelName = SourceChannelComboBox.SelectedItem as string ?? string.Empty;
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
                    string.Equals(channel.DisplayName, sourceChannelName, StringComparison.OrdinalIgnoreCase)
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
        SaveSummaryTextBox.Text = _project.BuildSaveSummary();
        MessageBox.Show(this, validation.ToDisplayText(), "Vérification", MessageBoxButton.OK, validation.HasErrors ? MessageBoxImage.Error : MessageBoxImage.Information);
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
            MainTabs.SelectedIndex = 2;
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
                CountsTextBlock.Text = "0 device - 0 TX - 0 RX";
                DeviceGrid.ItemsSource = null;
                DeviceComboBox.ItemsSource = null;
                SenderDeviceList.ItemsSource = new[] { AllSendersItem };
                SenderDeviceList.SelectedItem = AllSendersItem;
                ReceiverDeviceList.ItemsSource = new[] { AllReceiversItem };
                ReceiverDeviceList.SelectedItem = AllReceiversItem;
                SourceDeviceComboBox.ItemsSource = null;
                SaveSummaryTextBox.Text = "Aucun fichier chargé.";
                _searchResults.Clear();
                _patchRows.Clear();
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
        bool conflictsOnly = ConflictsOnlyCheckBox.IsChecked == true;

        IEnumerable<DanteSubscription> subscriptions = _project.PatchMatrix.Subscriptions;

        if (!string.IsNullOrWhiteSpace(search))
        {
            subscriptions = subscriptions.Where(subscription =>
                Contains(subscription.RxDevice, search)
                || Contains(subscription.RxChannelName, search)
                || Contains(subscription.TxDevice, search)
                || Contains(subscription.TxChannelName, search));
        }

        if (!string.Equals(sender, AllSendersItem, StringComparison.OrdinalIgnoreCase))
        {
            subscriptions = subscriptions.Where(subscription => string.Equals(subscription.TxDevice, sender, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(receiver, AllReceiversItem, StringComparison.OrdinalIgnoreCase))
        {
            subscriptions = subscriptions.Where(subscription => string.Equals(subscription.RxDevice, receiver, StringComparison.OrdinalIgnoreCase));
        }

        if (conflictsOnly)
        {
            subscriptions = subscriptions.Where(subscription => subscription.Status.StartsWith("Conflit", StringComparison.OrdinalIgnoreCase));
        }

        foreach (DanteSubscription subscription in subscriptions)
        {
            _patchRows.Add(subscription);
        }

        PatchSummaryTextBlock.Text = $"{_patchRows.Count} lignes affichées - {_project.PatchMatrix.ActivePatchCount} patchs actifs - {_project.PatchMatrix.ConflictCount} conflit(s)";
    }

    private void RefreshSourceChannels()
    {
        if (_project is null)
        {
            SourceChannelComboBox.ItemsSource = null;
            return;
        }

        DanteDevice? sourceDevice = _project.FindDevice(SourceDeviceComboBox.SelectedItem as string);
        string[] channels = sourceDevice?.TxChannels.Select(channel => channel.DisplayName).Prepend(string.Empty).ToArray() ?? [string.Empty];
        string previous = SourceChannelComboBox.SelectedItem as string ?? string.Empty;
        SourceChannelComboBox.ItemsSource = channels;
        SourceChannelComboBox.SelectedItem = channels.Contains(previous) ? previous : channels.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(SourceChannelComboBox.SelectedItem as string))
        {
            PatchTxRenameChannelTextBox.Text = SourceChannelComboBox.SelectedItem as string ?? string.Empty;
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
                || Contains(subscription.TxDevice, search)
                || Contains(subscription.TxChannelName, search)
                || Contains(subscription.Status, search))
            {
                _searchResults.Add(new GlobalSearchResult(
                    "Patch",
                    $"{subscription.RxDevice} / {subscription.RxChannelName} -> {Blank(subscription.TxDevice)} / {Blank(subscription.TxChannelName)}",
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
        UndoLastButton.IsEnabled = _project?.CanUndo == true;
        UndoLastButton.Content = _project?.CanUndo == true ? "Annuler action" : "Annuler action";
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
