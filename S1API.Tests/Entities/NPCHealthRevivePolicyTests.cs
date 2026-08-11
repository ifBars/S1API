using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCHealthRevivePolicyTests
{
    [Theory]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)]
    public void PreSpawnFallbackOnlyAppliesToUnspawnedCustomNPCsInMain(
        bool isInMainScene,
        bool isCustomNpc,
        bool isSpawned,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCHealthRevivePolicy.ShouldUsePreSpawnFallback(
                isInMainScene,
                isCustomNpc,
                isSpawned));
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void SpawnedClientReviveIsSuppressed(
        bool isSpawned,
        bool isServer,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCHealthRevivePolicy.ShouldSuppressSpawnedClientRevive(
                isSpawned,
                isServer));
    }
}
