#if IL2CPPMELON
using NativeProductDefinition = Il2CppScheduleOne.Product.ProductDefinition;
#elif MONOMELON
using NativeProductDefinition = ScheduleOne.Product.ProductDefinition;
#endif

using System;
using S1API.Internal.Products;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class ProductPackagingContentFallbackTests : IDisposable
{
    private readonly FakeProductRuntime _runtime = new();

    public ProductPackagingContentFallbackTests()
    {
        ProductPackagingContentProfileRegistry.ResetForTesting();
        CustomProductDefinitionRegistry.ResetForTesting(_runtime);
    }

    public void Dispose()
    {
        ProductPackagingContentProfileRegistry.ResetForTesting();
        CustomProductDefinitionRegistry.RestoreRuntimeAdapterForTesting();
    }

    [Fact]
    public void LogicalKindFallbackResolvesForDynamicallyAllocatedGeneratedMixes()
    {
        string kindId = "moredrugs:mdma-" + Guid.NewGuid().ToString("N");
        var profile = new ProductPackagingContentProfileBuilder()
            .WithContent(() => null)
            .Build();

        ProductPackagingContentProfileRegistry.RegisterForProductKind(
            "moredrugs", kindId, "baggie", profile);

        Assert.True(ProductPackagingContentProfileRegistry.TryResolveForProductKind(
            kindId, "baggie", out ProductPackagingContentProfileRegistration? resolved));
        Assert.Same(profile, resolved!.Profile);
    }

    [Fact]
    public void ProductSpecificBrickProfilePrecedesLogicalKindFallback()
    {
        string productId = "s1api-tests:brick-" + Guid.NewGuid().ToString("N");
        ProductKind kind =
            new ProductKindBuilder(
                    "s1api-tests:brick-kind-" + Guid.NewGuid().ToString("N"))
                .WithCompatibilityDrugType(DrugType.MDMA)
                .Build();
        ProductPackagingContentProfile kindProfile =
            new ProductPackagingContentProfileBuilder()
                .WithCompleteFilledVisual(() => null)
                .Build();
        ProductPackagingContentProfile productProfile =
            new ProductPackagingContentProfileBuilder()
                .WithNativeFilledVisualScaffold(
                    ProductPackagingVisualTemplate.Marijuana)
                .Build();

        CustomProductDefinitionRegistry.Register(
            "s1api-tests",
            productId,
            "Brick Product",
            10f,
            CreateDefinition(),
            new CustomProductDefinitionMetadata(kind, Quality.Standard));
        ProductPackagingContentProfileRegistry.RegisterForProductKind(
            "s1api-tests",
            kind.Id,
            "brick",
            kindProfile);
        ProductPackagingContentProfileRegistry.Register(
            "s1api-tests",
            productId,
            "brick",
            productProfile);

        Assert.True(
            ProductPackagingContentProfileRegistry.TryResolve(
                productId,
                "brick",
                out ProductPackagingContentProfileRegistration? resolved));
        Assert.Same(productProfile, resolved!.Profile);
    }

    private static NativeProductDefinition CreateDefinition()
    {
        return TestObjectFactory.CreateUninitialized<NativeProductDefinition>();
    }

    private sealed class FakeProductRuntime :
        ICustomProductDefinitionRuntimeAdapter
    {
        public bool Apply(CustomProductDefinitionRegistration registration) =>
            true;
    }
}
