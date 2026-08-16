using S1API.Entities;
using S1API.Internal.Entities;
using S1API.Internal.Patches;
using S1API.Internal.Utils;

namespace S1API.Tests.Entities;

public sealed class CustomNpcReadinessPolicyTests
{
    [Fact]
    public void ClientHydrationSignalsReadyOnlyAfterEveryCustomNpcTypeCompletes()
    {
        NPC.FinalizedCustomNpcTypes.Clear();
        NPCPatches.CustomNpcsReady = false;

        try
        {
            foreach (Type npcType in ReflectionUtils.GetDerivedClasses<NPC>())
            {
                if (npcType.Assembly != typeof(NPC).Assembly
                    && npcType != typeof(DealerNpc)
                    && npcType != typeof(CustomerNpc))
                {
                    NPC.FinalizedCustomNpcTypes.Add(npcType);
                }
            }

            var dealer = TestObjectFactory.CreateUninitialized<DealerNpc>();
            var customer = TestObjectFactory.CreateUninitialized<CustomerNpc>();

            dealer.CreateFromClientNetworkSpawn();

            Assert.False(NPC.CustomNpcsReady);

            customer.CreateFromClientNetworkSpawn();

            Assert.True(NPC.CustomNpcsReady);
        }
        finally
        {
            NPC.FinalizedCustomNpcTypes.Clear();
            NPCPatches.CustomNpcsReady = false;
        }
    }

    [Fact]
    public void ClientRemainsNotReadyWhileARegisteredTypeIsMissing()
    {
        Type[] registeredTypes = { typeof(DealerNpc), typeof(CustomerNpc) };
        HashSet<Type> finalizedTypes = new() { typeof(DealerNpc) };

        bool ready = CustomNpcReadinessPolicy.AreAllTypesFinalized(
            registeredTypes,
            finalizedTypes);

        Assert.False(ready);
    }

    [Fact]
    public void ClientBecomesReadyAfterEveryRegisteredTypeIsHydrated()
    {
        Type[] registeredTypes = { typeof(DealerNpc), typeof(CustomerNpc) };
        HashSet<Type> finalizedTypes = new() { typeof(DealerNpc) };

        CustomNpcReadinessPolicy.MarkFinalized(
            typeof(CustomerNpc),
            finalizedTypes);

        bool ready = CustomNpcReadinessPolicy.AreAllTypesFinalized(
            registeredTypes,
            finalizedTypes);

        Assert.True(ready);
    }

    [Fact]
    public void NoRegisteredTypesDoesNotSignalReady()
    {
        bool ready = CustomNpcReadinessPolicy.AreAllTypesFinalized(
            Array.Empty<Type>(),
            new HashSet<Type>());

        Assert.False(ready);
    }

    private sealed class DealerNpc : NPC
    {
        internal override void CreateInternal()
        {
        }
    }

    private sealed class CustomerNpc : NPC
    {
        internal override void CreateInternal()
        {
        }
    }
}
