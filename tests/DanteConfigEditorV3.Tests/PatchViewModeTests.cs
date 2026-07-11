using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class PatchViewModeTests
{
    [Theory]
    [InlineData(PatchViewMode.ExpertKey, true)]
    [InlineData(PatchViewMode.SimpleKey, false)]
    [InlineData("Expert", false)]
    [InlineData(null, false)]
    public void ExpertColumnsDependOnTheLocalizedOptionKey(string? selectedKey, bool expected)
    {
        Assert.Equal(expected, PatchViewMode.IsExpert(selectedKey));
    }
}
