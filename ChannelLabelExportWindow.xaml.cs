using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class ChannelLabelExportWindow : Window
{
    private readonly UiLanguage _language;
    private readonly DanteProject _project;
    private readonly ObservableCollection<ChannelLabelDeviceSelection> _devices;
    private readonly ObservableCollection<ChannelLabelExportPreviewRow> _preview = [];

    public ChannelLabelExportWindow(UiLanguage language, DanteProject project, string? initiallySelectedDevice)
    {
        InitializeComponent();
        _language = language;
        _project = project;
        _devices = new ObservableCollection<ChannelLabelDeviceSelection>(project.Devices.Select(device =>
            new ChannelLabelDeviceSelection(device.Name, device.TxCount, device.RxCount,
                string.Equals(device.Name, initiallySelectedDevice, StringComparison.OrdinalIgnoreCase))));
        DevicesGrid.ItemsSource = _devices;
        PreviewGrid.ItemsSource = _preview;
        FormatComboBox.SelectedIndex = 1;
        KindComboBox.SelectedIndex = 0;
        ApplyLanguage();
        RefreshPreview();
    }

    public string Format { get; private set; } = "json";

    public IReadOnlyList<string> DeviceNames { get; private set; } = [];

    public DanteChannelKind Kind { get; private set; } = DanteChannelKind.Tx;

    public int StartChannel { get; private set; } = 1;

    public int? Count { get; private set; }

    public bool AdaptConsoleLabels { get; private set; }

    private bool IsEnglish => _language == UiLanguage.English;

    private void ApplyLanguage()
    {
        Title = L("Exporter des labels de canaux", "Export channel labels");
        IntroTextBlock.Text = L(
            "Sélectionnez les machines et le format. CSV et JSON créent un nouveau fichier ; les formats console copient un export existant pour préserver sa structure.",
            "Select devices and a format. CSV and JSON create a new file; console formats copy an existing export to preserve its structure.");
        DevicesGroupBox.Header = L("Machines à exporter", "Devices to export");
        UseDeviceColumn.Header = L("Utiliser", "Use");
        DeviceNameColumn.Header = L("Machine", "Device");
        OptionsGroupBox.Header = L("Options", "Options");
        FormatLabel.Content = L("Format", "Format");
        JsonFormatItem.Content = L("JSON générique - nouveau fichier", "Generic JSON - new file");
        CsvFormatItem.Content = L("CSV générique - nouveau fichier", "Generic CSV - new file");
        DmtFormatItem.Content = L("XLSX DMT - copie d'un classeur existant", "DMT XLSX - copy an existing workbook");
        AllenHeathFormatItem.Content = L("CSV A&H - copie d'un export dLive / Avantis", "A&H CSV - copy a dLive / Avantis export");
        YamahaFormatItem.Content = L("Yamaha CL / QL - copie d'un ZIP ou CSV", "Yamaha CL / QL - copy a ZIP or CSV");
        KindLabel.Content = L("Canaux", "Channels");
        StartLabel.Content = L("Premier canal", "First channel");
        CountLabel.Content = L("Nombre (0 = tous)", "Count (0 = all)");
        AdaptDmtCheckBox.Content = L("Adapter au format console : ASCII et 8 caractères", "Adapt to console format: ASCII and 8 characters");
        PreviewGroupBox.Header = L("Labels exportés", "Exported labels");
        PreviewDeviceColumn.Header = L("Machine", "Device");
        PreviewChannelColumn.Header = L("Canal", "Channel");
        PreviewOriginalColumn.Header = L("Label Dante", "Dante label");
        PreviewExportedColumn.Header = L("Label exporté", "Exported label");
        PreviewWarningColumn.Header = L("Contrôle", "Check");
        CancelButton.Content = L("Annuler", "Cancel");
        ExportButton.Content = L("Exporter", "Export");
        UpdateFormatHelp();
    }

    private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool consoleTemplate = RequiresConsoleCompatibility(SelectedFormat());
        AdaptDmtCheckBox.IsEnabled = consoleTemplate;
        if (!consoleTemplate)
        {
            AdaptDmtCheckBox.IsChecked = false;
        }
        UpdateFormatHelp();
        RefreshPreview();
    }

    private void DeviceSelectionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(RefreshPreview);
    }

    private void UpdateFormatHelp()
    {
        if (!IsInitialized || FormatHelpTextBlock is null)
        {
            return;
        }

        FormatHelpTextBlock.Text = RequiresConsoleCompatibility(SelectedFormat())
            ? L(
                "Ce format natif doit partir d'un fichier exporté par DMT ou la console. Vous choisirez ce modèle, puis le nom de la copie.",
                "This native format must start from a file exported by DMT or the console. You will choose that template, then name the copy.")
            : L(
                "Un nouveau fichier sera créé : seul son nom et son dossier de destination seront demandés.",
                "A new file will be created: only its name and destination folder will be requested.");
    }

    private void OptionsChanged(object sender, RoutedEventArgs e) => RefreshPreview();

    private void DevicesGrid_CurrentCellChanged(object? sender, EventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        if (!IsInitialized)
        {
            return;
        }

        DevicesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        DevicesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        _preview.Clear();
        string[] selected = _devices.Where(device => device.IsSelected).Select(device => device.Name).ToArray();
        if (selected.Length == 0)
        {
            SummaryTextBlock.Text = L("Sélectionnez au moins une machine.", "Select at least one device.");
            return;
        }

        int start = int.TryParse(StartTextBox.Text, out int parsedStart) && parsedStart > 0 ? parsedStart : 1;
        int? count = int.TryParse(CountTextBox.Text, out int parsedCount) && parsedCount > 0 ? parsedCount : null;
        DanteChannelKind kind = KindComboBox.SelectedIndex == 1 ? DanteChannelKind.Rx : DanteChannelKind.Tx;
        bool consoleTemplate = RequiresConsoleCompatibility(SelectedFormat());
        bool adapt = AdaptDmtCheckBox.IsChecked == true;
        ChannelLabelDocument document = ChannelLabelExchangeService.CreateFromProject(_project, selected, kind, start, count);
        int incompatible = 0;
        foreach (ChannelLabelSet set in document.Sets)
        {
            foreach (ChannelLabelEntry channel in set.Channels)
            {
                DmtLabelCompatibility compatibility = DmtChannelWorkbookService.CheckCompatibility(channel.Label);
                string warning = consoleTemplate && !compatibility.IsCompatible
                    ? string.Join("; ", compatibility.Warnings)
                    : string.Empty;
                if (consoleTemplate && !compatibility.IsCompatible && !adapt)
                {
                    incompatible++;
                }

                _preview.Add(new ChannelLabelExportPreviewRow(
                    set.DeviceName,
                    kind,
                    channel.ChannelNumber,
                    channel.Label,
                    consoleTemplate && adapt ? compatibility.AdaptedLabel : channel.Label,
                    warning));
            }
        }

        SummaryTextBlock.Text = IsEnglish
            ? $"{document.Sets.Count} device(s), {_preview.Count} label(s), {incompatible} console-incompatible label(s)."
            : $"{document.Sets.Count} machine(s), {_preview.Count} label(s), {incompatible} label(s) incompatible(s) avec la console.";
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshPreview();
            DeviceNames = _devices.Where(device => device.IsSelected).Select(device => device.Name).ToArray();
            if (DeviceNames.Count == 0)
            {
                throw new InvalidOperationException(L("Sélectionnez au moins une machine.", "Select at least one device."));
            }

            Format = SelectedFormat();
            Kind = KindComboBox.SelectedIndex == 1 ? DanteChannelKind.Rx : DanteChannelKind.Tx;
            StartChannel = ParsePositive(StartTextBox.Text, L("premier canal", "first channel"));
            Count = ParseOptionalCount(CountTextBox.Text);
            AdaptConsoleLabels = AdaptDmtCheckBox.IsChecked == true;
            if (RequiresConsoleCompatibility(Format) && !AdaptConsoleLabels && _preview.Any(row => !string.IsNullOrWhiteSpace(row.Warning)))
            {
                throw new InvalidOperationException(L(
                    "Certains labels ne sont pas compatibles avec le format console. Activez l'adaptation explicite ou choisissez JSON/CSV générique.",
                    "Some labels are not console-compatible. Enable explicit adaptation or choose generic JSON/CSV."));
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, L("Export impossible", "Export unavailable"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string SelectedFormat() => (FormatComboBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "json";

    private static bool RequiresConsoleCompatibility(string format) => format is "xlsx" or "console-csv" or "yamaha";

    private static int ParsePositive(string value, string label) =>
        int.TryParse(value.Trim(), out int parsed) && parsed > 0
            ? parsed
            : throw new InvalidOperationException($"{label} : valeur invalide.");

    private static int? ParseOptionalCount(string value)
    {
        if (!int.TryParse(value.Trim(), out int parsed) || parsed < 0)
        {
            throw new InvalidOperationException("Nombre de canaux invalide.");
        }
        return parsed == 0 ? null : parsed;
    }

    private string L(string french, string english) => IsEnglish ? english : french;
}

public sealed record ChannelLabelExportPreviewRow(
    string DeviceName,
    DanteChannelKind Kind,
    int ChannelNumber,
    string OriginalLabel,
    string ExportedLabel,
    string Warning);
