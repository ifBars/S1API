using S1API.Internal.Patches;
using Xunit;

namespace S1API.Tests.Products;

public sealed class CustomProductMixingIngredientContractTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void NativeMixerContractAcceptsAnyNonEmptyPropertyList(
        int propertyCount,
        bool expected)
    {
        Assert.Equal(
            expected,
            CustomProductMixingIngredientContract.HasUsableProperty(propertyCount));
    }
}
