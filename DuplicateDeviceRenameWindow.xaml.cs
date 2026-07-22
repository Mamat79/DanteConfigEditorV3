using System.Collections.ObjectModel;
using System.Windows;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class DuplicateDeviceRenameWindow : Window
{
    private readonly UiLanguage _language;
    private readonly Func<string, IReadOnlyDictionary<string, string>> _automaticRenameFactory;
    private IReadOnlyDictionary<string, string> _automaticRenameMap = new Dictionary<string, string>();
    private bool _initializing = true;

    public DuplicateDeviceRenameWindow(
        UiLanguage language,
        IReadOnlyList<string> duplicateNames,
        Func<string, IReadOnlyDictionary<string, string>> automaticRenameFactory)
    {
        InitializeComponent();
        _language = language;
        _automaticRenameFactory = automaticRenameFactory;
        _automaticRenameMap = automaticRenameFactory("Import");
        RenameItems = new ObservableCollection<DuplicateDeviceRenameItem>(
            duplicateNames.Select(name => new DuplicateDeviceRenameItem(
                name,
                _automaticRenameMap.TryGetValue(name, out string? automaticName) ? automaticName : name + "-Import")));
        DataContext = this;
        ApplyLanguage();
        _initializing = false;
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
        SuffixLabel.Content = T("DuplicateDialog.Suffix");
        UniqueOnlyButton.Content = T("DuplicateDialog.UniqueOnly");
        AutoRenameButton.Content = T("DuplicateDialog.AutoRename");
        ManualRenameButton.Content = T("DuplicateDialog.ManualRename");
        CancelImportButton.Content = T("DuplicateDialog.Cancel");
    }

    private void SuffixTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_initializing || RenameItems is null)
        {
            return;
        }

        try
        {
            _automaticRenameMap = _automaticRenameFactory(SuffixTextBox.Text);
            foreach (DuplicateDeviceRenameItem item in RenameItems)
            {
                if (_automaticRenameMap.TryGetValue(item.OriginalName, out string? automaticName))
                {
                    item.NewName = automaticName;
                }
            }
            RenameGrid.Items.Refresh();
        }
        catch (InvalidOperationException)
        {
            // La validation détaillée est affichée seulement si l'utilisateur
            // demande réellement le renommage automatique.
        }
    }

    private void UniqueOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        Choice = DuplicateDeviceImportChoice.UniqueOnly;
        RenameMap = new Dictionary<string, string>();
        DialogResult = true;
    }

    private void AutoRenameButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _automaticRenameMap = _automaticRenameFactory(SuffixTextBox.Text);
            Choice = DuplicateDeviceImportChoice.AutoRename;
            RenameMap = new Dictionary<string, string>(_automaticRenameMap, StringComparer.OrdinalIgnoreCase);
            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, T("DuplicateDialog.InvalidTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
