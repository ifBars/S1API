using S1API.Internal.Rendering;
#if MONOMELON
using System.Reflection;
using ScheduleOne.Core.Avatar;
using ScheduleOne.Core.Avatar.Properties;
using UnityEngine;
#endif

namespace S1API.Tests.Rendering;

public sealed class CustomAvatarObjectsTests
{
    [Fact]
    public void IdIsTheTargetResourcePathWhenOneIsGiven()
    {
        Assert.Equal(
            "BigWillyMod/Accessories/StaySillyCap",
            CustomAvatarObjects.DeriveId(
                "BigWillyMod/Accessories/StaySillyCap",
                "avatar/accessories/head/cap/Cap",
                "StaySillyCap"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IdFallsBackToTheSourcePathAndNameWithoutATarget(string? target)
    {
        Assert.Equal(
            "avatar/accessories/head/cap/Cap/StaySillyCap",
            CustomAvatarObjects.DeriveId(target, "avatar/accessories/head/cap/Cap", "StaySillyCap"));
    }

    [Fact]
    public void FallbackIdDoesNotDoubleTheSeparator()
    {
        Assert.Equal(
            "avatar/accessories/head/cap/Cap/StaySillyCap",
            CustomAvatarObjects.DeriveId(null, "avatar/accessories/head/cap/Cap/", "StaySillyCap"));
    }

    [Fact]
    public void GameMembersTheCustomAvatarObjectPathReliesOnExist()
    {
        Assert.Empty(CustomAvatarObjects.FindMissingGameMembers());
    }

#if MONOMELON
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TintPreservesTheDefaultUnlessAnOverrideIsProvided(bool overrideTint)
    {
        var originalColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        var tint = new Color(0.8f, 0.7f, 0.6f, 1f);
        var color = new ColorProperty("Primary", originalColor);
        var properties = new AvatarPropertyCollection();
        typeof(AvatarPropertyCollection).GetField("_colors", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(properties, new[] { color });
        AvatarObject avatarObject = TestObjectFactory.CreateUninitialized<AvatarObject>();
        typeof(AvatarObject).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(avatarObject, properties);

        CustomAvatarObjects.ApplyColorTint(avatarObject, overrideTint ? tint : null);

        Color expected = overrideTint ? tint : originalColor;
        Assert.Equal(expected.r, color.Value.r);
        Assert.Equal(expected.g, color.Value.g);
        Assert.Equal(expected.b, color.Value.b);
        Assert.Equal(expected.a, color.Value.a);
        Assert.Equal(expected.r, avatarObject.Serialize().Colors[0].Value.r);
    }
#endif
}
