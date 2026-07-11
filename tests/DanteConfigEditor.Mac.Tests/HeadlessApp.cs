using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using DanteConfigEditor.Mac;

[assembly: AvaloniaTestApplication(typeof(DanteConfigEditor.Mac.Tests.HeadlessApp))]

namespace DanteConfigEditor.Mac.Tests;

public static class HeadlessApp
{
    // Chaque test démarre Avalonia sans fenêtre système ni dépendance à un écran.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
        .WithInterFont();
}
