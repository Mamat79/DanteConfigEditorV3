using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac;

internal sealed record DeviceDetailsResult(
    string DeviceName,
    bool IsRedundant,
    string Latency,
    string SampleRate,
    string Bits,
    bool PreferredMaster,
    bool UseStaticIp,
    string IpAddress,
    string Netmask,
    string Gateway,
    IReadOnlyList<EditableChannelRow> TxChannels,
    IReadOnlyList<EditableChannelRow> RxChannels);

internal sealed partial class DeviceDetailsDialog : Window
{
    private readonly List<EditableChannelRow> _txChannels = [];
    private readonly List<EditableChannelRow> _rxChannels = [];
    private UiLanguage _language;
    private bool _editable;

    public DeviceDetailsDialog()
    {
        InitializeComponent();
    }

    private T? FindControl<T>(string name) where T : Control => ControlExtensions.FindControl<T>(this, name);

    public static Task<DeviceDetailsResult?> ShowAsync(
        Window owner,
        DanteDevice device,
        UiLanguage language,
        bool editable)
    {
        DeviceDetailsDialog dialog = new();
        dialog._language = language;
        dialog._editable = editable;
        dialog.Populate(device);
        dialog.ApplyLanguage();
        return dialog.ShowDialog<DeviceDetailsResult?>(owner);
    }

    private void Populate(DanteDevice device)
    {
        FindControl<TextBlock>("DeviceTitleText")!.Text = device.Name;
        FindControl<TextBox>("NameTextBox")!.Text = device.Name;
        SetChoices("NetworkCombo",
        [
            new("redundant", Local("Redondant", "Redundant")),
            new("daisychain", "Daisychain")
        ], device.IsRedundant ? "redundant" : "daisychain");
        SetChoices("LatencyCombo",
        [
            new("250", Local("0,25 ms", "0.25 ms")),
            new("500", Local("0,5 ms", "0.5 ms")),
            new("1000", "1 ms"),
            new("2000", "2 ms"),
            new("5000", "5 ms"),
            new("10000", "10 ms")
        ], device.Latency);
        SetChoices("SampleRateCombo",
        [
            new("44100", "44.1 kHz"), new("48000", "48 kHz"), new("88200", "88.2 kHz"),
            new("96000", "96 kHz"), new("176400", "176.4 kHz"), new("192000", "192 kHz")
        ], device.Samplerate);
        SetChoices("BitsCombo", [new("16", "16 bit"), new("24", "24 bit"), new("32", "32 bit")], device.Encoding);
        SetChoices("IpModeCombo",
        [
            new("auto", Local("Automatique", "Automatic")),
            new("static", Local("Fixe", "Static"))
        ], device.UsesStaticIp ? "static" : "auto");

        FindControl<CheckBox>("PreferredCheckBox")!.IsChecked = device.PreferredMaster;
        FindControl<TextBox>("IpAddressTextBox")!.Text = device.StaticIpAddress;
        FindControl<TextBox>("NetmaskTextBox")!.Text = string.IsNullOrWhiteSpace(device.StaticIpNetmask) ? "255.255.255.0" : device.StaticIpNetmask;
        FindControl<TextBox>("GatewayTextBox")!.Text = string.IsNullOrWhiteSpace(device.StaticIpGateway) ? "0.0.0.0" : device.StaticIpGateway;

        _txChannels.AddRange(device.TxChannels.Select(channel => new EditableChannelRow(channel.Index, channel.DanteId, channel.Name)));
        _rxChannels.AddRange(device.RxChannels.Select(channel => new EditableChannelRow(channel.Index, channel.DanteId, channel.Name)));
        FindControl<DataGrid>("TxGrid")!.ItemsSource = _txChannels;
        FindControl<DataGrid>("RxGrid")!.ItemsSource = _rxChannels;

        foreach (Control control in new Control[]
                 {
                     FindControl<TextBox>("NameTextBox")!, FindControl<ComboBox>("NetworkCombo")!,
                     FindControl<ComboBox>("LatencyCombo")!, FindControl<ComboBox>("SampleRateCombo")!,
                     FindControl<ComboBox>("BitsCombo")!, FindControl<ComboBox>("IpModeCombo")!,
                     FindControl<CheckBox>("PreferredCheckBox")!, FindControl<TextBox>("IpAddressTextBox")!,
                     FindControl<TextBox>("NetmaskTextBox")!, FindControl<TextBox>("GatewayTextBox")!,
                     FindControl<DataGrid>("TxGrid")!, FindControl<DataGrid>("RxGrid")!
                 })
        {
            control.IsEnabled = _editable;
        }

        FindControl<Button>("ApplyButton")!.IsVisible = _editable;
        UpdateIpFields();
    }

    private void ApplyLanguage()
    {
        Title = Local("Détail machine", "Device details");
        FindControl<TextBlock>("NameLabel")!.Text = Local("Nom machine", "Device name");
        FindControl<TextBlock>("NetworkLabel")!.Text = Local("Mode réseau", "Network mode");
        FindControl<TextBlock>("LatencyLabel")!.Text = Local("Latence unicast", "Unicast latency");
        FindControl<TextBlock>("SampleRateLabel")!.Text = "Sample rate";
        FindControl<TextBlock>("BitsLabel")!.Text = Local("Bits par échantillon", "Bits per sample");
        FindControl<TextBlock>("IpModeLabel")!.Text = Local("Mode IP", "IP mode");
        FindControl<CheckBox>("PreferredCheckBox")!.Content = "Preferred master";
        FindControl<TextBlock>("HintText")!.Text = Local(
            "Les changements seront appliqués au XML après validation.",
            "Changes will be applied to the XML after confirmation.");
        FindControl<Button>("CancelButton")!.Content = _editable ? Local("Annuler", "Cancel") : Local("Fermer", "Close");
        FindControl<Button>("ApplyButton")!.Content = Local("Appliquer", "Apply");
        foreach (DataGridColumn column in FindControl<DataGrid>("TxGrid")!.Columns.Concat(FindControl<DataGrid>("RxGrid")!.Columns))
        {
            if (column.Header is string header)
            {
                column.Header = LocalizationService.TranslateLiteral(_language, header);
            }
        }
    }

    private void IpModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateIpFields();
    }

    private void UpdateIpFields()
    {
        bool enabled = _editable && SelectedValue("IpModeCombo") == "static";
        FindControl<TextBox>("IpAddressTextBox")!.IsEnabled = enabled;
        FindControl<TextBox>("NetmaskTextBox")!.IsEnabled = enabled;
        FindControl<TextBox>("GatewayTextBox")!.IsEnabled = enabled;
    }

    private void ApplyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string name = FindControl<TextBox>("NameTextBox")!.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Close(new DeviceDetailsResult(
            name,
            SelectedValue("NetworkCombo") == "redundant",
            SelectedValue("LatencyCombo"),
            SelectedValue("SampleRateCombo"),
            SelectedValue("BitsCombo"),
            FindControl<CheckBox>("PreferredCheckBox")!.IsChecked == true,
            SelectedValue("IpModeCombo") == "static",
            FindControl<TextBox>("IpAddressTextBox")!.Text ?? string.Empty,
            FindControl<TextBox>("NetmaskTextBox")!.Text ?? string.Empty,
            FindControl<TextBox>("GatewayTextBox")!.Text ?? string.Empty,
            _txChannels,
            _rxChannels));
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private void SetChoices(string controlName, IReadOnlyList<ChoiceValue> choices, string selectedValue)
    {
        ComboBox combo = FindControl<ComboBox>(controlName)!;
        combo.ItemsSource = choices;
        combo.SelectedItem = choices.FirstOrDefault(choice => choice.Value == selectedValue) ?? choices.FirstOrDefault();
    }

    private string SelectedValue(string controlName) =>
        (FindControl<ComboBox>(controlName)!.SelectedItem as ChoiceValue)?.Value ?? string.Empty;

    private string Local(string french, string english) => _language == UiLanguage.English ? english : french;
}
