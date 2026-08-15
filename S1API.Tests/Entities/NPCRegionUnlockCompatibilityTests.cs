using System.Reflection;
using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCRegionUnlockCompatibilityTests
{
    [Fact]
    public void RequiresRegionUnlockedRetainsItsPublicSurface()
    {
        PropertyInfo? property = typeof(NPC).GetProperty(
            nameof(NPC.RequiresRegionUnlocked),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property!.PropertyType);
        Assert.True(property.CanRead);
        Assert.True(property.CanWrite);
    }

    [Fact]
    public void OptionalRegionUnlockMemberSupportsFieldAndPropertyShapes()
    {
        var fieldShape = new FieldShape();
        var propertyShape = new PropertyShape();

        Assert.True(NPC.TrySetRequiresRegionUnlocked(fieldShape, false));
        Assert.True(NPC.TrySetRequiresRegionUnlocked(propertyShape, false));
        Assert.False(NPC.ResolveRequiresRegionUnlocked(fieldShape));
        Assert.False(NPC.ResolveRequiresRegionUnlocked(propertyShape));
    }

    [Fact]
    public void MissingRegionUnlockMemberUsesDefaultAndIgnoresWrites()
    {
        var missingShape = new MissingShape();

        Assert.True(NPC.ResolveRequiresRegionUnlocked(missingShape));
        Assert.False(NPC.TrySetRequiresRegionUnlocked(missingShape, false));
        Assert.True(NPC.ResolveRequiresRegionUnlocked(missingShape));
    }

    private sealed class FieldShape
    {
        public bool RequiresRegionUnlocked = true;
    }

    private sealed class PropertyShape
    {
        public bool RequiresRegionUnlocked { get; set; } = true;
    }

    private sealed class MissingShape
    {
    }
}
