using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class DeviceDetailsWindow : Window
{
    private readonly UiLanguage _language;
    private readonly ObservableCollection<DeviceChannelEditItem> _txChannels;
    private readonly ObservableCollection<DeviceChannelEditItem> _rxChannels;

    private readonly DeviceOption[] _latencies =
    [
        new("250", "0,25 ms"),
        new("1000", "1 ms"),
        new("2000", "2 ms"),
        new("5000", "5 ms")
    ];

    private readonly DeviceOption[] _sampleRates =
    [
        new("44100", "44,1 kHz"),
        new("48000", "48 kHz"),
        new("88200", "88,2 kHz"),
        new("96000", "96 kHz"),
        new("176400", "176,4 kHz"),
        new("192000", "192 kHz")
    ];

    private readonly DeviceOption[] _encodings =
    [
        new("16", "16 bit"),
        new("24", "24 bit"),
        new("32", "32 bit")
    ];

    public DeviceDetailsWindow(UiLanguage language, DanteDevice device)
    {
        InitializeComponent();
        _language = language;
        OriginalDeviceName = device.Name;
        TitleTextBlock.Text = device.Name;
        DeviceNameTextBox.Text = device.Name;
        RedundantRadioButton.IsChecked = device.IsRedundant;
        DaisychainRadioButton.IsChecked = !device.IsRedundant;
        PreferredMasterCheckBox.IsChecked = device.PreferredMaster;

        LatencyComboBox.ItemsSource = _latencies;
        SampleRateComboBox.ItemsSource = _sampleRates;
        EncodingComboBox.ItemsSource = _encodings;
        SelectOption(LatencyComboBox, _latencies, device.Latency);
        SelectOption(SampleRateComboBox, _sampleRates, device.Samplerate);
        SelectOption(EncodingComboBox, _encodings, device.Encoding);

        IpAutoRadioButton.IsChecked = !device.UsesStaticIp;
        IpStaticRadioButton.IsChecked = device.UsesStaticIp;
        IpAddressTextBox.Text = device.StaticIpAddress;
        IpNetmaskTextBox.Text = string.IsNullOrWhiteSpace(device.StaticIpNetmask) ? "255.255.255.0" : device.StaticIpNetmask;
        IpGatewayTextBox.Text = string.IsNullOrWhiteSpace(device.StaticIpGateway) ? "0.0.0.0" : device.StaticIpGateway;
        UpdateIpStaticFieldsState();

        _txChannels = new ObservableCollection<DeviceChannelEditItem>(
            device.TxChannels.Select(channel => new DeviceChannelEditItem(channel.Index, channel.DisplayName)));
        _rxChannels = new ObservableCollection<DeviceChannelEditItem>(
            device.RxChannels.Select(channel => new DeviceChannelEditItem(channel.Index, channel.DisplayName)));
        TxChannelsGrid.ItemsSource = _txChannels;
        RxChannelsGrid.ItemsSource = _rxChannels;

        TranslateDependencyObject(this, []);
    }

    public string OriginalDeviceName { get; }

    public DeviceDetailsResult? Result { get; private set; }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        Result = new DeviceDetailsResult(
            DeviceNameTextBox.Text.Trim(),
            RedundantRadioButton.IsChecked == true,
            SelectedValue(LatencyComboBox),
            SelectedValue(SampleRateComboBox),
            SelectedValue(EncodingComboBox),
            PreferredMasterCheckBox.IsChecked == true,
            IpStaticRadioButton.IsChecked == true,
            IpAddressTextBox.Text.Trim(),
            IpNetmaskTextBox.Text.Trim(),
            IpGatewayTextBox.Text.Trim(),
            _txChannels.Select(channel => new DeviceChannelEdit(channel.Index, channel.Name.Trim())).ToArray(),
            _rxChannels.Select(channel => new DeviceChannelEdit(channel.Index, channel.Name.Trim())).ToArray());

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void IpModeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        UpdateIpStaticFieldsState();
    }

    private void UpdateIpStaticFieldsState()
    {
        bool useStaticIp = IpStaticRadioButton.IsChecked == true;
        IpAddressTextBox.IsEnabled = useStaticIp;
        IpNetmaskTextBox.IsEnabled = useStaticIp;
        IpGatewayTextBox.IsEnabled = useStaticIp;
    }

    private static void SelectOption(ComboBox comboBox, IEnumerable<DeviceOption> options, string value)
    {
        comboBox.SelectedItem = options.FirstOrDefault(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase));
    }

    private static string SelectedValue(ComboBox comboBox)
    {
        return comboBox.SelectedItem is DeviceOption option ? option.Value : comboBox.Text.Trim();
    }

    private string LocalizeLiteral(string value)
    {
        return LocalizationService.TranslateLiteral(_language, value);
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
        }

        foreach (object child in LogicalTreeHelper.GetChildren(dependencyObject))
        {
            if (child is DependencyObject childObject)
            {
                TranslateDependencyObject(childObject, visited);
            }
        }
    }

    private sealed record DeviceOption(string Value, string Display)
    {
        public override string ToString()
        {
            return Display;
        }
    }
}

public sealed class DeviceChannelEditItem
{
    public DeviceChannelEditItem(int index, string name)
    {
        Index = index;
        Name = name;
    }

    public int Index { get; }

    public string Name { get; set; }
}

public sealed record DeviceChannelEdit(int Index, string Name);

public sealed record DeviceDetailsResult(
    string DeviceName,
    bool IsRedundant,
    string Latency,
    string Samplerate,
    string Encoding,
    bool PreferredMaster,
    bool UsesStaticIp,
    string StaticIpAddress,
    string StaticIpNetmask,
    string StaticIpGateway,
    IReadOnlyList<DeviceChannelEdit> TxChannels,
    IReadOnlyList<DeviceChannelEdit> RxChannels);
