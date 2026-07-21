using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac;

public partial class MainWindow
{
    private readonly ObservableCollection<SynopticDeviceRow> _synopticRows = [];
    private SynopticLayoutDocument? _synopticLayout;
    private string? _synopticLayoutPath;

    private void RefreshSynopticWorkspace(bool captureRows = true)
    {
        DataGrid grid = FindControl<DataGrid>("SynopticDeviceGrid")!;
        Canvas canvas = FindControl<Canvas>("SynopticCanvas")!;
        if (_project is null)
        {
            _synopticRows.Clear();
            grid.ItemsSource = _synopticRows;
            canvas.Children.Clear();
            _synopticLayout = null;
            _synopticLayoutPath = null;
            FindControl<ComboBox>("SynopticKnownLocationComboBox")!.ItemsSource = null;
            FindControl<TextBox>("SynopticLocationTextBox")!.Text = string.Empty;
            FindControl<TextBlock>("SynopticSummaryText")!.Text = LocalizationService.Text(_language, "Status.NoFileLoaded");
            return;
        }

        string path = SynopticExportService.ResolveLayoutPath(_project);
        if (_synopticLayout is null || !string.Equals(path, _synopticLayoutPath, StringComparison.OrdinalIgnoreCase))
        {
            _synopticLayout = SynopticExportService.LoadOrCreate(_project);
            _synopticLayoutPath = path;
        }
        else
        {
            if (captureRows)
            {
                CaptureSynopticRows();
            }
            _synopticLayout = SynopticExportService.Synchronize(_project, _synopticLayout);
        }

        string[] selected = grid.SelectedItems.OfType<SynopticDeviceRow>().Select(row => row.DeviceIdentity).ToArray();
        Dictionary<string, DanteDevice> devices = _project.Devices
            .GroupBy(device => device.StableIdentity, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _synopticRows.Clear();
        foreach (SynopticDevicePlacement placement in _synopticLayout.Devices.OrderBy(item => item.Order))
        {
            if (devices.TryGetValue(placement.DeviceIdentity, out DanteDevice? device))
            {
                _synopticRows.Add(new SynopticDeviceRow(placement, device.TxCount, device.RxCount));
            }
        }
        grid.ItemsSource = _synopticRows;
        foreach (SynopticDeviceRow row in _synopticRows.Where(row => selected.Contains(row.DeviceIdentity, StringComparer.OrdinalIgnoreCase)))
        {
            grid.SelectedItems.Add(row);
        }
        RefreshSynopticLocationChoices();
        RenderSynopticPreview();
    }

    private void CaptureSynopticRows()
    {
        if (_synopticLayout is null)
        {
            return;
        }

        Dictionary<string, SynopticDevicePlacement> placements = _synopticLayout.Devices
            .ToDictionary(item => item.DeviceIdentity, StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < _synopticRows.Count; index++)
        {
            SynopticDeviceRow row = _synopticRows[index];
            if (placements.TryGetValue(row.DeviceIdentity, out SynopticDevicePlacement? placement))
            {
                placement.DeviceName = row.DeviceName;
                placement.Location = row.Location.Trim();
                placement.IsVisible = row.IsVisible;
                placement.Order = index;
            }
        }
    }

    private void RenderSynopticPreview()
    {
        Canvas canvas = FindControl<Canvas>("SynopticCanvas")!;
        canvas.Children.Clear();
        if (_project is null || _synopticLayout is null)
        {
            return;
        }

        SynopticDiagram diagram = SynopticExportService.BuildDiagram(_project, _synopticLayout, _language == UiLanguage.English);
        canvas.Width = diagram.Width;
        canvas.Height = diagram.Height;

        foreach (SynopticLocationArea location in diagram.Locations)
        {
            Border area = new()
            {
                Width = location.Width,
                Height = location.Height,
                Background = Brushes.White,
                BorderBrush = ColorBrush(location.Color),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(7)
            };
            Canvas.SetLeft(area, location.X);
            Canvas.SetTop(area, location.Y);
            canvas.Children.Add(area);

            Border header = new()
            {
                Width = location.Width,
                Height = 36,
                Background = ColorBrush(location.Color),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Child = new TextBlock
                {
                    Text = location.Name,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(13, 0, 8, 0)
                }
            };
            Canvas.SetLeft(header, location.X);
            Canvas.SetTop(header, location.Y);
            canvas.Children.Add(header);
        }

        foreach (SynopticCable cable in diagram.Cables)
        {
            AddCableSegments(canvas, cable, Brushes.White, 8, 0.96);
            AddCableSegments(canvas, cable, ColorBrush(cable.Color), 3.5, 0.92);
        }

        foreach (SynopticDeviceNode node in diagram.Devices)
        {
            StackPanel content = new() { Spacing = 3 };
            content.Children.Add(new TextBlock { Text = node.Name, Foreground = ColorBrush("#172033"), FontWeight = FontWeight.Bold, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis });
            content.Children.Add(new TextBlock { Text = FriendlyLine(node), Foreground = ColorBrush("#526070"), FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis });
            content.Children.Add(new TextBlock { Text = $"TX {node.TxCount}   RX {node.RxCount}", Foreground = ColorBrush("#526070"), FontSize = 11 });
            Border device = new()
            {
                Width = node.Width,
                Height = node.Height,
                Background = Brushes.White,
                BorderBrush = ColorBrush(node.Color),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 10, 10, 8),
                Child = content
            };
            Canvas.SetLeft(device, node.X);
            Canvas.SetTop(device, node.Y);
            canvas.Children.Add(device);
        }

        if (diagram.Cables.Count <= 18)
        {
            for (int index = 0; index < diagram.Cables.Count; index++)
            {
                SynopticCable cable = diagram.Cables[index];
                Border badge = new()
                {
                    Width = 18,
                    Height = 18,
                    Background = Brushes.White,
                    BorderBrush = ColorBrush(cable.Color),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(9),
                    Child = new TextBlock
                    {
                        Text = (index + 1).ToString(),
                        Foreground = ColorBrush("#172033"),
                        FontWeight = FontWeight.Bold,
                        FontSize = 8,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    }
                };
                Canvas.SetLeft(badge, Math.Max(4, cable.LabelX - 9));
                Canvas.SetTop(badge, Math.Max(4, cable.LabelY - 9));
                canvas.Children.Add(badge);
            }
        }

        RenderSynopticLegend(canvas, diagram);

        FindControl<TextBlock>("SynopticSummaryText")!.Text = _language == UiLanguage.English
            ? $"{diagram.Devices.Count} devices - {diagram.Cables.Count} grouped cables - {diagram.HiddenDeviceCount} hidden"
            : $"{diagram.Devices.Count} machines - {diagram.Cables.Count} câbles regroupés - {diagram.HiddenDeviceCount} masquée(s)";
    }

    private void ApplySynopticLocationButton_Click(object? sender, RoutedEventArgs e)
    {
        DataGrid grid = FindControl<DataGrid>("SynopticDeviceGrid")!;
        SynopticDeviceRow[] selected = grid.SelectedItems.OfType<SynopticDeviceRow>().ToArray();
        if (selected.Length == 0)
        {
            _ = ShowInfoAsync(L("Aucune machine sélectionnée", "No device selected"), L("Sélectionnez une ou plusieurs machines.", "Select one or several devices."));
            return;
        }
        string location = FindControl<TextBox>("SynopticLocationTextBox")!.Text?.Trim() ?? string.Empty;
        foreach (SynopticDeviceRow row in selected)
        {
            row.Location = location;
        }
        SaveAndRefreshSynoptic();
    }

    private void SynopticKnownLocationComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FindControl<ComboBox>("SynopticKnownLocationComboBox")?.SelectedItem is string location)
        {
            FindControl<TextBox>("SynopticLocationTextBox")!.Text = location;
        }
    }

    private void SynopticVisibilityCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(SaveAndRefreshSynoptic);
    }

    private void RefreshSynopticLocationChoices()
    {
        FindControl<ComboBox>("SynopticKnownLocationComboBox")!.ItemsSource = _synopticRows
            .Select(row => row.Location.Trim())
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(location => location, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private void ShowAllSynopticDevicesButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (SynopticDeviceRow row in _synopticRows)
        {
            row.IsVisible = true;
        }
        SaveAndRefreshSynoptic();
    }

    private void HideSelectedSynopticDevicesButton_Click(object? sender, RoutedEventArgs e)
    {
        foreach (SynopticDeviceRow row in FindControl<DataGrid>("SynopticDeviceGrid")!.SelectedItems.OfType<SynopticDeviceRow>())
        {
            row.IsVisible = false;
        }
        SaveAndRefreshSynoptic();
    }

    private void MoveSynopticDeviceUpButton_Click(object? sender, RoutedEventArgs e) => MoveSelectedSynopticRow(-1);

    private void MoveSynopticDeviceDownButton_Click(object? sender, RoutedEventArgs e) => MoveSelectedSynopticRow(1);

    private void MoveSelectedSynopticRow(int offset)
    {
        DataGrid grid = FindControl<DataGrid>("SynopticDeviceGrid")!;
        if (grid.SelectedItem is not SynopticDeviceRow selected)
        {
            return;
        }
        int oldIndex = _synopticRows.IndexOf(selected);
        int newIndex = Math.Clamp(oldIndex + offset, 0, _synopticRows.Count - 1);
        if (oldIndex == newIndex)
        {
            return;
        }
        _synopticRows.Move(oldIndex, newIndex);
        grid.SelectedItem = selected;
        SaveAndRefreshSynoptic();
    }

    private void RefreshSynopticButton_Click(object? sender, RoutedEventArgs e) => SaveAndRefreshSynoptic();

    private void SaveAndRefreshSynoptic()
    {
        if (_project is null || _synopticLayout is null)
        {
            return;
        }
        try
        {
            CaptureSynopticRows();
            SynopticExportService.SaveLayout(_project, _synopticLayout);
            RefreshSynopticLocationChoices();
            RenderSynopticPreview();
        }
        catch (Exception exception)
        {
            _ = ShowErrorAsync(LocalizationService.Text(_language, "Dialog.SynopticLayoutErrorTitle"), exception);
        }
    }

    private async void ExportSynopticButton_Click(object? sender, RoutedEventArgs e)
        => await ExportSynopticAsync("svg");

    private async void ExportSynopticPdfButton_Click(object? sender, RoutedEventArgs e)
        => await ExportSynopticAsync("pdf");

    private async Task ExportSynopticAsync(string format)
    {
        if (_project is null || _synopticLayout is null)
        {
            return;
        }
        bool pdf = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase);
        string extension = pdf ? "pdf" : "svg";
        IStorageFile? file = await PickSaveFileAsync(
            Path.GetFileNameWithoutExtension(_project.OriginalFilePath) + "_synoptique." + extension,
            extension,
            extension.ToUpperInvariant(),
            [$"*.{extension}"]);
        string? path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            CaptureSynopticRows();
            SynopticExportService.SaveLayout(_project, _synopticLayout);
            SynopticDiagram diagram = SynopticExportService.BuildDiagram(_project, _synopticLayout, _language == UiLanguage.English);
            if (pdf)
            {
                SynopticExportService.ExportPdf(path, diagram, _language == UiLanguage.English);
            }
            else
            {
                SynopticExportService.ExportSvg(path, diagram, _language == UiLanguage.English);
            }
            SetStatus(_language == UiLanguage.English
                ? $"Synoptic {format.ToUpperInvariant()} exported."
                : $"Synoptique {format.ToUpperInvariant()} exporté.");
            if (FindControl<CheckBox>("OpenSynopticAfterExportCheckBox")!.IsChecked == true)
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(L("Export du synoptique impossible", "Synoptic export unavailable"), exception);
        }
    }

    private static string FriendlyLine(SynopticDeviceNode node)
    {
        return string.IsNullOrWhiteSpace(node.FriendlyName) || string.Equals(node.FriendlyName, node.Name, StringComparison.OrdinalIgnoreCase)
            ? node.Location
            : node.FriendlyName;
    }

    private static SolidColorBrush ColorBrush(string color) => new(Color.Parse(color));

    private static void AddCableSegments(Canvas canvas, SynopticCable cable, IBrush stroke, double thickness, double opacity)
    {
        IReadOnlyList<SynopticRoutePoint> points = cable.RoutePoints.Count == 0
            ? [new SynopticRoutePoint(cable.StartX, cable.StartY), new SynopticRoutePoint(cable.EndX, cable.EndY)]
            : cable.RoutePoints;
        for (int index = 1; index < points.Count; index++)
        {
            Avalonia.Controls.Shapes.Line line = new()
            {
                StartPoint = new Point(points[index - 1].X, points[index - 1].Y),
                EndPoint = new Point(points[index].X, points[index].Y),
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeLineCap = PenLineCap.Round,
                Opacity = opacity
            };
            canvas.Children.Add(line);
        }
    }

    private void RenderSynopticLegend(Canvas canvas, SynopticDiagram diagram)
    {
        double legendX = diagram.LegendX;
        double topMargin = diagram.Locations.Count == 0 ? 88 : diagram.Locations.Min(location => location.Y);
        Border panel = new()
        {
            Width = diagram.LegendWidth,
            Height = diagram.Height - topMargin - 76,
            Background = Brushes.White,
            BorderBrush = ColorBrush("#CBD5E1"),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(8)
        };
        Canvas.SetLeft(panel, legendX);
        Canvas.SetTop(panel, topMargin);
        canvas.Children.Add(panel);

        TextBlock title = new()
        {
            Text = _language == UiLanguage.English ? "Grouped subscriptions" : "Liaisons regroupées",
            Foreground = ColorBrush("#172033"),
            FontSize = 15,
            FontWeight = FontWeight.Bold
        };
        Canvas.SetLeft(title, legendX + 18);
        Canvas.SetTop(title, topMargin + 16);
        canvas.Children.Add(title);

        int columns = Math.Max(1, diagram.LegendColumns);
        double gap = 10;
        double itemWidth = (diagram.LegendWidth - 24 - (columns - 1) * gap) / columns;
        double y = topMargin + 48;
        for (int rowStart = 0; rowStart < diagram.Cables.Count; rowStart += columns)
        {
            SynopticCable[] row = diagram.Cables.Skip(rowStart).Take(columns).ToArray();
            double rowHeight = row.Max(cable => Math.Max(62, 50 + cable.Labels.Count * 16));
            for (int column = 0; column < row.Length; column++)
            {
                int index = rowStart + column;
                SynopticCable cable = row[column];
                StackPanel content = new() { Spacing = 5 };
                content.Children.Add(new TextBlock
                {
                    Text = $"{index + 1}. {cable.SourceDevice} → {cable.TargetDevice}",
                    Foreground = ColorBrush("#172033"),
                    FontWeight = FontWeight.Bold,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                content.Children.Add(new TextBlock
                {
                    Text = string.Join(Environment.NewLine, cable.Labels),
                    Foreground = ColorBrush("#526070"),
                    FontSize = 10.5,
                    TextWrapping = TextWrapping.Wrap
                });
                Border item = new()
                {
                    Width = itemWidth,
                    Height = rowHeight - 6,
                    Background = ColorBrush("#F8FAFC"),
                    BorderBrush = ColorBrush(cable.Color),
                    BorderThickness = new Thickness(5, 1, 1, 1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10, 7, 8, 6),
                    Child = content
                };
                Canvas.SetLeft(item, legendX + 12 + column * (itemWidth + gap));
                Canvas.SetTop(item, y);
                canvas.Children.Add(item);
            }
            y += rowHeight;
        }
    }

    private sealed class SynopticDeviceRow
    {
        public SynopticDeviceRow(SynopticDevicePlacement placement, int txCount, int rxCount)
        {
            DeviceIdentity = placement.DeviceIdentity;
            DeviceName = placement.DeviceName;
            Location = placement.Location;
            IsVisible = placement.IsVisible;
            TxCount = txCount;
            RxCount = rxCount;
        }

        public string DeviceIdentity { get; }
        public string DeviceName { get; }
        public int TxCount { get; }
        public int RxCount { get; }
        public string ChannelCount => $"{TxCount}/{RxCount}";
        public bool IsVisible { get; set; }
        public string Location { get; set; }
    }
}
