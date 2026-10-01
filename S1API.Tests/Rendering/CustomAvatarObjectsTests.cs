using S1API.Internal.Rendering;

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
        // If the game renames one of these, a custom accessory stops showing; this fails first.
        Assert.Empty(CustomAvatarObjects.FindMissingGameMembers());
    }
}
