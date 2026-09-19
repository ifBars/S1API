using S1API.Internal.Products;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products;

public sealed class ProductMixingColorContractTests
{
    [Fact]
    public void CocaineStrategyMatchesNativePrimaryColorFormula()
    {
        var lowerTier = new ProductMixingColorSample(
            3,
            new ProductMixingColorValue(255, 0, 0, 255));
        var higherTier = new ProductMixingColorSample(
            5,
            new ProductMixingColorValue(0, 0, 255, 255));

        ProductMixingColorValue actual =
            ProductMixingColorContract.CalculatePrimaryColor(
            ProductMixingMap.Cocaine,
            new[] { higherTier, lowerTier });

        Assert.Equal(
            new ProductMixingColorValue(255, 155, 155, 255),
            actual);
    }

    [Theory]
    [InlineData(ProductMixingMap.Marijuana, 90, 100, 70)]
    [InlineData(ProductMixingMap.Methamphetamine, 255, 255, 255)]
    [InlineData(ProductMixingMap.Cocaine, 255, 255, 255)]
    [InlineData(ProductMixingMap.Shrooms, 168, 125, 43)]
    public void EmptyPropertiesUseNativePrimaryBaseColor(
        ProductMixingMap map,
        byte red,
        byte green,
        byte blue)
    {
        Assert.Equal(
            new ProductMixingColorValue(red, green, blue, 255),
            ProductMixingColorContract.CalculatePrimaryColor(
                map,
                Array.Empty<ProductMixingColorSample>()));
    }
}
