using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor.Mac;

public sealed partial class PatchWorkspaceDialog : Window
{
    private const int MaxMatrixChannels = 128;
    private static readonly DataFormat<string> DragDataFormat =
        DataFormat.CreateStringApplicationFormat("DanteConfigEditor.PatchSources");

    private UiLanguage _language = UiLanguage.French;
    private DanteProject _project = null!;
    private PatchWorkspaceSession _session = null!;
    private readonly HashSet<string> _ambiguousSourceNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<MatrixCellKey, PatchMatrixCell> _matrixCells = [];
    private readonly Dictionary<int, TextBlock> _matrixRxHeaders = [];
    private IReadOnlyList<PatchSourceDescriptor> _visibleSources = [];
    private IReadOnlyList<PatchTargetDescriptor> _visibleTargets = [];
    private IReadOnlyList<PatchRxListItem> _rxRows = [];
    private Point _dragStart;
    private bool _dragReady;
    private bool _dragInProgress;
    private bool _initializing = true;

    internal int MatrixBuildCount { get; private set; }

    public PatchWorkspaceDialog()
    {
        InitializeComponent();
        TabControl tabs = FindControl<TabControl>("PatchModeTabs")!;
        TabItem matrix = FindControl<TabItem>("MatrixTab")!;
        tabs.Items.Remove(matrix);
        tabs.Items.Insert(0, matrix);
        tabs.SelectedItem = matrix;
    }

    public PatchWorkspaceDialog(
        UiLanguage language,
        DanteProject project,
        string? initialTxDeviceName = null,
        string? initialRxDeviceName = null)
        : this()
    {
        _language = language;
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _session = new PatchWorkspaceSession(project.PatchMatrix.Subscriptions);

        FindControl<ListBox>("TxChannelList")!.SelectionChanged += TxChannelList_SelectionChanged;
        ApplyLanguage();
        PopulateDeviceSelectors(initialTxDeviceName, initialRxDeviceName);
        _initializing = false;
        RefreshSourceChannels();
        RefreshTargetRows();
    }

    public IReadOnlyList<PatchEditRequest> Edits => _session.Edits;

    private T? FindControl<T>(string name) where T : Control => ControlExtensions.FindControl<T>(this, name);

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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

        ComboBox txCombo = FindControl<ComboBox>("TxDeviceCombo")!;
        ComboBox rxCombo = FindControl<ComboBox>("RxDeviceCombo")!;
        txCombo.ItemsSource = txDevices;
        rxCombo.ItemsSource = rxDevices;
        txCombo.SelectedItem = FindDeviceName(txDevices, initialTxDeviceName) ?? txDevices.FirstOrDefault();
        rxCombo.SelectedItem = FindDeviceName(rxDevices, initialRxDeviceName) ?? rxDevices.FirstOrDefault();
    }

    private void TxDeviceCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        RefreshSourceChannels();
        RefreshTargetRows();
    }

    private void RxDeviceCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_initializing)
        {
            RefreshTargetRows();
        }
    }

    private void TxChannelList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateCommandState();
    }

    private void RxChannelList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateCommandState();
    }

    private void RefreshSourceChannels()
    {
        string? deviceName = FindControl<ComboBox>("TxDeviceCombo")!.SelectedItem as string;
        DanteDevice? device = _project.FindDevice(deviceName);
        _visibleSources = device?.TxChannels
            .Select(channel => new PatchSourceDescriptor(
                device.Name,
                channel.DanteId,
                channel.PositionIndex,
                channel.DisplayName))
            .OrderBy(channel => channel.PositionIndex)
            .ToArray() ?? [];
        FindControl<ListBox>("TxChannelList")!.ItemsSource = _visibleSources;

        _ambiguousSourceNames.Clear();
        foreach (IGrouping<string, PatchSourceDescriptor> duplicate in _visibleSources
                     .GroupBy(source => source.ChannelName, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            _ambiguousSourceNames.Add(duplicate.Key);
        }

        if (_ambiguousSourceNames.Count > 0)
        {
            SetInfo(
                L(
                    "Cette machine contient des noms TX en double. Renommez-les avant le patch.",
                    "This device contains duplicate Tx names. Rename them before patching."),
                warning: true);
        }
        else
        {
            SetInfo(L(
                "Sélectionnez plusieurs TX avec Cmd, Ctrl ou Maj, puis choisissez le premier RX.",
                "Select multiple Tx channels with Cmd, Ctrl, or Shift, then choose the first Rx."));
        }
    }

    private void RefreshTargetRows()
    {
        ListBox rxList = FindControl<ListBox>("RxChannelList")!;
        int? selectedDanteId = (rxList.SelectedItem as PatchRxListItem)?.Target.DanteId;
        string? deviceName = FindControl<ComboBox>("RxDeviceCombo")!.SelectedItem as string;
        DanteDevice? device = _project.FindDevice(deviceName);
        _visibleTargets = device?.RxChannels
            .Select(channel => new PatchTargetDescriptor(
                device.Name,
                channel.DanteId,
                channel.PositionIndex,
                channel.DisplayName))
            .OrderBy(channel => channel.PositionIndex)
            .ToArray() ?? [];

        _rxRows = _visibleTargets.Select(BuildRxListItem).ToArray();
        rxList.ItemsSource = _rxRows;
        rxList.SelectedItem = _rxRows.FirstOrDefault(row => row.Target.DanteId == selectedDanteId)
            ?? _rxRows.FirstOrDefault();

        BuildMatrix();
        UpdateCommandState();
    }

    private PatchRxListItem BuildRxListItem(PatchTargetDescriptor target)
    {
        EffectivePatchAssignment assignment = _session.GetEffectiveAssignment(target);
        return new PatchRxListItem(target, BuildRxDisplay(target, assignment));
    }

    private void BuildMatrix()
    {
        MatrixBuildCount++;
        Grid matrix = FindControl<Grid>("MatrixPanel")!;
        Grid txHeaders = FindControl<Grid>("MatrixTxHeaderPanel")!;
        Grid rxHeaders = FindControl<Grid>("MatrixRxHeaderPanel")!;
        matrix.Children.Clear();
        matrix.RowDefinitions.Clear();
        matrix.ColumnDefinitions.Clear();
        txHeaders.Children.Clear();
        txHeaders.RowDefinitions.Clear();
        txHeaders.ColumnDefinitions.Clear();
        rxHeaders.Children.Clear();
        rxHeaders.RowDefinitions.Clear();
        rxHeaders.ColumnDefinitions.Clear();
        _matrixCells.Clear();
        _matrixRxHeaders.Clear();

        PatchSourceDescriptor[] sources = _visibleSources.Take(MaxMatrixChannels).ToArray();
        PatchTargetDescriptor[] targets = _visibleTargets.Take(MaxMatrixChannels).ToArray();
        txHeaders.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) });
        foreach (PatchSourceDescriptor _ in sources)
        {
            matrix.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
            txHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        }

        rxHeaders.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
        {
            PatchSourceDescriptor source = sources[sourceIndex];
            TextBlock header = AddMatrixText(
                txHeaders,
                $"TX {source.DanteId}\n{source.ChannelName}",
                0,
                sourceIndex,
                FontWeight.SemiBold,
                pending: false);
            header.Width = 86;
            header.TextAlignment = TextAlignment.Center;
            header.TextTrimming = TextTrimming.CharacterEllipsis;
            ToolTip.SetTip(header, source.FullDisplay);
            AutomationProperties.SetName(header, source.FullDisplay);
        }

        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            PatchTargetDescriptor target = targets[targetIndex];
            EffectivePatchAssignment assignment = _session.GetEffectiveAssignment(target);
            int assignedSourceIndex = FindAssignedSourceIndex(assignment, sources);
            matrix.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            rxHeaders.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            TextBlock rxHeader = AddMatrixText(
                rxHeaders,
                target.Display,
                targetIndex,
                0,
                FontWeight.Normal,
                assignment.IsPending);
            ToolTip.SetTip(rxHeader, target.FullDisplay);
            AutomationProperties.SetName(rxHeader, target.FullDisplay);
            _matrixRxHeaders[targetIndex] = rxHeader;

            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                PatchSourceDescriptor source = sources[sourceIndex];
                bool isAssigned = sourceIndex == assignedSourceIndex;
                PatchMatrixCell cell = new(source, target, sourceIndex, targetIndex, isAssigned);
                Button button = new()
                {
                    Content = isAssigned ? "●" : string.Empty,
                    DataContext = cell,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                button.Classes.Add("matrixCell");
                if (isAssigned)
                {
                    button.Classes.Add("assigned");
                }

                button.Click += MatrixCellButton_Click;
                ToolTip.SetTip(button, BuildCellToolTip(source, target, isAssigned, assignment.IsPending));
                AutomationProperties.SetName(button, BuildCellAutomationName(source, target, isAssigned));
                Grid.SetRow(button, targetIndex);
                Grid.SetColumn(button, sourceIndex);
                matrix.Children.Add(button);
                cell.Button = button;
                _matrixCells[new MatrixCellKey(sourceIndex, targetIndex)] = cell;
            }
        }

        FindControl<TextBlock>("MatrixCornerHeader")!.Text = L("Canal RX", "Rx channel");
        UpdateMatrixHint(sources.Length, targets.Length);
    }

    private void RefreshTargetStates(IEnumerable<PatchTargetDescriptor> targets)
    {
        HashSet<int> danteIds = targets
            .Where(target => string.Equals(
                target.DeviceName,
                FindControl<ComboBox>("RxDeviceCombo")!.SelectedItem as string,
                StringComparison.OrdinalIgnoreCase))
            .Select(target => target.DanteId)
            .ToHashSet();

        for (int targetIndex = 0; targetIndex < _visibleTargets.Count; targetIndex++)
        {
            if (danteIds.Contains(_visibleTargets[targetIndex].DanteId))
            {
                RefreshTargetState(targetIndex);
            }
        }

        UpdateCommandState();
    }

    private void RefreshAllTargetStates()
    {
        for (int targetIndex = 0; targetIndex < _visibleTargets.Count; targetIndex++)
        {
            RefreshTargetState(targetIndex);
        }

        UpdateCommandState();
    }

    private void RefreshTargetState(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= _visibleTargets.Count)
        {
            return;
        }

        PatchTargetDescriptor target = _visibleTargets[targetIndex];
        EffectivePatchAssignment assignment = _session.GetEffectiveAssignment(target);
        if (targetIndex < _rxRows.Count)
        {
            _rxRows[targetIndex].Display = BuildRxDisplay(target, assignment);
        }

        if (targetIndex >= MaxMatrixChannels)
        {
            return;
        }

        if (_matrixRxHeaders.TryGetValue(targetIndex, out TextBlock? rowHeader))
        {
            SetPendingRowClass(rowHeader, assignment.IsPending);
        }

        int assignedSourceIndex = FindAssignedSourceIndex(
            assignment,
            _visibleSources.Take(MaxMatrixChannels).ToArray());
        for (int sourceIndex = 0;
             sourceIndex < Math.Min(_visibleSources.Count, MaxMatrixChannels);
             sourceIndex++)
        {
            if (!_matrixCells.TryGetValue(new MatrixCellKey(sourceIndex, targetIndex), out PatchMatrixCell? cell))
            {
                continue;
            }

            bool isAssigned = sourceIndex == assignedSourceIndex;
            cell.IsAssigned = isAssigned;
            cell.Button.Content = isAssigned ? "●" : string.Empty;
            SetAssignedCellClass(cell.Button, isAssigned);
            ToolTip.SetTip(
                cell.Button,
                BuildCellToolTip(cell.Source, cell.Target, isAssigned, assignment.IsPending));
            AutomationProperties.SetName(
                cell.Button,
                BuildCellAutomationName(cell.Source, cell.Target, isAssigned));
        }
    }

    private string BuildRxDisplay(PatchTargetDescriptor target, EffectivePatchAssignment assignment)
    {
        string source = assignment.IsActive
            ? $"{assignment.TxDeviceName} / {assignment.TxChannelName}"
            : L("Libre", "Free");
        string marker = assignment.IsPending ? L("  [modifié]", "  [changed]") : string.Empty;
        return $"{target.Display}   <-   {source}{marker}";
    }

    private static void SetAssignedCellClass(Button button, bool assigned)
    {
        if (assigned)
        {
            if (!button.Classes.Contains("assigned"))
            {
                button.Classes.Add("assigned");
            }
        }
        else
        {
            button.Classes.Remove("assigned");
        }
    }

    private static void SetPendingRowClass(TextBlock rowHeader, bool pending)
    {
        if (pending)
        {
            if (!rowHeader.Classes.Contains("pendingRow"))
            {
                rowHeader.Classes.Add("pendingRow");
            }
        }
        else
        {
            rowHeader.Classes.Remove("pendingRow");
        }
    }

    private TextBlock AddMatrixText(
        Grid matrix,
        string text,
        int row,
        int column,
        FontWeight weight,
        bool pending)
    {
        TextBlock block = new()
        {
            Text = text,
            FontWeight = weight,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(8, 4),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        if (pending)
        {
            block.Classes.Add("pendingRow");
        }

        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        matrix.Children.Add(block);
        return block;
    }

    private static int FindAssignedSourceIndex(
        EffectivePatchAssignment assignment,
        IReadOnlyList<PatchSourceDescriptor> sources)
    {
        if (!assignment.IsActive)
        {
            return -1;
        }

        for (int index = 0; index < sources.Count; index++)
        {
            PatchSourceDescriptor source = sources[index];
            if (string.Equals(source.DeviceName, assignment.TxDeviceName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(source.ChannelName, assignment.TxChannelName, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private void AssignSequentialButton_Click(object? sender, RoutedEventArgs e)
    {
        PatchSourceDescriptor[] sources = SelectedSources();
        if (sources.Length == 0)
        {
            SetInfo(L("Sélectionnez au moins un canal TX.", "Select at least one Tx channel."), warning: true);
            return;
        }

        if (FindControl<ListBox>("RxChannelList")!.SelectedItem is not PatchRxListItem targetRow)
        {
            SetInfo(L("Sélectionnez le premier canal RX.", "Select the first Rx channel."), warning: true);
            return;
        }

        AssignSources(sources, targetRow.Target);
    }

    private void RemoveSelectedRxButton_Click(object? sender, RoutedEventArgs e)
    {
        if (FindControl<ListBox>("RxChannelList")!.SelectedItem is not PatchRxListItem targetRow)
        {
            SetInfo(L("Sélectionnez un canal RX à déconnecter.", "Select an Rx channel to disconnect."), warning: true);
            return;
        }

        _session.Remove(targetRow.Target);
        RefreshTargetStates([targetRow.Target]);
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
            RefreshTargetStates(plan.Assignments.Select(assignment => assignment.Target));
            SetInfo(plan.UnassignedSources.Count == 0
                ? L(
                    $"{plan.Assignments.Count} affectation(s) préparée(s).",
                    $"{plan.Assignments.Count} assignment(s) staged.")
                : L(
                    $"{plan.Assignments.Count} affectation(s), {plan.UnassignedSources.Count} TX sans RX disponible.",
                    $"{plan.Assignments.Count} assignment(s), {plan.UnassignedSources.Count} Tx channel(s) without an available Rx."),
                warning: plan.UnassignedSources.Count > 0);
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

    private void MatrixCellButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: PatchMatrixCell cell })
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
                return;
            }

            _session.Assign(new PlannedPatchAssignment(cell.Source, cell.Target));
            SetInfo(L("Affectation préparée.", "Assignment staged."));
        }

        RefreshTargetStates([cell.Target]);
    }

    private void MatrixBodyScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        ScrollViewer body = FindControl<ScrollViewer>("MatrixBodyScrollViewer")!;
        FindControl<ScrollViewer>("MatrixTxHeaderScrollViewer")!.Offset =
            new Vector(body.Offset.X, 0);
        FindControl<ScrollViewer>("MatrixRxHeaderScrollViewer")!.Offset =
            new Vector(0, body.Offset.Y);
    }

    private void TxChannelList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ListBox listBox
            || !e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragStart = e.GetPosition(listBox);
        _dragReady = true;
    }

    private async void TxChannelList_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not ListBox listBox || !_dragReady || _dragInProgress)
        {
            return;
        }

        if (!e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
        {
            _dragReady = false;
            return;
        }

        Point current = e.GetPosition(listBox);
        if (Math.Abs(current.X - _dragStart.X) < 5 && Math.Abs(current.Y - _dragStart.Y) < 5)
        {
            return;
        }

        PatchSourceDescriptor[] sources = SelectedSources();
        if (sources.Length == 0 || !SourcesAreUnambiguous(sources))
        {
            _dragReady = false;
            return;
        }

        _dragReady = false;
        _dragInProgress = true;
        try
        {
            DataTransfer data = new();
            data.Add(DataTransferItem.Create(
                DragDataFormat,
                JsonSerializer.Serialize(sources)));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy);
        }
        finally
        {
            _dragInProgress = false;
        }
    }

    private void TxChannelList_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragReady = false;
    }

    private void RxChannelList_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DragDataFormat)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        FindControl<Border>("RxDropBorder")!.BorderBrush = e.DragEffects == DragDropEffects.Copy
            ? ResourceBrush("AccentBrush")
            : ResourceBrush("PanelBorderBrush");
    }

    private void RxChannelList_DragLeave(object? sender, DragEventArgs e)
    {
        FindControl<Border>("RxDropBorder")!.BorderBrush = ResourceBrush("PanelBorderBrush");
    }

    private void RxChannelList_Drop(object? sender, DragEventArgs e)
    {
        FindControl<Border>("RxDropBorder")!.BorderBrush = ResourceBrush("PanelBorderBrush");
        string? payload = e.DataTransfer.TryGetValue(DragDataFormat);
        PatchSourceDescriptor[]? sources = string.IsNullOrWhiteSpace(payload)
            ? null
            : JsonSerializer.Deserialize<PatchSourceDescriptor[]>(payload);
        if (sources is null || sources.Length == 0)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        Control? sourceControl = e.Source as Control;
        ListBoxItem? container = sourceControl as ListBoxItem
            ?? sourceControl?.FindAncestorOfType<ListBoxItem>();
        PatchRxListItem? targetRow = container?.Content as PatchRxListItem
            ?? container?.DataContext as PatchRxListItem;
        if (targetRow is null)
        {
            SetInfo(L("Déposez les TX sur une ligne RX.", "Drop the Tx channels on an Rx row."), warning: true);
            e.DragEffects = DragDropEffects.None;
            return;
        }

        FindControl<ListBox>("RxChannelList")!.SelectedItem = targetRow;
        AssignSources(sources, targetRow.Target);
        e.DragEffects = DragDropEffects.Copy;
    }

    private PatchSourceDescriptor[] SelectedSources()
    {
        return FindControl<ListBox>("TxChannelList")!.SelectedItems?
            .OfType<PatchSourceDescriptor>()
            .OrderBy(source => source.PositionIndex)
            .ToArray() ?? [];
    }

    private void ResetPendingButton_Click(object? sender, RoutedEventArgs e)
    {
        _session.Reset();
        RefreshAllTargetStates();
        SetInfo(L("Tous les changements visuels ont été annulés.", "All visual changes were discarded."));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_session.HasChanges)
        {
            SetInfo(L("Aucun changement à appliquer.", "No changes to apply."), warning: true);
            return;
        }

        Close(true);
    }

    private void UpdateCommandState()
    {
        bool hasTxSelection = FindControl<ListBox>("TxChannelList")!.SelectedItems?.Count > 0;
        bool hasRxSelection = FindControl<ListBox>("RxChannelList")!.SelectedItem is PatchRxListItem;
        FindControl<Button>("AssignSequentialButton")!.IsEnabled = hasTxSelection && hasRxSelection;
        FindControl<Button>("RemoveSelectedRxButton")!.IsEnabled = hasRxSelection;
        FindControl<Button>("ResetPendingButton")!.IsEnabled = _session.HasChanges;
        FindControl<Button>("ApplyButton")!.IsEnabled = _session.HasChanges;

        string pending = _session.PendingCount == 0
            ? L("Aucun changement en attente", "No pending changes")
            : L(
                $"{_session.PendingCount} changement(s) en attente",
                $"{_session.PendingCount} pending change(s)");
        FindControl<TextBlock>("PendingHeaderText")!.Text = pending;
        FindControl<TextBlock>("PendingFooterText")!.Text = pending;
    }

    private void UpdateMatrixHint(int displayedSourceCount, int displayedTargetCount)
    {
        bool truncated = displayedSourceCount < _visibleSources.Count || displayedTargetCount < _visibleTargets.Count;
        FindControl<TextBlock>("MatrixHintText")!.Text = truncated
            ? L(
                "RX en lignes, TX en colonnes. Affichage limité aux 128 premiers canaux de chaque côté.",
                "Rx channels are rows and Tx channels are columns. Display is limited to the first 128 channels on each side.")
            : L(
                "RX en lignes, TX en colonnes. Cliquez dans une case pour affecter ou retirer un patch.",
                "Rx channels are rows and Tx channels are columns. Click a cell to assign or remove a subscription.");
    }

    private void ApplyLanguage()
    {
        Title = L("Patch visuel", "Visual patch");
        FindControl<TextBlock>("TitleText")!.Text = Title;
        FindControl<TextBlock>("IntroText")!.Text = L(
            "Préparez les affectations puis appliquez-les en une seule opération.",
            "Stage assignments, then apply them in a single operation.");
        FindControl<TextBlock>("TxDeviceLabel")!.Text = L("Machine émettrice TX", "Tx transmitting device");
        FindControl<TextBlock>("RxDeviceLabel")!.Text = L("Machine réceptrice RX", "Rx receiving device");
        FindControl<TabItem>("AssignmentTab")!.Header = L("Glisser-déposer et série", "Drag and drop / sequential");
        FindControl<TabItem>("MatrixTab")!.Header = L("Grille de patch", "Patch matrix");
        FindControl<TextBlock>("TxListHeading")!.Text = L("Canaux TX disponibles", "Available Tx channels");
        FindControl<TextBlock>("RxListHeading")!.Text = L("Canaux RX et source actuelle", "Rx channels and current source");
        FindControl<TextBlock>("DragHintText")!.Text = L(
            "Sélectionnez un ou plusieurs TX puis déposez-les sur le premier RX.",
            "Select one or more Tx channels, then drop them on the first Rx.");
        FindControl<Button>("AssignSequentialButton")!.Content = L("Affecter à partir du RX sélectionné", "Assign from selected Rx");
        FindControl<Button>("RemoveSelectedRxButton")!.Content = L("Déconnecter le RX sélectionné", "Disconnect selected Rx");
        FindControl<Button>("ResetPendingButton")!.Content = L("Annuler les changements visuels", "Discard visual changes");
        FindControl<Button>("CancelButton")!.Content = L("Fermer sans appliquer", "Close without applying");
        FindControl<Button>("ApplyButton")!.Content = L("Appliquer au projet", "Apply to project");

        AutomationProperties.SetName(FindControl<ComboBox>("TxDeviceCombo")!, FindControl<TextBlock>("TxDeviceLabel")!.Text);
        AutomationProperties.SetName(FindControl<ComboBox>("RxDeviceCombo")!, FindControl<TextBlock>("RxDeviceLabel")!.Text);
        AutomationProperties.SetName(FindControl<ListBox>("TxChannelList")!, FindControl<TextBlock>("TxListHeading")!.Text);
        AutomationProperties.SetName(FindControl<ListBox>("RxChannelList")!, FindControl<TextBlock>("RxListHeading")!.Text);
        AutomationProperties.SetName(FindControl<Grid>("MatrixPanel")!, FindControl<TabItem>("MatrixTab")!.Header?.ToString() ?? string.Empty);
    }

    private void SetInfo(string message, bool warning = false)
    {
        TextBlock info = FindControl<TextBlock>("InfoText")!;
        info.Text = message;
        info.Foreground = warning ? ResourceBrush("WarningBrush") : ResourceBrush("MutedTextBrush");
    }

    private IBrush ResourceBrush(string key)
    {
        return Application.Current?.Resources[key] as IBrush ?? Brushes.Gray;
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

    private string L(string french, string english) => _language == UiLanguage.English ? english : french;

    private sealed class PatchRxListItem(PatchTargetDescriptor target, string display)
        : System.ComponentModel.INotifyPropertyChanged
    {
        private string _display = display;

        public PatchTargetDescriptor Target { get; } = target;

        public string Display
        {
            get => _display;
            set
            {
                if (string.Equals(_display, value, StringComparison.Ordinal))
                {
                    return;
                }

                _display = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Display)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class PatchMatrixCell(
        PatchSourceDescriptor source,
        PatchTargetDescriptor target,
        int sourceIndex,
        int targetIndex,
        bool isAssigned)
    {
        public PatchSourceDescriptor Source { get; } = source;

        public PatchTargetDescriptor Target { get; } = target;

        public int SourceIndex { get; } = sourceIndex;

        public int TargetIndex { get; } = targetIndex;

        public bool IsAssigned { get; set; } = isAssigned;

        public Button Button { get; set; } = null!;
    }

    private readonly record struct MatrixCellKey(int SourceIndex, int TargetIndex);
}
