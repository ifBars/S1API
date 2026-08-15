using System.Reflection;
using S1API.Casino;

#if IL2CPPMELON
using S1Casino = Il2CppScheduleOne.Casino;
#elif MONOMELON
using S1Casino = ScheduleOne.Casino;
#endif

namespace S1API.Tests.Casino;

public sealed class CasinoApiContractTests
{
    [Fact]
    public void NativeLifecyclePatchPointsExistInTargetRuntime()
    {
        Assert.NotNull(typeof(S1Casino.BlackjackGameController).GetMethod(
            "set_CurrentStage",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
        Assert.NotNull(typeof(S1Casino.RTBGameController).GetMethod(
            "set_CurrentStage",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
        Assert.NotNull(typeof(S1Casino.SlotMachine).GetMethod(
            "RpcLogic___StartSpin_2659526290",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
        Assert.NotNull(typeof(S1Casino.SlotMachine).GetMethod(
            "DisplayOutcome",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
    }

    [Fact]
    public void ManagedEnumsPreserveNativeWireValues()
    {
        Assert.Equal((int)S1Casino.BlackjackGameController.EStage.WaitingForPlayers, (int)BlackjackStage.WaitingForPlayers);
        Assert.Equal((int)S1Casino.BlackjackGameController.EStage.Ending, (int)BlackjackStage.Ending);
        Assert.Equal((int)S1Casino.RTBGameController.EStage.RedOrBlack, (int)RideTheBusStage.RedOrBlack);
        Assert.Equal((int)S1Casino.RTBGameController.EStage.Suit, (int)RideTheBusStage.Suit);
        Assert.Equal((int)S1Casino.PlayingCard.ECardSuit.Clubs, (int)CasinoCardSuit.Clubs);
        Assert.Equal((int)S1Casino.PlayingCard.ECardValue.King, (int)CasinoCardValue.King);
        Assert.Equal((int)S1Casino.SlotMachine.ESymbol.Seven, (int)SlotSymbol.Seven);
        Assert.Equal((int)S1Casino.SlotMachine.EOutcome.NoWin, (int)SlotOutcome.NoWin);
    }

    [Theory]
    [InlineData(typeof(CasinoPlayerSnapshot))]
    [InlineData(typeof(SlotSpinSnapshot))]
    public void SnapshotReferenceTypesExposeNoPublicSetters(Type snapshotType)
    {
        Assert.All(
            snapshotType.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.Null(property.SetMethod));
        Assert.Empty(snapshotType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void CasinoWrappersCannotBePubliclyConstructedOrMutated()
    {
        Type[] wrapperTypes =
        {
            typeof(BlackjackGame),
            typeof(RideTheBusGame),
            typeof(SlotMachine)
        };

        foreach (Type wrapperType in wrapperTypes)
        {
            Assert.Empty(wrapperType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            Assert.All(
                wrapperType.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                property => Assert.Null(property.SetMethod));
        }
    }

    [Fact]
    public void RegistrySurfaceIsReadOnlyDiscoveryAndQueriesOnly()
    {
        MethodInfo[] publicMethods = typeof(CasinoGameRegistry)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.NotEmpty(publicMethods);
        Assert.All(publicMethods, method =>
            Assert.True(
                method.Name.StartsWith("Get", StringComparison.Ordinal) ||
                method.Name.StartsWith("Find", StringComparison.Ordinal),
                $"Unexpected registry method: {method.Name}"));
        Assert.DoesNotContain(publicMethods, method => method.ReturnType == typeof(void));
    }

    [Fact]
    public void LegacyNativeSlotLookupRemainsAsAnObsoleteCompatibilityShim()
    {
        MethodInfo method = typeof(SlotMachineHelper).GetMethod(
            nameof(SlotMachineHelper.FindNearestSlotMachine),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(UnityEngine.Vector3), typeof(float) },
            modifiers: null)!;

        Assert.NotNull(method);
        Assert.NotNull(method.GetCustomAttribute<ObsoleteAttribute>());
        Assert.Equal(typeof(S1Casino.SlotMachine), method.ReturnType);

        MethodInfo managedMethod = typeof(CasinoGameRegistry).GetMethod(
            nameof(CasinoGameRegistry.FindNearestSlotMachine),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(UnityEngine.Vector3), typeof(float) },
            modifiers: null)!;

        Assert.NotNull(managedMethod);
        Assert.Equal(typeof(SlotMachine), managedMethod.ReturnType);
    }

    [Fact]
    public void PublicCasinoApiDoesNotExposeNativeCasinoTypes()
    {
        Type[] publicCasinoTypes =
        {
            typeof(CasinoGameRegistry),
            typeof(CasinoGameTable),
            typeof(BlackjackGame),
            typeof(RideTheBusGame),
            typeof(SlotMachine),
            typeof(CasinoPlayerSnapshot),
            typeof(CasinoCardSnapshot),
            typeof(SlotSpinSnapshot)
        };

        foreach (Type type in publicCasinoTypes)
        {
            IEnumerable<Type> exposedTypes = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .SelectMany(GetExposedTypes);

            Assert.DoesNotContain(exposedTypes, exposed =>
                exposed.Namespace?.Contains("ScheduleOne.Casino", StringComparison.Ordinal) == true);
        }
    }

    private static IEnumerable<Type> GetExposedTypes(MemberInfo member)
    {
        switch (member)
        {
            case PropertyInfo property:
                yield return property.PropertyType;
                break;
            case FieldInfo field:
                yield return field.FieldType;
                break;
            case EventInfo eventInfo when eventInfo.EventHandlerType != null:
                yield return eventInfo.EventHandlerType;
                break;
            case MethodInfo method:
                yield return method.ReturnType;
                foreach (ParameterInfo parameter in method.GetParameters())
                    yield return parameter.ParameterType;
                break;
            case ConstructorInfo constructor:
                foreach (ParameterInfo parameter in constructor.GetParameters())
                    yield return parameter.ParameterType;
                break;
        }
    }
}
