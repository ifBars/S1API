using System.Reflection;
using S1API.Internal.Building;
using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Tests.Items;

public sealed class FurnitureApiCompatibilityTests
{
    [Fact]
    public void FurnitureBuilderExposesRuntimeAgnosticFluentSurface()
    {
        MethodInfo? createBuilder = typeof(FurnitureCreator).GetMethod(
            nameof(FurnitureCreator.CreateBuilder),
            Type.EmptyTypes);

        Assert.NotNull(createBuilder);
        Assert.Equal(typeof(FurnitureDefinitionBuilder), createBuilder!.ReturnType);
        AssertCreatorCloneOverload(typeof(string), "sourceItemId");
        AssertCreatorCloneOverload(typeof(BuildableItemDefinition), "source");
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithBasicInfo), typeof(string), typeof(string), typeof(string));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithModel), typeof(GameObject));
        AssertFluent(nameof(FurnitureDefinitionBuilder.ConfigureModel), typeof(Action<GameObject>));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithPlacement), typeof(FurniturePlacementMode));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithFootprint), typeof(int), typeof(int));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithSurfacePlacement), typeof(FurnitureSurfaceType), typeof(bool));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithBuildSound), typeof(BuildSoundType));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithPricing), typeof(float), typeof(float));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithStackLimit), typeof(int));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithIcon), typeof(Sprite));
        AssertFluent(nameof(FurnitureDefinitionBuilder.WithGeneratedIcon), typeof(int));

        MethodInfo? build = typeof(FurnitureDefinitionBuilder).GetMethod(
            nameof(FurnitureDefinitionBuilder.Build),
            Type.EmptyTypes);
        Assert.NotNull(build);
        Assert.Equal(typeof(BuildableItemDefinition), build!.ReturnType);
    }

    [Fact]
    public void PlacementEnumsExposeOnlySupportedNativeFamilies()
    {
        Assert.Equal(
            new[] { FurniturePlacementMode.Grid, FurniturePlacementMode.Surface },
            Enum.GetValues<FurniturePlacementMode>());
        Assert.Equal(
            FurnitureSurfaceType.Wall | FurnitureSurfaceType.Roof,
            FurnitureSurfaceType.All);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void FootprintRejectsNonPositiveDimensions(int width, int depth)
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithFootprint(width, depth));
    }

    [Theory]
    [InlineData(FurnitureSurfaceType.None)]
    [InlineData((FurnitureSurfaceType)8)]
    public void SurfacePlacementRejectsEmptyOrUnknownFlags(FurnitureSurfaceType surfaceTypes)
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithSurfacePlacement(surfaceTypes));
    }

    [Fact]
    public void ModelAndIconRejectNull()
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithModel(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithIcon(null!));
        Assert.Throws<ArgumentNullException>(() => builder.ConfigureModel(null!));
    }

    [Fact]
    public void ConfigureModelRejectsCreateBuilderPath()
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();

        Assert.Throws<InvalidOperationException>(
            () => builder.ConfigureModel(_ => { }));
    }

    [Fact]
    public void CloneFromRejectsInvalidPublicInputsBeforeNativeResolution()
    {
        Assert.Throws<ArgumentException>(() => FurnitureCreator.CloneFrom(" "));
        Assert.Throws<ArgumentNullException>(
            () => FurnitureCreator.CloneFrom((BuildableItemDefinition)null!));
    }

    [Fact]
    public void DefaultBuildSoundIsWood()
    {
        Assert.Equal(BuildSoundType.Wood, FurnitureBuildSoundMapper.Default);
    }

    [Theory]
    [InlineData(BuildSoundType.Cardboard, 0)]
    [InlineData(BuildSoundType.Wood, 1)]
    [InlineData(BuildSoundType.Metal, 2)]
    [InlineData(BuildSoundType.Plastic, 2)]
    public void BuildSoundsMapToNativeValues(BuildSoundType soundType, int nativeValue)
    {
        Assert.Equal(
            nativeValue,
            Convert.ToInt32(FurnitureBuildSoundMapper.ToNative(soundType)));
    }

    [Fact]
    public void BuildSoundRejectsUnknownValue()
    {
        FurnitureDefinitionBuilder builder = FurnitureCreator.CreateBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithBuildSound((BuildSoundType)int.MaxValue));
    }

    [Theory]
    [InlineData(0, BuildSoundType.Cardboard)]
    [InlineData(1, BuildSoundType.Wood)]
    [InlineData(2, BuildSoundType.Metal)]
    public void NativeBuildSoundsMapBackToPublicValues(int nativeValue, BuildSoundType soundType)
    {
#if IL2CPPMELON
        var native = (Il2CppScheduleOne.ItemFramework.BuildableItemDefinition.EBuildSoundType)nativeValue;
#else
        var native = (ScheduleOne.ItemFramework.BuildableItemDefinition.EBuildSoundType)nativeValue;
#endif
        Assert.Equal(soundType, FurnitureBuildSoundMapper.FromNative(native));
    }

    private static void AssertCreatorCloneOverload(Type parameterType, string parameterName)
    {
        MethodInfo? method = typeof(FurnitureCreator).GetMethod(
            nameof(FurnitureCreator.CloneFrom),
            new[] { parameterType });

        Assert.NotNull(method);
        Assert.Equal(typeof(FurnitureDefinitionBuilder), method!.ReturnType);
        Assert.Equal(parameterName, Assert.Single(method.GetParameters()).Name);
    }

    private static void AssertFluent(string name, params Type[] parameterTypes)
    {
        MethodInfo? method = typeof(FurnitureDefinitionBuilder).GetMethod(name, parameterTypes);
        Assert.NotNull(method);
        Assert.Equal(typeof(FurnitureDefinitionBuilder), method!.ReturnType);
    }
}
