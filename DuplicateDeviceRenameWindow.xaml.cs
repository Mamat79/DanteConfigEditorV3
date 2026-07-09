using System.Collections.ObjectModel;
using System.Windows;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class DuplicateDeviceRenameWindow : Window
{
    private readonly UiLanguage _language;
    private readonly IReadOnlyDictionary<string, string> _automaticRenameMap;

    public DuplicateDeviceRenameWindow(
        UiLanguage language,
        IReadOnlyList<string> duplicateNames,
        IReadOnlyDictionary<string, string> automaticRenameMap)
    {
        InitializeComponent();
        _language = language;
        _automaticRenameMap = automaticRenameMap;
        RenameItems = new ObservableCollection<DuplicateDeviceRenameItem>(
            duplicateNames.Select(name => new DuplicateDeviceRenameItem(
                name,
                automaticRenameMap.TryGetValue(name, out string? automaticName) ? automaticName : name + " (import)")));
        DataContext = this;
        ApplyLanguage();
    }

    public ObservableCollection<DuplicateDeviceRenameItem> RenameItems { get; }

    public DuplicateDeviceImportChoice Choice { get; private set; } = DuplicateDeviceImportChoice.Cancel;

    public IReadOnlyDictionary<string, string> RenameMap { get; private set; } = new Dictionary<string, string>();

    private void ApplyLanguage()
    {
        Title = T("DuplicateDialog.Title");
        IntroTextBlock.Text = T("DuplicateDialog.Intro");
        OriginalNameColumn.Header = T("DuplicateDialog.OriginalName");
        NewNameColumn.Header = T("DuplicateDialog.NewName");
        UniqueOnlyButton.Content = T("DuplicateDialog.UniqueOnly");
        AutoRenameButton.Content = T("DuplicateDialog.AutoRename");
        ManualRenameButton.Content = T("DuplicateDialog.ManualRename");
        CancelImportButton.Content = T("DuplicateDialog.Cancel");
    }

    private void UniqueOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        Choice = DuplicateDeviceImportChoice.UniqueOnly;
        RenameMap = new Dictionary<string, string>();
        DialogResult = true;
    }

    private void AutoRenameButton_Click(object sender, RoutedEventArgs e)
    {
        Choice = DuplicateDeviceImportChoice.AutoRename;
        RenameMap = new Dictionary<string, string>(_automaticRenameMap, StringComparer.OrdinalIgnoreCase);
        DialogResult = true;
    }

    private void ManualRenameButton_Click(object sender, RoutedEventArgs e)
    {
        Dictionary<string, string> renameMap = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> newNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (DuplicateDeviceRenameItem item in RenameItems)
        {
            string newName = item.NewName.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show(this, T("DuplicateDialog.EmptyName"), T("DuplicateDialog.InvalidTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!newNames.Add(newName))
            {
                MessageBox.Show(this, T("DuplicateDialog.DuplicateNewName"), T("DuplicateDialog.InvalidTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            renameMap[item.OriginalName] = newName;
        }

        Choice = DuplicateDeviceImportChoice.ManualRename;
        RenameMap = renameMap;
        DialogResult = true;
    }

    private void CancelImportButton_Click(object sender, RoutedEventArgs e)
    {
        Choice = DuplicateDeviceImportChoice.Cancel;
        DialogResult = false;
    }

    private string T(string key)
    {
        return LocalizationService.Text(_language, key);
    }
}

public enum DuplicateDeviceImportChoice
{
    Cancel,
    UniqueOnly,
    AutoRename,
    ManualRename
}

public sealed class DuplicateDeviceRenameItem
{
    public DuplicateDeviceRenameItem(string originalName, string newName)
    {
        OriginalName = originalName;
        NewName = newName;
    }

    public string OriginalName { get; }

    public string NewName { get; set; }
}
