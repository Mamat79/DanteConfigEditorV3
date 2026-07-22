using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class InterfaceSettingsTests
{
    [Fact]
    public void ConfigurationEditorsAreExpandedOnFirstLaunchAndPreferencePersists()
    {
        string path = Path.Combine(Path.GetTempPath(), "DanteConfigEditorV3.Tests", Guid.NewGuid().ToString("N"), "configuration-editors.txt");

        Assert.True(InterfaceSettingsService.LoadConfigurationEditorsExpanded(path));

        InterfaceSettingsService.SaveConfigurationEditorsExpanded(false, path);
        Assert.False(InterfaceSettingsService.LoadConfigurationEditorsExpanded(path));

        InterfaceSettingsService.SaveConfigurationEditorsExpanded(true, path);
        Assert.True(InterfaceSettingsService.LoadConfigurationEditorsExpanded(path));
    }
}
