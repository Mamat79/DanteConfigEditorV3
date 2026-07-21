using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac;

internal sealed partial class ChannelLabelImportDialog : Window
{
    private readonly ObservableCollection<MacChannelLabelDeviceSelection> _targetDevices = [];
    private readonly ObservableCollection<MacChannelLabelPreviewRow> _previewRows = [];
    private DanteProject? _project;
    private ChannelLabelDocument? _document;
    private UiLanguage _language;

    public ChannelLabelImportDialog()
    {
        InitializeComponent();
    }

    private T? FindControl<T>(string name) where T : Control => ControlExtensions.FindControl<T>(this, name);

    public static Task<IReadOnlyList<ChannelLabelAssignment>?> ShowAsync(
        Window owner,
        DanteProject project,
        ChannelLabelDocument document,
        UiLanguage language,
        string? initiallySelectedDevice)
    {
        ChannelLabelImportDialog dialog = new();
        dialog._project = project;
        dialog._document = document;
        dialog._language = language;
        dialog.Populate(initiallySelectedDevice);
        dialog.ApplyLanguage();
        return dialog.ShowDialog<IReadOnlyList<ChannelLabelAssignment>?>(owner);
    }

    private void Populate(string? initiallySelectedDevice)
    {
        ChannelLabelDocument document = _document!;
        foreach (DanteDevice device in _project!.Devices)
        {
            _targetDevices.Add(new MacChannelLabelDeviceSelection(
                device.Name,
                device.TxCount,
                device.RxCount,
                string.Equals(device.Name, initiallySelectedDevice, StringComparison.OrdinalIgnoreCase)));
        }

        FindControl<DataGrid>("TargetDevicesGrid")!.ItemsSource = _targetDevices;
        FindControl<DataGrid>("PreviewGrid")!.ItemsSource = _previewRows;
        FindControl<ComboBox>("SourceSetCombo")!.ItemsSource = document.Sets.Select(set => new MacChannelLabelSetChoice(set)).ToArray();
        FindControl<ComboBox>("SourceSetCombo")!.SelectedIndex = 0;
        FindControl<ComboBox>("KindCombo")!.ItemsSource = new ChoiceValue[]
        {
            new ChoiceValue("tx", "TX"),
            new ChoiceValue("rx", "RX")
        };
        FindControl<ComboBox>("KindCombo")!.SelectedIndex = document.Sets[0].Direction == ChannelLabelDirection.Rx ? 1 : 0;
        CheckBox autoMatch = FindControl<CheckBox>("AutoMatchCheckBox")!;
        autoMatch.IsEnabled = document.Sets.Count > 1;
        autoMatch.IsChecked = document.Sets.Count > 1;
        FindControl<TextBlock>("SourceInfoText")!.Text = $"{document.SourceApplication} {document.SourceVersion} - {document.Sets.Count} {Local("liste(s)", "set(s)")}".Trim();
        UpdateRangeDefaults();
        UpdateAutoMatchState();
    }

    private void ApplyLanguage()
    {
        Title = Local("Importer des labels de canaux", "Import channel labels");
        FindControl<TextBlock>("IntroText")!.Text = Local(
            "Choisissez la liste source, les machines Dante cibles et la plage. Aucun XML n'est modifié avant Appliquer.",
            "Choose the source list, target Dante devices and range. No XML is changed before Apply.");
        FindControl<TextBlock>("SourceTitle")!.Text = Local("Source", "Source");
        FindControl<TextBlock>("SourceSetLabel")!.Text = Local("Liste de labels", "Label set");
        FindControl<CheckBox>("AutoMatchCheckBox")!.Content = Local(
            "Associer les listes aux machines de même nom",
            "Match sets to devices with the same name");
        FindControl<TextBlock>("TargetsTitle")!.Text = Local("Machines Dante cibles", "Target Dante devices");
        FindControl<DataGrid>("TargetDevicesGrid")!.Columns[0].Header = Local("Utiliser", "Use");
        FindControl<DataGrid>("TargetDevicesGrid")!.Columns[1].Header = Local("Machine", "Device");
        FindControl<TextBlock>("MappingTitle")!.Text = Local("Correspondance", "Mapping");
        FindControl<TextBlock>("KindLabel")!.Text = Local("Type de canal Dante", "Dante channel type");
        FindControl<TextBlock>("SourceStartLabel")!.Text = Local("Premier canal source", "First source channel");
        FindControl<TextBlock>("TargetStartLabel")!.Text = Local("Premier canal Dante", "First Dante channel");
        FindControl<TextBlock>("CountLabel")!.Text = Local("Nombre de canaux", "Channel count");
        FindControl<Button>("PreviewButton")!.Content = Local("Prévisualiser", "Preview");
        FindControl<TextBlock>("PreviewTitle")!.Text = Local("Prévisualisation", "Preview");
        DataGrid preview = FindControl<DataGrid>("PreviewGrid")!;
        preview.Columns[0].Header = Local("Source", "Source");
        preview.Columns[1].Header = Local("Canal source", "Source channel");
        preview.Columns[2].Header = Local("Machine Dante", "Dante device");
        preview.Columns[3].Header = Local("Canal Dante", "Dante channel");
        preview.Columns[4].Header = Local("Avant", "Before");
        preview.Columns[5].Header = Local("Après", "After");
        preview.Columns[6].Header = Local("État", "Status");
        FindControl<TextBlock>("SafetyText")!.Text = Local(
            "Les renommages TX mettent à jour les subscriptions reconnues.",
            "TX renames update recognized subscriptions.");
        FindControl<Button>("CancelButton")!.Content = Local("Annuler", "Cancel");
        FindControl<Button>("ApplyButton")!.Content = Local("Appliquer", "Apply");
        ClearPreview();
    }

    private void SourceSetCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FindControl<ComboBox>("SourceSetCombo")?.SelectedItem is MacChannelLabelSetChoice choice)
        {
            FindControl<ComboBox>("KindCombo")!.SelectedIndex = choice.Set.Direction == ChannelLabelDirection.Rx ? 1 : 0;
            UpdateRangeDefaults();
        }
    }

    private void AutoMatchCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        UpdateAutoMatchState();
        ClearPreview();
    }

    private void UpdateAutoMatchState()
    {
        bool automatic = FindControl<CheckBox>("AutoMatchCheckBox")?.IsChecked == true;
        FindControl<ComboBox>("SourceSetCombo")!.IsEnabled = !automatic;
        FindControl<DataGrid>("TargetDevicesGrid")!.IsEnabled = !automatic;
    }

    private void UpdateRangeDefaults()
    {
        if (FindControl<ComboBox>("SourceSetCombo")?.SelectedItem is not MacChannelLabelSetChoice choice
            || choice.Set.Channels.Count == 0)
        {
            return;
        }

        FindControl<TextBox>("SourceStartTextBox")!.Text = choice.Set.Channels.Min(channel => channel.ChannelNumber).ToString();
        FindControl<TextBox>("TargetStartTextBox")!.Text = "1";
        FindControl<TextBox>("CountTextBox")!.Text = choice.Set.Channels.Count.ToString();
        ClearPreview();
    }

    private async void PreviewButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            IReadOnlyList<ChannelLabelTransferPreviewRow> rows = BuildPreview();
            _previewRows.Clear();
            foreach (ChannelLabelTransferPreviewRow row in rows)
            {
                _previewRows.Add(new MacChannelLabelPreviewRow(row, _language));
            }

            int changes = rows.Count(row => row.WillChange);
            int errors = rows.Count(row => !row.CanApply);
            int unchanged = rows.Count(row => row.Status == ChannelLabelTransferStatus.Unchanged);
            FindControl<TextBlock>("PreviewSummaryText")!.Text = _language == UiLanguage.English
                ? $"{rows.Count} row(s): {changes} change(s), {unchanged} unchanged, {errors} issue(s)."
                : $"{rows.Count} ligne(s) : {changes} changement(s), {unchanged} inchangée(s), {errors} problème(s).";
            FindControl<Button>("ApplyButton")!.IsEnabled = changes > 0 && errors == 0;
        }
        catch (Exception exception)
        {
            ClearPreview();
            await MessageDialog.ShowInfoAsync(this, Local("Prévisualisation impossible", "Preview unavailable"), exception.Message, "OK");
        }
    }

    private IReadOnlyList<ChannelLabelTransferPreviewRow> BuildPreview()
    {
        int sourceStart = ParsePositive(FindControl<TextBox>("SourceStartTextBox")!.Text, Local("premier canal source", "first source channel"));
        int targetStart = ParsePositive(FindControl<TextBox>("TargetStartTextBox")!.Text, Local("premier canal Dante", "first Dante channel"));
        int count = ParsePositive(FindControl<TextBox>("CountTextBox")!.Text, Local("nombre de canaux", "channel count"));
        DanteChannelKind selectedKind = SelectedValue("KindCombo") == "rx" ? DanteChannelKind.Rx : DanteChannelKind.Tx;
        List<ChannelLabelTransferPreviewRow> rows = [];

        if (FindControl<CheckBox>("AutoMatchCheckBox")!.IsChecked == true)
        {
            foreach (ChannelLabelSet set in _document!.Sets)
            {
                DanteChannelKind kind = set.Direction switch
                {
                    ChannelLabelDirection.Tx => DanteChannelKind.Tx,
                    ChannelLabelDirection.Rx => DanteChannelKind.Rx,
                    _ => selectedKind
                };
                rows.AddRange(ChannelLabelTransferPlanner.BuildPreview(
                    _project!, set, [set.DeviceName], kind, sourceStart, targetStart, Math.Min(count, set.Channels.Count)));
            }
        }
        else
        {
            ChannelLabelSet source = (FindControl<ComboBox>("SourceSetCombo")!.SelectedItem as MacChannelLabelSetChoice)?.Set
                ?? throw new InvalidOperationException(Local("Sélectionnez une liste source.", "Select a source set."));
            string[] targets = _targetDevices.Where(device => device.IsSelected).Select(device => device.Name).ToArray();
            rows.AddRange(ChannelLabelTransferPlanner.BuildPreview(
                _project!, source, targets, selectedKind, sourceStart, targetStart, count));
        }

        return rows;
    }

    private async void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            IReadOnlyList<ChannelLabelAssignment> assignments = ChannelLabelTransferPlanner.BuildAssignments(_previewRows.Select(row => row.Source));
            if (assignments.Count == 0)
            {
                throw new InvalidOperationException(Local("Aucun changement à appliquer.", "No change to apply."));
            }
            Close(assignments);
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this, Local("Import impossible", "Import unavailable"), exception.Message, "OK");
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void ClearPreview()
    {
        _previewRows.Clear();
        if (FindControl<TextBlock>("PreviewSummaryText") is { } summary)
        {
            summary.Text = Local(
                "Prévisualisez le transfert pour vérifier chaque correspondance.",
                "Preview the transfer to verify each mapping.");
        }
        if (FindControl<Button>("ApplyButton") is { } apply)
        {
            apply.IsEnabled = false;
        }
    }

    private string SelectedValue(string controlName) =>
        (FindControl<ComboBox>(controlName)!.SelectedItem as ChoiceValue)?.Value ?? string.Empty;

    private static int ParsePositive(string? value, string label) =>
        int.TryParse(value?.Trim(), out int parsed) && parsed > 0
            ? parsed
            : throw new InvalidOperationException($"{label} : valeur invalide.");

    private string Local(string french, string english) => _language == UiLanguage.English ? english : french;
}

internal sealed record MacChannelLabelSetChoice(ChannelLabelSet Set)
{
    public override string ToString() => Set.DisplayName;
}

internal sealed class MacChannelLabelDeviceSelection
{
    public MacChannelLabelDeviceSelection(string name, int txCount, int rxCount, bool isSelected)
    {
        Name = name;
        TxCount = txCount;
        RxCount = rxCount;
        IsSelected = isSelected;
    }

    public string Name { get; }
    public int TxCount { get; }
    public int RxCount { get; }
    public bool IsSelected { get; set; }
    public string Counts => $"{TxCount} / {RxCount}";
}

internal sealed record MacChannelLabelPreviewRow(ChannelLabelTransferPreviewRow Source, UiLanguage Language)
{
    public string SourceDevice => Source.SourceDevice;
    public int SourceChannel => Source.SourceChannel;
    public string TargetDevice => Source.TargetDevice;
    public int TargetDanteId => Source.TargetDanteId;
    public string CurrentLabel => Source.CurrentLabel;
    public string NewLabel => Source.NewLabel;
    public string LocalizedStatus => Language == UiLanguage.English ? Source.StatusDisplayEnglish : Source.StatusDisplay;
}
