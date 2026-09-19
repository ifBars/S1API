using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

public sealed class ProductPackagingContentProfileTests
{
    [Fact]
    public void NullContentProviderIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .WithContent(null!));
    }

    [Fact]
    public void ContentProviderIsRequired()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => new ProductPackagingContentProfileBuilder().Build());

        Assert.Contains("content provider", exception.Message);
    }

    [Fact]
    public void EmptyPlacementsPreserveOneAuthoredContentTransform()
    {
        Func<GameObject?> provider = () => null;

        ProductPackagingContentProfile profile =
            new ProductPackagingContentProfileBuilder()
                .WithContent(provider)
                .Build();

        Assert.Same(provider, profile.ContentProvider);
        Assert.Empty(profile.Placements);
        Assert.Equal(
            ProductPackagingContentSource.RepeatedContent,
            profile.Source);
        Assert.Null(profile.CompleteVisualTransform);
        Assert.Null(profile.NativeVisualTemplate);
        Assert.Null(profile.NativeVisualCustomizer);
    }

    [Fact]
    public void CompleteFilledVisualSnapshotsItsProviderAndTransform()
    {
        Func<GameObject?> provider = () => null;
        ProductPresentationTransform transform =
            CreatePlacementWithoutUnityRuntime();

        ProductPackagingContentProfile profile =
            new ProductPackagingContentProfileBuilder()
                .WithCompleteFilledVisual(provider, transform)
                .Build();

        Assert.Equal(
            ProductPackagingContentSource.CompleteFilledVisual,
            profile.Source);
        Assert.Same(provider, profile.ContentProvider);
        Assert.Same(transform, profile.CompleteVisualTransform);
        Assert.Empty(profile.Placements);
        Assert.Null(profile.NativeVisualTemplate);
        Assert.Null(profile.NativeVisualCustomizer);
    }

    [Fact]
    public void NullCompleteFilledVisualProviderIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .WithCompleteFilledVisual(null!));
    }

    [Fact]
    public void NativeFilledVisualScaffoldSnapshotsTemplateCustomizerAndTransform()
    {
        Action<GameObject> customize = _ => { };
        ProductPresentationTransform transform =
            CreatePlacementWithoutUnityRuntime();

        ProductPackagingContentProfile profile =
            new ProductPackagingContentProfileBuilder()
                .WithNativeFilledVisualScaffold(
                    ProductPackagingVisualTemplate.Marijuana,
                    customize,
                    transform)
                .Build();

        Assert.Equal(
            ProductPackagingContentSource.NativeFilledVisualScaffold,
            profile.Source);
        Assert.Null(profile.ContentProvider);
        Assert.Same(transform, profile.CompleteVisualTransform);
        Assert.Equal(
            ProductPackagingVisualTemplate.Marijuana,
            profile.NativeVisualTemplate);
        Assert.Same(customize, profile.NativeVisualCustomizer);
        Assert.Empty(profile.Placements);
    }

    [Fact]
    public void UndefinedNativeFilledVisualTemplateIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .WithNativeFilledVisualScaffold(
                        (ProductPackagingVisualTemplate)int.MaxValue));
    }

    [Fact]
    public void CompleteFilledVisualCannotRepeatContentPlacements()
    {
        ProductPackagingContentProfileBuilder builder =
            new ProductPackagingContentProfileBuilder()
                .WithCompleteFilledVisual(() => null)
                .AddPlacement(CreatePlacementWithoutUnityRuntime());

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("placements", exception.Message);
        Assert.Contains("complete filled visual", exception.Message);
    }

    [Fact]
    public void BuilderCanReturnToLegacyRepeatedContentAfterSelectingScaffold()
    {
        Func<GameObject?> provider = () => null;
        ProductPresentationTransform placement =
            CreatePlacementWithoutUnityRuntime();
        ProductPackagingContentProfileBuilder builder =
            new ProductPackagingContentProfileBuilder()
                .AddPlacement(placement)
                .WithNativeFilledVisualScaffold(
                    ProductPackagingVisualTemplate.Cocaine);

        Assert.Throws<InvalidOperationException>(() => builder.Build());

        ProductPackagingContentProfile legacy =
            builder.WithContent(provider).Build();

        Assert.Equal(
            ProductPackagingContentSource.RepeatedContent,
            legacy.Source);
        Assert.Same(provider, legacy.ContentProvider);
        Assert.Equal(new[] { placement }, legacy.Placements);
        Assert.Null(legacy.CompleteVisualTransform);
        Assert.Null(legacy.NativeVisualTemplate);
        Assert.Null(legacy.NativeVisualCustomizer);
    }

    [Fact]
    public void BuildSnapshotsOrderedPlacementsAcrossBuilderReuse()
    {
        ProductPresentationTransform first = CreatePlacementWithoutUnityRuntime();
        ProductPresentationTransform second = CreatePlacementWithoutUnityRuntime();
        var builder =
            new ProductPackagingContentProfileBuilder()
                .WithContent(() => null)
                .AddPlacement(first);
        ProductPackagingContentProfile profile = builder.Build();

        builder.AddPlacement(second);
        ProductPackagingContentProfile repeated = builder.Build();

        Assert.Equal(new[] { first }, profile.Placements);
        Assert.Equal(new[] { first, second }, repeated.Placements);
    }

    [Fact]
    public void NullPlacementIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .AddPlacement(null!));
    }

    [Fact]
    public void NullPlacementArrayIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .AddPlacements(null!));
    }

    [Fact]
    public void NullPlacementInsideArrayIsRejected()
    {
        ProductPresentationTransform valid = CreatePlacementWithoutUnityRuntime();

        Assert.Throws<ArgumentNullException>(
            () =>
                new ProductPackagingContentProfileBuilder()
                    .AddPlacements(valid, null!));
    }

    private static ProductPresentationTransform CreatePlacementWithoutUnityRuntime()
    {
        return TestObjectFactory.CreateUninitialized<ProductPresentationTransform>();
    }
}
