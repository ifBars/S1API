using S1API.Internal.Products;
using UnityEngine;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class ProductManagerUiRuntimeContractTests : IDisposable
{
    public ProductManagerUiRuntimeContractTests()
    {
#if IL2CPPMELON
        ProductKindIconLifetime.SetUnityNullEvaluatorForTesting(_ => false);
#endif
    }

    public void Dispose()
    {
#if IL2CPPMELON
        ProductKindIconLifetime.SetUnityNullEvaluatorForTesting(null);
#endif
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void ContainerReconciliationRunsOnlyForMissingVisibleSections(
        bool isVisible,
        bool hasContainer,
        bool expected)
    {
        Assert.Equal(
            expected,
            ProductManagerUiRuntime.RequiresContainerReconciliation(
                isVisible,
                hasContainer));
    }

    [Fact]
    public void FavouriteRemovalIsNeverBlockedByCreationVisibility()
    {
        Assert.True(ProductManagerUiRuntime.AllowFavouriteRemoval());
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void StaleFavouriteSlotsArePurged(
        bool hasEntry,
        bool hasDefinition,
        bool expected)
    {
        Assert.Equal(
            expected,
            ProductManagerUiRuntime.ShouldPurgeFavouriteEntry(
                hasEntry,
                hasDefinition));
    }

    [Fact]
    public void MissingSectionIconClearsClonedSpriteAndUsesNeutralTint()
    {
        ProductManagerUiRuntime.SectionIconState state =
            ProductManagerUiRuntime.CreateSectionIconState(null);

        Assert.Null(state.Sprite);
        AssertNeutralTint(state.Tint);
    }

    [Fact]
    public void LiveSectionIconUsesTheRegisteredSpriteAndNeutralTint()
    {
        Sprite sprite = TestObjectFactory.CreateUninitialized<Sprite>();
#if MONOMELON
        typeof(UnityEngine.Object)
            .GetField(
                "m_CachedPtr",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(sprite, new IntPtr(1));
#endif

        ProductManagerUiRuntime.SectionIconState state =
            ProductManagerUiRuntime.CreateSectionIconState(sprite);

        Assert.Same(sprite, state.Sprite);
        AssertNeutralTint(state.Tint);
    }

    private static void AssertNeutralTint(Color tint)
    {
        Assert.Equal(1f, tint.r);
        Assert.Equal(1f, tint.g);
        Assert.Equal(1f, tint.b);
        Assert.Equal(1f, tint.a);
    }
}
