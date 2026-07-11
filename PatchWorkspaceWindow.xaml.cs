using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class PatchWorkspaceWindow : Window
{
    private readonly UiLanguage _language;
    private readonly DanteProject _project;
    private readonly PatchWorkspaceSession _session;
    private readonly HashSet<string> _ambiguousSourceNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<PatchSourceDescriptor> _visibleSources = [];
    private IReadOnlyList<PatchTargetDescriptor> _visibleTargets = [];
    private PatchBatchPreview? _currentPreview;
    private bool _initializing = true;

    public PatchWorkspaceWindow(
        UiLanguage language,
        DanteProject project,
        bool useLightTheme,
        string? initialTxDeviceName = null,
        string? initialRxDeviceName = null,
        IEnumerable<PatchEditRequest>? initialEdits = null)
    {
        InitializeComponent();
        _language = language;
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _session = new PatchWorkspaceSession(project.PatchMatrix.Subscriptions, initialEdits);

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
                "Sélectionnez plusieurs TX avec Ctrl ou Maj, puis choisissez le premier RX.",
                "Select multiple Tx channels with Ctrl or Shift, then choose the first Rx."));
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
            PatchAssignmentPlan plan = PatchAssignmentPlanner.PlanSelection(SelectedSources(), SelectedTargets());
            ShowPreview(plan);
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private void PreviewRangeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (RangeStartTxComboBox.SelectedItem is not PatchSourceDescriptor firstSource
                || RangeStartRxComboBox.SelectedItem is not PatchTargetDescriptor firstTarget
                || !int.TryParse(RangeCountTextBox.Text, out int count))
            {
                throw new InvalidOperationException(L(
                    "Renseignez le premier TX, le premier RX et un nombre de canaux valide.",
                    "Choose the first Tx, first Rx and a valid channel count."));
            }

            PatchAssignmentPlan plan = PatchAssignmentPlanner.PlanRange(
                _visibleSources,
                firstSource,
                _visibleTargets,
                firstTarget,
                count);
            ShowPreview(plan);
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
    }

    private void ShowPreview(PatchAssignmentPlan plan)
    {
        if (!SourcesAreUnambiguous(plan.Assignments.Select(assignment => assignment.Source)))
        {
            return;
        }

        _currentPreview = _session.BuildPreview(plan.Assignments);
        PreviewGrid.ItemsSource = _currentPreview.Items.Select(BuildPreviewRow).ToArray();
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

    private void StagePreviewButton_Click(object sender, RoutedEventArgs e)
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
                SetInfo(L("Aucun changement préparé : résolution annulée.", "No change staged: conflict resolution cancelled."), warning: true);
                return;
            }

            string message = L(
                $"{result.StagedCount} changement(s) préparé(s), {result.SkippedConflictCount} conflit(s) ignoré(s), {result.UnchangedCount} ligne(s) inchangée(s).",
                $"{result.StagedCount} change(s) staged, {result.SkippedConflictCount} conflict(s) skipped, {result.UnchangedCount} row(s) unchanged.");
            ClearPreview();
            RefreshTargetRows();
            SetInfo(message, warning: result.SkippedConflictCount > 0);
        }
        catch (Exception exception)
        {
            SetInfo(exception.Message, warning: true);
        }
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

        MessageBoxResult confirmation = MessageBox.Show(
            this,
            L(
                $"Préparer la déconnexion de {targets.Length} canal(aux) RX sélectionné(s) ?",
                $"Stage disconnection of {targets.Length} selected Rx channel(s)?"),
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
            SetInfo(L($"{removed} déconnexion(s) préparée(s).", $"{removed} disconnection(s) staged."));
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

            PatchBatchPreview preview = _session.BuildPreview(
                [new PlannedPatchAssignment(cell.Source, cell.Target)]);
            PatchConflictResolution resolution = PatchConflictResolution.Replace;
            if (preview.HasConflicts)
            {
                MessageBoxResult choice = MessageBox.Show(
                    this,
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
                ? L("Aucune affectation préparée.", "No assignment staged.")
                : L("Affectation préparée.", "Assignment staged."),
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
        bool hasRxSelection = RxChannelListBox.SelectedItems.Count > 0;
        bool hasTxSelection = TxChannelListBox.SelectedItems.Count > 0;
        bool hasValidRangeCount = int.TryParse(RangeCountTextBox.Text, out int rangeCount) && rangeCount > 0;
        PreviewSelectionButton.IsEnabled = hasRxSelection && hasTxSelection;
        RemoveSelectedRxButton.IsEnabled = hasRxSelection;
        ClearSelectionButton.IsEnabled = hasRxSelection || hasTxSelection;
        SelectAllTxButton.IsEnabled = _visibleSources.Count > 0;
        SelectAllRxButton.IsEnabled = _visibleTargets.Count > 0;
        PreviewRangeButton.IsEnabled = RangeStartTxComboBox.SelectedItem is PatchSourceDescriptor
            && RangeStartRxComboBox.SelectedItem is PatchTargetDescriptor
            && hasValidRangeCount;
        StagePreviewButton.IsEnabled = _currentPreview is not null;
        CancelPreviewButton.IsEnabled = _currentPreview is not null;
        ConflictResolutionComboBox.IsEnabled = _currentPreview?.HasConflicts == true;
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
        AssignmentTab.Header = L("Sélection et plage", "Selection and range");
        MatrixTab.Header = L("Grille de patch", "Patch matrix");
        TxListHeadingTextBlock.Text = L("Canaux TX disponibles", "Available Tx channels");
        RxListHeadingTextBlock.Text = L("Canaux RX et source actuelle", "Rx channels and current source");
        SelectAllTxButton.Content = L("Tout sélectionner", "Select all");
        SelectAllRxButton.Content = L("Tout sélectionner", "Select all");
        SelectionHintTextBlock.Text = L(
            "Sélectionnez les TX et les RX à relier. Un TX peut alimenter plusieurs RX.",
            "Select the Tx and Rx channels to connect. One Tx may feed several Rx channels.");
        PreviewSelectionButton.Content = L("Prévisualiser la sélection", "Preview selection");
        RemoveSelectedRxButton.Content = L("Déconnecter les RX sélectionnés", "Disconnect selected Rx channels");
        ClearSelectionButton.Content = L("Effacer la sélection", "Clear selection");
        RangeHeadingTextBlock.Text = L("Patch par plage", "Range patch");
        RangeStartTxLabel.Content = L("Premier TX", "First Tx");
        RangeStartRxLabel.Content = L("Premier RX", "First Rx");
        RangeCountLabel.Content = L("Nombre de canaux", "Channel count");
        PreviewRangeButton.Content = L("Prévisualiser la plage", "Preview range");
        PreviewGroupBox.Header = L("Prévisualisation", "Preview");
        PreviewTargetColumn.Header = L("RX cible", "Target Rx");
        PreviewCurrentColumn.Header = L("Source actuelle", "Current source");
        PreviewProposedColumn.Header = L("Nouvelle source", "New source");
        PreviewActionColumn.Header = L("Action", "Action");
        ConflictResolutionLabel.Content = L("Conflits", "Conflicts");
        StagePreviewButton.Content = L("Préparer ces changements", "Stage these changes");
        CancelPreviewButton.Content = L("Annuler l'aperçu", "Cancel preview");
        PreviewSafetyTextBlock.Text = L(
            "Aucun changement XML avant Appliquer au projet.",
            "No XML change until Apply to project.");
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
        ApplyButton.Content = L("Appliquer au projet", "Apply to project");

        AutomationProperties.SetName(TxDeviceComboBox, TxDeviceLabel.Content.ToString()!);
        AutomationProperties.SetName(RxDeviceComboBox, RxDeviceLabel.Content.ToString()!);
        AutomationProperties.SetName(TxChannelListBox, TxListHeadingTextBlock.Text);
        AutomationProperties.SetName(RxChannelListBox, RxListHeadingTextBlock.Text);
        AutomationProperties.SetName(MatrixGrid, MatrixTab.Header.ToString()!);
        AutomationProperties.SetName(PreviewGrid, PreviewGroupBox.Header.ToString()!);
    }

    private void ClearPreview()
    {
        _currentPreview = null;
        PreviewGrid.ItemsSource = Array.Empty<PatchPreviewRow>();
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
