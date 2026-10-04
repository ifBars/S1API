using S1API.Internal.Rendering;

namespace S1API.Tests.Rendering;

public sealed class RuntimePreviewLayerTests
{
    [Fact]
    public void BetaLayerIsSelectedWhenLegacyLayerIsAbsent()
    {
        Assert.Equal(20, RuntimePreviewLayer.Resolve(name =>
            name == "RuntimePreviewGeneration" ? 20 : -1));
    }

    [Fact]
    public void CurrentLayerTakesPriorityWhenBothNamesExist()
    {
        Assert.Equal(20, RuntimePreviewLayer.Resolve(name =>
            name == "RuntimePreviewGeneration" ? 20 : 30));
    }

    [Fact]
    public void OlderGamesRetainTheirLegacyLayer()
    {
        Assert.Equal(30, RuntimePreviewLayer.Resolve(name =>
            name == "IconGeneration" ? 30 : -1));
    }

    [Fact]
    public void LayerZeroIsValidAndDoesNotTriggerFallback()
    {
        Assert.Equal(0, RuntimePreviewLayer.Resolve(name =>
            name == "RuntimePreviewGeneration" ? 0 : 30));
    }

    [Fact]
    public void MissingLayersRemainUnavailableInsteadOfUsingAnUnrelatedLayer()
    {
        Assert.Equal(-1, RuntimePreviewLayer.Resolve(_ => -1));
    }
}
