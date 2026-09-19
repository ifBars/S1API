#if IL2CPPMELON
using NativeProductDefinition = Il2CppScheduleOne.Product.ProductDefinition;
#elif MONOMELON
using NativeProductDefinition = ScheduleOne.Product.ProductDefinition;
#endif

using S1API.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductDefinitionBuilderContractTests
{
    [Fact]
    public void NormalizeIdUsesStableNamespacedProductKindGrammar()
    {
        Assert.Equal(
            "example.mod:products/focus-tablet",
            CustomProductDefinitionBuilderContract.NormalizeId(
                " example.mod:products/focus-tablet "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("missing-namespace")]
    [InlineData(":missing-owner")]
    [InlineData("missing-name:")]
    [InlineData("too:many:separators")]
    [InlineData("unsafe name:tablet")]
    public void NormalizeIdRejectsInvalidDurableIds(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => CustomProductDefinitionBuilderContract.NormalizeId(id!));
    }

    [Fact]
    public void OwnerIdComesFromStableProductNamespace()
    {
        Assert.Equal(
            "example.mod",
            CustomProductDefinitionBuilderContract.GetOwnerId(
                "example.mod:focus-tablet"));
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(12.6f, 13f)]
    [InlineData(999f, 999f)]
    [InlineData(2000f, 999f)]
    public void ProductPriceMatchesNativeClampAndRoundPolicy(
        float input,
        float expected)
    {
        Assert.Equal(
            expected,
            CustomProductDefinitionBuilderContract.NormalizeProductPrice(input));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ProductPriceMustBeFinite(float price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CustomProductDefinitionBuilderContract.NormalizeProductPrice(
                    price));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(0.25f, 0.25f)]
    [InlineData(1f, 1f)]
    [InlineData(5f, 1f)]
    public void AddictivenessUsesNativeRange(float input, float expected)
    {
        Assert.Equal(
            expected,
            CustomProductDefinitionBuilderContract.NormalizeAddictiveness(
                input));
    }

    [Fact]
    public void ZeroPropertiesAreSupported()
    {
        CustomProductDefinitionBuilderContract.ValidatePropertyCounts(0, 0);
    }

    [Fact]
    public void EveryPropertyMustResolveDistinctly()
    {
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CustomProductDefinitionBuilderContract
                        .ValidatePropertyCounts(2, 1));

        Assert.Contains("distinct native property", exception.Message);
    }

    [Fact]
    public void NativePropertyLimitIsEnforced()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
                CustomProductDefinitionBuilderContract.ValidatePropertyCounts(
                    9,
                    9));
    }

    [Fact]
    public void QualityAndDurationValidationRejectUndefinedInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CustomProductDefinitionBuilderContract.ValidateQuality(
                    (Quality)int.MaxValue,
                    "quality"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CustomProductDefinitionBuilderContract.ValidateEffectDuration(
                    -1,
                    "seconds"));
    }

    [Fact]
    public void BuilderDefaultsFailRequiredFieldsInStableOrder()
    {
        ProductKind kind =
            new ProductKindBuilder(
                    $"s1api-tests:{Guid.NewGuid():N}")
                .WithCompatibilityDrugType(DrugType.MDMA)
                .Build();
        var builder =
            new CustomProductDefinitionBuilder(
                $"s1api-tests:{Guid.NewGuid():N}",
                kind);

        InvalidOperationException missingName =
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal(
            "WithName must be called before Build().",
            missingName.Message);

        builder.WithName("Contract Product");
        InvalidOperationException missingPrice =
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal(
            "WithProductPrice must be called before Build().",
            missingPrice.Message);

        builder.WithProductPrice(10f);
        InvalidOperationException missingRepresentations =
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Equal(
            "WithRepresentationsFrom must be called before Build().",
            missingRepresentations.Message);
    }

    [Fact]
    public void ProductKindCompatibilityMappingIsRequiredBeforeNativeCreation()
    {
        ProductKind kind =
            new ProductKindBuilder(
                    $"s1api-tests:{Guid.NewGuid():N}")
                .Build();
        var builder =
            new CustomProductDefinitionBuilder(
                    $"s1api-tests:{Guid.NewGuid():N}",
                    kind)
                .WithName("Contract Product")
                .WithProductPrice(10f)
                .WithRepresentationsFrom(CreateUninitializedTemplate());

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("does not define a compatibility drug type", exception.Message);
    }

    private static ProductDefinition CreateUninitializedTemplate()
    {
        var native = TestObjectFactory.CreateUninitialized<NativeProductDefinition>();
        return new ProductDefinition(native);
    }
}
