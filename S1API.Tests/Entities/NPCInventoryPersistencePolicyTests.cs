using S1API.Internal.Patches;
using S1API.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCInventoryPersistencePolicyTests
{
    [Fact]
    public void SavedInventoryRestoresAfterSlotInitialization()
    {
        var calls = new List<string>();

        NPCPatches.RestoreInventoryAfterInitialization(
            () => calls.Add("initialize"),
            () => calls.Add("restore"));

        Assert.Equal(new[] { "initialize", "restore" }, calls);
    }

    [Fact]
    public void AlreadyAwakeInventoryStillRestoresExactlyOnce()
    {
        bool awakeCompleted = true;
        int initializationCount = 0;
        int restoreCount = 0;

        NPCPatches.RestoreInventoryAfterInitialization(
            () =>
            {
                Assert.True(awakeCompleted);
                initializationCount++;
            },
            () =>
            {
                Assert.True(awakeCompleted);
                restoreCount++;
            });

        Assert.Equal(1, initializationCount);
        Assert.Equal(1, restoreCount);
    }

    [Fact]
    public void FailedInitializationDoesNotAttemptRestore()
    {
        bool restoreAttempted = false;

        Assert.Throws<InvalidOperationException>(() =>
            NPCPatches.RestoreInventoryAfterInitialization(
                () => throw new InvalidOperationException("initialization failed"),
                () => restoreAttempted = true));

        Assert.False(restoreAttempted);
    }

    [Fact]
    public void CurrentNpcDataSlotCountIsUsedWhenLegacyMemberIsMissing()
    {
        int result = NPCInventory.ResolveTargetSlotCount(
            legacySlotCount: null,
            npcDataSlotCount: 5,
            fallback: 0,
            isCustomNpc: true);

        Assert.Equal(5, result);
    }

    [Fact]
    public void LegacySlotCountRemainsPreferredForOlderGameVersions()
    {
        int result = NPCInventory.ResolveTargetSlotCount(
            legacySlotCount: 6,
            npcDataSlotCount: 5,
            fallback: 0,
            isCustomNpc: true);

        Assert.Equal(6, result);
    }

    [Fact]
    public void ExistingCollectionCountIsFinalFallback()
    {
        int result = NPCInventory.ResolveTargetSlotCount(
            legacySlotCount: -1,
            npcDataSlotCount: null,
            fallback: 4,
            isCustomNpc: true);

        Assert.Equal(4, result);
    }

    [Fact]
    public void CustomNpcUsesVanillaFiveSlotDefaultWhenNativeCountsAreZero()
    {
        int result = NPCInventory.ResolveTargetSlotCount(
            legacySlotCount: null,
            npcDataSlotCount: 0,
            fallback: 0,
            isCustomNpc: true);

        Assert.Equal(5, result);
    }

    [Fact]
    public void BaseNpcCanRetainAnIntentionallyEmptyInventory()
    {
        int result = NPCInventory.ResolveTargetSlotCount(
            legacySlotCount: null,
            npcDataSlotCount: 0,
            fallback: 0,
            isCustomNpc: false);

        Assert.Equal(0, result);
    }
}
