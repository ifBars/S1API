#if IL2CPPMELON
using NativePackagingDefinition = Il2CppScheduleOne.Product.Packaging.PackagingDefinition;
using NativeProductDefinition = Il2CppScheduleOne.Product.ProductDefinition;
#elif MONOMELON
using NativePackagingDefinition = ScheduleOne.Product.Packaging.PackagingDefinition;
using NativeProductDefinition = ScheduleOne.Product.ProductDefinition;
#endif

using S1API.Internal.Products;
using S1API.Items;
using S1API.Products;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class CustomProductDefinitionRegistryTests : IDisposable
{
    private readonly FakeRuntimeAdapter _runtimeAdapter = new FakeRuntimeAdapter();

    public CustomProductDefinitionRegistryTests()
    {
        CustomProductDefinitionRegistry.ResetForTesting(_runtimeAdapter);
    }

    public void Dispose()
    {
        CustomProductDefinitionRegistry.RestoreRuntimeAdapterForTesting();
    }

    [Fact]
    public void RepeatedSameOwnerRegistrationIsCaseInsensitiveAndIdempotent()
    {
        string productId = CreateProductId();
        NativeProductDefinition firstDefinition = CreateDefinition();
        NativeProductDefinition ignoredDefinition = CreateDefinition();

        NativeProductDefinition first = CustomProductDefinitionRegistry.Register(
            "ExampleMod",
            productId,
            "First Product Name",
            125f,
            firstDefinition);
        NativeProductDefinition repeated = CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId.ToUpperInvariant(),
            "Ignored Product Name",
            900f,
            ignoredDefinition);

        Assert.Same(firstDefinition, first);
        Assert.Same(firstDefinition, repeated);
        Assert.Single(_runtimeAdapter.RegisteredDefinitions);
        Assert.Single(_runtimeAdapter.AllProducts);
        Assert.Single(_runtimeAdapter.ProductNames);
        Assert.Equal("First Product Name", _runtimeAdapter.ProductNames.Single());
        Assert.Equal(125f, _runtimeAdapter.ProductPrices[productId]);
    }

    [Fact]
    public void SaveDescriptorsAreStableScalarSnapshotsOrderedByProductId()
    {
        string secondId = "examplemod:zeta-save";
        string firstId = "examplemod:alpha-save";
        CustomProductDefinitionMetadata metadata = CreateMetadata(firstId);
        CustomProductDefinitionRegistry.Register(
            "examplemod", secondId, "Zeta", 80f, CreateDefinition(), metadata,
            new CustomProductSaveDescriptorData
            {
                ProductId = secondId,
                OwnerId = "examplemod",
                ProductName = "Zeta",
                Description = "scalar",
                InitialPrice = 80f,
                ProductKindId = "examplemod:kind",
                RepresentationTemplateId = "weed",
                ProviderId = "examplemod:provider",
                ProviderVersion = 1,
                ProviderData = "v1",
                HasGeneratedMixColor = true,
                GeneratedMixColorR = 0x12,
                GeneratedMixColorG = 0x34,
                GeneratedMixColorB = 0x56,
                GeneratedMixColorA = 0xFF
            });
        CustomProductDefinitionRegistry.Register(
            "examplemod", firstId, "Alpha", 50f, CreateDefinition(), metadata,
            new CustomProductSaveDescriptorData
            {
                ProductId = firstId,
                OwnerId = "examplemod",
                ProductName = "Alpha",
                Description = "scalar",
                InitialPrice = 50f,
                ProductKindId = "examplemod:kind",
                RepresentationTemplateId = "weed"
            });

        CustomProductSaveDescriptorData[] descriptors =
            CustomProductDefinitionRegistry.GetSaveDescriptors();

        Assert.Equal(new[] { firstId, secondId }, descriptors.Select(item => item.ProductId));
        Assert.Equal("examplemod:provider", descriptors[1].ProviderId);
        Assert.Equal("v1", descriptors[1].ProviderData);
        Assert.True(descriptors[1].HasGeneratedMixColor);
        Assert.Equal(0x12, descriptors[1].GeneratedMixColorR);
        Assert.Equal(0x34, descriptors[1].GeneratedMixColorG);
        Assert.Equal(0x56, descriptors[1].GeneratedMixColorB);
        Assert.Equal(0xFF, descriptors[1].GeneratedMixColorA);
        Assert.DoesNotContain(descriptors, item => item.GetType().GetFields()
            .Any(field => typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)));
    }

    [Fact]
    public void ManifestSnapshotUsesCommittedDescriptorsWithoutReapplyingDefinitions()
    {
        string productId = CreateProductId();
        CustomProductDefinitionMetadata metadata = CreateMetadata(productId);
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Manifest Product",
            80f,
            CreateDefinition(),
            metadata,
            new CustomProductSaveDescriptorData
            {
                ProductId = productId,
                OwnerId = "examplemod",
                ProductName = "Manifest Product",
                Description = "scalar metadata",
                InitialPrice = 80f,
                ProductKindId = metadata.ProductKind.Id,
                CompatibilityDrugType = (int)DrugType.MDMA,
                RepresentationTemplateId = "weed",
                ProviderId = "examplemod:provider",
                ProviderVersion = 2,
                ProviderData = "local-only-provider-data"
            });
        int applyCount = _runtimeAdapter.ApplyCount;

        CustomProductManifestData first =
            CustomProductDefinitionRegistry.CreateManifest();
        CustomProductManifestData second =
            CustomProductDefinitionRegistry.CreateManifest();

        CustomProductManifestEntryData entry = Assert.Single(first.Entries);
        Assert.Equal(productId, entry.ProductId);
        Assert.Equal("examplemod:provider", entry.ProviderId);
        Assert.Equal(2, entry.ProviderVersion);
        Assert.Equal(first.CompatibilityHash, second.CompatibilityHash);
        Assert.Equal(applyCount, _runtimeAdapter.ApplyCount);
        Assert.DoesNotContain(
            entry.GetType().GetFields(),
            field => field.Name == "ProviderData");
    }

    [Fact]
    public void FailedRegistrationDoesNotContaminateManifestSnapshot()
    {
        string productId = CreateProductId();
        CustomProductDefinitionMetadata metadata = CreateMetadata(productId);
        _runtimeAdapter.NextApplyException =
            new InvalidOperationException("Native registration failed.");

        Assert.Throws<InvalidOperationException>(
            () => CustomProductDefinitionRegistry.Register(
                "examplemod",
                productId,
                "Rejected Manifest Product",
                50f,
                CreateDefinition(),
                metadata,
                new CustomProductSaveDescriptorData
                {
                    ProductId = productId,
                    OwnerId = "examplemod",
                    ProductName = "Rejected Manifest Product",
                    ProductKindId = metadata.ProductKind.Id,
                    CompatibilityDrugType = (int)DrugType.MDMA,
                    RepresentationTemplateId = "weed"
                }));

        Assert.Empty(CustomProductDefinitionRegistry.CreateManifest().Entries);
        Assert.False(CustomProductDefinitionRegistry.IsRegistered(productId));
    }

    [Fact]
    public void ConflictingOwnerFailsWithActionableIdentity()
    {
        string productId = CreateProductId();
        CustomProductDefinitionRegistry.Register(
            "first-mod",
            productId,
            "Owned Product",
            50f,
            CreateDefinition());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => CustomProductDefinitionRegistry.Register(
                "second-mod",
                productId.ToUpperInvariant(),
                "Conflicting Product",
                75f,
                CreateDefinition()));

        Assert.Contains(productId, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("first-mod", exception.Message);
        Assert.Contains("second-mod", exception.Message);
        Assert.Contains("case-insensitive", exception.Message);
        Assert.Single(_runtimeAdapter.RegisteredDefinitions);
    }

    [Fact]
    public void FailedInitialApplyDoesNotRetainOwnership()
    {
        string productId = CreateProductId();
        NativeProductDefinition rejectedDefinition = CreateDefinition();
        NativeProductDefinition correctedDefinition = CreateDefinition();
        _runtimeAdapter.NextApplyException =
            new InvalidOperationException("Native registration failed.");

        Assert.Throws<InvalidOperationException>(
            () => CustomProductDefinitionRegistry.Register(
                "examplemod",
                productId,
                "Rejected Product",
                50f,
                rejectedDefinition));

        NativeProductDefinition registered = CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Corrected Product",
            75f,
            correctedDefinition);

        Assert.Same(correctedDefinition, registered);
        Assert.Same(
            correctedDefinition,
            _runtimeAdapter.RegisteredDefinitions[productId]);
        Assert.Equal(2, _runtimeAdapter.ApplyCount);
    }

    [Fact]
    public void FailedInitialApplyDestroysTheRejectedCreatedDefinition()
    {
        string productId = CreateProductId();
        NativeProductDefinition rejectedDefinition = CreateDefinition();
        CustomProductDefinitionMetadata metadata = CreateMetadata(productId);
        NativeProductDefinition? destroyedDefinition = null;
        _runtimeAdapter.NextApplyException =
            new InvalidOperationException("Native registration failed.");

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => CustomProductDefinitionBuilder.RegisterCreatedDefinition(
                    "examplemod",
                    productId,
                    "Rejected Product",
                    50f,
                    rejectedDefinition,
                    metadata,
                    definition => destroyedDefinition = definition));

        Assert.Equal("Native registration failed.", exception.Message);
        Assert.Same(rejectedDefinition, destroyedDefinition);
        Assert.False(
            CustomProductDefinitionRegistry.TryGetMetadata(
                productId,
                rejectedDefinition,
                out _));
    }

    [Fact]
    public void SameOwnerCollisionDestroysOnlyTheUnregisteredCandidate()
    {
        string productId = CreateProductId();
        NativeProductDefinition retainedDefinition = CreateDefinition();
        NativeProductDefinition rejectedDefinition = CreateDefinition();
        CustomProductDefinitionMetadata metadata = CreateMetadata(productId);
        NativeProductDefinition? destroyedDefinition = null;
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Retained Product",
            50f,
            retainedDefinition,
            metadata);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => CustomProductDefinitionBuilder.RegisterCreatedDefinition(
                    "examplemod",
                    productId.ToUpperInvariant(),
                    "Rejected Product",
                    75f,
                    rejectedDefinition,
                    metadata,
                    definition => destroyedDefinition = definition));

        Assert.Contains("already registered by this owner", exception.Message);
        Assert.Same(rejectedDefinition, destroyedDefinition);
        Assert.Same(
            retainedDefinition,
            _runtimeAdapter.RegisteredDefinitions[productId]);
        Assert.True(
            CustomProductDefinitionRegistry.TryGetMetadata(
                productId,
                retainedDefinition,
                out CustomProductDefinitionMetadata? retained));
        Assert.Same(metadata, retained);
    }

    [Fact]
    public void PreLoadRestoresDefinitionsBeforeLoadAndLoadCompletePreservesSavedPrice()
    {
        string productId = CreateProductId();
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Lifecycle Product",
            120f,
            CreateDefinition());

        _runtimeAdapter.ResetSceneState();
        CustomProductDefinitionRegistry.InvokePreLoadForTesting();

        Assert.True(_runtimeAdapter.RegisteredDefinitions.ContainsKey(productId));
        Assert.True(_runtimeAdapter.AllProducts.ContainsKey(productId));
        Assert.Contains("Lifecycle Product", _runtimeAdapter.ProductNames);
        Assert.Equal(120f, _runtimeAdapter.ProductPrices[productId]);

        _runtimeAdapter.ProductPrices[productId] = 275f;
        CustomProductDefinitionRegistry.InvokeLoadCompleteForTesting();
        CustomProductDefinitionRegistry.InvokeLoadCompleteForTesting();

        Assert.Single(_runtimeAdapter.RegisteredDefinitions);
        Assert.Single(_runtimeAdapter.AllProducts);
        Assert.Single(_runtimeAdapter.ProductNames);
        Assert.Single(_runtimeAdapter.ProductPrices);
        Assert.Equal(275f, _runtimeAdapter.ProductPrices[productId]);
        Assert.Equal(0, _runtimeAdapter.CreatedProductsCount);
    }

    [Theory]
    [InlineData(null, "product", "name", 1f)]
    [InlineData(" ", "product", "name", 1f)]
    [InlineData("owner", null, "name", 1f)]
    [InlineData("owner", " ", "name", 1f)]
    [InlineData("owner", "product", null, 1f)]
    [InlineData("owner", "product", " ", 1f)]
    [InlineData("owner", "product", "name", float.NaN)]
    [InlineData("owner", "product", "name", float.PositiveInfinity)]
    [InlineData("owner", "product", "name", float.NegativeInfinity)]
    public void InvalidRegistrationMetadataFailsBeforeRuntimeMutation(
        string? ownerId,
        string? productId,
        string? productName,
        float initialPrice)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => CustomProductDefinitionRegistry.Register(
                ownerId!,
                productId!,
                productName!,
                initialPrice,
                CreateDefinition()));

        Assert.Equal(0, _runtimeAdapter.ApplyCount);
    }

    [Fact]
    public void NullDefinitionFailsBeforeRuntimeMutation()
    {
        Assert.Throws<ArgumentNullException>(
            () => CustomProductDefinitionRegistry.Register(
                "owner",
                CreateProductId(),
                "Product",
                1f,
                null!));

        Assert.Equal(0, _runtimeAdapter.ApplyCount);
    }

    [Fact]
    public void RegistryRetainsDefinitionForProcessLifetime()
    {
        string productId = CreateProductId();
        WeakReference reference = RegisterWithoutRetainingDefinition(productId);

        _runtimeAdapter.ResetSceneState();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.True(reference.IsAlive);
        CustomProductDefinitionRegistry.InvokePreLoadForTesting();
        Assert.Same(reference.Target, _runtimeAdapter.RegisteredDefinitions[productId]);
    }

    [Fact]
    public void GenericMetadataSurvivesLifecycleRegistrationAndSelectsCustomWrapper()
    {
        string productId = CreateProductId();
        NativeProductDefinition definition = CreateDefinition(productId);
        var productKind =
            new ProductKindBuilder($"{productId}/kind")
                .WithCompatibilityDrugType(DrugType.MDMA)
                .Build();
        var metadata =
            new CustomProductDefinitionMetadata(
                productKind,
                Quality.Premium);

        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Metadata Product",
            20f,
            definition,
            metadata);

        Assert.True(
            CustomProductDefinitionRegistry.TryGetMetadata(
                productId,
                definition,
                out CustomProductDefinitionMetadata? retained));
        Assert.Same(metadata, retained);

#if MONOMELON
        CustomProductDefinition wrapped =
            Assert.IsType<CustomProductDefinition>(
                ProductDefinitionWrapper.Wrap(definition));
        var buildResult =
            new CustomProductDefinition(definition, metadata);
        Assert.NotSame(buildResult, wrapped);
        Assert.True((ItemDefinition)buildResult == wrapped);
        Assert.Same(productKind, wrapped.ProductKind);
        Assert.Equal(Quality.Premium, wrapped.DefaultQuality);
#endif
    }

    [Fact]
    public void MetadataLookupUsesStableIdAndVerifiesTheNativeDefinition()
    {
        string firstProductId = CreateProductId();
        string secondProductId = CreateProductId();
        NativeProductDefinition firstDefinition = CreateDefinition();
        NativeProductDefinition secondDefinition = CreateDefinition();
        CustomProductDefinitionMetadata firstMetadata =
            CreateMetadata(firstProductId);
        CustomProductDefinitionMetadata secondMetadata =
            CreateMetadata(secondProductId);
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            firstProductId,
            "First Product",
            10f,
            firstDefinition,
            firstMetadata);
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            secondProductId,
            "Second Product",
            20f,
            secondDefinition,
            secondMetadata);

        Assert.False(
            CustomProductDefinitionRegistry.TryGetMetadata(
                CreateProductId(),
                secondDefinition,
                out _));
#if MONOMELON
        Assert.False(
            CustomProductDefinitionRegistry.TryGetMetadata(
                firstProductId,
                secondDefinition,
                out _));
#endif
        Assert.True(
            CustomProductDefinitionRegistry.TryGetMetadata(
                secondProductId,
                secondDefinition,
                out CustomProductDefinitionMetadata? retained));
        Assert.Same(secondMetadata, retained);
    }

    [Fact]
    public void ValidPackagingReturnsOneImmutableMetadataSnapshot()
    {
        string productId = CreateProductId();
        NativeProductDefinition definition = CreateDefinition();
        var nativePackaging =
            TestObjectFactory.CreateUninitialized<NativePackagingDefinition>();
        var packaging = new PackagingDefinition(nativePackaging);
        CustomProductDefinitionMetadata metadata =
            CreateMetadata(productId, new[] { packaging });
        var firstWrapper =
            new CustomProductDefinition(definition, metadata);
        var secondWrapper =
            new CustomProductDefinition(definition, metadata);

        IReadOnlyList<PackagingDefinition> first = firstWrapper.ValidPackaging;
        IReadOnlyList<PackagingDefinition> repeated =
            firstWrapper.ValidPackaging;

        Assert.Same(first, repeated);
        Assert.Same(packaging, Assert.Single(first));
        Assert.Same(first, secondWrapper.ValidPackaging);
        Assert.Throws<NotSupportedException>(
            () => ((IList<PackagingDefinition>)first).Add(packaging));
    }

    [Fact]
    public void LegacyLifecycleRegistrationRetainsGenericWrapperFallback()
    {
        string productId = CreateProductId();
        NativeProductDefinition definition = CreateDefinition();
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Legacy Lifecycle Product",
            20f,
            definition);

        Assert.False(
            CustomProductDefinitionRegistry.TryGetMetadata(
                productId,
                definition,
                out CustomProductDefinitionMetadata? metadata));
        Assert.Null(metadata);
#if MONOMELON
        Assert.IsType<ProductDefinition>(
            ProductDefinitionWrapper.Wrap(definition));
#endif
    }

    [Theory]
    [InlineData("S1API.Internal.Products.CustomProductDefinitionRegistry")]
    [InlineData("S1API.Internal.Products.CustomProductDefinitionRuntimeAdapter")]
    public void LifecycleRegistrationTypesRemainOutsideThePublicApi(string typeName)
    {
        Type? type = typeof(CustomProductDefinitionRegistry).Assembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.False(type.IsPublic);
        Assert.False(type.IsNestedPublic);
    }

    private static WeakReference RegisterWithoutRetainingDefinition(string productId)
    {
        NativeProductDefinition definition = CreateDefinition();
        CustomProductDefinitionRegistry.Register(
            "examplemod",
            productId,
            "Retained Product",
            10f,
            definition);
        return new WeakReference(definition);
    }

    private static CustomProductDefinitionMetadata CreateMetadata(
        string productId,
        IReadOnlyList<PackagingDefinition>? validPackaging = null)
    {
        ProductKind productKind =
            new ProductKindBuilder($"{productId}/kind")
                .WithCompatibilityDrugType(DrugType.MDMA)
                .Build();
        return validPackaging == null
            ? new CustomProductDefinitionMetadata(
                productKind,
                Quality.Standard)
            : new CustomProductDefinitionMetadata(
                productKind,
                Quality.Standard,
                validPackaging);
    }

    private static NativeProductDefinition CreateDefinition(
        string? productId = null)
    {
        var definition = TestObjectFactory.CreateUninitialized<NativeProductDefinition>();
#if MONOMELON
        if (productId != null)
            definition.ID = productId;
#endif
        return definition;
    }

    private static string CreateProductId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }

    private sealed class FakeRuntimeAdapter :
        ICustomProductDefinitionRuntimeAdapter
    {
        internal Dictionary<string, NativeProductDefinition> RegisteredDefinitions { get; } =
            new Dictionary<string, NativeProductDefinition>(
                StringComparer.OrdinalIgnoreCase);

        internal Dictionary<string, NativeProductDefinition> AllProducts { get; } =
            new Dictionary<string, NativeProductDefinition>(
                StringComparer.OrdinalIgnoreCase);

        internal HashSet<string> ProductNames { get; } =
            new HashSet<string>(StringComparer.Ordinal);

        internal Dictionary<string, float> ProductPrices { get; } =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        internal int ApplyCount { get; private set; }

        internal int CreatedProductsCount =>
            0;

        internal Exception? NextApplyException { get; set; }

        public bool Apply(CustomProductDefinitionRegistration registration)
        {
            ApplyCount++;

            if (NextApplyException != null)
            {
                Exception exception = NextApplyException;
                NextApplyException = null;
                throw exception;
            }

            RegisteredDefinitions.TryAdd(
                registration.ProductId,
                registration.Definition);
            AllProducts.TryAdd(
                registration.ProductId,
                registration.Definition);
            ProductNames.Add(registration.ProductName);
            ProductPrices.TryAdd(
                registration.ProductId,
                registration.InitialPrice);
            return true;
        }

        internal void ResetSceneState()
        {
            RegisteredDefinitions.Clear();
            AllProducts.Clear();
            ProductNames.Clear();
            ProductPrices.Clear();
        }
    }
}
