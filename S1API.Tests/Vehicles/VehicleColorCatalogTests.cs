using S1API.Vehicles;
using UnityEngine;

namespace S1API.Tests.Vehicles;

[Collection(VehicleColorCatalogCollection.Name)]
public sealed class VehicleColorCatalogTests : IDisposable
{
    private readonly FakeVehicleColorMetadataProvider _provider = new();

    public VehicleColorCatalogTests()
    {
        VehicleColorCatalog.ResetForTesting(_provider);
    }

    public void Dispose()
    {
        VehicleColorCatalog.RestoreProviderForTesting();
    }

    [Fact]
    public void LookupsExposeS1ApiOwnedMetadata()
    {
        Color materialColor = CreateColor(0.2f, 0.4f, 0.6f);
#if IL2CPPMELON
        Color32 uiColor = default;
#else
        Color32 uiColor = new(12, 34, 56, 255);
#endif
        var metadata = new VehicleColorMetadata(
            VehicleColor.DarkBlue,
            "Dark Blue",
            materialColor,
            uiColor);
        _provider.Colors.Add(metadata);

        Assert.Same(metadata, VehicleColorCatalog.GetColor(VehicleColor.DarkBlue));
        Assert.True(
            VehicleColorCatalog.TryGetColor(
                VehicleColor.DarkBlue,
                out VehicleColorMetadata? resolved));
        Assert.Same(metadata, resolved);
        Assert.Equal("Dark Blue", resolved!.DisplayName);
        AssertColor(materialColor, resolved.MaterialColor);
#if !IL2CPPMELON
        Assert.Equal(uiColor, resolved.UIColor);
#endif
    }

    [Fact]
    public void ColorsReturnsOrderedReadOnlySnapshots()
    {
        var red = new VehicleColorMetadata(
            VehicleColor.Red,
            "Red",
            default,
            default);
        var black = new VehicleColorMetadata(
            VehicleColor.Black,
            "Black",
            default,
            default);
        _provider.Colors.Add(red);
        _provider.Colors.Add(black);

        IReadOnlyList<VehicleColorMetadata> snapshot = VehicleColorCatalog.Colors;
        _provider.Colors.Remove(red);

        Assert.Equal(new[] { red, black }, snapshot);
        Assert.Equal(new[] { black }, VehicleColorCatalog.Colors);

        IList<VehicleColorMetadata> mutableView =
            Assert.IsAssignableFrom<IList<VehicleColorMetadata>>(snapshot);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(red));
    }

    [Fact]
    public void InvalidValuesDoNotReachNativeProvider()
    {
        Assert.False(
            VehicleColorCatalog.TryGetColor(
                (VehicleColor)(-1),
                out VehicleColorMetadata? negativeMetadata));
        Assert.False(
            VehicleColorCatalog.TryGetColor(
                (VehicleColor)999,
                out VehicleColorMetadata? largeMetadata));

        Assert.Null(negativeMetadata);
        Assert.Null(largeMetadata);
        Assert.Equal(0, _provider.ColorLookupCount);
    }

    [Fact]
    public void UnavailableRuntimeReturnsEmptyAndNullResults()
    {
        Assert.Empty(VehicleColorCatalog.Colors);
        Assert.Null(VehicleColorCatalog.GetColor(VehicleColor.Black));
    }

    [Fact]
    public void MissingNativeColorsReturnFalse()
    {
        _provider.Colors.Add(new VehicleColorMetadata(
            VehicleColor.Black,
            "Black",
            default,
            default));

        Assert.False(
            VehicleColorCatalog.TryGetColor(
                VehicleColor.Custom,
                out VehicleColorMetadata? customMetadata));
        Assert.Null(customMetadata);
    }

    [Fact]
    public void MetadataPropertiesAreReadOnly()
    {
        Assert.All(
            typeof(VehicleColorMetadata).GetProperties(),
            property => Assert.False(property.CanWrite));
    }

    private static Color CreateColor(float red, float green, float blue)
    {
        Color color = default;
        color.r = red;
        color.g = green;
        color.b = blue;
        color.a = 1f;
        return color;
    }

    private static void AssertColor(Color expected, Color actual)
    {
        Assert.Equal(expected.r, actual.r);
        Assert.Equal(expected.g, actual.g);
        Assert.Equal(expected.b, actual.b);
        Assert.Equal(expected.a, actual.a);
    }

    private sealed class FakeVehicleColorMetadataProvider :
        IVehicleColorMetadataProvider
    {
        internal List<VehicleColorMetadata> Colors { get; } = new();

        internal int ColorLookupCount { get; private set; }

        public bool TryGetColors(out IReadOnlyList<VehicleColorMetadata>? metadata)
        {
            metadata = Colors.Count == 0 ? null : Colors;
            return metadata != null;
        }

        public bool TryGetColor(
            VehicleColor color,
            out VehicleColorMetadata? metadata)
        {
            ColorLookupCount++;

            for (int i = 0; i < Colors.Count; i++)
            {
                VehicleColorMetadata entry = Colors[i];
                if (entry.Color == color)
                {
                    metadata = entry;
                    return true;
                }
            }

            metadata = null;
            return false;
        }
    }
}
