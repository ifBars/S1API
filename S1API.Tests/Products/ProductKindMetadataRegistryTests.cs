using S1API.Internal.Products;
using S1API.Products;
using UnityEngine;

namespace S1API.Tests.Products;

[Collection(CustomProductRegistryCollection.Name)]
public sealed class ProductKindMetadataRegistryTests : IDisposable
{
    private readonly FakeRuntimeAdapter _runtimeAdapter = new();

    public ProductKindMetadataRegistryTests()
    {
        ProductKindMetadataRegistrationRegistry.ResetForTesting(_runtimeAdapter);
#if IL2CPPMELON
        ProductKindIconLifetime.SetUnityNullEvaluatorForTesting(_ => false);
#endif
    }

    public void Dispose()
    {
#if IL2CPPMELON
        ProductKindIconLifetime.SetUnityNullEvaluatorForTesting(null);
#endif
        ProductKindMetadataRegistrationRegistry.RestoreRuntimeAdapterForTesting();
    }

    [Fact]
    public void BuilderRequiresDisplayNameAndColor()
    {
        ProductKind kind = CreateKind();

        Assert.Throws<InvalidOperationException>(
            () => new ProductKindMetadataBuilder(kind).Build());
        Assert.Throws<InvalidOperationException>(
            () => new ProductKindMetadataBuilder(kind)
                .WithDisplayName("MDMA")
                .Build());
        Assert.Throws<InvalidOperationException>(
            () => new ProductKindMetadataBuilder(kind)
                .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
                .Build());
    }

    [Fact]
    public void HiddenMetadataUsesCompatibleOptInDefaults()
    {
        ProductKind kind = CreateKind();
        ProductKindMetadata metadata = new ProductKindMetadataBuilder(kind)
            .WithDisplayName("Dormant")
            .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
            .Build();

        Assert.False(metadata.IsVisibleInProductManager);
        Assert.Null(metadata.Icon);
        Assert.Equal(0, metadata.SortOrder);
        Assert.Empty(metadata.SearchAliases);
    }

    [Fact]
    public void VisibleMetadataRequiresCompatibilityTypeAndIcon()
    {
        ProductKind withoutMapping =
            new ProductKindBuilder(CreateId()).Build();
        ProductKind withMapping = CreateKind();

        InvalidOperationException missingMapping =
            Assert.Throws<InvalidOperationException>(
                () => new ProductKindMetadataBuilder(withoutMapping)
                    .WithDisplayName("MDMA")
                    .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
                    .WithIcon(CreateSprite())
                    .WithProductManagerVisibility()
                    .Build());
        InvalidOperationException missingIcon =
            Assert.Throws<InvalidOperationException>(
                () => new ProductKindMetadataBuilder(withMapping)
                    .WithDisplayName("MDMA")
                    .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
                    .WithProductManagerVisibility()
                    .Build());

        Assert.Contains("compatibility drug type", missingMapping.Message);
        Assert.Contains("icon", missingIcon.Message);
    }

    [Fact]
    public void InputValidationRejectsNullEmptyAndNonFiniteValues()
    {
        ProductKind kind = CreateKind();
        var builder = new ProductKindMetadataBuilder(kind);

        Assert.Throws<ArgumentNullException>(() => builder.WithDisplayName(null!));
        Assert.Throws<ArgumentException>(() => builder.WithDisplayName("  "));
        Assert.Throws<ArgumentNullException>(() => builder.WithIcon(null!));
        Assert.Throws<ArgumentNullException>(
            () => builder.WithSearchAliases(null!));
        Assert.Throws<ArgumentNullException>(
            () => builder.WithSearchAliases("valid", null!));
        Assert.Throws<ArgumentException>(
            () => builder.WithSearchAliases("valid", "  "));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithColor(CreateColor(float.NaN, 0f, 0f)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder.WithColor(
                CreateColor(0f, float.PositiveInfinity, 0f)));
    }

    [Fact]
    public void EquivalentRegistrationIsCaseInsensitiveAndIdempotent()
    {
        string id = CreateId();
        ProductKind firstKind = new ProductKindBuilder(id)
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();
        ProductKind secondKind = new ProductKindBuilder(id.ToUpperInvariant())
            .WithCompatibilityDrugType(DrugType.MDMA)
            .Build();
        Sprite icon = CreateSprite();

        ProductKindMetadata first = CreateVisibleMetadata(
            firstKind,
            icon,
            "MDMA",
            CreateColor(0.8f, 0.2f, 0.7f));
        ProductKindMetadata repeated = CreateVisibleMetadata(
            secondKind,
            icon,
            "MDMA",
            CreateColor(0.8f, 0.2f, 0.7f));

        Assert.Same(first, repeated);
        Assert.Same(first, ProductKindMetadataRegistry.Get(id.ToUpperInvariant()));
        Assert.True(
            ProductKindMetadataRegistry.TryGet(secondKind, out ProductKindMetadata? found));
        Assert.Same(first, found);
        Assert.Equal(2, _runtimeAdapter.Applications.Count);
    }

    [Fact]
    public void ConflictingRegistrationDoesNotReplaceExistingMetadata()
    {
        ProductKind kind = CreateKind();
        Sprite icon = CreateSprite();
        ProductKindMetadata first = CreateVisibleMetadata(
            kind,
            icon,
            "MDMA",
            CreateColor(0.8f, 0.2f, 0.7f));

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => new ProductKindMetadataBuilder(kind)
                    .WithDisplayName("Changed")
                    .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
                    .WithIcon(icon)
                    .WithProductManagerVisibility()
                    .Build());

        Assert.Contains(kind.Id, exception.Message);
        Assert.Contains("case-insensitive", exception.Message);
        Assert.Same(first, ProductKindMetadataRegistry.Get(kind));
    }

    [Fact]
    public void DormantNativeTypeConflictsAreRejectedBeforeRuntimeOrderMatters()
    {
        ProductKind firstKind = CreateKind(DrugType.Heroin);
        ProductKind secondKind = CreateKind(DrugType.Heroin);
        CreateVisibleMetadata(
            firstKind,
            CreateSprite(),
            "Heroin",
            CreateColor(0.5f, 0.5f, 0.5f));

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => CreateVisibleMetadata(
                    secondKind,
                    CreateSprite(),
                    "Opioid",
                    CreateColor(1f, 0f, 0f)));

        Assert.Contains(nameof(DrugType.Heroin), exception.Message);
        Assert.Contains(firstKind.Id, exception.Message);
        Assert.Contains(secondKind.Id, exception.Message);
    }

    [Fact]
    public void ExistingVanillaCompatibilityTypeCanBackDistinctLogicalSections()
    {
        ProductKind firstKind = CreateKind(DrugType.Marijuana);
        ProductKind secondKind = CreateKind(DrugType.Marijuana);

        ProductKindMetadata first = CreateVisibleMetadata(
            firstKind,
            CreateSprite(),
            "Flower",
            CreateColor(0f, 1f, 0f));
        ProductKindMetadata second = CreateVisibleMetadata(
            secondKind,
            CreateSprite(),
            "Concentrates",
            CreateColor(1f, 1f, 0f));

        Assert.Same(first, ProductKindMetadataRegistry.Get(firstKind));
        Assert.Same(second, ProductKindMetadataRegistry.Get(secondKind));
    }

    [Fact]
    public void AliasesAreNormalizedImmutableAndSearchable()
    {
        ProductKind kind = CreateKind();
        ProductKindMetadata metadata = new ProductKindMetadataBuilder(kind)
            .WithDisplayName("Ecstasy")
            .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
            .WithSearchAliases(" MDMA ", "Molly", "molly")
            .Build();

        Assert.Equal(new[] { "MDMA", "Molly" }, metadata.SearchAliases);
        Assert.True(metadata.MatchesSearch("ecst"));
        Assert.True(metadata.MatchesSearch("mdma"));
        Assert.True(metadata.MatchesSearch(kind.Id));
        Assert.True(metadata.MatchesSearch("  "));
        Assert.False(metadata.MatchesSearch("cannabis"));
        Assert.Throws<ArgumentNullException>(() => metadata.MatchesSearch(null!));

        ICollection<string> aliases =
            Assert.IsAssignableFrom<ICollection<string>>(metadata.SearchAliases);
        Assert.True(aliases.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => aliases.Add("tablet"));
    }

    [Fact]
    public void AliasOrderIsPartOfRegistrationEquivalence()
    {
        ProductKind kind = CreateKind();
        ProductKindMetadata first = new ProductKindMetadataBuilder(kind)
            .WithDisplayName("Ecstasy")
            .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
            .WithSearchAliases("ecstasy", "molly")
            .Build();

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => new ProductKindMetadataBuilder(kind)
                    .WithDisplayName("Ecstasy")
                    .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
                    .WithSearchAliases("molly", "ecstasy")
                    .Build());

        Assert.Contains(kind.Id, exception.Message);
        Assert.Equal(
            new[] { "ecstasy", "molly" },
            first.SearchAliases);
        Assert.Same(first, ProductKindMetadataRegistry.Get(kind));
    }

    [Fact]
    public void NativeMetadataFallbackSetMatchesVerifiedDormantTypes()
    {
        DrugType[] fallbackTypes = Enum.GetValues<DrugType>()
            .Where(ProductKindNativeMetadataTypes.RequiresFallback)
            .ToArray();

        Assert.Equal(
            new[] { DrugType.MDMA, DrugType.Heroin },
            fallbackTypes);
        Assert.False(
            ProductKindNativeMetadataTypes.RequiresFallback(
                DrugType.Marijuana));
    }

    [Fact]
    public void IconValidationUsesUnityLifetimeSemantics()
    {
        ProductKind kind = CreateKind();
        Sprite liveIcon = CreateSprite();

        Assert.False(ProductKindIconLifetime.IsNullOrDestroyed(liveIcon));
        Assert.True(ProductKindIconLifetime.IsNullOrDestroyed(null));

#if MONOMELON
        Sprite destroyedIcon = TestObjectFactory.CreateUninitialized<Sprite>();
        Assert.True(ProductKindIconLifetime.IsNullOrDestroyed(destroyedIcon));
        Assert.Throws<ArgumentNullException>(
            () => new ProductKindMetadataBuilder(kind)
                .WithIcon(destroyedIcon));

        var builder = new ProductKindMetadataBuilder(kind)
            .WithDisplayName("MDMA")
            .WithColor(CreateColor(0.8f, 0.2f, 0.7f))
            .WithIcon(liveIcon)
            .WithProductManagerVisibility();
        SetCachedPointer(liveIcon, IntPtr.Zero);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
#else
        ProductKindIconLifetime.SetUnityNullEvaluatorForTesting(_ => true);
        Assert.True(ProductKindIconLifetime.IsNullOrDestroyed(liveIcon));
        ProductKindIconLifetime.SetUnityNullEvaluatorForTesting(_ => false);
#endif
    }

    [Fact]
    public void RegistrySnapshotsAreOrderedAndIsolated()
    {
        ProductKindMetadata later = new ProductKindMetadataBuilder(CreateKind())
            .WithDisplayName("Zulu")
            .WithColor(CreateColor(1f, 0f, 0f))
            .WithSortOrder(20)
            .Build();
        IReadOnlyCollection<ProductKindMetadata> before =
            ProductKindMetadataRegistry.All;
        ProductKindMetadata earlier = new ProductKindMetadataBuilder(CreateKind())
            .WithDisplayName("Alpha")
            .WithColor(CreateColor(0f, 0f, 1f))
            .WithSortOrder(-10)
            .Build();

        Assert.DoesNotContain(before, metadata => metadata == earlier);
        ProductKindMetadata[] relevant = ProductKindMetadataRegistry.All
            .Where(metadata => metadata == later || metadata == earlier)
            .ToArray();
        Assert.Equal(new[] { earlier, later }, relevant);

        ICollection<ProductKindMetadata> snapshot =
            Assert.IsAssignableFrom<ICollection<ProductKindMetadata>>(
                ProductKindMetadataRegistry.All);
        Assert.True(snapshot.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => snapshot.Add(earlier));
    }

    [Fact]
    public void LifecycleReappliesOneDeduplicatedSnapshot()
    {
        ProductKindMetadata first = new ProductKindMetadataBuilder(CreateKind())
            .WithDisplayName("Zulu")
            .WithColor(CreateColor(1f, 0f, 0f))
            .WithSortOrder(20)
            .Build();
        ProductKindMetadata second = new ProductKindMetadataBuilder(CreateKind())
            .WithDisplayName("Alpha")
            .WithColor(CreateColor(0f, 0f, 1f))
            .WithSortOrder(-10)
            .Build();

        ProductKindMetadataRegistrationRegistry.InvokePreLoadForTesting();
        ProductKindMetadataRegistrationRegistry.InvokeLoadCompleteForTesting();

        Assert.Equal(4, _runtimeAdapter.Applications.Count);
        Assert.Equal(new[] { first }, _runtimeAdapter.Applications[0]);
        Assert.All(
            _runtimeAdapter.Applications.Skip(1),
            application => Assert.Equal(
                new[] { second, first },
                application));
        Assert.All(
            _runtimeAdapter.Applications.Skip(1),
            application => Assert.Equal(
                application.Count,
                application.Select(item => item.ProductKind.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count()));
    }

    [Fact]
    public void RuntimeFailureDoesNotLoseProcessLifetimeRegistration()
    {
        _runtimeAdapter.Exception = new InvalidOperationException("runtime unavailable");
        ProductKind kind = CreateKind();

        ProductKindMetadata registered = new ProductKindMetadataBuilder(kind)
            .WithDisplayName("Deferred")
            .WithColor(CreateColor(0f, 1f, 1f))
            .Build();

        Assert.Same(registered, ProductKindMetadataRegistry.Get(kind));
        Assert.Single(ProductKindMetadataRegistry.All);
    }

    private static ProductKindMetadata CreateVisibleMetadata(
        ProductKind kind,
        Sprite icon,
        string displayName,
        Color color)
    {
        return new ProductKindMetadataBuilder(kind)
            .WithDisplayName(displayName)
            .WithColor(color)
            .WithIcon(icon)
            .WithSearchAliases(displayName)
            .WithProductManagerVisibility()
            .Build();
    }

    private static ProductKind CreateKind(DrugType drugType = DrugType.Marijuana)
    {
        return new ProductKindBuilder(CreateId())
            .WithCompatibilityDrugType(drugType)
            .Build();
    }

    private static Sprite CreateSprite()
    {
        var sprite = TestObjectFactory.CreateUninitialized<Sprite>();
#if MONOMELON
        SetCachedPointer(sprite, new IntPtr(1));
#endif
        return sprite;
    }

#if MONOMELON
    private static void SetCachedPointer(Sprite sprite, IntPtr pointer)
    {
        typeof(UnityEngine.Object)
            .GetField(
                "m_CachedPtr",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(sprite, pointer);
    }
#endif

    private static Color CreateColor(float red, float green, float blue)
    {
        Color color = default;
        color.r = red;
        color.g = green;
        color.b = blue;
        color.a = 1f;
        return color;
    }

    private static string CreateId()
    {
        return $"s1api-tests:{Guid.NewGuid():N}";
    }

    private sealed class FakeRuntimeAdapter :
        IProductKindMetadataRuntimeAdapter
    {
        internal List<IReadOnlyList<ProductKindMetadata>> Applications { get; } =
            new();

        internal Exception? Exception { get; set; }

        public void Apply(IReadOnlyList<ProductKindMetadata> metadata)
        {
            Applications.Add(metadata.ToArray());
            if (Exception != null)
                throw Exception;
        }
    }
}
