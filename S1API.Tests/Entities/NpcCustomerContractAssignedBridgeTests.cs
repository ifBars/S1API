#if IL2CPPMELON
using S1Quests = Il2CppScheduleOne.Quests;
#else
using S1Quests = ScheduleOne.Quests;
#endif

using System.Reflection;
using S1API.Entities;
using UnityEngine.Events;

namespace S1API.Tests.Entities;

public sealed class NpcCustomerContractAssignedBridgeTests
{
    [Fact]
    public void BridgeMatchesRuntimeUnityEventSignature()
    {
        Type customerType = typeof(NPCCustomer);
        FieldInfo bridgeField = customerType.GetField(
            "_contractAssignedBridge",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo eventField = customerType.GetField(
            "_contractAssignedUnityEvent",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo handler = customerType.GetMethod(
            "HandleContractAssigned",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.Equal(
            typeof(UnityAction<S1Quests.Contract>),
            bridgeField.FieldType);
        Assert.Equal(
            typeof(UnityEvent<S1Quests.Contract>),
            eventField.FieldType);
        Assert.Equal(
            typeof(S1Quests.Contract),
            Assert.Single(handler.GetParameters()).ParameterType);
    }

    [Fact]
    public void UnityActionDelegateInheritanceMatchesRuntime()
    {
        bool derivesFromManagedDelegate = typeof(Delegate).IsAssignableFrom(
            typeof(UnityAction<S1Quests.Contract>));

#if IL2CPPMELON
        Assert.False(derivesFromManagedDelegate);
#else
        Assert.True(derivesFromManagedDelegate);
#endif
    }
}
