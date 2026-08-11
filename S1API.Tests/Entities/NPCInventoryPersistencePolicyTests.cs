using S1API.Internal.Patches;

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
}
