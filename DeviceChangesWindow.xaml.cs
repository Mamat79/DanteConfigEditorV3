using System.Collections.ObjectModel;
using System.Windows;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class DeviceChangesWindow : Window
{
    public DeviceChangesWindow(UiLanguage language, IEnumerable<DeviceChangeRow> changes)
    {
        InitializeComponent();
        bool english = language == UiLanguage.English;
        ObservableCollection<DeviceChangeViewRow> rows = new(changes.Select(change => new DeviceChangeViewRow(
            change.DeviceName,
            TranslateParameter(change.Parameter, english),
            TranslateValue(change.Before, english),
            TranslateValue(change.After, english),
            TranslateStatus(change.Status, english))));
        ChangesGrid.ItemsSource = rows;
        int deviceCount = rows.Select(row => row.DeviceName).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Title = english ? "Before / after changes" : "Modifications avant / après";
        SummaryTextBlock.Text = english
            ? $"{rows.Count} change(s) across {deviceCount} device(s)."
            : $"{rows.Count} modification(s) sur {deviceCount} machine(s).";
        DeviceColumn.Header = english ? "Device" : "Machine";
        ParameterColumn.Header = english ? "Setting" : "Paramètre";
        BeforeColumn.Header = english ? "Before" : "Avant";
        AfterColumn.Header = english ? "After" : "Après";
        StatusColumn.Header = english ? "Status" : "État";
        CloseButton.Content = english ? "Close" : "Fermer";
    }

    private static string TranslateParameter(string value, bool english)
    {
        if (!english)
        {
            return value;
        }

        string translated = value switch
        {
            "Machine" => "Device",
            "Nom de machine" => "Device name",
            "Mode réseau" => "Network mode",
            "Latence" => "Latency",
            "Bits par échantillon" => "Bits per sample",
            "Adresse IP" => "IP address",
            _ => value
        };
        if (translated.StartsWith("Canal TX ", StringComparison.Ordinal))
        {
            translated = "TX channel " + translated[9..];
        }
        else if (translated.StartsWith("Canal RX ", StringComparison.Ordinal))
        {
            translated = "RX channel " + translated[9..];
        }

        return translated.Replace(" - nom", " - name", StringComparison.Ordinal);
    }

    private static string TranslateValue(string value, bool english)
    {
        if (!english)
        {
            return value;
        }

        string translated = value switch
        {
            "Absente" or "Absent" => "Absent",
            "Présente" => "Present",
            "Ajoutée" or "Ajouté" => "Added",
            "Supprimée" or "Supprimé" => "Removed",
            "Oui" => "Yes",
            "Non" => "No",
            "Automatique" => "Automatic",
            "Redondant" => "Redundant",
            _ => value
        };
        return translated.StartsWith("Fixe : ", StringComparison.Ordinal)
            ? "Static: " + translated[7..]
            : translated;
    }

    private static string TranslateStatus(string value, bool english)
    {
        if (!english)
        {
            return value;
        }

        return value switch
        {
            "Modifié" => "Modified",
            "Ajoutée" or "Ajouté" => "Added",
            "Supprimée" or "Supprimé" => "Removed",
            _ => value
        };
    }

    private sealed record DeviceChangeViewRow(
        string DeviceName,
        string Parameter,
        string Before,
        string After,
        string Status);
}
