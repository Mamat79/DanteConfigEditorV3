using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac;

internal sealed record ChannelLabelExportDialogResult(
    string Format,
    IReadOnlyList<string> DeviceNames,
    DanteChannelKind Kind,
    int StartChannel,
    int? Count,
    bool AdaptConsoleLabels);

internal sealed partial class ChannelLabelExportDialog : Window
{
    private readonly ObservableCollection<MacChannelLabelDeviceSelection> _devices = [];
    private readonly ObservableCollection<MacChannelLabelExportPreviewRow> _preview = [];
    private DanteProject? _project;
    private UiLanguage _language;

    public ChannelLabelExportDialog()
    {
        InitializeComponent();
    }

    private T? FindControl<T>(string name) where T : Control => ControlExtensions.FindControl<T>(this, name);

    public static Task<ChannelLabelExportDialogResult?> ShowAsync(
        Window owner,
        DanteProject project,
        UiLanguage language,
        string? initiallySelectedDevice)
    {
        ChannelLabelExportDialog dialog = new();
        dialog._project = project;
        dialog._language = language;
        dialog.Populate(initiallySelectedDevice);
        dialog.ApplyLanguage();
        dialog.RefreshPreview();
        return dialog.ShowDialog<ChannelLabelExportDialogResult?>(owner);
    }

    private void Populate(string? initiallySelectedDevice)
    {
        foreach (DanteDevice device in _project!.Devices)
        {
            _devices.Add(new MacChannelLabelDeviceSelection(
                device.Name,
                device.TxCount,
                device.RxCount,
                string.Equals(device.Name, initiallySelectedDevice, StringComparison.OrdinalIgnoreCase)));
        }
        FindControl<DataGrid>("DevicesGrid")!.ItemsSource = _devices;
        FindControl<DataGrid>("PreviewGrid")!.ItemsSource = _preview;
        FindControl<ComboBox>("FormatCombo")!.ItemsSource = new ChoiceValue[]
        {
            new ChoiceValue("json", Local("JSON - échange recommandé", "JSON - recommended exchange")),
            new ChoiceValue("csv", Local("CSV - tableau universel", "CSV - universal table")),
            new ChoiceValue("xlsx", Local("XLSX - modèle DMT dLive / Avantis", "XLSX - DMT template for dLive / Avantis")),
            new ChoiceValue("console-csv", Local("CSV console - A&H dLive / Avantis", "Console CSV - A&H dLive / Avantis")),
            new ChoiceValue("yamaha", Local("ZIP/CSV console - Yamaha CL / QL", "Console ZIP/CSV - Yamaha CL / QL"))
        };
        FindControl<ComboBox>("FormatCombo")!.SelectedIndex = 0;
        FindControl<ComboBox>("KindCombo")!.ItemsSource = new ChoiceValue[]
        {
            new("tx", "TX"),
            new("rx", "RX")
        };
        FindControl<ComboBox>("KindCombo")!.SelectedIndex = 0;
    }

    private void ApplyLanguage()
    {
        Title = Local("Exporter des labels de canaux", "Export channel labels");
        FindControl<TextBlock>("IntroText")!.Text = Local(
            "Sélectionnez les machines et le format. Les formats DMT, A&H et Yamaha utilisent une copie du modèle choisi et conservent ses autres réglages.",
            "Select devices and a format. DMT, A&H and Yamaha formats use a copy of the selected template and preserve its other settings.");
        FindControl<TextBlock>("DevicesTitle")!.Text = Local("Machines à exporter", "Devices to export");
        DataGrid devices = FindControl<DataGrid>("DevicesGrid")!;
        devices.Columns[0].Header = Local("Utiliser", "Use");
        devices.Columns[1].Header = Local("Machine", "Device");
        FindControl<TextBlock>("OptionsTitle")!.Text = Local("Options", "Options");
        FindControl<TextBlock>("FormatLabel")!.Text = Local("Format", "Format");
        FindControl<TextBlock>("KindLabel")!.Text = Local("Canaux", "Channels");
        FindControl<TextBlock>("StartLabel")!.Text = Local("Premier canal", "First channel");
        FindControl<TextBlock>("CountLabel")!.Text = Local("Nombre (0 = tous)", "Count (0 = all)");
        FindControl<CheckBox>("AdaptDmtCheckBox")!.Content = Local(
            "Adapter au format console : ASCII et 8 caractères",
            "Adapt to console format: ASCII and 8 characters");
        FindControl<Button>("PreviewButton")!.Content = Local("Actualiser l'aperçu", "Refresh preview");
        FindControl<TextBlock>("PreviewTitle")!.Text = Local("Labels exportés", "Exported labels");
        DataGrid preview = FindControl<DataGrid>("PreviewGrid")!;
        preview.Columns[0].Header = Local("Machine", "Device");
        preview.Columns[2].Header = Local("Canal", "Channel");
        preview.Columns[3].Header = Local("Label Dante", "Dante label");
        preview.Columns[4].Header = Local("Label exporté", "Exported label");
        preview.Columns[5].Header = Local("Contrôle", "Check");
        FindControl<Button>("CancelButton")!.Content = Local("Annuler", "Cancel");
        FindControl<Button>("ExportButton")!.Content = Local("Exporter", "Export");
    }

    private void FormatCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FindControl<CheckBox>("AdaptDmtCheckBox") is not { } adapt)
        {
            return;
        }
        bool consoleTemplate = RequiresConsoleCompatibility(SelectedValue("FormatCombo"));
        adapt.IsEnabled = consoleTemplate;
        if (!consoleTemplate)
        {
            adapt.IsChecked = false;
        }
    }

    private async void PreviewButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            RefreshPreview();
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this, Local("Aperçu impossible", "Preview unavailable"), exception.Message, "OK");
        }
    }

    private void RefreshPreview()
    {
        _preview.Clear();
        string[] selected = _devices.Where(device => device.IsSelected).Select(device => device.Name).ToArray();
        if (selected.Length == 0)
        {
            FindControl<TextBlock>("SummaryText")!.Text = Local("Sélectionnez au moins une machine.", "Select at least one device.");
            return;
        }

        int start = int.TryParse(FindControl<TextBox>("StartTextBox")!.Text, out int parsedStart) && parsedStart > 0 ? parsedStart : 1;
        int? count = int.TryParse(FindControl<TextBox>("CountTextBox")!.Text, out int parsedCount) && parsedCount > 0 ? parsedCount : null;
        DanteChannelKind kind = SelectedValue("KindCombo") == "rx" ? DanteChannelKind.Rx : DanteChannelKind.Tx;
        bool consoleTemplate = RequiresConsoleCompatibility(SelectedValue("FormatCombo"));
        bool adapt = FindControl<CheckBox>("AdaptDmtCheckBox")!.IsChecked == true;
        ChannelLabelDocument document = ChannelLabelExchangeService.CreateFromProject(_project!, selected, kind, start, count);
        int incompatible = 0;
        foreach (ChannelLabelSet set in document.Sets)
        {
            foreach (ChannelLabelEntry channel in set.Channels)
            {
                DmtLabelCompatibility compatibility = DmtChannelWorkbookService.CheckCompatibility(channel.Label);
                string warning = consoleTemplate && !compatibility.IsCompatible ? string.Join("; ", compatibility.Warnings) : string.Empty;
                if (consoleTemplate && !compatibility.IsCompatible && !adapt)
                {
                    incompatible++;
                }
                _preview.Add(new MacChannelLabelExportPreviewRow(
                    set.DeviceName,
                    kind,
                    channel.ChannelNumber,
                    channel.Label,
                    consoleTemplate && adapt ? compatibility.AdaptedLabel : channel.Label,
                    warning));
            }
        }

        FindControl<TextBlock>("SummaryText")!.Text = _language == UiLanguage.English
            ? $"{document.Sets.Count} device(s), {_preview.Count} label(s), {incompatible} console-incompatible label(s)."
            : $"{document.Sets.Count} machine(s), {_preview.Count} label(s), {incompatible} label(s) incompatible(s) avec la console.";
    }

    private async void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            RefreshPreview();
            string[] devices = _devices.Where(device => device.IsSelected).Select(device => device.Name).ToArray();
            if (devices.Length == 0)
            {
                throw new InvalidOperationException(Local("Sélectionnez au moins une machine.", "Select at least one device."));
            }

            string format = SelectedValue("FormatCombo");
            bool adapt = FindControl<CheckBox>("AdaptDmtCheckBox")!.IsChecked == true;
            if (RequiresConsoleCompatibility(format) && !adapt && _preview.Any(row => !string.IsNullOrWhiteSpace(row.Warning)))
            {
                throw new InvalidOperationException(Local(
                    "Certains labels ne sont pas compatibles avec le format console. Activez l'adaptation explicite ou choisissez JSON/CSV générique.",
                    "Some labels are not console-compatible. Enable explicit adaptation or choose generic JSON/CSV."));
            }

            Close(new ChannelLabelExportDialogResult(
                format,
                devices,
                SelectedValue("KindCombo") == "rx" ? DanteChannelKind.Rx : DanteChannelKind.Tx,
                ParsePositive(FindControl<TextBox>("StartTextBox")!.Text, Local("premier canal", "first channel")),
                ParseOptionalCount(FindControl<TextBox>("CountTextBox")!.Text),
                adapt));
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this, Local("Export impossible", "Export unavailable"), exception.Message, "OK");
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);

    private string SelectedValue(string controlName) =>
        (FindControl<ComboBox>(controlName)!.SelectedItem as ChoiceValue)?.Value ?? string.Empty;

    private static bool RequiresConsoleCompatibility(string format) => format is "xlsx" or "console-csv" or "yamaha";

    private static int ParsePositive(string? value, string label) =>
        int.TryParse(value?.Trim(), out int parsed) && parsed > 0
            ? parsed
            : throw new InvalidOperationException($"{label} : valeur invalide.");

    private static int? ParseOptionalCount(string? value)
    {
        if (!int.TryParse(value?.Trim(), out int parsed) || parsed < 0)
        {
            throw new InvalidOperationException("Nombre de canaux invalide.");
        }
        return parsed == 0 ? null : parsed;
    }

    private string Local(string french, string english) => _language == UiLanguage.English ? english : french;
}

internal sealed record MacChannelLabelExportPreviewRow(
    string DeviceName,
    DanteChannelKind Kind,
    int ChannelNumber,
    string OriginalLabel,
    string ExportedLabel,
    string Warning);
