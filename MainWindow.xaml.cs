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
    private string AllSendersItem => T("Filter.AllSenders");
    private string AllReceiversItem => T("Filter.AllReceivers");

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
    private readonly SampleRateChoice[] _sampleRates =
    [
        new("44100", "44,1 kHz"),
        new("48000", "48 kHz"),
        new("88200", "88,2 kHz"),
        new("96000", "96 kHz"),
        new("176400", "176,4 kHz"),
        new("192000", "192 kHz")
    ];
    private readonly EncodingChoice[] _encodings =
    [
        new("16", "16 bit"),
        new("24", "24 bit"),
        new("32", "32 bit")
    ];
    private readonly string[] _patchViewModeKeys = ["PatchView.Simple", "PatchView.Expert"];
    private readonly string[] _patchStateFilterKeys =
    [
        "Filter.AllRx",
        "Filter.ActivePatches",
        "Filter.FreeRx",
        "Filter.LocalPatches",
        "Filter.MissingTxDevices",
        "Filter.MissingTxChannels",
        "Filter.Warnings",
        "Filter.Conflicts",
        "Filter.Modified"
    ];
    private readonly string[] _healthFilterKeys =
    [
        "Filter.All",
        "Filter.Info",
        "Filter.HealthWarnings",
        "Filter.Errors",
        "Filter.Patches",
        "Filter.Devices",
        "Filter.Clock",
        "Filter.Network",
        "Filter.XmlCompatibility"
    ];
    private readonly string[] _patchbookScopeKeys = ["Filter.AllRx", "Filter.ActivePatches", "Filter.WarningsConflicts"];
    private DanteProject? _project;
    private UiLanguage _language = UiLanguage.French;
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

    private sealed record SampleRateChoice(string XmlValue, string Display)
    {
        public override string ToString()
        {
            return Display;
        }
    }

    private sealed record EncodingChoice(string XmlValue, string Display)
    {
        public override string ToString()
        {
            return Display;
        }
    }

    private sealed record LocalizedOption(string Key, string Display)
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
        _language = LanguageSettingsService.Load();
        SetupLanguageComboBox();
        LatencyComboBox.ItemsSource = _latencies;
        GlobalLatencyComboBox.ItemsSource = _latencies;
        GlobalSampleRateComboBox.ItemsSource = _sampleRates;
        GlobalEncodingComboBox.ItemsSource = _encodings;
        ChannelKindComboBox.ItemsSource = new[] { "TX", "RX" };
        ChannelKindComboBox.SelectedItem = "TX";
        RefreshLocalizedOptionSources();
        PatchGrid.ItemsSource = _patchRows;
        LogListBox.ItemsSource = _logs;
        GlobalSearchListBox.ItemsSource = _searchResults;
        HealthIssuesGrid.ItemsSource = _healthIssues;
        GlobalDaisychainRadioButton.IsChecked = true;
        DaisychainRadioButton.IsChecked = true;
        SetTheme(useLightTheme: false);
        ApplyLanguageToInterface();
        RefreshRecentFiles();
        RefreshAll();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = T("Dialog.XmlFilter"),
            Title = T("Dialog.OpenXmlTitle")
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LoadProjectFromPath(dialog.FileName);
    }

    private void MergeXmlButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        OpenFileDialog dialog = new()
        {
            Filter = T("Dialog.XmlFilter"),
            Title = T("Dialog.MergeXmlTitle")
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        IReadOnlyDictionary<string, string> renameMap = new Dictionary<string, string>();
        IReadOnlyList<string> duplicateNames;
        try
        {
            duplicateNames = _project!.FindDuplicateDeviceNamesInXml(dialog.FileName);
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.OpenFailedTitle"), ex.Message);
            return;
        }

        if (duplicateNames.Count > 0)
        {
            DuplicateDeviceRenameWindow duplicateWindow = new(
                _language,
                duplicateNames,
                _project!.BuildAutomaticDuplicateRenameMap(dialog.FileName))
            {
                Owner = this
            };

            if (duplicateWindow.ShowDialog() != true || duplicateWindow.Choice == DuplicateDeviceImportChoice.Cancel)
            {
                return;
            }

            renameMap = duplicateWindow.RenameMap;
        }
        else
        {
            MessageBoxResult confirm = MessageBox.Show(this, T("Dialog.MergeXmlWarning"), T("Dialog.ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }
        }

        DanteMergeResult? mergeResult = null;
        RunProjectAction(
            T("Action.XmlMerged"),
            () => mergeResult = _project!.MergeDevicesFromXml(dialog.FileName, renameMap));

        if (mergeResult is not null)
        {
            AddLog(BuildMergeResultLog(dialog.FileName, mergeResult));
            SetStatus(BuildMergeResultStatus(mergeResult));
        }
    }

    private void OpenRecentButton_Click(object sender, RoutedEventArgs e)
    {
        if (RecentFilesComboBox.SelectedItem is not string path || string.IsNullOrWhiteSpace(path))
        {
            ShowError(T("Dialog.NoRecentFileTitle"), T("Dialog.NoRecentFileMessage"));
            return;
        }

        if (!File.Exists(path))
        {
            ShowError(T("Dialog.FileMissingTitle"), T("Dialog.FileMissingMessage"));
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
            _editModeEnabled = true;
            _logs.Clear();
            RecentFilesService.Add(path);
            RefreshRecentFiles();
            AddLog(Tf("Log.FileLoaded", path));
            RefreshAll();
            SetStatus(T("Status.FileLoaded"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.OpenFailedTitle"), ex.Message);
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
            T("Status.ActivateEditButton"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        _editModeEnabled = true;
        AddLog(T("Log.EditEnabled"));
        RefreshAll();
        SetStatus(T("Status.EditEnabled"));
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
            ShowError(T("Dialog.SaveImpossibleTitle"), validation.ToDisplayText());
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = T("Dialog.XmlFilter"),
            Title = T("Dialog.SaveXmlTitle"),
            FileName = Path.GetFileName(SafeFileService.BuildDefaultSavePath(_project.OriginalFilePath)),
            InitialDirectory = Path.GetDirectoryName(_project.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (IsOriginalProjectPath(dialog.FileName))
        {
            ShowError(
                T("Dialog.ChooseAnotherNameTitle"),
                T("Dialog.ChooseAnotherNameMessage"));
            return;
        }

        if (File.Exists(dialog.FileName))
        {
            MessageBoxResult overwrite = MessageBox.Show(
                this,
                T("Dialog.OverwriteMessage"),
                T("Dialog.ConfirmTitle"),
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
            summary + Environment.NewLine + Environment.NewLine + T("Dialog.OriginalBackupMessage"),
            T("Dialog.SaveSummaryTitle"),
            MessageBoxButton.YesNo,
            validation.HasWarnings ? MessageBoxImage.Warning : MessageBoxImage.Information);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            string backupPath = _project.SaveAs(dialog.FileName);
            AddLog(Tf("Log.OriginalBackupCreated", backupPath));
            AddLog(Tf("Log.FileSaved", dialog.FileName));
            RefreshAll();
            SetStatus(T("Status.FileSaved"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.SaveErrorTitle"), ex.Message);
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
                T("Dialog.RevertMessage"),
                T("Dialog.RevertTitle"),
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
            AddLog(T("Log.ReloadOriginal"));
            RefreshAll();
            SetStatus(T("Log.ReloadOriginal"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.ReloadErrorTitle"), ex.Message);
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
            AddLog(Tf("Log.ActionUndone", label));
            RefreshAll();
            SetStatus(T("Status.LastActionUndone"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.UndoErrorTitle"), ex.Message);
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
        RunProjectAction(T("Action.DeviceRenamed"), () =>
        {
            string oldName = SelectedDeviceName();
            string newName = NewNameTextBox.Text;
            _project!.RenameDevice(oldName, newName);
        });
    }

    private void ApplyNetworkButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(T("Action.NetworkModeUpdated"), () =>
        {
            _project!.SetNetworkMode(SelectedDeviceName(), RedundantRadioButton.IsChecked == true);
        });
    }

    private void ApplyLatencyButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(T("Action.LatencyUpdated"), () =>
        {
            string latency = SelectedLatencyXmlValue(LatencyComboBox);
            _project!.SetLatency(SelectedDeviceName(), latency);
        },
        T("Dialog.LatencyWarning"));
    }

    private void ApplyIpAutoButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            T("Action.IpAutoApplied"),
            () => _project!.SetIpAddressDynamic(SelectedDeviceName()));
    }

    private void ResetDevicePatchesButton_Click(object sender, RoutedEventArgs e)
    {
        string deviceName = SelectedDeviceName();
        RunProjectAction(
            T("Action.DevicePatchesReset"),
            () => _project!.ResetDevicePatches(deviceName),
            Tf("Dialog.ResetDevicePatchesWarning", deviceName));
    }

    private void ApplyPreferredMasterButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(T("Action.PreferredMasterUpdated"), () =>
        {
            _project!.SetPreferredMaster(SelectedDeviceName(), PreferredMasterCheckBox.IsChecked == true);
        });
    }

    private void DeleteDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        string deviceName = SelectedDeviceName();
        RunProjectAction(
            T("Action.DeviceDeleted"),
            () => _project!.DeleteDevice(deviceName),
            Tf("Dialog.DeleteDeviceWarning", deviceName));
    }

    private void ResetDeviceChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            T("Action.ChannelsReset"),
            () => _project!.ResetChannels(SelectedDeviceName()),
            T("Dialog.ResetDeviceChannelsWarning"));
    }

    private void RenameChannelButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChannelComboBox.SelectedItem is not ChannelChoice channel)
        {
            ShowError(T("Dialog.NoChannelTitle"), T("Dialog.NoChannelMessage"));
            return;
        }

        RunProjectAction(T("Action.ChannelRenamed"), () =>
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
            ShowError(T("Dialog.InvalidRangeTitle"), T("Dialog.InvalidRangeMessage"));
            return;
        }

        if (startChannel.Kind != kind || endChannel.Kind != kind || startChannel.Index > endChannel.Index)
        {
            ShowError(T("Dialog.InvalidRangeTitle"), T("Dialog.InvalidRangeOrderMessage"));
            return;
        }

        if (!int.TryParse(BatchRenameStartTextBox.Text.Trim(), out int firstNumber))
        {
            ShowError(T("Dialog.InvalidNumberTitle"), T("Dialog.InvalidNumberMessage"));
            return;
        }

        RunProjectAction(
            T("Action.BatchRenameApplied"),
            () => _project!.BatchRenameChannels(SelectedDeviceName(), kind, BatchRenamePrefixTextBox.Text, firstNumber, startChannel.Index, endChannel.Index),
            Tf("Dialog.BatchRenameWarning", kind, startChannel.Index, endChannel.Index));
    }

    private void ApplyAllNetworkButton_Click(object sender, RoutedEventArgs e)
    {
        bool redundant = GlobalRedundantRadioButton.IsChecked == true;
        RunProjectAction(
            T("Action.AllNetworkModesApplied"),
            () => _project!.SetAllNetworkModes(redundant),
            _project?.BuildAllNetworkModePreview(redundant) + Environment.NewLine + T("Dialog.Continue"));
    }

    private void ApplyAllLatencyButton_Click(object sender, RoutedEventArgs e)
    {
        string latency = SelectedLatencyXmlValue(GlobalLatencyComboBox);
        RunProjectAction(
            T("Action.AllLatenciesApplied"),
            () =>
            {
                _project!.SetAllLatencies(latency);
            },
            _project?.BuildAllLatencyPreview(latency)
                + Environment.NewLine
                + T("Dialog.LatencyWarningContinue"));
    }

    private void ApplyAllSampleRateButton_Click(object sender, RoutedEventArgs e)
    {
        string samplerate = SelectedSampleRateXmlValue(GlobalSampleRateComboBox);
        RunProjectAction(
            T("Action.AllSampleRatesApplied"),
            () => _project!.SetAllSamplerates(samplerate),
            _project?.BuildAllSampleratePreview(samplerate)
                + Environment.NewLine
                + T("Dialog.AudioFormatWarningContinue"));
    }

    private void ApplyAllEncodingButton_Click(object sender, RoutedEventArgs e)
    {
        string encoding = SelectedEncodingXmlValue(GlobalEncodingComboBox);
        RunProjectAction(
            T("Action.AllEncodingsApplied"),
            () => _project!.SetAllEncodings(encoding),
            _project?.BuildAllEncodingPreview(encoding)
                + Environment.NewLine
                + T("Dialog.AudioFormatWarningContinue"));
    }

    private void ApplyAllIpAutoButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            T("Action.AllIpAutoApplied"),
            () => _project!.SetAllIpAddressesDynamic(),
            _project?.BuildAllIpAutoPreview() + Environment.NewLine + T("Dialog.Continue"));
    }

    private void ApplyAllIpStaticButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(GlobalIpStartTextBox.Text.Trim(), out int startHost))
        {
            ShowError(T("Dialog.InvalidNumberTitle"), T("Dialog.InvalidNumberMessage"));
            return;
        }

        string preview;
        try
        {
            preview = _project?.BuildAllStaticIpPreview(
                GlobalIpPrefixTextBox.Text,
                startHost,
                GlobalIpNetmaskTextBox.Text,
                GlobalIpGatewayTextBox.Text) ?? string.Empty;
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.ActionImpossibleTitle"), ex.Message);
            return;
        }

        RunProjectAction(
            T("Action.AllIpStaticApplied"),
            () => _project!.SetAllIpAddressesStaticSequential(
                GlobalIpPrefixTextBox.Text,
                startHost,
                GlobalIpNetmaskTextBox.Text,
                GlobalIpGatewayTextBox.Text),
            preview + Environment.NewLine + T("Dialog.IpStaticWarningContinue"));
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
                T("Action.SolePreferredMasterApplied"),
                () => _project!.SetSolePreferredMaster(deviceName),
                _project?.BuildSolePreferredMasterPreview(deviceName) + Environment.NewLine + T("Dialog.Continue"));
            return;
        }

        RunProjectAction(
            T("Action.AllPreferredMastersCleared"),
            () => _project!.SetAllPreferredMasters(false),
            _project?.BuildClearPreferredMastersPreview() + Environment.NewLine + T("Dialog.Continue"));
    }

    private void ResetAllChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        RunProjectAction(
            T("Action.AllChannelsReset"),
            () => _project!.ResetAllChannels(),
            _project?.BuildResetAllChannelsPreview() + Environment.NewLine + T("Dialog.Continue"));
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

    private void ListSampleRatesButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("Sample rates", project => project.ListSamplerates());
    }

    private void ListEncodingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("Bits par échantillon", project => project.ListEncodings());
    }

    private void ListStaticIpsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowProjectList("IP fixes", project => project.ListStaticIpDevices());
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

    private void DeviceGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DeviceGrid.SelectedItem is not DanteDevice device)
        {
            return;
        }

        OpenDeviceDetailsWindow(device.Name);
    }

    private void DevicePreferredMasterCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not DanteDevice device)
        {
            return;
        }

        RunProjectAction(
            T("Action.PreferredMasterUpdated"),
            () => _project!.SetPreferredMaster(device.Name, checkBox.IsChecked == true));
    }

    private void OpenDeviceDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenDeviceDetailsWindow(SelectedDeviceName());
    }

    private void OpenDeviceDetailsWindow(string deviceName)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        DanteDevice device = _project!.FindDevice(deviceName) ?? throw new InvalidOperationException("Device introuvable.");
        DeviceDetailsWindow window = new(_language, device)
        {
            Owner = this
        };

        if (window.ShowDialog() != true || window.Result is null)
        {
            return;
        }

        RunProjectAction(
            T("Action.DeviceDetailsUpdated"),
            () => ApplyDeviceDetails(window.OriginalDeviceName, window.Result),
            T("Dialog.DeviceDetailsWarning"));
    }

    private void ApplyDeviceDetails(string originalDeviceName, DeviceDetailsResult result)
    {
        DanteDevice originalDevice = _project!.FindDevice(originalDeviceName) ?? throw new InvalidOperationException("Device introuvable.");
        string currentName = originalDevice.Name;

        if (!string.Equals(currentName, result.DeviceName, StringComparison.Ordinal))
        {
            _project.RenameDevice(currentName, result.DeviceName);
            currentName = result.DeviceName;
        }

        if (originalDevice.IsRedundant != result.IsRedundant)
        {
            _project.SetNetworkMode(currentName, result.IsRedundant);
        }

        if (!string.Equals(originalDevice.Latency, result.Latency, StringComparison.OrdinalIgnoreCase))
        {
            _project.SetLatency(currentName, result.Latency);
        }

        if (!string.Equals(originalDevice.Samplerate, result.Samplerate, StringComparison.OrdinalIgnoreCase))
        {
            _project.SetSamplerate(currentName, result.Samplerate);
        }

        if (!string.Equals(originalDevice.Encoding, result.Encoding, StringComparison.OrdinalIgnoreCase))
        {
            _project.SetEncoding(currentName, result.Encoding);
        }

        if (originalDevice.PreferredMaster != result.PreferredMaster)
        {
            _project.SetPreferredMaster(currentName, result.PreferredMaster);
        }

        if (result.UsesStaticIp)
        {
            if (!originalDevice.UsesStaticIp
                || !string.Equals(originalDevice.StaticIpAddress, result.StaticIpAddress, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(originalDevice.StaticIpNetmask, result.StaticIpNetmask, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(originalDevice.StaticIpGateway, result.StaticIpGateway, StringComparison.OrdinalIgnoreCase))
            {
                _project.SetIpAddressStatic(currentName, result.StaticIpAddress, result.StaticIpNetmask, result.StaticIpGateway);
            }
        }
        else if (originalDevice.UsesStaticIp)
        {
            _project.SetIpAddressDynamic(currentName);
        }

        foreach (DeviceChannelEdit channel in result.TxChannels)
        {
            DanteDevice? currentDevice = _project.FindDevice(currentName);
            DanteChannel? currentChannel = currentDevice?.TxChannels.FirstOrDefault(candidate => candidate.Index == channel.Index);
            if (currentChannel is not null && !string.Equals(currentChannel.DisplayName, channel.Name, StringComparison.Ordinal))
            {
                _project.RenameChannel(currentName, DanteChannelKind.Tx, channel.Index, channel.Name);
            }
        }

        foreach (DeviceChannelEdit channel in result.RxChannels)
        {
            DanteDevice? currentDevice = _project.FindDevice(currentName);
            DanteChannel? currentChannel = currentDevice?.RxChannels.FirstOrDefault(candidate => candidate.Index == channel.Index);
            if (currentChannel is not null && !string.Equals(currentChannel.DisplayName, channel.Name, StringComparison.Ordinal))
            {
                _project.RenameChannel(currentName, DanteChannelKind.Rx, channel.Index, channel.Name);
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
            SetStatus(T("Dialog.ExternalPatchStatus"));
        }
    }

    private void ApplyPatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            ShowError(T("Dialog.NoRxTitle"), T("Dialog.NoRxMessage"));
            return;
        }

        RunProjectAction(
            T("Action.PatchApplied"),
            () =>
            {
                string txDevice = SourceDeviceComboBox.SelectedItem as string ?? string.Empty;
                string txChannel = SelectedSourceChannelName();
                _project!.ApplyPatch(subscription.RxDevice, subscription.RxIndex, txDevice, txChannel);
            },
            subscription.IsExternalMissingDevice
                ? T("Dialog.ExternalPatchWarning")
                : null);
    }

    private void RemovePatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatchGrid.SelectedItem is not DanteSubscription subscription)
        {
            ShowError(T("Dialog.NoRxTitle"), T("Dialog.NoRxMessage"));
            return;
        }

        RunProjectAction(
            T("Action.PatchRemoved"),
            () => _project!.RemovePatch(subscription.RxDevice, subscription.RxIndex),
            T("Dialog.RemovePatchWarning"));
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
            ShowError(T("Dialog.MissingTxTitle"), T("Dialog.MissingTxMessage"));
            return;
        }

        RunProjectAction(T("Action.TxChannelRenamed"), () =>
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
            ShowError(T("Dialog.NoRxTitle"), T("Dialog.NoRxLineMessage"));
            return;
        }

        RunProjectAction(T("Action.RxChannelRenamed"), () =>
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
        MessageBox.Show(this, validation.ToDisplayText(), LocalizeLiteral("Vérification"), MessageBoxButton.OK, validation.HasErrors ? MessageBoxImage.Error : MessageBoxImage.Information);
    }

    private void CompatibilityReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveSummaryTextBox.Text = _project!.BuildCompatibilityReport();
        MainTabs.SelectedIndex = 3;
        SetStatus(LocalizeLiteral("Rapport compatibilité Dante Controller affiché."));
    }

    private void RefreshSummaryButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSummaryTextBox.Text = _project?.BuildSaveSummary() ?? T("Status.NoFileLoaded");
    }

    private void ExportTxtButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProjectLoaded())
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            Filter = T("Dialog.TxtFilter"),
            Title = T("Dialog.ExportTxtTitle"),
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
            AddLog(Tf("Log.TxtExported", dialog.FileName));
            SetStatus(T("Status.TxtExported"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.ExportImpossibleTitle"), ex.Message);
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
            Filter = T("Dialog.PdfFilter"),
            Title = T("Dialog.ExportPdfTitle"),
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
            AddLog(Tf("Log.PdfExported", dialog.FileName));
            SetStatus(T("Status.PdfExported"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.ExportImpossibleTitle"), ex.Message);
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
            Filter = T("Dialog.PatchbookTxtFilter"),
            Title = T("Dialog.ExportPatchbookTxtTitle"),
            FileName = BuildDefaultReportFileName("_patchbook.txt"),
            InitialDirectory = Path.GetDirectoryName(_project!.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            string scope = SelectedOptionKey(PatchbookScopeComboBox, _patchbookScopeKeys[0]);
            string scopeDisplay = SelectedOptionDisplay(PatchbookScopeComboBox, T(_patchbookScopeKeys[0]));
            ReportExportService.ExportText(dialog.FileName, _project!.BuildPatchbookText(scope, scopeDisplay));
            AddLog(Tf("Log.PatchbookTxtExported", dialog.FileName));
            SetStatus(T("Status.PatchbookTxtExported"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.ExportPatchbookImpossibleTitle"), ex.Message);
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
            Filter = T("Dialog.PatchbookCsvFilter"),
            Title = T("Dialog.ExportPatchbookCsvTitle"),
            FileName = BuildDefaultReportFileName("_patchbook.csv"),
            InitialDirectory = Path.GetDirectoryName(_project!.OriginalFilePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            string scope = SelectedOptionKey(PatchbookScopeComboBox, _patchbookScopeKeys[0]);
            ReportExportService.ExportText(dialog.FileName, _project!.BuildPatchbookCsv(scope), includeSignature: false);
            AddLog(Tf("Log.PatchbookCsvExported", dialog.FileName));
            SetStatus(T("Status.PatchbookCsvExported"));
        }
        catch (Exception ex)
        {
            ShowError(T("Dialog.ExportPatchbookCsvImpossibleTitle"), ex.Message);
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
        SetStatus(T("Status.TopologyDisplayed"));
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
        ThemeToggleButton.Content = LocalizeLiteral("Thème sombre");
    }

    private void ThemeToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        SetTheme(useLightTheme: false);
        ThemeToggleButton.Content = LocalizeLiteral("Thème clair");
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
                FilePathTextBlock.Text = T("Status.NoFileOpen");
                ProjectSummaryTextBlock.Text = T("Status.LoadXmlToStart");
                ConfigSummaryTextBlock.Text = T("Status.NoFileLoaded");
                ImportantWarningsBorder.Visibility = Visibility.Collapsed;
                ImportantWarningsTextBlock.Text = string.Empty;
                DirtyStateTextBlock.Text = T("Status.Unmodified");
                ModeTextBlock.Text = T("Status.ReadOnlyMode");
                CountsTextBlock.Text = "0 device - 0 TX - 0 RX";
                DeviceGrid.ItemsSource = null;
                DeviceComboBox.ItemsSource = null;
                SenderDeviceList.ItemsSource = new[] { AllSendersItem };
                SenderDeviceList.SelectedItem = AllSendersItem;
                ReceiverDeviceList.ItemsSource = new[] { AllReceiversItem };
                ReceiverDeviceList.SelectedItem = AllReceiversItem;
                SourceDeviceComboBox.ItemsSource = null;
                SaveSummaryTextBox.Text = T("Status.NoFileLoaded");
                HealthSummaryTextBlock.Text = T("Status.NoFileLoaded");
                _searchResults.Clear();
                UpdateGlobalSearchHint(T("Search.NoFileLoaded"));
                _patchRows.Clear();
                _healthIssues.Clear();
                UpdateCommandState();
                return;
            }

            IReadOnlyList<DanteDevice> devices = _project.Devices;
            string[] deviceNames = devices.Select(device => device.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            string selectedDevice = DeviceComboBox.SelectedItem as string ?? deviceNames.FirstOrDefault() ?? string.Empty;
            string selectedSenderFilter = SenderDeviceList.SelectedItem as string ?? string.Empty;
            string selectedReceiverFilter = ReceiverDeviceList.SelectedItem as string ?? string.Empty;
            string selectedSourceDevice = SourceDeviceComboBox.SelectedItem as string ?? deviceNames.FirstOrDefault() ?? string.Empty;

            FilePathTextBlock.Text = _project.OriginalFilePath;
            DirtyStateTextBlock.Text = _project.IsModified ? T("Status.ModifiedUnsaved") : T("Status.Unmodified");
            DirtyStateTextBlock.Foreground = _project.IsModified ? Resources["DangerBrush"] as Brush : Resources["MutedTextBrush"] as Brush;
            ModeTextBlock.Text = _editModeEnabled ? T("Status.EditMode") : T("Status.ReadOnlyMode");

            int txCount = devices.Sum(device => device.TxCount);
            int rxCount = devices.Sum(device => device.RxCount);
            ProjectSummaryTextBlock.Text = _language == UiLanguage.English
                ? $"{devices.Count} devices\n{txCount} TX channels\n{rxCount} RX channels\n{_project.PatchMatrix.ActivePatchCount} active subscriptions"
                : $"{devices.Count} devices\n{txCount} canaux TX\n{rxCount} canaux RX\n{_project.PatchMatrix.ActivePatchCount} patchs actifs";
            ConfigSummaryTextBlock.Text = $"{Path.GetFileName(_project.OriginalFilePath)} - {devices.Count} devices, {txCount} TX, {rxCount} RX.";
            CountsTextBlock.Text = $"{devices.Count} devices - {txCount} TX - {rxCount} RX";

            string importantWarnings = string.Join(Environment.NewLine, _project.BuildImportantWarnings());
            ImportantWarningsTextBlock.Text = importantWarnings;
            ImportantWarningsBorder.Visibility = string.IsNullOrWhiteSpace(importantWarnings) ? Visibility.Collapsed : Visibility.Visible;

            DeviceGrid.ItemsSource = devices;
            DeviceComboBox.ItemsSource = deviceNames;
            DeviceComboBox.SelectedItem = deviceNames.Contains(selectedDevice) ? selectedDevice : deviceNames.FirstOrDefault();
            SelectLatency(GlobalLatencyComboBox, _latencies.First().XmlValue);
            SelectSampleRate(GlobalSampleRateComboBox, devices.Select(device => device.Samplerate).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? _sampleRates.First().XmlValue);
            SelectEncoding(GlobalEncodingComboBox, devices.Select(device => device.Encoding).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? _encodings.First().XmlValue);

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
            PatchSummaryTextBlock.Text = T("Status.NoFileLoaded");
            return;
        }

        string search = PatchSearchTextBox.Text.Trim();
        string sender = SenderDeviceList.SelectedItem as string ?? AllSendersItem;
        string receiver = ReceiverDeviceList.SelectedItem as string ?? AllReceiversItem;
        string stateFilter = SelectedOptionKey(PatchStateFilterComboBox, _patchStateFilterKeys[0]);
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
            "Filter.ActivePatches" => subscriptions.Where(subscription => subscription.IsActive),
            "Filter.FreeRx" => subscriptions.Where(subscription => !subscription.IsActive),
            "Filter.LocalPatches" => subscriptions.Where(subscription => subscription.IsLocalSubscription),
            "Filter.MissingTxDevices" => subscriptions.Where(subscription => subscription.IsExternalMissingDevice),
            "Filter.MissingTxChannels" => subscriptions.Where(subscription => subscription.IsTxChannelMissing),
            "Filter.Warnings" => subscriptions.Where(subscription => subscription.IsWarning),
            "Filter.Conflicts" => subscriptions.Where(subscription => subscription.IsConflict),
            "Filter.Modified" => subscriptions.Where(subscription => subscription.IsModified),
            _ => subscriptions
        };

        foreach (DanteSubscription subscription in subscriptions)
        {
            _patchRows.Add(subscription);
        }

        PatchSummaryTextBlock.Text = Tf(
            "Summary.PatchRows",
            _patchRows.Count,
            _project.PatchMatrix.ActivePatchCount,
            _project.PatchMatrix.LocalPatchCount,
            _project.PatchMatrix.WarningCount,
            _project.PatchMatrix.ConflictCount);
    }

    private void RefreshHealthPage()
    {
        _healthIssues.Clear();

        if (_project is null)
        {
            HealthSummaryTextBlock.Text = T("Status.NoFileLoaded");
            return;
        }

        DanteValidationResult validation = _project.Validate();
        string filter = SelectedOptionKey(HealthFilterComboBox, _healthFilterKeys[0]);
        IEnumerable<DanteValidationIssue> issues = validation.Issues;
        issues = filter switch
        {
            "Filter.Info" => issues.Where(issue => issue.Severity == DanteIssueSeverity.Info),
            "Filter.HealthWarnings" => issues.Where(issue => issue.Severity == DanteIssueSeverity.Warning),
            "Filter.Errors" => issues.Where(issue => issue.Severity == DanteIssueSeverity.Error),
            "Filter.Patches" => issues.Where(issue => issue.Category == DanteIssueCategory.Patch),
            "Filter.Devices" => issues.Where(issue => issue.Category == DanteIssueCategory.Device),
            "Filter.Clock" => issues.Where(issue => issue.Category == DanteIssueCategory.Clock),
            "Filter.Network" => issues.Where(issue => issue.Category == DanteIssueCategory.Network),
            "Filter.XmlCompatibility" => issues.Where(issue => issue.Category == DanteIssueCategory.XmlCompatibility),
            _ => issues
        };

        foreach (DanteValidationIssue issue in issues.OrderByDescending(issue => issue.Severity).ThenBy(issue => issue.CategoryLabel).Take(500))
        {
            _healthIssues.Add(issue);
        }

        HealthSummaryTextBlock.Text = Tf(
            "Summary.Health",
            _project.PresetName,
            Blank(_project.PresetVersion),
            _editModeEnabled ? T("Status.EditMode").Replace("Mode : ", string.Empty).Replace("Mode: ", string.Empty) : T("Status.ReadOnlyMode").Replace("Mode : ", string.Empty).Replace("Mode: ", string.Empty),
            _project.OriginalFilePath,
            _project.Devices.Count,
            _project.Devices.Sum(device => device.TxCount),
            _project.Devices.Sum(device => device.RxCount),
            _project.PatchMatrix.ActivePatchCount,
            _project.PatchMatrix.FreeRxCount,
            _project.PatchMatrix.LocalPatchCount,
            _project.PatchMatrix.ExternalMissingDeviceCount,
            _project.PatchMatrix.MissingTxChannelCount,
            _project.Devices.Count(device => device.PreferredMaster),
            DistinctDeviceValues("samplerate"),
            DistinctDeviceValues("encoding"),
            DistinctLatencies(),
            _project.Devices.Count(device => device.IsRedundant),
            _project.Devices.Count(device => !device.IsRedundant),
            _project.Devices.Count(device => device.UsesStaticIp),
            validation.Errors.Count,
            validation.Warnings.Count);
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

        if (_project is null)
        {
            UpdateGlobalSearchHint(T("Search.NoFileLoaded"));
            return;
        }

        if (search.Length < 2)
        {
            UpdateGlobalSearchHint(T("Search.Hint"));
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

        UpdateGlobalSearchHint(_searchResults.Count == 0 ? T("Search.NoResult") : string.Empty);
    }

    private void UpdateGlobalSearchHint(string message)
    {
        GlobalSearchHintTextBlock.Text = message;
        GlobalSearchHintTextBlock.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RefreshRecentFiles()
    {
        IReadOnlyList<string> recentFiles = RecentFilesService.Load();
        RecentFilesComboBox.ItemsSource = recentFiles;
        RecentFilesComboBox.SelectedItem = recentFiles.FirstOrDefault();
    }

    private void SetupLanguageComboBox()
    {
        bool previousRefreshing = _refreshingUi;
        _refreshingUi = true;
        try
        {
            LocalizedOption[] languages =
            [
                new("Language.French", T("Language.French")),
                new("Language.English", T("Language.English"))
            ];

            string selectedKey = _language == UiLanguage.English ? "Language.English" : "Language.French";
            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.SelectedItem = languages.First(option => option.Key == selectedKey);
        }
        finally
        {
            _refreshingUi = previousRefreshing;
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshingUi || LanguageComboBox.SelectedItem is not LocalizedOption option)
        {
            return;
        }

        _language = option.Key == "Language.English" ? UiLanguage.English : UiLanguage.French;
        LanguageSettingsService.Save(_language);
        SetupLanguageComboBox();
        RefreshLocalizedOptionSources();
        ApplyLanguageToInterface();
        RefreshAll();
        SetStatus(T("Status.Ready"));
    }

    private void RefreshLocalizedOptionSources()
    {
        bool previousRefreshing = _refreshingUi;
        _refreshingUi = true;
        try
        {
            SetOptions(PatchStateFilterComboBox, _patchStateFilterKeys, SelectedOptionKey(PatchStateFilterComboBox, _patchStateFilterKeys[0]));
            SetOptions(PatchViewModeComboBox, _patchViewModeKeys, SelectedOptionKey(PatchViewModeComboBox, _patchViewModeKeys[0]));
            SetOptions(HealthFilterComboBox, _healthFilterKeys, SelectedOptionKey(HealthFilterComboBox, _healthFilterKeys[0]));
            SetOptions(PatchbookScopeComboBox, _patchbookScopeKeys, SelectedOptionKey(PatchbookScopeComboBox, _patchbookScopeKeys[0]));
        }
        finally
        {
            _refreshingUi = previousRefreshing;
        }
    }

    private void SetOptions(ComboBox comboBox, IEnumerable<string> keys, string selectedKey)
    {
        LocalizedOption[] options = keys.Select(key => new LocalizedOption(key, T(key))).ToArray();
        comboBox.ItemsSource = options;
        comboBox.SelectedItem = options.FirstOrDefault(option => option.Key == selectedKey) ?? options.FirstOrDefault();
    }

    private string SelectedOptionKey(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is LocalizedOption option ? option.Key : fallback;
    }

    private string SelectedOptionDisplay(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is LocalizedOption option ? option.Display : fallback;
    }

    private void ApplyLanguageToInterface()
    {
        LanguageLabelTextBlock.Text = T("Language.Label");
        TranslateDependencyObject(this, []);
        ApplyDataGridColumnHeaders();
        RefreshGlobalSearchResults();
        UpdateCommandState();
    }

    private void TranslateDependencyObject(DependencyObject dependencyObject, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(dependencyObject))
        {
            return;
        }

        switch (dependencyObject)
        {
            case HeaderedContentControl headeredContentControl when headeredContentControl.Header is string header:
                headeredContentControl.Header = LocalizeLiteral(header);
                break;
            case ContentControl contentControl when contentControl.Content is string content:
                contentControl.Content = LocalizeLiteral(content);
                break;
            case TextBlock textBlock:
                textBlock.Text = LocalizeLiteral(textBlock.Text);
                break;
            case FrameworkElement frameworkElement when frameworkElement.ToolTip is string tooltip:
                frameworkElement.ToolTip = LocalizeLiteral(tooltip);
                break;
        }

        foreach (object child in LogicalTreeHelper.GetChildren(dependencyObject))
        {
            if (child is DependencyObject childObject)
            {
                TranslateDependencyObject(childObject, visited);
            }
        }
    }

    private void ApplyDataGridColumnHeaders()
    {
        foreach (DataGrid dataGrid in new[] { DeviceGrid, PatchGrid, HealthIssuesGrid })
        {
            foreach (DataGridColumn column in dataGrid.Columns)
            {
                if (column.Header is string header)
                {
                    column.Header = LocalizeLiteral(header);
                }
            }
        }
    }

    private void UpdateCommandState()
    {
        bool hasProject = _project is not null;
        bool canUseProjectActions = hasProject;

        ActivateEditButton.IsEnabled = hasProject && !_editModeEnabled;
        ActivateEditButton.Content = hasProject && _editModeEnabled ? T("Status.EditActiveButton") : T("Status.ActivateEditButton");
        SaveAsButton.IsEnabled = hasProject;
        RevertButton.IsEnabled = hasProject;
        UndoLastButton.IsEnabled = hasProject && _project?.CanUndo == true;
        UndoLastButton.Content = LocalizeLiteral("Annuler action");

        foreach (Control control in EditableControls())
        {
            control.IsEnabled = canUseProjectActions;
        }
    }

    private IEnumerable<Control> EditableControls()
    {
        yield return MergeXmlButton;
        yield return ApplyRenameButton;
        yield return DeleteDeviceButton;
        yield return ApplyNetworkButton;
        yield return ApplyLatencyButton;
        yield return ApplyIpAutoButton;
        yield return ResetDevicePatchesButton;
        yield return OpenDeviceDetailsButton;
        yield return ApplyPreferredMasterButton;
        yield return RenameChannelButton;
        yield return ResetDeviceChannelsButton;
        yield return BatchRenameButton;
        yield return ApplyAllNetworkButton;
        yield return ApplyAllLatencyButton;
        yield return ApplyAllSampleRateButton;
        yield return ApplyAllEncodingButton;
        yield return ApplyAllIpAutoButton;
        yield return ApplyAllIpStaticButton;
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

    private string BuildMergeResultLog(string path, DanteMergeResult result)
    {
        string details = _language == UiLanguage.English
            ? $"XML import: {Path.GetFileName(path)} - {result.ImportedDeviceCount} device(s) imported, {result.RenamedDeviceCount} renamed, {result.SkippedDuplicateDeviceCount} duplicate(s) skipped."
            : $"Import XML : {Path.GetFileName(path)} - {result.ImportedDeviceCount} machine(s) importée(s), {result.RenamedDeviceCount} renommée(s), {result.SkippedDuplicateDeviceCount} doublon(s) ignoré(s).";

        if (result.RenamedDevices.Count > 0)
        {
            details += " " + string.Join(", ", result.RenamedDevices.Select(item => $"{item.Key} -> {item.Value}"));
        }

        return details;
    }

    private string BuildMergeResultStatus(DanteMergeResult result)
    {
        return _language == UiLanguage.English
            ? $"XML added: {result.ImportedDeviceCount} imported, {result.RenamedDeviceCount} renamed, {result.SkippedDuplicateDeviceCount} skipped."
            : $"XML ajouté : {result.ImportedDeviceCount} importée(s), {result.RenamedDeviceCount} renommée(s), {result.SkippedDuplicateDeviceCount} ignorée(s).";
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

        _editModeEnabled = true;
        AddLog("Mode édition activé automatiquement.");
        UpdateCommandState();
        SetStatus("Mode édition activé.");
        return true;
    }

    private bool IsOriginalProjectPath(string candidatePath)
    {
        if (_project is null || string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        string originalPath = Path.GetFullPath(_project.OriginalFilePath);
        string selectedPath = Path.GetFullPath(candidatePath);
        return string.Equals(originalPath, selectedPath, StringComparison.OrdinalIgnoreCase);
    }

    private string SelectedLatencyXmlValue(ComboBox comboBox)
    {
        return comboBox.SelectedItem is LatencyChoice latencyChoice
            ? latencyChoice.XmlValue
            : comboBox.SelectedItem as string ?? string.Empty;
    }

    private string SelectedSampleRateXmlValue(ComboBox comboBox)
    {
        return comboBox.SelectedItem is SampleRateChoice sampleRateChoice
            ? sampleRateChoice.XmlValue
            : !string.IsNullOrWhiteSpace(comboBox.Text)
                ? comboBox.Text
                : comboBox.SelectedItem as string ?? string.Empty;
    }

    private string SelectedEncodingXmlValue(ComboBox comboBox)
    {
        return comboBox.SelectedItem is EncodingChoice encodingChoice
            ? encodingChoice.XmlValue
            : !string.IsNullOrWhiteSpace(comboBox.Text)
                ? comboBox.Text
                : comboBox.SelectedItem as string ?? string.Empty;
    }

    private void SelectLatency(ComboBox comboBox, string xmlValue)
    {
        comboBox.SelectedItem = _latencies.FirstOrDefault(latency => string.Equals(latency.XmlValue, xmlValue, StringComparison.OrdinalIgnoreCase));
    }

    private void SelectSampleRate(ComboBox comboBox, string xmlValue)
    {
        SampleRateChoice? choice = _sampleRates.FirstOrDefault(sampleRate => string.Equals(sampleRate.XmlValue, xmlValue, StringComparison.OrdinalIgnoreCase));
        comboBox.SelectedItem = choice;
        comboBox.Text = choice?.Display ?? xmlValue;
    }

    private void SelectEncoding(ComboBox comboBox, string xmlValue)
    {
        EncodingChoice? choice = _encodings.FirstOrDefault(encoding => string.Equals(encoding.XmlValue, xmlValue, StringComparison.OrdinalIgnoreCase));
        comboBox.SelectedItem = choice;
        comboBox.Text = choice?.Display ?? xmlValue;
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

    private string T(string key)
    {
        return LocalizationService.Text(_language, key);
    }

    private string Tf(string key, params object[] args)
    {
        return LocalizationService.Format(_language, key, args);
    }

    private string LocalizeLiteral(string value)
    {
        return LocalizationService.TranslateLiteral(_language, value);
    }

    private string Blank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? T("Blank") : value;
    }
}
