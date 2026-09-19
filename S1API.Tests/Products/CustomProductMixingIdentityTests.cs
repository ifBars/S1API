using S1API.Internal.Products;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products;

public sealed class CustomProductMixingIdentityTests
{
    [Fact]
    public void PostSanitizationNativeIdGetsStableNamespacedGeneratedIdentity()
    {
        // ProductManager.FinishAndNameMix lowercases, calls MakeIDFileSafe, then strips ':'
        // before the private FinishAndNameMix/RPC seam. This is the actual post-sanitization
        // value, not an assumed MakeIDFileSafe result.
        const string nativePostSanitizationId = "moremdmablue";

        string generated = CustomProductMixingIdentity.CreateGeneratedProductId(
            "moredrugs:mdma",
            nativePostSanitizationId);

        Assert.StartsWith("moredrugs:mix/mdma/", generated);
        Assert.Equal(83, generated.Length);
        Assert.Equal("moredrugs",
            CustomProductDefinitionBuilderContract.GetOwnerId(generated));
        Assert.Equal(generated, CustomProductMixingIdentity.CreateGeneratedProductId(
            "moredrugs:mdma", nativePostSanitizationId));
        Assert.True(CustomProductMixingIdentity.IsGeneratedIdForSource(
            "moredrugs:mdma", generated));
    }

    [Fact]
    public void FourthAndDeeperGeneratedMixesStayWithinManifestIdentifierLimit()
    {
        string source = "ifbars.moredrugs:products/mdma";

        for (int depth = 1; depth <= 16; depth++)
        {
            string generated = CustomProductMixingIdentity.CreateGeneratedProductId(
                source,
                "native-mix-" + depth);

            Assert.True(CustomProductManifestData.IsBoundedIdentifier(generated));
            if (depth == 4)
                Assert.Equal(150, generated.Length);
            Assert.True(CustomProductMixingIdentity.IsGeneratedIdForSource(
                source,
                generated));
            source = generated;
        }
    }

    [Fact]
    public void LongManifestValidSourceUsesBoundedFrameworkFallbackOwner()
    {
        string source = new string('a', 250) + ":x";

        string generated = CustomProductMixingIdentity.CreateGeneratedProductId(
            source,
            "native-mix");

        Assert.True(CustomProductManifestData.IsBoundedIdentifier(generated));
        Assert.StartsWith("s1api:mix/", generated);
        Assert.True(CustomProductMixingIdentity.IsGeneratedIdForSource(
            source,
            generated));
    }

    [Fact]
    public void LegacyGeneratedMixIdRemainsRecognizedForItsSource()
    {
        const string source = "moredrugs:mdma";
        const string legacyGenerated = "moredrugs:mix/mdma/"
            + "7b1f0cb91f2bb6ca444caa221377fcc14c1354e500f8d0b1a2b039d0f73fe78b";

        Assert.True(CustomProductMixingIdentity.IsGeneratedIdForSource(
            source,
            legacyGenerated));
    }
}
