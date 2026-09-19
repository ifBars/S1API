using S1API.Internal.Building;
using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Tests.Items;

public sealed class FurnitureClonePolicyTests
{
    [Theory]
    [InlineData("couch", "couch")]
    [InlineData("Couch", "couch")]
    public void VariantRejectsDonorIdReuse(string itemId, string donorId)
    {
        Assert.Throws<InvalidOperationException>(
            () => FurnitureClonePolicy.ValidateNewId(itemId, donorId));
    }

    [Fact]
    public void VariantAcceptsNewStableId()
    {
        FurnitureClonePolicy.ValidateNewId("example.mod:blue-couch", "couch");
    }

    [Fact]
    public void CreateBuilderHasNoDonorIdentityConstraint()
    {
        FurnitureClonePolicy.ValidateNewId("example.mod:chair", donorId: null);
    }

    [Fact]
    public void CloneBuilderLeavesIdentityUnsetAndCopiesSafeDefaults()
    {
        var footprint = new[]
        {
            new FurnitureFootprintCoordinate(0, 0),
            new FurnitureFootprintCoordinate(1, 0),
        };
        Sprite icon = TestObjectFactory.CreateUninitialized<Sprite>();
        var source = new FurnitureCloneSource(
            "native-chair",
            model: null!,
            FurniturePlacementMode.Grid,
            footprint,
            FurnitureSurfaceType.Roof,
            allowSurfaceRotation: false,
            BuildSoundType.Metal,
            stackLimit: 4,
            purchasePrice: 125f,
            resellMultiplier: 0.25f,
            icon);

        var builder = new FurnitureDefinitionBuilder(source);

        Assert.Null(GetField<string?>(builder, "_id"));
        Assert.Equal("native-chair", GetField<string>(builder, "_donorId"));
        Assert.Same(footprint, GetField<object>(builder, "_donorFootprint"));
        Assert.Equal(FurniturePlacementMode.Grid, GetField<FurniturePlacementMode>(builder, "_placementMode"));
        Assert.Equal(2, GetField<int>(builder, "_footprintWidth"));
        Assert.Equal(1, GetField<int>(builder, "_footprintDepth"));
        Assert.Equal(FurnitureSurfaceType.Roof, GetField<FurnitureSurfaceType>(builder, "_surfaceTypes"));
        Assert.False(GetField<bool>(builder, "_allowSurfaceRotation"));
        Assert.Equal(BuildSoundType.Metal, GetField<BuildSoundType>(builder, "_buildSound"));
        Assert.Equal(4, GetField<int>(builder, "_stackLimit"));
        Assert.Equal(125f, GetField<float>(builder, "_purchasePrice"));
        Assert.Equal(0.25f, GetField<float>(builder, "_resellMultiplier"));
        Assert.Same(icon, GetField<Sprite>(builder, "_fallbackIcon"));
        Assert.True(GetField<bool>(builder, "_generateIcon"));
        Assert.False(GetField<bool>(builder, "_centerModelOnFootprint"));
        Assert.True(GetField<bool>(builder, "_isolateRepresentationMaterials"));
    }

    [Fact]
    public void ExplicitPlacementOverridesReplaceDonorDefaults()
    {
        var footprint = new[] { new FurnitureFootprintCoordinate(0, 0) };
        var source = new FurnitureCloneSource(
            "native-picture",
            model: null!,
            FurniturePlacementMode.Surface,
            footprint,
            FurnitureSurfaceType.Wall,
            allowSurfaceRotation: true,
            BuildSoundType.Wood,
            stackLimit: 1,
            purchasePrice: 1f,
            resellMultiplier: 0.5f,
            icon: null);
        var builder = new FurnitureDefinitionBuilder(source)
            .WithPlacement(FurniturePlacementMode.Grid)
            .WithFootprint(3, 2)
            .WithFootprint(4, 3)
            .WithSurfacePlacement(FurnitureSurfaceType.Roof, allowRotation: false);

        Assert.Equal(FurniturePlacementMode.Grid, GetField<FurniturePlacementMode>(builder, "_placementMode"));
        Assert.Null(GetField<object?>(builder, "_donorFootprint"));
        Assert.Equal(4, GetField<int>(builder, "_footprintWidth"));
        Assert.Equal(3, GetField<int>(builder, "_footprintDepth"));
        Assert.Equal(FurnitureSurfaceType.Roof, GetField<FurnitureSurfaceType>(builder, "_surfaceTypes"));
        Assert.False(GetField<bool>(builder, "_allowSurfaceRotation"));
    }

    private static T GetField<T>(FurnitureDefinitionBuilder builder, string name)
    {
        object? value = typeof(FurnitureDefinitionBuilder)
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder);
        return (T)value!;
    }
}
