using S1API.Internal.Patches;

namespace S1API.Tests.NPCs;

public sealed class NPCBehaviourOwnershipTests
{
    [Fact]
    public void GameMembersTheBehaviourOwnershipRepairReliesOnExist()
    {
        // If the game renames one of these, the repair stops working and a custom NPC can stop a save from loading.
        Assert.Empty(NPCPatches.FindMissingBehaviourOwnershipMembers());
    }
}
