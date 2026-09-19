using S1API.Internal.Building;

namespace S1API.Tests.Items;

public sealed class FurnitureNativeCompositionContractTests
{
    [Fact]
    public void ComposerUsesVerifiedNativeGridAndSurfaceTemplates()
    {
        Assert.Equal("grandfatherclock", FurnitureTemplateCatalog.GridItemId);
        Assert.Equal("wallclock", FurnitureTemplateCatalog.SurfaceItemId);
    }

    [Fact]
    public void ComposerDoesNotExposeProceduralGridWithoutGenericNativeTemplate()
    {
        Assert.DoesNotContain(
            Enum.GetNames<global::S1API.Items.Buildable.FurniturePlacementMode>(),
            name => name.StartsWith("Procedural", StringComparison.Ordinal));
    }

    [Fact]
    public void ComposerAndGhostRuntimeShareAStableVisualMarker()
    {
        Assert.Equal("FurnitureVisual", BuildableGhostRuntime.FurnitureVisualName);
        Assert.Equal("FurnitureGhostVisual", BuildableGhostRuntime.FurnitureGhostVisualName);
    }
}
