using S1API.Entities;

namespace S1API.Tests.NPCs;

public sealed class NPCNetworkObjectTests
{
    [Fact]
    public void AnNpcCreatedBeforeTheNetworkStartsKeepsItsNetworkObject()
    {
        // The Big Pimpin creates its escorts at scene load on the second load of a session, before the server is
        // up. Removing their NetworkObject then left them bound to @Managers/@NPCs and their spawn failed.
        Assert.False(NPC.ShouldRemoveNetworkObject(hasNetworkManager: true, isClient: false, isServer: false));
        Assert.False(NPC.ShouldRemoveNetworkObject(hasNetworkManager: false, isClient: false, isServer: false));
    }

    [Fact]
    public void TheServerAndHostKeepIt()
    {
        Assert.False(NPC.ShouldRemoveNetworkObject(hasNetworkManager: true, isClient: false, isServer: true));
        Assert.False(NPC.ShouldRemoveNetworkObject(hasNetworkManager: true, isClient: true, isServer: true));
    }

    [Fact]
    public void AClientOnlyPeerRemovesItBecauseTheServerSpawnsTheNpcToIt()
    {
        Assert.True(NPC.ShouldRemoveNetworkObject(hasNetworkManager: true, isClient: true, isServer: false));
    }
}
