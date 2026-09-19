#if IL2CPPMELON
using NativeProductDefinition = Il2CppScheduleOne.Product.ProductDefinition;
#elif MONOMELON
using NativeProductDefinition = ScheduleOne.Product.ProductDefinition;
#endif

using S1API.Internal.Products;
using S1API.Products;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class ProductPresentationProfileRegistryTests : IDisposable
{
    private readonly FakeProductRuntime _productRuntime = new();
    private readonly FakePresentationRuntime _presentationRuntime = new();

    public ProductPresentationProfileRegistryTests()
    {
        CustomProductDefinitionRegistry.ResetForTesting(
            _productRuntime,
            _presentationRuntime);
    }

    public void Dispose()
    {
        CustomProductDefinitionRegistry.RestoreRuntimeAdapterForTesting();
    }

    [Fact]
    public void ProductSpecificProfileTakesPrecedenceOverKindProfile()
    {
        string productId = CreateId();
        ProductKind kind = CreateKind();
        ProductPresentationProfile kindProfile = CreateProfile();
        ProductPresentationProfile productProfile = CreateProfile();
        ProductPresentationProfileRegistry.RegisterForProductKind(
            "examplemod",
            kind,
            kindProfile);
        ProductPresentationProfileRegistry.RegisterForProduct(
            "examplemod",
            productId,
            productProfile);

        RegisterProduct(productId, kind);

        ProductPresentationProfileRegistration applied =
            Assert.Single(_presentationRuntime.AppliedProfiles);
        Assert.Same(productProfile, applied.Profile);
        Assert.Equal(productId, applied.Key, ignoreCase: true);
    }

    [Fact]
    public void KindProfileIsUsedWhenProductProfileIsAbsent()
    {
        string productId = CreateId();
        ProductKind kind = CreateKind();
        ProductPresentationProfile profile = CreateProfile();
        ProductPresentationProfileRegistry.RegisterForProductKind(
            "examplemod",
            kind,
            profile);

        RegisterProduct(productId, kind);

        Assert.Same(
            profile,
            Assert.Single(_presentationRuntime.AppliedProfiles).Profile);
    }

    [Fact]
    public void LateRegistrationAppliesToAnExistingProduct()
    {
        string productId = CreateId();
        ProductKind kind = CreateKind();
        RegisterProduct(productId, kind);
        _presentationRuntime.AppliedProfiles.Clear();

        ProductPresentationProfile profile = CreateProfile();
        ProductPresentationProfileRegistry.RegisterForProduct(
            "examplemod",
            productId,
            profile);

        Assert.Same(
            profile,
            Assert.Single(_presentationRuntime.AppliedProfiles).Profile);
    }

    [Fact]
    public void LifecycleRestorationReappliesTheResolvedProfile()
    {
        string productId = CreateId();
        ProductKind kind = CreateKind();
        ProductPresentationProfile profile = CreateProfile();
        ProductPresentationProfileRegistry.RegisterForProduct(
            "examplemod",
            productId,
            profile);
        RegisterProduct(productId, kind);
        _presentationRuntime.AppliedProfiles.Clear();

        CustomProductDefinitionRegistry.InvokePreLoadForTesting();
        CustomProductDefinitionRegistry.InvokeLoadCompleteForTesting();

        Assert.Equal(2, _presentationRuntime.AppliedProfiles.Count);
        Assert.All(
            _presentationRuntime.AppliedProfiles,
            registration => Assert.Same(profile, registration.Profile));
    }

    [Fact]
    public void PresentationFailurePrecedesNativeMutationAndReleasesProductId()
    {
        string productId = CreateId();
        ProductKind kind = CreateKind();
        ProductPresentationProfile profile = CreateProfile();
        ProductPresentationProfileRegistry.RegisterForProduct(
            "examplemod",
            productId,
            profile);
        _presentationRuntime.NextApplyException =
            new InvalidOperationException("Required presentation is unavailable.");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => RegisterProduct(productId, kind));

        Assert.Equal("Required presentation is unavailable.", exception.Message);
        Assert.Equal(0, _productRuntime.ApplyCount);

        RegisterProduct(productId, kind);

        Assert.Equal(1, _productRuntime.ApplyCount);
        Assert.Same(
            profile,
            Assert.Single(_presentationRuntime.AppliedProfiles).Profile);
    }

    [Fact]
    public void LegacyRegistrationWithoutGenericMetadataRemainsUnprofiled()
    {
        string productId = CreateId();
        ProductPresentationProfileRegistry.RegisterForProduct(
            "examplemod",
            productId,
            CreateProfile());

        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Legacy Product",
            10f,
            CreateDefinition());

        Assert.Single(_presentationRuntime.NullProfileProducts);
        Assert.Empty(_presentationRuntime.AppliedProfiles);
    }

    [Fact]
    public void SameOwnerSameProfileRegistrationIsCaseInsensitiveAndIdempotent()
    {
        string productId = CreateId();
        ProductPresentationProfile profile = CreateProfile();

        ProductPresentationProfile first =
            ProductPresentationProfileRegistry.RegisterForProduct(
                "ExampleMod",
                productId,
                profile);
        ProductPresentationProfile repeated =
            ProductPresentationProfileRegistry.RegisterForProduct(
                "examplemod",
                productId.ToUpperInvariant(),
                profile);

        Assert.Same(profile, first);
        Assert.Same(first, repeated);
    }

    [Fact]
    public void OverlongManifestIdentityUsesABoundedCaseInsensitiveDigest()
    {
        string ownerId = "owner-" + new string('o', 150);
        string productId = "example:" + new string('p', 110);
        ProductPresentationProfile profile = CreateProfile();
        ProductPresentationProfileRegistry.RegisterForProduct(
            ownerId,
            productId,
            profile);
        Assert.True(ProductPresentationProfileRegistry.TryGetManifestIdentity(
            productId,
            string.Empty,
            out string lowerIdentity));

        ProductPresentationProfileRegistry.ResetForTesting();
        ProductPresentationProfileRegistry.RegisterForProduct(
            ownerId.ToUpperInvariant(),
            productId.ToUpperInvariant(),
            profile);
        Assert.True(ProductPresentationProfileRegistry.TryGetManifestIdentity(
            productId,
            string.Empty,
            out string upperIdentity));

        Assert.StartsWith("sha256:", lowerIdentity, StringComparison.Ordinal);
        Assert.Equal(lowerIdentity, upperIdentity);
        Assert.True(
            lowerIdentity.Length <=
            CustomProductManifestData.MaximumIdentifierLength);
    }

    [Fact]
    public void ConflictingOwnerFailsWithActionableIdentity()
    {
        string productId = CreateId();
        ProductPresentationProfileRegistry.RegisterForProduct(
            "first-mod",
            productId,
            CreateProfile());

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    ProductPresentationProfileRegistry.RegisterForProduct(
                        "second-mod",
                        productId.ToUpperInvariant(),
                        CreateProfile()));

        Assert.Contains(productId, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("first-mod", exception.Message);
        Assert.Contains("second-mod", exception.Message);
        Assert.Contains("case-insensitive", exception.Message);
    }

    [Fact]
    public void SameOwnerCannotSilentlyReplaceARegisteredProfile()
    {
        string productId = CreateId();
        ProductPresentationProfileRegistry.RegisterForProduct(
            "examplemod",
            productId,
            CreateProfile());

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () =>
                    ProductPresentationProfileRegistry.RegisterForProduct(
                        "examplemod",
                        productId,
                        CreateProfile()));

        Assert.Contains("another profile", exception.Message);
    }

    private void RegisterProduct(string productId, ProductKind kind)
    {
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Profile Product",
            10f,
            CreateDefinition(),
            new CustomProductDefinitionMetadata(kind, Quality.Standard));
    }

    private static ProductPresentationProfile CreateProfile()
    {
        return new ProductPresentationProfileBuilder()
            .WithLooseVisual(() => null)
            .Build();
    }

    private static ProductKind CreateKind()
    {
        return new ProductKindBuilder(CreateId())
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();
    }

    private static NativeProductDefinition CreateDefinition()
    {
        return TestObjectFactory.CreateUninitialized<NativeProductDefinition>();
    }

    private static string CreateId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }

    private sealed class FakeProductRuntime :
        ICustomProductDefinitionRuntimeAdapter
    {
        internal int ApplyCount { get; private set; }

        public bool Apply(CustomProductDefinitionRegistration registration)
        {
            ApplyCount++;
            return true;
        }
    }

    private sealed class FakePresentationRuntime :
        ICustomProductPresentationRuntime
    {
        internal List<ProductPresentationProfileRegistration> AppliedProfiles { get; } =
            new();

        internal List<CustomProductDefinitionRegistration> NullProfileProducts { get; } =
            new();

        internal Exception? NextApplyException { get; set; }

        public void Apply(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfileRegistration? profile)
        {
            if (NextApplyException != null)
            {
                Exception exception = NextApplyException;
                NextApplyException = null;
                throw exception;
            }

            if (profile == null)
                NullProfileProducts.Add(product);
            else
                AppliedProfiles.Add(profile);
        }
    }
}
