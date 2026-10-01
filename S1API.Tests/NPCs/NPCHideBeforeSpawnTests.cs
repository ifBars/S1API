using S1API.Internal.Patches;

namespace S1API.Tests.NPCs;

public sealed class NPCHideBeforeSpawnTests
{
    [Fact]
    public void AHideOnAnS1ApiNpcThatIsNotSpawnedYetIsIgnored()
    {
        // The Big Pimpin hides its escorts before S1API spawns them; applied, the hide switched their Avatar off and
        // the spawn was refused.
        Assert.True(NPCHideBeforeSpawnPatch.ShouldIgnoreHide(hasNetworkObject: true, isSpawned: false, isS1ApiNpc: true));
    }

    [Fact]
    public void SpawnedAndNonS1ApiNpcsAreHiddenAsBefore()
    {
        Assert.False(NPCHideBeforeSpawnPatch.ShouldIgnoreHide(hasNetworkObject: true, isSpawned: true, isS1ApiNpc: true));
        Assert.False(NPCHideBeforeSpawnPatch.ShouldIgnoreHide(hasNetworkObject: true, isSpawned: false, isS1ApiNpc: false));
        Assert.False(NPCHideBeforeSpawnPatch.ShouldIgnoreHide(hasNetworkObject: false, isSpawned: false, isS1ApiNpc: true));
    }

    [Fact]
    public void ThePatchedGameMethodExists()
    {
        // The patch names SetVisible(bool, bool); if the game changes it, the patch silently stops applying.
        Assert.True(NPCHideBeforeSpawnPatch.TargetExists());
    }
}
