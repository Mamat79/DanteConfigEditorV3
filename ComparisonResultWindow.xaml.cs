using System.Collections.ObjectModel;
using System.Windows;

namespace DanteConfigEditor;

public partial class ComparisonResultWindow : Window
{
    public ComparisonResultWindow(IEnumerable<ComparisonDisplayRow> rows)
    {
        InitializeComponent();
        ObservableCollection<ComparisonDisplayRow> materializedRows = new(rows);
        ComparisonGrid.ItemsSource = materializedRows;
        SummaryTextBlock.Text = $"{materializedRows.Count} différence(s) affichée(s).";
    }
}

public sealed record ComparisonDisplayRow(string Item, string OpenValue, string ComparedValue, string Status);
