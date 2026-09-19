using System.Reflection;
using S1API.Items;
using UnityEngine;

namespace S1API.Tests.Items;

#pragma warning disable CS0618
public sealed class ItemCreatorApiCompatibilityTests
{
    [Fact]
    public void LegacyCreateItemOverloadsPreserveSourceAndBinaryShapes()
    {
        MethodInfo? nineArgumentOverload = typeof(ItemCreator).GetMethod(
            nameof(ItemCreator.CreateItem),
            new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(ItemCategory),
                typeof(int), typeof(float), typeof(float), typeof(LegalStatus), typeof(Sprite)
            });
        MethodInfo? tenArgumentOverload = typeof(ItemCreator).GetMethod(
            nameof(ItemCreator.CreateItem),
            new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(ItemCategory),
                typeof(int), typeof(float), typeof(float), typeof(LegalStatus), typeof(Sprite),
                typeof(Equippable)
            });

        Assert.NotNull(nineArgumentOverload);
        Assert.NotNull(tenArgumentOverload);
        Assert.Equal("legacyIcon", nineArgumentOverload.GetParameters()[8].Name);
        Assert.Equal("icon", tenArgumentOverload.GetParameters()[8].Name);
        Assert.Equal("equippable", tenArgumentOverload.GetParameters()[9].Name);
        Assert.All(nineArgumentOverload.GetParameters(), parameter => Assert.False(parameter.IsOptional));
        Assert.All(tenArgumentOverload.GetParameters(), parameter => Assert.False(parameter.IsOptional));
        Assert.Single(nineArgumentOverload.GetCustomAttributes<ObsoleteAttribute>());
        Assert.Single(tenArgumentOverload.GetCustomAttributes<ObsoleteAttribute>());
    }
}
#pragma warning restore CS0618
