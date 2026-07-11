using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class PatchWorkspaceWindow : Window
{
    private const string DragDataFormat = "DanteConfigEditor.VisualPatchSources";

    private readonly UiLanguage _language;
    private readonly DanteProject _project;
    private readonly PatchWorkspaceSession _session;
    private readonly HashSet<string> _ambiguousSourceNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<PatchSourceDescriptor> _visibleSources = [];
    private IReadOnlyList<PatchTargetDescriptor> _visibleTargets = [];
    private Point _dragStart;
    private bool _initializing = true;

    public PatchWorkspaceWindow(
        UiLanguage language,
        DanteProject project,
        bool useLightTheme,
        string? initialTxDeviceName = null,
        string? initialRxDeviceName = null)
    {
        InitializeComponent();
        _language = language;
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _session = new PatchWorkspaceSession(project.PatchMatrix.Subscriptions);

        ApplyTheme(useLightTheme);
        ApplyLanguage();
        PopulateDeviceSelectors(initialTxDeviceName, initialRxDeviceName);
        _initializing = false;
        RefreshSourceChannelsAndMatrixColumns();
        RefreshTargetRows();
    }

    public IReadOnlyList<PatchEditRequest> Edits => _session.Edits;

    private void PopulateDeviceSelectors(string? initialTxDeviceName, string? initialRxDeviceName)
    {
        string[] txDevices = _project.Devices
            .Where(device => device.TxCount > 0)
            .Select(device => device.Name)
            .ToArray();
        string[] rxDevices = _project.Devices
            .Where(device => device.RxCount > 0)
            .Select(device => device.Name)
            .ToArray();

        TxDeviceComboBox.ItemsSource = txDevices;
        RxDeviceComboBox.ItemsSource = rxDevices;
        TxDeviceComboBox.SelectedItem = FindDeviceName(txDevices, initialTxDeviceName) ?? txDevices.FirstOrDefault();
        RxDeviceComboBox.SelectedItem = FindDeviceName(rxDevices, initialRxDeviceName) ?? rxDevices.FirstOrDefault();
    }

    private void TxDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        RefreshSourceChannelsAndMatrixColumns();
        RefreshTargetRows();
    }

    private void RxDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing)
        {
            RefreshTargetRows();
        }
    }

    private void TxChannelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCommandState();
    }

    private void RxChannelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCommandState();
    }

    private void RefreshSourceChannelsAndMatrixColumns()
    {
        DanteDevice? device = _project.FindDevice(TxDeviceComboBox.SelectedItem as string);
        _visibleSources = device?.TxChannels
            .Select(channel => new PatchSourceDescriptor(
                device.Name,
                channel.DanteId,
                channel.PositionIndex,
                channel.DisplayName))
            .OrderBy(channel => channel.PositionIndex)
            .ToArray() ?? [];

        TxChannelListBox.ItemsSource = _visibleSources;
        _ambiguousSourceNames.Clear();
        foreach (IGrouping<string, PatchSourceDescriptor> duplicate in _visibleSources
                     .GroupBy(source => source.ChannelName, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            _ambiguousSourceNames.Add(duplicate.Key);
        }

        BuildMatrixColumns();
        if (_ambiguousSourceNames.Count > 0)
        {
            SetInfo(
                L(
                    "Cette machine contient des noms TX en double. Renommez-les avant de les utiliser dans la grille.",
                    "This device contains duplicate Tx names. Rename them before using them in the matrix."),
                warning: true);
        }
        else
        {
            SetInfo(L(
                "Sélectionnez plusieurs TX avec Ctrl ou Maj, puis choisissez le premier RX.",
                "Select multiple Tx channels with Ctrl or Shift, then choose the first Rx."));
        }
    }

    private void RefreshTargetRows()
    {
        int? selectedDanteId = (RxChannelListBox.SelectedItem as PatchRxListItem)?.Target.DanteId;
        DanteDevice? device = _project.FindDevice(RxDeviceComboBox.SelectedItem as string);
        _visibleTargets = device?.RxChannels
            .Select(channel => new PatchTargetDescriptor(
                device.Name,
                channel.DanteId,
                channel.PositionIndex,
                channel.DisplayName))
            .OrderBy(channel => channel.PositionIndex)
            .ToArray() ?? [];

        PatchRxListItem[] rows = _visibleTargets
            .Select(BuildRxListItem)
            .ToArray();
        RxChannelListBox.ItemsSource = rows;
        RxChannelListBox.SelectedItem = rows.FirstOrDefault(row => row.Target.DanteId == selectedDanteId)
            ?? rows.FirstOrDefault();

        MatrixGrid.ItemsSource = _visibleTargets.Select(BuildMatrixRow).ToArray();
        UpdateCommandState();
    }

    private PatchRxListItem BuildRxListItem(PatchTargetDescriptor target)
    {
        EffectivePatchAssignment assignment = _session.GetEffectiveAssignment(target);
        string source = assignment.IsActive
            ? $"{assignment.TxDeviceName} / {assignment.TxChannelName}"
            : L("Libre", "Free");
        string marker = assignment.IsPending ? L("  [modifié]", "  [changed]") : string.Empty;
        return new PatchRxListItem(target, $"{target.Display}   <-   {source}{marker}");
    }

    private PatchMatrixRow BuildMatrixRow(PatchTargetDescriptor target)
    {
        EffectivePatchAssignment assignment = _session.GetEffectiveAssignment(target);
        int assignedSourceIndex = FindAssignedSourceIndex(assignment);
        PatchMatrixCell[] cells = _visibleSources
            .Select((source, index) => new PatchMatrixCell(
                source,
                target,
                IsAssigned: index == assignedSourceIndex,
                IsPending: assignment.IsPending,
                Marker: index == assignedSourceIndex ? "●" : string.Empty,
                ToolTip: BuildCellToolTip(source, target, index == assignedSourceIndex, assignment.IsPending),
                AutomationName: BuildCellAutomationName(source, target, index == assignedSourceIndex)))
            .ToArray();

        return new PatchMatrixRow(target.Display, assignment.IsPending, cells);
    }

    private int FindAssignedSourceIndex(EffectivePatchAssignment assignment)
    {
        if (!assignment.IsActive)
        {
            return -1;
        }

        for (int index = 0; index < _visibleSources.Count; index++)
        {
            PatchSourceDescriptor source = _visibleSources[index];
            if (string.Equals(source.DeviceName, assignment.TxDeviceName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(source.ChannelName, assignment.TxChannelName, StringComparison.Ordinal))
            {
                // Le XML référence le nom du TX et non son numéro. En cas de
                // doublon, une seule cellule est montrée et toute nouvelle
                // affectation ambiguë reste bloquée.
                return index;
            }
        }

        return -1;
    }

    private void BuildMatrixColumns()
    {
        MatrixGrid.Columns.Clear();
        MatrixGrid.Columns.Add(new DataGridTextColumn
        {
            Header = L("Canal RX", "Rx channel"),
            Binding = new Binding(nameof(PatchMatrixRow.TargetDisplay)),
            Width = new DataGridLength(220),
            IsReadOnly = true
        });

        Style cellStyle = (Style)FindResource("MatrixCellToggleStyle");
        for (int index = 0; index < _visibleSources.Count; index++)
        {
            PatchSourceDescriptor source = _visibleSources[index];
            FrameworkElementFactory toggle = new(typeof(ToggleButton));
            toggle.SetValue(FrameworkElement.StyleProperty, cellStyle);
            toggle.SetBinding(FrameworkElement.TagProperty, new Binding($"Cells[{index}]"));
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding($"Cells[{index}].IsAssigned") { Mode = BindingMode.OneWay });
            toggle.SetBinding(ContentControl.ContentProperty, new Binding($"Cells[{index}].Marker"));
            toggle.SetBinding(ToolTipService.ToolTipProperty, new Binding($"Cells[{index}].ToolTip"));
            toggle.SetBinding(AutomationProperties.NameProperty, new Binding($"Cells[{index}].AutomationName"));
            toggle.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(MatrixCellButton_Click));

            MatrixGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = BuildMatrixHeader(source),
                CellTemplate = new DataTemplate { VisualTree = toggle },
                Width = new DataGridLength(96),
                IsReadOnly = true
            });
        }
    }

    private FrameworkElement BuildMatrixHeader(PatchSourceDescriptor source)
    {
        StackPanel panel = new() { MaxWidth = 88 };
        panel.Children.Add(new TextBlock
        {
            Text = $"TX {source.DanteId}",
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = source.ChannelName,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 11
        });
        ToolTipService.SetToolTip(panel, source.FullDisplay);
        AutomationProperties.SetName(panel, source.FullDisplay);
        return panel;
    }

    private void AssignSequentialButton_Click(object sender, RoutedEventArgs e)
    {
        PatchSourceDescriptor[] selectedSources = SelectedSources();
        if (selectedSources.Length == 0)
        {
            SetInfo(L("Sélectionnez au moins un canal TX.", "Select at least one Tx channel."), warning: true);
            return;
        }

        if (RxChannelListBox.SelectedItem is not PatchRxListItem targetRow)
        {
            SetInfo(L("Sélectionnez le premier canal RX.", "Select the first Rx channel."), warning: true);
            return;
        }

        AssignSources(selectedSources, targetRow.Target);
    }

    private void RemoveSelectedRxButton_Click(object sender, RoutedEventArgs e)
    {
        if (RxChannelListBox.SelectedItem is not PatchRxListItem targetRow)
        {
            SetInfo(L("Sélectionnez un canal RX à déconnecter.", "Select an Rx channel to disconnect."), warning: true);
            return;
        }

        _session.Remove(targetRow.Target);
        RefreshTargetRows();
        SetInfo(L("Déconnexion préparée.", "Disconnection staged."));
    }

    private void AssignSources(IReadOnlyList<PatchSourceDescriptor> sources, PatchTargetDescriptor firstTarget)
    {
        if (!SourcesAreUnambiguous(sources))
        {
            return;
        }

        try
        {
            SequentialPatchPlan plan = _session.AssignSequential(sources, _visibleTargets, firstTarget);
            RefreshTargetRows();

            if (plan.UnassignedSources.Count > 0)
            {
                SetInfo(
                    L(
                        $"{plan.Assignments.Count} affectation(s) préparée(s), {plan.UnassignedSources.Count} TX sans RX disponible.",
                        $"{plan.Assignments.Count} assignment(s) staged, {plan.UnassignedSources.Count} Tx channel(s) without an available Rx."),
                    warning: true);
            }
            else
            {
                SetInfo(L(
                    $"{plan.Assignments.Count} affectation(s) préparée(s).",
                    $"{plan.Assignments.Count} assignment(s) staged."));
            }
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private bool SourcesAreUnambiguous(IEnumerable<PatchSourceDescriptor> sources)
    {
        string[] ambiguous = sources
            .Where(source => _ambiguousSourceNames.Contains(source.ChannelName))
            .Select(source => source.ChannelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ambiguous.Length == 0)
        {
            return true;
        }

        SetInfo(
            L(
                $"Nom(s) TX ambigu(s) : {string.Join(", ", ambiguous)}. Renommez-les avant le patch.",
                $"Ambiguous Tx name(s): {string.Join(", ", ambiguous)}. Rename them before patching."),
            warning: true);
        return false;
    }

    private void MatrixCellButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: PatchMatrixCell cell })
        {
            return;
        }

        if (cell.IsAssigned)
        {
            _session.Remove(cell.Target);
            SetInfo(L("Déconnexion préparée.", "Disconnection staged."));
        }
        else
        {
            if (!SourcesAreUnambiguous([cell.Source]))
            {
                RefreshTargetRows();
                return;
            }

            _session.Assign(new PlannedPatchAssignment(cell.Source, cell.Target));
            SetInfo(L("Affectation préparée.", "Assignment staged."));
        }

        RefreshTargetRows();
    }

    private void TxChannelListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(TxChannelListBox);
    }

    private void TxChannelListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current = e.GetPosition(TxChannelListBox);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        PatchSourceDescriptor[] selectedSources = SelectedSources();
        if (selectedSources.Length == 0 || !SourcesAreUnambiguous(selectedSources))
        {
            return;
        }

        DataObject data = new();
        data.SetData(DragDataFormat, selectedSources);
        DragDrop.DoDragDrop(TxChannelListBox, data, DragDropEffects.Copy);
    }

    private void RxChannelListBox_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DragDataFormat) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void RxChannelListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DragDataFormat) is not PatchSourceDescriptor[] sources)
        {
            return;
        }

        DependencyObject? origin = e.OriginalSource as DependencyObject;
        ListBoxItem? container = origin is null
            ? null
            : ItemsControl.ContainerFromElement(RxChannelListBox, origin) as ListBoxItem;
        if (container?.DataContext is not PatchRxListItem targetRow)
        {
            SetInfo(L("Déposez les TX sur une ligne RX.", "Drop the Tx channels on an Rx row."), warning: true);
            return;
        }

        RxChannelListBox.SelectedItem = targetRow;
        AssignSources(sources, targetRow.Target);
        e.Handled = true;
    }

    private PatchSourceDescriptor[] SelectedSources()
    {
        return TxChannelListBox.SelectedItems
            .OfType<PatchSourceDescriptor>()
            .OrderBy(source => source.PositionIndex)
            .ToArray();
    }

    private void ResetPendingButton_Click(object sender, RoutedEventArgs e)
    {
        _session.Reset();
        RefreshTargetRows();
        SetInfo(L("Tous les changements visuels ont été annulés.", "All visual changes were discarded."));
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_session.HasChanges)
        {
            SetInfo(L("Aucun changement à appliquer.", "No changes to apply."), warning: true);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void UpdateCommandState()
    {
        bool hasRxSelection = RxChannelListBox.SelectedItem is PatchRxListItem;
        bool hasTxSelection = TxChannelListBox.SelectedItems.Count > 0;
        AssignSequentialButton.IsEnabled = hasRxSelection && hasTxSelection;
        RemoveSelectedRxButton.IsEnabled = hasRxSelection;
        ResetPendingButton.IsEnabled = _session.HasChanges;
        ApplyButton.IsEnabled = _session.HasChanges;

        string pending = _session.PendingCount == 0
            ? L("Aucun changement en attente", "No pending changes")
            : L(
                $"{_session.PendingCount} changement(s) en attente",
                $"{_session.PendingCount} pending change(s)");
        PendingHeaderTextBlock.Text = pending;
        PendingFooterTextBlock.Text = pending;
    }

    private void ApplyLanguage()
    {
        Title = L("Patch visuel", "Visual patch");
        TitleTextBlock.Text = Title;
        IntroTextBlock.Text = L(
            "Préparez les affectations puis appliquez-les en une seule opération.",
            "Stage assignments, then apply them in a single operation.");
        TxDeviceLabel.Content = L("Machine émettrice TX", "Tx transmitting device");
        RxDeviceLabel.Content = L("Machine réceptrice RX", "Rx receiving device");
        AssignmentTab.Header = L("Glisser-déposer et série", "Drag and drop / sequential");
        MatrixTab.Header = L("Grille de patch", "Patch matrix");
        TxListHeadingTextBlock.Text = L("Canaux TX disponibles", "Available Tx channels");
        RxListHeadingTextBlock.Text = L("Canaux RX et source actuelle", "Rx channels and current source");
        DragHintTextBlock.Text = L(
            "Sélectionnez un ou plusieurs TX puis déposez-les sur le premier RX.",
            "Select one or more Tx channels, then drop them on the first Rx.");
        AssignSequentialButton.Content = L("Affecter à partir du RX sélectionné", "Assign from selected Rx");
        RemoveSelectedRxButton.Content = L("Déconnecter le RX sélectionné", "Disconnect selected Rx");
        MatrixHintTextBlock.Text = L(
            "RX en lignes, TX en colonnes. Cliquez dans une case pour affecter ou retirer un patch.",
            "Rx channels are rows and Tx channels are columns. Click a cell to assign or remove a subscription.");
        ResetPendingButton.Content = L("Annuler les changements visuels", "Discard visual changes");
        CancelButton.Content = L("Fermer sans appliquer", "Close without applying");
        ApplyButton.Content = L("Appliquer au projet", "Apply to project");

        AutomationProperties.SetName(TxDeviceComboBox, TxDeviceLabel.Content.ToString()!);
        AutomationProperties.SetName(RxDeviceComboBox, RxDeviceLabel.Content.ToString()!);
        AutomationProperties.SetName(TxChannelListBox, TxListHeadingTextBlock.Text);
        AutomationProperties.SetName(RxChannelListBox, RxListHeadingTextBlock.Text);
        AutomationProperties.SetName(MatrixGrid, MatrixTab.Header.ToString()!);
    }

    private void ApplyTheme(bool useLightTheme)
    {
        SetBrush("WindowBackgroundBrush", useLightTheme ? Color.FromRgb(244, 247, 251) : Color.FromRgb(16, 20, 31));
        SetBrush("SurfaceBrush", useLightTheme ? Colors.White : Color.FromRgb(23, 29, 43));
        SetBrush("SurfaceAltBrush", useLightTheme ? Color.FromRgb(233, 238, 246) : Color.FromRgb(32, 40, 56));
        SetBrush("TextBrush", useLightTheme ? Color.FromRgb(17, 24, 39) : Color.FromRgb(246, 248, 251));
        SetBrush("MutedTextBrush", useLightTheme ? Color.FromRgb(71, 85, 105) : Color.FromRgb(170, 180, 197));
        SetBrush("BorderLineBrush", useLightTheme ? Color.FromRgb(203, 213, 225) : Color.FromRgb(51, 64, 87));
        SetBrush("WarningBrush", useLightTheme ? Color.FromRgb(180, 83, 9) : Color.FromRgb(245, 158, 11));
    }

    private void SetBrush(string key, Color color)
    {
        Resources[key] = new SolidColorBrush(color);
    }

    private void SetInfo(string message, bool warning = false)
    {
        InfoTextBlock.Text = message;
        InfoTextBlock.Foreground = warning
            ? (Brush)FindResource("WarningBrush")
            : (Brush)FindResource("MutedTextBrush");
    }

    private string BuildCellToolTip(
        PatchSourceDescriptor source,
        PatchTargetDescriptor target,
        bool isAssigned,
        bool isPending)
    {
        string action = isAssigned
            ? L("Cliquer pour déconnecter", "Click to disconnect")
            : L("Cliquer pour affecter", "Click to assign");
        string pending = isPending ? L(" - changement en attente", " - pending change") : string.Empty;
        return $"{target.FullDisplay} <- {source.FullDisplay}\n{action}{pending}";
    }

    private string BuildCellAutomationName(
        PatchSourceDescriptor source,
        PatchTargetDescriptor target,
        bool isAssigned)
    {
        return isAssigned
            ? L($"Patch actif : {source.FullDisplay} vers {target.FullDisplay}", $"Active subscription: {source.FullDisplay} to {target.FullDisplay}")
            : L($"Affecter {source.FullDisplay} vers {target.FullDisplay}", $"Assign {source.FullDisplay} to {target.FullDisplay}");
    }

    private static string? FindDeviceName(IEnumerable<string> names, string? requested)
    {
        return names.FirstOrDefault(name => string.Equals(name, requested, StringComparison.OrdinalIgnoreCase));
    }

    private string L(string french, string english)
    {
        return _language == UiLanguage.English ? english : french;
    }

    private sealed record PatchRxListItem(PatchTargetDescriptor Target, string Display);

    private sealed record PatchMatrixRow(
        string TargetDisplay,
        bool IsPending,
        IReadOnlyList<PatchMatrixCell> Cells);

    private sealed record PatchMatrixCell(
        PatchSourceDescriptor Source,
        PatchTargetDescriptor Target,
        bool IsAssigned,
        bool IsPending,
        string Marker,
        string ToolTip,
        string AutomationName);
}
