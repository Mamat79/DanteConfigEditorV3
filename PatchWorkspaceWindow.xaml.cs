using System.Windows;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class PatchWorkspaceWindow : Window
{
    private readonly PatchWorkspaceView _workspace;

    public PatchWorkspaceWindow(
        UiLanguage language,
        DanteProject project,
        bool useLightTheme,
        string? initialTxDeviceName = null,
        string? initialRxDeviceName = null,
        IEnumerable<PatchEditRequest>? initialEdits = null,
        bool returnEditsOnly = false,
        bool lockRxDeviceSelection = false)
    {
        InitializeComponent();
        _workspace = new PatchWorkspaceView(
            language,
            project,
            useLightTheme,
            initialTxDeviceName,
            initialRxDeviceName,
            initialEdits,
            returnEditsOnly,
            lockRxDeviceSelection);
        _workspace.ApplyRequested += Workspace_ApplyRequested;
        _workspace.CancelRequested += Workspace_CancelRequested;
        WorkspaceHost.Content = _workspace;
    }

    public IReadOnlyList<PatchEditRequest> Edits => _workspace.Edits;

    private void Workspace_ApplyRequested(object? sender, EventArgs e)
    {
        DialogResult = true;
    }

    private void Workspace_CancelRequested(object? sender, EventArgs e)
    {
        DialogResult = false;
    }
}
