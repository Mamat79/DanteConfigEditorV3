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

public partial class PatchWorkspaceView : UserControl
{
    private readonly UiLanguage _language;
    private readonly DanteProject _project;
    private readonly PatchWorkspaceSession _session;
    private readonly bool _returnEditsOnly;
    private readonly bool _lockRxDeviceSelection;
    private readonly bool _embedded;
    private readonly HashSet<string> _ambiguousSourceNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ToggleButton> _matrixGestureHighlights = [];
    private IReadOnlyList<PatchSourceDescriptor> _visibleSources = [];
    private IReadOnlyList<PatchTargetDescriptor> _visibleTargets = [];
    private PatchMatrixCell? _matrixGestureStart;
    private PatchMatrixCell? _matrixGestureCurrent;
    private bool _matrixGestureActive;
    private bool _initializing = true;

    public PatchWorkspaceView(
        UiLanguage language,
        DanteProject project,
        bool useLightTheme,
        string? initialTxDeviceName = null,
        string? initialRxDeviceName = null,
        IEnumerable<PatchEditRequest>? initialEdits = null,
        bool returnEditsOnly = false,
        bool lockRxDeviceSelection = false,
        bool embedded = false)
    {
        InitializeComponent();
        _language = language;
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _session = new PatchWorkspaceSession(project.PatchMatrix.Subscriptions, initialEdits);
        _returnEditsOnly = returnEditsOnly;
        _lockRxDeviceSelection = lockRxDeviceSelection;
        _embedded = embedded;

        ApplyTheme(useLightTheme);
        ApplyLanguage();
        PopulateDeviceSelectors(initialTxDeviceName, initialRxDeviceName);
        _initializing = false;
        RefreshSourceChannelsAndMatrixColumns();
        RefreshTargetRows();
    }

    public IReadOnlyList<PatchEditRequest> Edits => _session.Edits;

    public bool HasChanges => _session.HasChanges;

    public string? SelectedTxDeviceName => TxDeviceComboBox.SelectedItem as string;

    public string? SelectedRxDeviceName => RxDeviceComboBox.SelectedItem as string;

    public event EventHandler? ApplyRequested;

    public event EventHandler? CancelRequested;

    public void ResetPendingChanges()
    {
        _session.Reset();
        RefreshTargetRows();
        SetInfo(L("Tous les changements Easy patch ont été appliqués.", "All Easy patch changes were applied."));
    }

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
        RxDeviceComboBox.IsEnabled = !_lockRxDeviceSelection;
        UpdateDeviceNavigationState();
    }

    private void PreviousRxDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        MoveDeviceSelection(RxDeviceComboBox, -1);
    }

    private void NextRxDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        MoveDeviceSelection(RxDeviceComboBox, 1);
    }

    private void PreviousTxDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        MoveDeviceSelection(TxDeviceComboBox, -1);
    }

    private void NextTxDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        MoveDeviceSelection(TxDeviceComboBox, 1);
    }

    private static void MoveDeviceSelection(ComboBox selector, int offset)
    {
        if (!selector.IsEnabled || selector.Items.Count <= 1)
        {
            return;
        }

        int currentIndex = selector.SelectedIndex >= 0 ? selector.SelectedIndex : 0;
        selector.SelectedIndex = (currentIndex + offset + selector.Items.Count) % selector.Items.Count;
    }

    private void UpdateDeviceNavigationState()
    {
        bool canMoveRx = !_lockRxDeviceSelection && RxDeviceComboBox.Items.Count > 1;
        PreviousRxDeviceButton.IsEnabled = canMoveRx;
        NextRxDeviceButton.IsEnabled = canMoveRx;

        bool canMoveTx = TxDeviceComboBox.Items.Count > 1;
        PreviousTxDeviceButton.IsEnabled = canMoveTx;
        NextTxDeviceButton.IsEnabled = canMoveTx;
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
        int? selectedRangeDanteId = (RangeStartTxComboBox.SelectedItem as PatchSourceDescriptor)?.DanteId;
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
        RangeStartTxComboBox.ItemsSource = _visibleSources;
        RangeStartTxComboBox.SelectedItem = _visibleSources.FirstOrDefault(source => source.DanteId == selectedRangeDanteId)
            ?? _visibleSources.FirstOrDefault();
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
                "Sélectionnez les TX et les RX, puis prévisualisez la sélection ou une plage.",
                "Select the Tx and Rx channels, then preview the selection or a range."));
        }
    }

    private void RefreshTargetRows()
    {
        string? requestedDeviceName = RxDeviceComboBox.SelectedItem as string;
        HashSet<int> selectedDanteIds = RxChannelListBox.SelectedItems
            .OfType<PatchRxListItem>()
            .Where(row => string.Equals(row.Target.DeviceName, requestedDeviceName, StringComparison.OrdinalIgnoreCase))
            .Select(row => row.Target.DanteId)
            .ToHashSet();
        int? selectedRangeDanteId = (RangeStartRxComboBox.SelectedItem as PatchTargetDescriptor)?.DanteId;
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
        foreach (PatchRxListItem row in rows.Where(row => selectedDanteIds.Contains(row.Target.DanteId)))
        {
            RxChannelListBox.SelectedItems.Add(row);
        }

        if (RxChannelListBox.SelectedItems.Count == 0 && rows.Length > 0)
        {
            RxChannelListBox.SelectedItem = rows[0];
        }

        RangeStartRxComboBox.ItemsSource = _visibleTargets;
        RangeStartRxComboBox.SelectedItem = _visibleTargets.FirstOrDefault(target => target.DanteId == selectedRangeDanteId)
            ?? _visibleTargets.FirstOrDefault();

        MatrixGrid.ItemsSource = _visibleTargets
            .Select((target, targetIndex) => BuildMatrixRow(target, targetIndex))
            .ToArray();
        RefreshPendingPreview();
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

    private PatchMatrixRow BuildMatrixRow(PatchTargetDescriptor target, int targetIndex)
    {
        EffectivePatchAssignment assignment = _session.GetEffectiveAssignment(target);
        int assignedSourceIndex = FindAssignedSourceIndex(assignment);
        PatchMatrixCell[] cells = _visibleSources
            .Select((source, index) => new PatchMatrixCell(
                source,
                target,
                SourceIndex: index,
                TargetIndex: targetIndex,
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
            Width = new DataGridLength(190),
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
                Width = new DataGridLength(32),
                IsReadOnly = true
            });
        }
    }

    private FrameworkElement BuildMatrixHeader(PatchSourceDescriptor source)
    {
        TextBlock header = new()
        {
            Text = source.DanteId.ToString("000"),
            TextWrapping = TextWrapping.NoWrap,
            TextAlignment = TextAlignment.Center,
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Width = 28
        };
        ToolTipService.SetToolTip(header, source.FullDisplay);
        AutomationProperties.SetName(header, source.FullDisplay);
        return header;
    }

    private void PreviewSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StagePlanAsPreview(BuildSelectionPlan());
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private void ApplySelectionDirectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPlanDirectly(BuildSelectionPlan());
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private PatchAssignmentPlan BuildSelectionPlan()
    {
        return PatchAssignmentPlanner.PlanSelection(SelectedSources(), SelectedTargets());
    }

    private void PreviewRangeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StagePlanAsPreview(BuildRangePlan());
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private void ApplyRangeDirectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyPlanDirectly(BuildRangePlan());
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private PatchAssignmentPlan BuildRangePlan()
    {
        if (RangeStartTxComboBox.SelectedItem is not PatchSourceDescriptor firstSource
            || RangeStartRxComboBox.SelectedItem is not PatchTargetDescriptor firstTarget
            || !int.TryParse(RangeCountTextBox.Text, out int count))
        {
            throw new InvalidOperationException(L(
                "Renseignez le premier TX, le premier RX et un nombre de canaux valide.",
                "Choose the first Tx, first Rx and a valid channel count."));
        }

        return PatchAssignmentPlanner.PlanRange(
            _visibleSources,
            firstSource,
            _visibleTargets,
            firstTarget,
            count);
    }

    private void StagePlanAsPreview(PatchAssignmentPlan plan)
    {
        if (!SourcesAreUnambiguous(plan.Assignments.Select(assignment => assignment.Source)))
        {
            return;
        }

        try
        {
            PatchBatchPreview preview = _session.BuildPreview(plan.Assignments);
            PatchConflictResolution resolution = ChooseConflictResolution(preview);
            PatchStageResult result = _session.StagePreview(preview, resolution);
            if (result.IsCancelled)
            {
                SetInfo(L("Prévisualisation annulée : le lot reste inchangé.", "Preview cancelled: the batch is unchanged."), warning: true);
                return;
            }

            string message = L(
                $"{result.StagedCount} changement(s) prévisualisé(s) et ajouté(s) au lot, {result.SkippedConflictCount} conflit(s) ignoré(s), {result.UnchangedCount} ligne(s) inchangée(s).",
                $"{result.StagedCount} change(s) previewed and added to the batch, {result.SkippedConflictCount} conflict(s) skipped, {result.UnchangedCount} row(s) unchanged.");
            RefreshTargetRows();
            SetInfo(message, warning: result.SkippedConflictCount > 0);
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private void RefreshPendingPreview()
    {
        IReadOnlyList<PendingPatchChange> changes = _session.PendingChanges;
        if (changes.Count == 0)
        {
            PreviewGrid.ItemsSource = Array.Empty<PatchPreviewRow>();
            PreviewGroupBox.Visibility = Visibility.Collapsed;
            PreviewSummaryTextBlock.Text = L("Aucun changement dans le lot.", "No changes in the batch.");
            return;
        }

        PatchPreviewRow[] rows = changes.Select(BuildPendingPreviewRow).ToArray();
        PreviewGrid.ItemsSource = rows;
        PreviewGroupBox.Visibility = Visibility.Visible;
        int creations = changes.Count(change => change.IsCreation);
        int removals = changes.Count(change => change.IsRemoval);
        int replacements = changes.Count - creations - removals;
        PreviewSummaryTextBlock.Text = L(
            $"Lot cumulatif : {changes.Count} changement(s), {creations} création(s), {replacements} remplacement(s), {removals} déconnexion(s). Le XML n'est pas encore modifié.",
            $"Cumulative batch: {changes.Count} change(s), {creations} new, {replacements} replacement(s), {removals} disconnection(s). The XML is not modified yet.");
    }

    private PatchPreviewRow BuildPendingPreviewRow(PendingPatchChange change)
    {
        string action = change.IsRemoval
            ? L("Déconnecter", "Disconnect")
            : change.IsCreation
                ? L("Créer", "Create")
                : L("Remplacer", "Replace");
        string actionKey = change.IsRemoval
            ? "Remove"
            : change.IsCreation ? "Create" : "Replace";

        return new PatchPreviewRow(
            DescribeTarget(change),
            DescribeSource(change.OriginalTxDeviceName, change.OriginalTxChannelName),
            DescribeSource(change.DesiredTxDeviceName, change.DesiredTxChannelName),
            action,
            actionKey);
    }

    private string DescribeTarget(PendingPatchChange change)
    {
        DanteChannel? channel = _project.FindDevice(change.RxDeviceName)?.RxChannels
            .FirstOrDefault(item => item.DanteId == change.RxDanteId);
        return channel is null
            ? $"{change.RxDeviceName} / {change.RxDanteId:000}"
            : $"{change.RxDeviceName} / {change.RxDanteId:000} - {channel.DisplayName}";
    }

    private string DescribeSource(string? deviceName, string? channelName)
    {
        return string.IsNullOrWhiteSpace(deviceName)
            ? L("Libre", "Free")
            : $"{deviceName} / {channelName}";
    }

    private void ApplyPlanDirectly(PatchAssignmentPlan plan)
    {
        if (!SourcesAreUnambiguous(plan.Assignments.Select(assignment => assignment.Source)))
        {
            return;
        }

        if (_session.HasChanges)
        {
            MessageBoxResult pendingChoice = ShowConfirmation(
                L(
                    $"Cette action appliquera aussi les {_session.PendingCount} changement(s) déjà ajouté(s) au lot. Continuer ?",
                    $"This action will also apply the {_session.PendingCount} change(s) already added to the batch. Continue?"),
                L("Appliquer directement", "Apply directly"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (pendingChoice != MessageBoxResult.Yes)
            {
                return;
            }
        }

        PatchBatchPreview preview = _session.BuildPreview(plan.Assignments);
        PatchConflictResolution resolution = ChooseConflictResolution(preview);
        PatchStageResult result = _session.StagePreview(preview, resolution);
        if (result.IsCancelled)
        {
            SetInfo(L("Application directe annulée.", "Direct apply cancelled."), warning: true);
            return;
        }

        RefreshTargetRows();
        if (!_session.HasChanges)
        {
            SetInfo(L(
                $"Aucun changement à appliquer ({result.SkippedConflictCount} conflit(s) ignoré(s), {result.UnchangedCount} ligne(s) inchangée(s)).",
                $"No change to apply ({result.SkippedConflictCount} conflict(s) skipped, {result.UnchangedCount} row(s) unchanged)."),
                warning: result.SkippedConflictCount > 0);
            return;
        }

        SetInfo(L("Application directe en cours.", "Applying changes directly."));
        ApplyRequested?.Invoke(this, EventArgs.Empty);
    }

    private PatchConflictResolution ChooseConflictResolution(PatchBatchPreview preview)
    {
        if (!preview.HasConflicts)
        {
            return PatchConflictResolution.Replace;
        }

        MessageBoxResult choice = ShowConfirmation(
            L(
                $"{preview.ReplaceCount} RX possède(nt) déjà une source. Oui = remplacer, Non = ignorer ces conflits, Annuler = ne rien appliquer.",
                $"{preview.ReplaceCount} Rx channel(s) already have a source. Yes = replace, No = skip these conflicts, Cancel = apply nothing."),
            L("Conflits de patch", "Patch conflicts"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        return choice switch
        {
            MessageBoxResult.Yes => PatchConflictResolution.Replace,
            MessageBoxResult.No => PatchConflictResolution.Skip,
            _ => PatchConflictResolution.Cancel
        };
    }

    private void RemoveSelectedRxButton_Click(object sender, RoutedEventArgs e)
    {
        PatchTargetDescriptor[] targets = SelectedTargets();
        if (targets.Length == 0)
        {
            SetInfo(L("Sélectionnez au moins un canal RX à déconnecter.", "Select at least one Rx channel to disconnect."), warning: true);
            return;
        }

        MessageBoxResult confirmation = ShowConfirmation(
            L(
                $"Ajouter la déconnexion de {targets.Length} canal(aux) RX sélectionné(s) au lot ?",
                $"Add the disconnection of {targets.Length} selected Rx channel(s) to the batch?"),
            L("Confirmer la déconnexion", "Confirm disconnection"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            int removed = _session.RemoveMany(targets);
            RefreshTargetRows();
            SetInfo(L($"{removed} déconnexion(s) ajoutée(s) au lot.", $"{removed} disconnection(s) added to the batch."));
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

        ExecuteMatrixGesture(cell, cell);
    }

    private void MatrixGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!TryGetMatrixCell(e.GetPosition(MatrixGrid), out PatchMatrixCell cell))
        {
            return;
        }

        e.Handled = true;
        _matrixGestureActive = true;
        _matrixGestureStart = cell;
        _matrixGestureCurrent = cell;
        MatrixGrid.CaptureMouse();
        UpdateMatrixGestureHighlight();
    }

    private void MatrixGrid_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_matrixGestureActive)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            CancelMatrixGesture();
            return;
        }

        if (TryGetMatrixCell(e.GetPosition(MatrixGrid), out PatchMatrixCell cell)
            && (_matrixGestureCurrent?.SourceIndex != cell.SourceIndex
                || _matrixGestureCurrent.TargetIndex != cell.TargetIndex))
        {
            _matrixGestureCurrent = cell;
            UpdateMatrixGestureHighlight();
        }

        e.Handled = true;
    }

    private void MatrixGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_matrixGestureActive || _matrixGestureStart is null)
        {
            return;
        }

        e.Handled = true;
        PatchMatrixCell start = _matrixGestureStart;
        PatchMatrixCell end = TryGetMatrixCell(e.GetPosition(MatrixGrid), out PatchMatrixCell releasedCell)
            ? releasedCell
            : _matrixGestureCurrent ?? start;

        _matrixGestureActive = false;
        _matrixGestureStart = null;
        _matrixGestureCurrent = null;
        ClearMatrixGestureHighlight();
        MatrixGrid.ReleaseMouseCapture();
        ExecuteMatrixGesture(start, end);
    }

    private void MatrixGrid_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_matrixGestureActive)
        {
            CancelMatrixGesture();
        }
    }

    private void ExecuteMatrixGesture(PatchMatrixCell start, PatchMatrixCell end)
    {
        if (start.SourceIndex == end.SourceIndex
            && start.TargetIndex == end.TargetIndex
            && start.IsAssigned)
        {
            _session.Remove(start.Target);
            RefreshTargetRows();
            SetInfo(L("Déconnexion ajoutée au lot.", "Disconnection added to the batch."));
            return;
        }

        try
        {
            PatchAssignmentPlan plan = PatchAssignmentPlanner.PlanMatrixGesture(
                _visibleSources,
                _visibleTargets,
                start.SourceIndex,
                start.TargetIndex,
                end.SourceIndex,
                end.TargetIndex);
            StagePlanAsPreview(plan);
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private void UpdateMatrixGestureHighlight()
    {
        ClearMatrixGestureHighlight();
        if (_matrixGestureStart is null || _matrixGestureCurrent is null)
        {
            return;
        }

        try
        {
            PatchAssignmentPlan plan = PatchAssignmentPlanner.PlanMatrixGesture(
                _visibleSources,
                _visibleTargets,
                _matrixGestureStart.SourceIndex,
                _matrixGestureStart.TargetIndex,
                _matrixGestureCurrent.SourceIndex,
                _matrixGestureCurrent.TargetIndex);
            HashSet<(int SourceId, int TargetId)> plannedCells = plan.Assignments
                .Select(item => (item.Source.DanteId, item.Target.DanteId))
                .ToHashSet();
            foreach (ToggleButton button in FindVisualChildren<ToggleButton>(MatrixGrid)
                         .Where(button => button.Tag is PatchMatrixCell cell
                             && plannedCells.Contains((cell.Source.DanteId, cell.Target.DanteId))))
            {
                HighlightMatrixButton(button, invalid: false);
            }
        }
        catch (InvalidOperationException)
        {
            HighlightMatrixCell(_matrixGestureStart, invalid: true);
            HighlightMatrixCell(_matrixGestureCurrent, invalid: true);
        }
    }

    private void HighlightMatrixCell(PatchMatrixCell cell, bool invalid)
    {
        ToggleButton? button = FindVisualChildren<ToggleButton>(MatrixGrid)
            .FirstOrDefault(candidate => candidate.Tag is PatchMatrixCell visible
                && visible.SourceIndex == cell.SourceIndex
                && visible.TargetIndex == cell.TargetIndex);
        if (button is not null)
        {
            HighlightMatrixButton(button, invalid);
        }
    }

    private void HighlightMatrixButton(ToggleButton button, bool invalid)
    {
        Color background = invalid ? Color.FromRgb(127, 29, 29) : Color.FromRgb(180, 83, 9);
        button.Background = new SolidColorBrush(background);
        button.Foreground = Brushes.White;
        button.BorderBrush = (Brush)FindResource("WarningBrush");
        button.BorderThickness = new Thickness(2);
        _matrixGestureHighlights.Add(button);
    }

    private void ClearMatrixGestureHighlight()
    {
        foreach (ToggleButton button in _matrixGestureHighlights)
        {
            button.ClearValue(Control.BackgroundProperty);
            button.ClearValue(Control.ForegroundProperty);
            button.ClearValue(Control.BorderBrushProperty);
            button.ClearValue(Control.BorderThicknessProperty);
        }

        _matrixGestureHighlights.Clear();
    }

    private void CancelMatrixGesture()
    {
        _matrixGestureActive = false;
        _matrixGestureStart = null;
        _matrixGestureCurrent = null;
        ClearMatrixGestureHighlight();
        if (MatrixGrid.IsMouseCaptured)
        {
            MatrixGrid.ReleaseMouseCapture();
        }
    }

    private bool TryGetMatrixCell(Point point, out PatchMatrixCell cell)
    {
        DependencyObject? hit = MatrixGrid.InputHitTest(point) as DependencyObject;
        ToggleButton? button = FindVisualParent<ToggleButton>(hit);
        if (button?.Tag is PatchMatrixCell match)
        {
            cell = match;
            return true;
        }

        cell = null!;
        return false;
    }

    private static T? FindVisualParent<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (T descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void SelectAllTxButton_Click(object sender, RoutedEventArgs e)
    {
        TxChannelListBox.SelectAll();
    }

    private void SelectAllRxButton_Click(object sender, RoutedEventArgs e)
    {
        RxChannelListBox.SelectAll();
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        TxChannelListBox.UnselectAll();
        RxChannelListBox.UnselectAll();
        UpdateCommandState();
    }

    private void RangeControl_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initializing)
        {
            UpdateCommandState();
        }
    }

    private PatchSourceDescriptor[] SelectedSources()
    {
        return TxChannelListBox.SelectedItems
            .OfType<PatchSourceDescriptor>()
            .OrderBy(source => source.PositionIndex)
            .ToArray();
    }

    private PatchTargetDescriptor[] SelectedTargets()
    {
        return RxChannelListBox.SelectedItems
            .OfType<PatchRxListItem>()
            .Select(row => row.Target)
            .OrderBy(target => target.PositionIndex)
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
            if (_returnEditsOnly)
            {
                // Dans le détail machine, une liste vide est un résultat utile :
                // elle permet d'effacer des patchs précédemment préparés.
                ApplyRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            SetInfo(L("Aucun changement à appliquer.", "No changes to apply."), warning: true);
            return;
        }

        ApplyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateCommandState()
    {
        bool hasRxSelection = RxChannelListBox.SelectedItems.Count > 0;
        bool hasTxSelection = TxChannelListBox.SelectedItems.Count > 0;
        bool hasValidRangeCount = int.TryParse(RangeCountTextBox.Text, out int rangeCount) && rangeCount > 0;
        PreviewSelectionButton.IsEnabled = hasRxSelection && hasTxSelection;
        ApplySelectionDirectButton.IsEnabled = hasRxSelection && hasTxSelection;
        RemoveSelectedRxButton.IsEnabled = hasRxSelection;
        ClearSelectionButton.IsEnabled = hasRxSelection || hasTxSelection;
        SelectAllTxButton.IsEnabled = _visibleSources.Count > 0;
        SelectAllRxButton.IsEnabled = _visibleTargets.Count > 0;
        PreviewRangeButton.IsEnabled = RangeStartTxComboBox.SelectedItem is PatchSourceDescriptor
            && RangeStartRxComboBox.SelectedItem is PatchTargetDescriptor
            && hasValidRangeCount;
        ApplyRangeDirectButton.IsEnabled = PreviewRangeButton.IsEnabled;
        ResetPendingButton.IsEnabled = _session.HasChanges;
        ApplyButton.IsEnabled = _session.HasChanges || _returnEditsOnly;

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
        TitleTextBlock.Text = "Easy patch";
        IntroTextBlock.Text = L(
            "Chaque prévisualisation s'ajoute au lot. Appliquez tout en une seule fois quand il est prêt.",
            "Each preview is added to the batch. Apply everything once when it is ready.");
        TxDeviceLabel.Content = L("Machine émettrice TX", "Tx transmitting device");
        RxDeviceLabel.Content = L("Machine réceptrice RX", "Rx receiving device");
        PreviousRxDeviceButton.ToolTip = L("Machine RX précédente", "Previous Rx device");
        NextRxDeviceButton.ToolTip = L("Machine RX suivante", "Next Rx device");
        PreviousTxDeviceButton.ToolTip = L("Machine TX précédente", "Previous Tx device");
        NextTxDeviceButton.ToolTip = L("Machine TX suivante", "Next Tx device");
        AssignmentTab.Header = L("Sélection et plage", "Selection and range");
        MatrixTab.Header = L("Grille de patch", "Patch matrix");
        TxListHeadingTextBlock.Text = L("Canaux TX disponibles", "Available Tx channels");
        RxListHeadingTextBlock.Text = L("Canaux RX et source actuelle", "Rx channels and current source");
        SelectAllTxButton.Content = L("Tout sélectionner", "Select all");
        SelectAllRxButton.Content = L("Tout sélectionner", "Select all");
        SelectionHintTextBlock.Text = L(
            "Sélection",
            "Selection");
        PreviewSelectionButton.Content = L("Prévisualiser", "Preview");
        ApplySelectionDirectButton.Content = L("Appliquer", "Apply now");
        ApplySelectionDirectButton.ToolTip = L(
            "Applique immédiatement la sélection et les éventuels changements déjà ajoutés au lot.",
            "Immediately applies the selection and any changes already added to the batch.");
        RemoveSelectedRxButton.Content = L("Déconnecter", "Disconnect");
        ClearSelectionButton.Content = L("Effacer", "Clear");
        RangeHeadingTextBlock.Text = L("Patch par plage", "Range patch");
        RangeStartTxLabel.Content = L("Premier TX", "First Tx");
        RangeStartRxLabel.Content = L("Premier RX", "First Rx");
        RangeCountLabel.Content = L("Nombre", "Count");
        PreviewRangeButton.Content = L("Prévisualiser", "Preview");
        ApplyRangeDirectButton.Content = L("Appliquer", "Apply now");
        ApplyRangeDirectButton.ToolTip = L(
            "Applique immédiatement la plage et les éventuels changements déjà ajoutés au lot.",
            "Immediately applies the range and any changes already added to the batch.");
        PreviewGroupBox.Header = L("Lot prévisualisé", "Previewed batch");
        PreviewTargetColumn.Header = L("RX cible", "Target Rx");
        PreviewCurrentColumn.Header = L("Source actuelle", "Current source");
        PreviewProposedColumn.Header = L("Nouvelle source", "New source");
        PreviewActionColumn.Header = L("Action", "Action");
        PreviewSummaryTextBlock.Text = L("Aucun changement dans le lot.", "No changes in the batch.");
        MatrixHintTextBlock.Text = L(
            "RX en lignes, TX en colonnes. Cliquez pour un patch, ou maintenez et glissez : horizontal = série TX/RX, vertical = un TX vers plusieurs RX, diagonale = série un-à-un.",
            "Rx channels are rows and Tx channels are columns. Click for one patch, or hold and drag: horizontal = Tx/Rx range, vertical = one Tx to several Rx channels, diagonal = one-to-one range.");
        ResetPendingButton.Content = L("Annuler les changements visuels", "Discard visual changes");
        CancelButton.Content = L("Fermer sans appliquer", "Close without applying");
        CancelButton.Visibility = _embedded ? Visibility.Collapsed : Visibility.Visible;
        ApplyButton.Content = _embedded
            ? L("Appliquer tout le lot", "Apply entire batch")
            : _returnEditsOnly
            ? L("Valider dans le détail machine", "Return to device details")
            : L("Appliquer tout le lot", "Apply entire batch");

        AutomationProperties.SetName(TxDeviceComboBox, TxDeviceLabel.Content.ToString()!);
        AutomationProperties.SetName(RxDeviceComboBox, RxDeviceLabel.Content.ToString()!);
        AutomationProperties.SetName(PreviousRxDeviceButton, PreviousRxDeviceButton.ToolTip.ToString()!);
        AutomationProperties.SetName(NextRxDeviceButton, NextRxDeviceButton.ToolTip.ToString()!);
        AutomationProperties.SetName(PreviousTxDeviceButton, PreviousTxDeviceButton.ToolTip.ToString()!);
        AutomationProperties.SetName(NextTxDeviceButton, NextTxDeviceButton.ToolTip.ToString()!);
        AutomationProperties.SetName(TxChannelListBox, TxListHeadingTextBlock.Text);
        AutomationProperties.SetName(RxChannelListBox, RxListHeadingTextBlock.Text);
        AutomationProperties.SetName(ApplySelectionDirectButton, ApplySelectionDirectButton.Content.ToString()!);
        AutomationProperties.SetName(ApplyRangeDirectButton, ApplyRangeDirectButton.Content.ToString()!);
        AutomationProperties.SetName(MatrixGrid, MatrixTab.Header.ToString()!);
        AutomationProperties.SetName(PreviewGrid, PreviewGroupBox.Header.ToString()!);
    }

    private MessageBoxResult ShowConfirmation(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage image)
    {
        Window? owner = Window.GetWindow(this);
        return owner is null
            ? MessageBox.Show(message, title, buttons, image)
            : MessageBox.Show(owner, message, title, buttons, image);
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
            ? L("Cliquer pour déconnecter, ou glisser pour préparer une série", "Click to disconnect, or drag to prepare a range")
            : L("Cliquer pour affecter, ou glisser pour préparer une série", "Click to assign, or drag to prepare a range");
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

    private sealed record PatchPreviewRow(
        string Target,
        string Current,
        string Proposed,
        string Action,
        string ActionKey);

    private sealed record PatchMatrixRow(
        string TargetDisplay,
        bool IsPending,
        IReadOnlyList<PatchMatrixCell> Cells);

    private sealed record PatchMatrixCell(
        PatchSourceDescriptor Source,
        PatchTargetDescriptor Target,
        int SourceIndex,
        int TargetIndex,
        bool IsAssigned,
        bool IsPending,
        string Marker,
        string ToolTip,
        string AutomationName);
}
