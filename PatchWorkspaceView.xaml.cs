using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
    private IReadOnlyList<PatchSourceDescriptor> _visibleSources = [];
    private IReadOnlyList<PatchTargetDescriptor> _visibleTargets = [];
    private PatchBatchPreview? _currentPreview;
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
        ClearPreview();
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

        ClearPreview();
        RefreshSourceChannelsAndMatrixColumns();
        RefreshTargetRows();
    }

    private void RxDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing)
        {
            ClearPreview();
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

    private void PreviewSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ShowPreview(BuildSelectionPlan());
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
            ShowPreview(BuildRangePlan());
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

    private void ShowPreview(PatchAssignmentPlan plan)
    {
        if (!SourcesAreUnambiguous(plan.Assignments.Select(assignment => assignment.Source)))
        {
            return;
        }

        _currentPreview = _session.BuildPreview(plan.Assignments);
        PreviewGrid.ItemsSource = _currentPreview.Items.Select(BuildPreviewRow).ToArray();
        PreviewGroupBox.Visibility = Visibility.Visible;
        PreviewSummaryTextBlock.Text = L(
            $"{_currentPreview.Items.Count} ligne(s) : {_currentPreview.CreateCount} création(s), {_currentPreview.ReplaceCount} remplacement(s), {_currentPreview.UnchangedCount} inchangée(s).",
            $"{_currentPreview.Items.Count} row(s): {_currentPreview.CreateCount} new, {_currentPreview.ReplaceCount} replacement(s), {_currentPreview.UnchangedCount} unchanged.");

        SelectConflictResolution(_currentPreview.HasConflicts
            ? PatchConflictResolution.Cancel
            : PatchConflictResolution.Replace);
        SetInfo(_currentPreview.HasConflicts
            ? L("Des RX sont déjà patchés. Choisissez explicitement comment traiter les conflits.", "Some Rx channels are already patched. Explicitly choose how to handle conflicts.")
            : L("Prévisualisation prête. Aucun conflit de remplacement.", "Preview ready. No replacement conflict."),
            warning: _currentPreview.HasConflicts);
        UpdateCommandState();
    }

    private PatchPreviewRow BuildPreviewRow(PatchPreviewItem item)
    {
        string current = item.Current.IsActive
            ? $"{item.Current.TxDeviceName} / {item.Current.TxChannelName}"
            : L("Libre", "Free");
        string action = item.Action switch
        {
            PatchPreviewAction.Create => L("Créer", "Create"),
            PatchPreviewAction.Replace => L("Remplacer", "Replace"),
            _ => L("Inchangé", "Unchanged")
        };

        return new PatchPreviewRow(
            item.Assignment.Target.FullDisplay,
            current,
            item.Assignment.Source.FullDisplay,
            action,
            item.Action.ToString());
    }

    private void AddPreviewToBatchButton_Click(object sender, RoutedEventArgs e)
    {
        StageCurrentPreview(applyImmediately: false);
    }

    private void ApplyPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        StageCurrentPreview(applyImmediately: true);
    }

    private void StageCurrentPreview(bool applyImmediately)
    {
        if (_currentPreview is null)
        {
            SetInfo(L("Créez d'abord une prévisualisation.", "Create a preview first."), warning: true);
            return;
        }

        try
        {
            PatchConflictResolution resolution = SelectedConflictResolution();
            PatchStageResult result = _session.StagePreview(_currentPreview, resolution);
            if (result.IsCancelled)
            {
                SetInfo(L("Aucun changement appliqué : résolution annulée.", "No change applied: conflict resolution cancelled."), warning: true);
                return;
            }

            string message = L(
                $"{result.StagedCount} changement(s) ajouté(s) au lot, {result.SkippedConflictCount} conflit(s) ignoré(s), {result.UnchangedCount} ligne(s) inchangée(s).",
                $"{result.StagedCount} change(s) added to the batch, {result.SkippedConflictCount} conflict(s) skipped, {result.UnchangedCount} row(s) unchanged.");
            ClearPreview();
            RefreshTargetRows();

            if (result.StagedCount == 0)
            {
                SetInfo(message, warning: result.SkippedConflictCount > 0);
                return;
            }

            if (applyImmediately)
            {
                SetInfo(L("Application des changements prévisualisés.", "Applying previewed changes."));
                ApplyRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            SetInfo(message, warning: result.SkippedConflictCount > 0);
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
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
        PatchConflictResolution resolution = ChooseDirectConflictResolution(preview);
        PatchStageResult result = _session.StagePreview(preview, resolution);
        if (result.IsCancelled)
        {
            SetInfo(L("Application directe annulée.", "Direct apply cancelled."), warning: true);
            return;
        }

        ClearPreview();
        RefreshTargetRows();
        if (result.StagedCount == 0)
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

    private PatchConflictResolution ChooseDirectConflictResolution(PatchBatchPreview preview)
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

    private void CancelPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPreview();
        SetInfo(L("Prévisualisation annulée.", "Preview cancelled."));
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
            ClearPreview();
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

        if (cell.IsAssigned)
        {
            _session.Remove(cell.Target);
            SetInfo(L("Déconnexion ajoutée au lot.", "Disconnection added to the batch."));
        }
        else
        {
            if (!SourcesAreUnambiguous([cell.Source]))
            {
                RefreshTargetRows();
                return;
            }

            PatchBatchPreview preview = _session.BuildPreview(
                [new PlannedPatchAssignment(cell.Source, cell.Target)]);
            PatchConflictResolution resolution = PatchConflictResolution.Replace;
            if (preview.HasConflicts)
            {
                MessageBoxResult choice = ShowConfirmation(
                    L(
                        "Ce RX possède déjà une source. Oui = remplacer, Non = ignorer, Annuler = ne rien changer.",
                        "This Rx already has a source. Yes = replace, No = skip, Cancel = make no change."),
                    L("Conflit de patch", "Patch conflict"),
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                resolution = choice switch
                {
                    MessageBoxResult.Yes => PatchConflictResolution.Replace,
                    MessageBoxResult.No => PatchConflictResolution.Skip,
                    _ => PatchConflictResolution.Cancel
                };
            }

            PatchStageResult result = _session.StagePreview(preview, resolution);
            SetInfo(result.IsCancelled || result.SkippedConflictCount > 0
                ? L("Aucune affectation ajoutée au lot.", "No assignment added to the batch.")
                : L("Affectation ajoutée au lot.", "Assignment added to the batch."),
                warning: result.IsCancelled || result.SkippedConflictCount > 0);
        }

        ClearPreview();
        RefreshTargetRows();
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
        ClearPreview();
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
        AddPreviewToBatchButton.IsEnabled = _currentPreview is not null;
        ApplyPreviewButton.IsEnabled = _currentPreview is not null;
        CancelPreviewButton.IsEnabled = _currentPreview is not null;
        ConflictResolutionComboBox.IsEnabled = _currentPreview?.HasConflicts == true;
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
            "Prévisualisez une opération, ajoutez-la au lot ou appliquez-la directement.",
            "Preview an operation, add it to the batch, or apply it directly.");
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
        PreviewGroupBox.Header = L("Prévisualisation", "Preview");
        PreviewTargetColumn.Header = L("RX cible", "Target Rx");
        PreviewCurrentColumn.Header = L("Source actuelle", "Current source");
        PreviewProposedColumn.Header = L("Nouvelle source", "New source");
        PreviewActionColumn.Header = L("Action", "Action");
        ConflictResolutionLabel.Content = L("Conflits", "Conflicts");
        AddPreviewToBatchButton.Content = L("Ajouter au lot", "Add to batch");
        ApplyPreviewButton.Content = L("Appliquer ces changements", "Apply these changes");
        CancelPreviewButton.Content = L("Annuler l'aperçu", "Cancel preview");
        PreviewSafetyTextBlock.Text = L(
            "La prévisualisation ne modifie rien. Ajoutez au lot ou appliquez maintenant.",
            "Previewing changes nothing. Add to the batch or apply now.");
        ConflictResolutionComboBox.ItemsSource = new[]
        {
            new ConflictChoice(PatchConflictResolution.Cancel, L("Annuler le lot", "Cancel batch")),
            new ConflictChoice(PatchConflictResolution.Skip, L("Ignorer les conflits", "Skip conflicts")),
            new ConflictChoice(PatchConflictResolution.Replace, L("Remplacer les patchs", "Replace subscriptions"))
        };
        SelectConflictResolution(PatchConflictResolution.Cancel);
        PreviewSummaryTextBlock.Text = L("Aucune prévisualisation.", "No preview.");
        MatrixHintTextBlock.Text = L(
            "RX en lignes, TX en colonnes. Cliquez dans une case pour affecter ou retirer un patch.",
            "Rx channels are rows and Tx channels are columns. Click a cell to assign or remove a subscription.");
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
        AutomationProperties.SetName(AddPreviewToBatchButton, AddPreviewToBatchButton.Content.ToString()!);
        AutomationProperties.SetName(ApplyPreviewButton, ApplyPreviewButton.Content.ToString()!);
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

    private void ClearPreview()
    {
        _currentPreview = null;
        PreviewGrid.ItemsSource = Array.Empty<PatchPreviewRow>();
        PreviewGroupBox.Visibility = Visibility.Collapsed;
        PreviewSummaryTextBlock.Text = L("Aucune prévisualisation.", "No preview.");
        SelectConflictResolution(PatchConflictResolution.Cancel);
        UpdateCommandState();
    }

    private PatchConflictResolution SelectedConflictResolution()
    {
        return ConflictResolutionComboBox.SelectedItem is ConflictChoice choice
            ? choice.Resolution
            : PatchConflictResolution.Cancel;
    }

    private void SelectConflictResolution(PatchConflictResolution resolution)
    {
        if (ConflictResolutionComboBox.ItemsSource is not IEnumerable<ConflictChoice> choices)
        {
            return;
        }

        ConflictResolutionComboBox.SelectedItem = choices.FirstOrDefault(choice => choice.Resolution == resolution);
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

    private sealed record PatchPreviewRow(
        string Target,
        string Current,
        string Proposed,
        string Action,
        string ActionKey);

    private sealed record ConflictChoice(PatchConflictResolution Resolution, string Display)
    {
        public override string ToString()
        {
            return Display;
        }
    }

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
