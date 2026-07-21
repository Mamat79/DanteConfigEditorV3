using System.IO;
using System.Windows;
using System.Windows.Threading;
using DanteConfigEditor.Services;

namespace DanteConfigEditor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Ces gestionnaires évitent qu'une erreur UI ferme brutalement l'application.
        // Le détail est écrit dans l'espace local propre à la V3.1.
        DispatcherUnhandledException += HandleDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
        base.OnStartup(e);

        MainWindow window = new();
        MainWindow = window;
        window.Show();

        // Permet d'ouvrir directement un XML depuis un raccourci ou un glisser-déposer
        // sur l'exécutable.
        if (e.Args.Length > 0 && File.Exists(e.Args[0]))
        {
            window.LoadProjectFromPath(e.Args[0]);
        }
    }

    private void HandleDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowSafeError(e.Exception);
        e.Handled = true;
    }

    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            WriteCrashLog(exception);
        }
    }

    private void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
        e.SetObserved();
    }

    private static void ShowSafeError(Exception exception)
    {
        WriteCrashLog(exception);
        MessageBox.Show(
            "Une erreur interne a été interceptée. L'application reste ouverte." + Environment.NewLine + Environment.NewLine + exception.Message,
            "Erreur Dante Config Editor V3",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteCrashLog(Exception exception)
    {
        try
        {
            string directory = ApplicationStoragePaths.Resolve("Logs");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(path, exception.ToString());
        }
        catch
        {
            // Le journal de secours ne doit jamais faire planter l'application.
        }
    }
}
