using System;
using S1API.Internal.Products;
using S1API.Products;
using Xunit;

namespace S1API.Tests.Products
{
    public sealed class ProductMixingProfileTests
    {
        [Fact]
        public void ProfileRegistrationRetainsKindAndInvokesDeterministicFactory()
        {
            ProductKind kind = CreateKind();
            Func<ProductMixingOutput, ProductMixingOutputDefinition> factory = input =>
                new ProductMixingOutputDefinition(input.MixName + " Output", input.SourceKind, input.SourcePrice + 10f);

            ProductMixingProfile profile = new ProductMixingProfileBuilder(kind)
                .WithMixerMap(ProductMixingMap.Marijuana)
                .WithOutputFactory(factory)
                .Build();

            Assert.Same(profile, ProductMixingProfiles.Get(kind));
            Assert.Equal(ProductMixingMap.Marijuana, profile.MixerMap);
            Assert.False(profile.UsePropertyColorMixing);
            ProductMixingOutputDefinition output = profile.OutputFactory(
                new ProductMixingOutput("example:output", "Named Mix", "example:source", kind, 20f));
            Assert.Equal("Named Mix Output", output.Name);
            Assert.Same(kind, output.ProductKind);
            Assert.Equal(30f, output.Price);
        }

        [Fact]
        public void PropertyColorMixingIsExplicitlyOptIn()
        {
            ProductKind kind = CreateKind();

            ProductMixingProfile profile = new ProductMixingProfileBuilder(kind)
                .WithMixerMap(ProductMixingMap.Cocaine)
                .WithPropertyColorMixing()
                .WithOutputFactory(input => new ProductMixingOutputDefinition(
                    input.MixName,
                    input.SourceKind,
                    input.SourcePrice))
                .Build();

            Assert.True(profile.UsePropertyColorMixing);
        }

        [Fact]
        public void PropertyColorMixingParticipatesInMultiplayerCompatibility()
        {
            ProductKind kind = CreateKind();
            ProductMixingProfile baseline = new ProductMixingProfileBuilder(kind)
                .WithMixerMap(ProductMixingMap.Cocaine)
                .WithOutputFactoryCompatibility("mixingtests:factory", 1)
                .WithOutputFactory(input => new ProductMixingOutputDefinition(
                    input.MixName,
                    input.SourceKind,
                    input.SourcePrice))
                .Build();
            ProductKind coloredKind = CreateKind();
            ProductMixingProfile colored = new ProductMixingProfileBuilder(coloredKind)
                .WithMixerMap(ProductMixingMap.Cocaine)
                .WithPropertyColorMixing()
                .WithOutputFactoryCompatibility("mixingtests:factory", 1)
                .WithOutputFactory(input => new ProductMixingOutputDefinition(
                    input.MixName,
                    input.SourceKind,
                    input.SourcePrice))
                .Build();

            var baselineEntry = CustomProductMixingProfileManifestEntryData.Create(baseline);
            var coloredEntry = CustomProductMixingProfileManifestEntryData.Create(colored);
            coloredEntry.ProductKindId = baselineEntry.ProductKindId;

            Assert.Equal(
                "property-color mixing strategy differs",
                baselineEntry.DescribeMismatch(coloredEntry));
        }

        [Fact]
        public void MissingOutputFactoryFailsBeforeRegistration()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => new ProductMixingProfileBuilder(CreateKind()).Build());

            Assert.Contains("WithOutputFactory", exception.Message);
        }

        [Fact]
        public void OutputDefinitionRejectsInvalidPriceAndMissingKind()
        {
            ProductKind kind = CreateKind();

            Assert.Throws<ArgumentNullException>(
                () => new ProductMixingOutputDefinition("Output", null!, 5f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ProductMixingOutputDefinition("Output", kind, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ProductMixingOutputDefinition("Output", kind, 1000f));
        }

        [Fact]
        public void CustomLogicalKindWithoutNativeEnumCanSelectAnExplicitMixerStrategy()
        {
            ProductKind futureKind = new ProductKindBuilder(
                    "consumer-shape:" + Guid.NewGuid().ToString("N"))
                .Build();

            ProductMixingProfile profile = new ProductMixingProfileBuilder(futureKind)
                .WithMixerMap(ProductMixingMap.Cocaine)
                .WithOutputFactoryCompatibility("consumer-shape:mix-output", 3)
                .WithOutputFactory(input => new ProductMixingOutputDefinition(
                    input.MixName, input.SourceKind, input.SourcePrice))
                .Build();

            Assert.Same(futureKind, profile.ProductKind);
            Assert.Equal(ProductMixingMap.Cocaine, profile.MixerMap);
            Assert.Equal("consumer-shape:mix-output", profile.OutputFactoryIdentity);
            Assert.Equal(3, profile.OutputFactoryVersion);
        }

        private static ProductKind CreateKind()
        {
            return new ProductKindBuilder("mixingtests:" + Guid.NewGuid().ToString("N"))
                .WithCompatibilityDrugType(DrugType.Marijuana)
                .Build();
        }
    }
}
