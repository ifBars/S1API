using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class MugshotCapturePolicyTests
{
    [Theory]
    [InlineData(500, 4096, 0.60f, 0.90f, true)]
    [InlineData(500, 4096, 0.60f, 0.80f, false)]
    [InlineData(200, 4096, 0.60f, 0.90f, false)]
    [InlineData(500, 4096, 0.30f, 0.90f, false)]
    [InlineData(500, 4096, 0.60f, 0.40f, false)]
    [InlineData(500, 0, 0.60f, 0.80f, false)]
    public void PortraitCoverageRequiresSubstantialVisibleBounds(
        int visibleSamples,
        int totalSamples,
        float contentWidth,
        float contentHeight,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCAppearance.IsPortraitCoverageSufficient(
                visibleSamples,
                totalSamples,
                contentWidth,
                contentHeight));
    }
}
