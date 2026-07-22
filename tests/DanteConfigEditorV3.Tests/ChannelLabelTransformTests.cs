using DanteConfigEditor.Models;
using DanteConfigEditor.Services;

namespace DanteConfigEditorV3.Tests;

public sealed class ChannelLabelTransformTests
{
    [Theory]
    [InlineData(ChannelLabelCaseMode.Preserve, "mIC Chant", "mIC Chant")]
    [InlineData(ChannelLabelCaseMode.Lowercase, "mIC Chant", "mic chant")]
    [InlineData(ChannelLabelCaseMode.Uppercase, "mIC Chant", "MIC CHANT")]
    [InlineData(ChannelLabelCaseMode.FirstLetterUppercase, "mIC CHANT", "Mic chant")]
    public void CaseModesAreAppliedPredictably(ChannelLabelCaseMode mode, string source, string expected)
    {
        ChannelLabelTransformOptions options = new(false, mode, MaximumLength: 0, StartPosition: 1, FromEnd: false);

        Assert.Equal(expected, ChannelLabelTransformService.Transform(source, options));
    }

    [Theory]
    [InlineData(1, false, "001-MicroChant", "001-Micr")]
    [InlineData(5, false, "001-MicroChant", "MicroCha")]
    [InlineData(1, true, "001-MicroChant", "croChant")]
    [InlineData(3, true, "001-MicroChant", "MicroCha")]
    public void StartPositionAndDirectionControlTruncation(int start, bool fromEnd, string source, string expected)
    {
        ChannelLabelTransformOptions options = new(false, ChannelLabelCaseMode.Preserve, 8, start, fromEnd);

        Assert.Equal(expected, ChannelLabelTransformService.Transform(source, options));
    }

    [Fact]
    public void ConsoleAdaptationProducesAsciiAndReportsCollisions()
    {
        ChannelLabelTransformOptions options = new(true, ChannelLabelCaseMode.Uppercase, 8, 1, false);
        ChannelLabelSet source = new("DANTE-A", ChannelLabelDirection.Tx,
        [
            new ChannelLabelEntry(1, "TrèsLongLabel"),
            new ChannelLabelEntry(2, "TresLongAutre")
        ]);

        ChannelLabelTransformResult result = ChannelLabelTransformService.Transform(source, options);

        Assert.Equal(["TRESLONG", "TRESLONG"], result.Labels.Channels.Select(channel => channel.Label));
        Assert.Single(result.Collisions);
        Assert.Equal("TRESLONG", result.Collisions[0].Label);
    }
}
