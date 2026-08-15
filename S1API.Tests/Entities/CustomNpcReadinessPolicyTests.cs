using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class CustomNpcReadinessPolicyTests
{
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

    private sealed class DealerNpc
    {
    }

    private sealed class CustomerNpc
    {
    }
}
