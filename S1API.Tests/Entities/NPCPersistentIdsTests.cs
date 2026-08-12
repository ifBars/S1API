using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCPersistentIdsTests
{
    [Fact]
    public void IdentityIdProducesTheSameGuidAcrossConstructionPaths()
    {
        Assert.True(NPCPersistentIds.TryGetGuid("mod.author:custom_npc", out Guid first));
        Assert.True(NPCPersistentIds.TryGetGuid(" MOD.AUTHOR:CUSTOM_NPC ", out Guid second));

        Assert.Equal(first, second);
        Assert.NotEqual(Guid.Empty, first);
    }

    [Fact]
    public void DifferentIdentityIdsProduceDifferentGuids()
    {
        NPCPersistentIds.TryGetGuid("mod.author:customer_a", out Guid first);
        NPCPersistentIds.TryGetGuid("mod.author:customer_b", out Guid second);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingIdentityIdDoesNotProducePersistentGuid(string id)
    {
        Assert.False(NPCPersistentIds.TryGetGuid(id, out Guid guid));
        Assert.Equal(Guid.Empty, guid);
    }
}
