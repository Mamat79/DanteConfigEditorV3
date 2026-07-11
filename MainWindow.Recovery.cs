using System.ComponentModel;
using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class MainWindow
{
    private void ScheduleRecoverySnapshot()
    {
        if (_project is null)
        {
            return;
        }

        _recoveryTimer.Stop();
        _recoveryTimer.Start();
    }

    private async void RecoveryTimer_Tick(object? sender, EventArgs e)
    {
        _recoveryTimer.Stop();
        await UpdateRecoverySnapshotAsync();
    }

    private async Task UpdateRecoverySnapshotAsync()
    {
        DanteProject? project = _project;
        if (project is null)
        {
            return;
        }

        _recoveryWriteCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        _recoveryWriteCancellation = cancellation;
        try
        {
            if (project.IsModified)
            {
                await SessionRecoveryService.SaveAsync(project, cancellationToken: cancellation.Token);
            }
            else
            {
                await SessionRecoveryService.DeleteAsync(project.OriginalFilePath, cancellationToken: cancellation.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddLog(Tf("Log.RecoveryUnavailable", ex.Message));
        }
        finally
        {
            if (ReferenceEquals(_recoveryWriteCancellation, cancellation))
            {
                _recoveryWriteCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void CancelPendingRecoveryWrite()
    {
        _recoveryTimer.Stop();
        _recoveryWriteCancellation?.Cancel();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        CancelPendingRecoveryWrite();
        if (_project?.IsModified == true)
        {
            try
            {
                SessionRecoveryService.Save(_project);
            }
            catch
            {
                // La fermeture ne doit pas être bloquée si le fichier de récupération est indisponible.
            }
        }

        base.OnClosing(e);
    }
}
