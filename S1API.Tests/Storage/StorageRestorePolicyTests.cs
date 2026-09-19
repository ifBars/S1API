using S1API.Internal.Patches;

namespace S1API.Tests.Storage;

public sealed class StorageRestorePolicyTests
{
    [Fact]
    public void PersistedSlotCountCanExceedTheDefaultWrapperLimit()
    {
        Assert.Equal(24, StoragePatches.ResolveRestoreMaxSlots(20, 24));
    }

    [Fact]
    public void PersistedSlotCountDoesNotLowerAnExplicitLimit()
    {
        Assert.Equal(32, StoragePatches.ResolveRestoreMaxSlots(32, 24));
    }
}
