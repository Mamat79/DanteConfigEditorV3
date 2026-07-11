using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac;

internal enum DuplicateMergeMode
{
    UniqueOnly,
    Automatic,
    Manual
}

internal sealed record DuplicateMergeDialogResult(
    DuplicateMergeMode Mode,
    IReadOnlyDictionary<string, string>? RenameMap);

internal sealed partial class DuplicateMergeDialog : Window
{
    private readonly ObservableCollection<DuplicateRenameRow> _rows = [];
    private UiLanguage _language;

    public DuplicateMergeDialog()
    {
        InitializeComponent();
    }

    private T? FindControl<T>(string name) where T : Control => ControlExtensions.FindControl<T>(this, name);

    public static Task<DuplicateMergeDialogResult?> ShowAsync(
        Window owner,
        IReadOnlyList<string> duplicateNames,
        IReadOnlyDictionary<string, string> automaticNames,
        UiLanguage language)
    {
        DuplicateMergeDialog dialog = new();
        dialog._language = language;
        foreach (string name in duplicateNames)
        {
            automaticNames.TryGetValue(name, out string? automaticName);
            dialog._rows.Add(new DuplicateRenameRow(name, automaticName ?? name + "-IMPORT"));
        }

        dialog.FindControl<DataGrid>("DuplicateGrid")!.ItemsSource = dialog._rows;
        dialog.ApplyLanguage();
        return dialog.ShowDialog<DuplicateMergeDialogResult?>(owner);
    }

    private void ApplyLanguage()
    {
        FindControl<TextBlock>("TitleText")!.Text = LocalizationService.Text(_language, "DuplicateDialog.Title");
        FindControl<TextBlock>("IntroText")!.Text = LocalizationService.Text(_language, "DuplicateDialog.Intro");
        DataGrid grid = FindControl<DataGrid>("DuplicateGrid")!;
        grid.Columns[0].Header = LocalizationService.Text(_language, "DuplicateDialog.OriginalName");
        grid.Columns[1].Header = LocalizationService.Text(_language, "DuplicateDialog.NewName");
        FindControl<Button>("UniqueOnlyButton")!.Content = LocalizationService.Text(_language, "DuplicateDialog.UniqueOnly");
        FindControl<Button>("AutoButton")!.Content = LocalizationService.Text(_language, "DuplicateDialog.AutoRename");
        FindControl<Button>("ManualButton")!.Content = LocalizationService.Text(_language, "DuplicateDialog.ManualRename");
        FindControl<Button>("CancelButton")!.Content = LocalizationService.Text(_language, "DuplicateDialog.Cancel");
    }

    private void UniqueOnlyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(new DuplicateMergeDialogResult(DuplicateMergeMode.UniqueOnly, null));
    }

    private void AutoButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(new DuplicateMergeDialogResult(DuplicateMergeMode.Automatic, BuildRenameMap()));
    }

    private async void ManualButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_rows.Any(row => string.IsNullOrWhiteSpace(row.NewName)))
        {
            await MessageDialog.ShowInfoAsync(
                this,
                LocalizationService.Text(_language, "DuplicateDialog.InvalidTitle"),
                LocalizationService.Text(_language, "DuplicateDialog.EmptyName"),
                "OK");
            return;
        }

        if (_rows.GroupBy(row => row.NewName.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            await MessageDialog.ShowInfoAsync(
                this,
                LocalizationService.Text(_language, "DuplicateDialog.InvalidTitle"),
                LocalizationService.Text(_language, "DuplicateDialog.DuplicateNewName"),
                "OK");
            return;
        }

        Close(new DuplicateMergeDialogResult(DuplicateMergeMode.Manual, BuildRenameMap()));
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private IReadOnlyDictionary<string, string> BuildRenameMap()
    {
        return _rows.ToDictionary(row => row.OriginalName, row => row.NewName.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
