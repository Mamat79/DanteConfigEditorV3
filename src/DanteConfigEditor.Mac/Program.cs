using Avalonia;
using System;

namespace DanteConfigEditor.Mac;

class Program
{
    // Aucun composant Avalonia ne doit être utilisé avant l'initialisation
    // du cycle de vie desktop.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Configuration commune au lancement normal et au designer Avalonia.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
