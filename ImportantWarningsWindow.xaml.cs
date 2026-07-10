using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class ImportantWarningsWindow : Window
{
    public ImportantWarningsWindow(UiLanguage language, IEnumerable<DanteImportantWarning> warnings)
    {
        InitializeComponent();
        bool english = language == UiLanguage.English;
        ObservableCollection<ImportantWarningRow> rows = new(warnings.Select(warning => new ImportantWarningRow(
            warning,
            warning.LocalizedMessage(english),
            warning.DeviceCount,
            FormatDevices(warning.DeviceNames, english))));
        WarningsGrid.ItemsSource = rows;
        WarningsGrid.SelectedIndex = rows.Count > 0 ? 0 : -1;
        ShowDevicesButton.IsEnabled = rows.Count > 0;

        Title = english ? "Items to check" : "Points à vérifier";
        SummaryTextBlock.Text = english
            ? $"{rows.Count} warning(s). Select one to show the affected devices."
            : $"{rows.Count} alerte(s). Sélectionnez-en une pour afficher les machines concernées.";
        WarningColumn.Header = english ? "Warning" : "Alerte";
        DeviceCountColumn.Header = english ? "Devices" : "Machines";
        AffectedDevicesColumn.Header = english ? "Affected devices" : "Machines concernées";
        CloseButton.Content = english ? "Close" : "Fermer";
        ShowDevicesButton.Content = english ? "Show devices" : "Afficher les machines";
    }

    public DanteImportantWarning? SelectedWarning { get; private set; }

    private void ShowDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        ConfirmSelection();
    }

    private void WarningsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (WarningsGrid.SelectedItem is not ImportantWarningRow row)
        {
            return;
        }

        SelectedWarning = row.Warning;
        DialogResult = true;
    }

    private static string FormatDevices(IReadOnlyList<string> deviceNames, bool english)
    {
        if (deviceNames.Count == 0)
        {
            return english ? "No targeted device" : "Aucune machine ciblée";
        }

        return string.Join(", ", deviceNames.Take(8))
            + (deviceNames.Count > 8
                ? english ? $", +{deviceNames.Count - 8} more" : $", +{deviceNames.Count - 8} autre(s)"
                : string.Empty);
    }

    private sealed record ImportantWarningRow(
        DanteImportantWarning Warning,
        string Message,
        int DeviceCount,
        string DevicesDisplay);
}
