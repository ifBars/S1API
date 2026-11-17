using S1API.Products;

namespace S1API.Tests.Products;

public sealed class ProductMixingOutputTests
{
    [Fact]
    public void LegacyInputKeepsScalarMembersAndExposesNoEffects()
    {
        ProductKind kind = CreateKind();

        var input = new ProductMixingOutput("example:output", "Named Mix", "example:source", kind, 20f);

        Assert.Equal("example:output", input.OutputId);
        Assert.Equal("Named Mix", input.MixName);
        Assert.Equal("example:source", input.SourceProductId);
        Assert.Same(kind, input.SourceKind);
        Assert.Equal(20f, input.SourcePrice);
        Assert.Empty(input.Properties);
        Assert.Throws<InvalidOperationException>(() => input.CalculateNativeValue(20f));
    }

    [Fact]
    public void PropertiesPreserveNativeOrderAndValues()
    {
        var calming = new ProductMixingEffect("calming", "Calming", 1, 0f, 1.1f);
        var toxic = new ProductMixingEffect("toxic", "Toxic", 3, -4f, 1f);

        ProductMixingOutput input = CreateInput(new[] { calming, toxic }, null);

        Assert.Equal(new[] { "calming", "toxic" }, input.Properties.Select(effect => effect.Id));
        Assert.Same(calming, input.Properties[0]);
        Assert.Equal("Toxic", input.Properties[1].Name);
        Assert.Equal(3, input.Properties[1].Tier);
        Assert.Equal(-4f, input.Properties[1].ValueChange);
        Assert.Equal(1.1f, input.Properties[0].ValueMultiplier);
    }

    [Fact]
    public void PropertiesAreAReadOnlySnapshot()
    {
        var source = new List<ProductMixingEffect>
        {
            new ProductMixingEffect("calming", "Calming", 1, 0f, 1.1f)
        };

        ProductMixingOutput input = CreateInput(source, null);
        source.Add(new ProductMixingEffect("toxic", "Toxic", 3, -4f, 1f));

        Assert.Single(input.Properties);
        var asList = Assert.IsAssignableFrom<IList<ProductMixingEffect>>(input.Properties);
        Assert.True(asList.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => asList.Add(source[1]));
    }

    [Fact]
    public void EffectSnapshotRejectsNullIdAndNormalizesNullName()
    {
        Assert.Throws<ArgumentNullException>(() => new ProductMixingEffect(null!, "Name", 1, 0f, 1f));
        Assert.Equal(string.Empty, new ProductMixingEffect("id", null, 1, 0f, 1f).Name);
    }

    [Fact]
    public void NullPropertiesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => CreateInput(null!, null));
    }

    [Fact]
    public void CalculateNativeValueForwardsTheBaseValueToTheNativeCalculation()
    {
        var received = new List<float>();
        ProductMixingOutput input = CreateInput(
            Array.Empty<ProductMixingEffect>(),
            baseValue =>
            {
                received.Add(baseValue);
                return baseValue * 2f;
            });

        Assert.Equal(70f, input.CalculateNativeValue(35f));
        Assert.Equal(new[] { 35f }, received);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void CalculateNativeValueRejectsNonFiniteBaseValues(float baseValue)
    {
        var called = false;
        ProductMixingOutput input = CreateInput(
            Array.Empty<ProductMixingEffect>(),
            value =>
            {
                called = true;
                return value;
            });

        Assert.Throws<ArgumentOutOfRangeException>(() => input.CalculateNativeValue(baseValue));
        Assert.False(called);
    }

    [Fact]
    public void FactoryCanPriceOutputFromNativeEffectValue()
    {
        ProductKind kind = CreateKind();
        ProductMixingProfile profile = new ProductMixingProfileBuilder(kind)
            .WithMixerMap(ProductMixingMap.Cocaine)
            .WithOutputFactory(input => new ProductMixingOutputDefinition(
                input.MixName,
                input.SourceKind,
                input.CalculateNativeValue(40f)))
            .Build();

        ProductMixingOutputDefinition output = profile.OutputFactory(new ProductMixingOutput(
            "example:output",
            "Named Mix",
            "example:source",
            kind,
            50f,
            new[] { new ProductMixingEffect("calming", "Calming", 1, 0f, 1.5f) },
            baseValue => baseValue * 1.5f));

        Assert.Equal(60f, output.Price);
    }

    [Fact]
    public void ExistingPublicSurfaceRemainsAvailable()
    {
        Type type = typeof(ProductMixingOutput);

        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
        Assert.Equal(typeof(string), type.GetProperty(nameof(ProductMixingOutput.OutputId))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(ProductMixingOutput.MixName))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(ProductMixingOutput.SourceProductId))!.PropertyType);
        Assert.Equal(typeof(ProductKind), type.GetProperty(nameof(ProductMixingOutput.SourceKind))!.PropertyType);
        Assert.Equal(typeof(float), type.GetProperty(nameof(ProductMixingOutput.SourcePrice))!.PropertyType);
        Assert.Empty(typeof(ProductMixingEffect).GetConstructors());
    }

    private static ProductMixingOutput CreateInput(
        IReadOnlyList<ProductMixingEffect> properties,
        Func<float, float>? nativeValueCalculator)
    {
        return new ProductMixingOutput(
            "example:output",
            "Named Mix",
            "example:source",
            CreateKind(),
            20f,
            properties,
            nativeValueCalculator);
    }

    private static ProductKind CreateKind()
    {
        return new ProductKindBuilder("mixingoutputtests:" + Guid.NewGuid().ToString("N"))
            .WithCompatibilityDrugType(DrugType.Cocaine)
            .Build();
    }
}
