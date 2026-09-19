#if IL2CPPMELON
using NativeProductDefinition = Il2CppScheduleOne.Product.ProductDefinition;
#elif MONOMELON
using NativeProductDefinition = ScheduleOne.Product.ProductDefinition;
#endif

using S1API.Items;
using S1API.Products;
using NamespacedStorableItemDefinition = S1API.Items.Storable.StorableItemDefinition;

namespace S1API.Tests.Products;

public sealed class ProductDefinitionWrapperTests
{
    [Fact]
    public void PublicNullPreservesLegacyExceptionBehavior()
    {
        Assert.Throws<NullReferenceException>(
            () => ProductDefinitionWrapper.Wrap((ProductDefinition)null!));
    }

    [Fact]
    public void NativeNullPreservesLegacyGenericFallback()
    {
        ProductDefinition wrapped =
            ProductDefinitionWrapper.Wrap((NativeProductDefinition)null!);

        Assert.IsType<ProductDefinition>(wrapped);
    }

    [Fact]
    public void ProductDefinitionsPreserveStorableAndPropertySemantics()
    {
        Assert.True(typeof(ItemDefinition).IsAssignableFrom(typeof(ProductDefinition)));
        Assert.True(typeof(NamespacedStorableItemDefinition).IsAssignableFrom(typeof(ProductDefinition)));
        Assert.NotNull(typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.BasePurchasePrice)));
        Assert.NotNull(typeof(ProductDefinition).GetProperty(nameof(ProductDefinition.Properties)));
    }

#if MONOMELON
    public static IEnumerable<object[]> WrapperCases
    {
        get
        {
            yield return new object[] { typeof(ScheduleOne.Product.ProductDefinition), typeof(ProductDefinition) };
            yield return new object[] { typeof(ScheduleOne.Product.WeedDefinition), typeof(WeedDefinition) };
            yield return new object[] { typeof(ScheduleOne.Product.MethDefinition), typeof(MethDefinition) };
            yield return new object[] { typeof(ScheduleOne.Product.CocaineDefinition), typeof(CocaineDefinition) };
            yield return new object[] { typeof(ScheduleOne.Product.ShroomDefinition), typeof(ShroomDefinition) };
        }
    }

    [Theory]
    [MemberData(nameof(WrapperCases))]
    public void NativeAndExistingWrapperPathsReturnTheSameTypedWrapper(
        Type nativeDefinitionType,
        Type expectedWrapperType)
    {
        var nativeDefinition = (NativeProductDefinition)
            TestObjectFactory.CreateUninitialized(nativeDefinitionType);

        var wrappedFromNative = ProductDefinitionWrapper.Wrap(nativeDefinition);
        var wrappedFromExistingWrapper = ProductDefinitionWrapper.Wrap(new ProductDefinition(nativeDefinition));

        Assert.IsType(expectedWrapperType, wrappedFromNative);
        Assert.IsType(expectedWrapperType, wrappedFromExistingWrapper);
    }

    [Fact]
    public void ExistingGenericWrapperRemainsTheFallbackInstance()
    {
        var nativeDefinition = TestObjectFactory.CreateUninitialized<NativeProductDefinition>();
        var existingWrapper = new ProductDefinition(nativeDefinition);

        var wrapped = ProductDefinitionWrapper.Wrap(existingWrapper);

        Assert.Same(existingWrapper, wrapped);
    }
#endif
}
