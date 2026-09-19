using S1API.Items.Clothing;
using UnityEngine;

namespace S1API.Tests.Items;

[Collection(ClothingMetadataCatalogCollection.Name)]
public sealed class ClothingMetadataCatalogTests : IDisposable
{
    private readonly FakeClothingMetadataProvider _provider = new();

    public ClothingMetadataCatalogTests()
    {
        ClothingMetadataCatalog.ResetForTesting(_provider);
    }

    public void Dispose()
    {
        ClothingMetadataCatalog.RestoreProviderForTesting();
    }

    [Fact]
    public void LookupsExposeS1ApiOwnedMetadata()
    {
        Color actualColor = CreateColor(0.2f, 0.4f, 0.6f);
        Color labelColor = CreateColor(1f, 1f, 1f);
        var slotMetadata = new ClothingSlotMetadata(
            ClothingSlot.Head,
            "Headwear",
            null);
        var colorMetadata = new ClothingColorMetadata(
            ClothingColor.Blue,
            "Blue",
            actualColor,
            labelColor);
        _provider.Slots[ClothingSlot.Head] = slotMetadata;
        _provider.Colors[ClothingColor.Blue] = colorMetadata;

        Assert.Same(slotMetadata, ClothingMetadataCatalog.GetSlot(ClothingSlot.Head));
        Assert.True(
            ClothingMetadataCatalog.TryGetColor(
                ClothingColor.Blue,
                out ClothingColorMetadata? resolvedColor));
        Assert.Same(colorMetadata, resolvedColor);
        AssertColor(actualColor, resolvedColor!.ActualColor);
        AssertColor(labelColor, resolvedColor.LabelColor);
    }

    [Fact]
    public void CatalogPropertiesReturnOrderedReadOnlySnapshots()
    {
        var feet = new ClothingSlotMetadata(ClothingSlot.Feet, "Shoes", null);
        var head = new ClothingSlotMetadata(ClothingSlot.Head, "Headwear", null);
        _provider.Slots[ClothingSlot.Head] = head;
        _provider.Slots[ClothingSlot.Feet] = feet;

        IReadOnlyList<ClothingSlotMetadata> snapshot =
            ClothingMetadataCatalog.Slots;
        _provider.Slots.Remove(ClothingSlot.Head);

        Assert.Equal(new[] { feet, head }, snapshot);
        Assert.Equal(new[] { feet }, ClothingMetadataCatalog.Slots);

        IList<ClothingSlotMetadata> mutableView =
            Assert.IsAssignableFrom<IList<ClothingSlotMetadata>>(snapshot);
        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add(feet));
    }

    [Fact]
    public void InvalidValuesDoNotReachNativeProvider()
    {
        Assert.False(
            ClothingMetadataCatalog.TryGetSlot(
                (ClothingSlot)(-1),
                out ClothingSlotMetadata? slotMetadata));
        Assert.False(
            ClothingMetadataCatalog.TryGetColor(
                (ClothingColor)999,
                out ClothingColorMetadata? colorMetadata));

        Assert.Null(slotMetadata);
        Assert.Null(colorMetadata);
        Assert.Equal(0, _provider.SlotLookupCount);
        Assert.Equal(0, _provider.ColorLookupCount);
    }

    [Fact]
    public void UnavailableRuntimeReturnsEmptyAndNullResults()
    {
        Assert.Empty(ClothingMetadataCatalog.Slots);
        Assert.Empty(ClothingMetadataCatalog.Colors);
        Assert.Null(ClothingMetadataCatalog.GetSlot(ClothingSlot.Head));
        Assert.Null(ClothingMetadataCatalog.GetColor(ClothingColor.Blue));
    }

    [Fact]
    public void MetadataPropertiesAreReadOnly()
    {
        Assert.All(
            typeof(ClothingSlotMetadata).GetProperties(),
            property => Assert.False(property.CanWrite));
        Assert.All(
            typeof(ClothingColorMetadata).GetProperties(),
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

    private sealed class FakeClothingMetadataProvider :
        IClothingMetadataProvider
    {
        internal Dictionary<ClothingSlot, ClothingSlotMetadata> Slots { get; } =
            new();

        internal Dictionary<ClothingColor, ClothingColorMetadata> Colors { get; } =
            new();

        internal int SlotLookupCount { get; private set; }

        internal int ColorLookupCount { get; private set; }

        public bool TryGetSlot(
            ClothingSlot slot,
            out ClothingSlotMetadata? metadata)
        {
            SlotLookupCount++;
            return Slots.TryGetValue(slot, out metadata);
        }

        public bool TryGetColor(
            ClothingColor color,
            out ClothingColorMetadata? metadata)
        {
            ColorLookupCount++;
            return Colors.TryGetValue(color, out metadata);
        }
    }
}
