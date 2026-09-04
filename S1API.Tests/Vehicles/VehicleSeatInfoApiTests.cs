using System.Reflection;
using S1API.Entities;
using S1API.Vehicles;

#if IL2CPPMELON
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1Vehicles = ScheduleOne.Vehicles;
#endif

namespace S1API.Tests.Vehicles;

public sealed class VehicleSeatInfoApiTests
{
    [Fact]
    public void NativeSeatAssignmentPatchPointExistsInTargetRuntime()
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        Assert.NotNull(typeof(S1Vehicles.LandVehicle).GetMethod(
            "SetSeatOccupant",
            flags));
    }

    [Fact]
    public void WrapperSurfaceIsReadOnlyAndManaged()
    {
        Assert.Equal(
            typeof(IReadOnlyList<VehicleSeatInfo>),
            typeof(LandVehicle).GetProperty(nameof(LandVehicle.Seats))!.PropertyType);
        Assert.Empty(typeof(VehicleSeatInfo).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));

        Assert.Equal(typeof(int), GetProperty(nameof(VehicleSeatInfo.Index)).PropertyType);
        Assert.Equal(typeof(bool), GetProperty(nameof(VehicleSeatInfo.IsDriverSeat)).PropertyType);
        Assert.Equal(typeof(bool), GetProperty(nameof(VehicleSeatInfo.IsOccupied)).PropertyType);
        Assert.Equal(typeof(Player), GetProperty(nameof(VehicleSeatInfo.Occupant)).PropertyType);
        Assert.All(
            typeof(VehicleSeatInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.Null(property.SetMethod));
        Assert.Equal(
            typeof(Action<Player?, Player?>),
            typeof(VehicleSeatInfo).GetEvent(nameof(VehicleSeatInfo.OccupantChanged))!.EventHandlerType);

        Assert.DoesNotContain(
            typeof(VehicleSeatInfo).GetMembers(BindingFlags.Public | BindingFlags.Instance),
            ExposesNativeVehicleType);
    }

    [Fact]
    public void ManagedNotificationsSuppressNoOpsAndIsolateSubscribers()
    {
        var seat = TestObjectFactory.CreateUninitialized<S1Vehicles.VehicleSeat>();
        var info = new VehicleSeatInfo(1, seat);
        var currentNative = TestObjectFactory.CreateUninitialized<S1PlayerScripts.Player>();
        var current = new Player(currentNative);
        int calls = 0;

        try
        {
            info.OccupantChanged += (_, _) => throw new InvalidOperationException("expected");
            info.OccupantChanged += (observedPrevious, observedCurrent) =>
            {
                Assert.Null(observedPrevious);
                Assert.Same(current, observedCurrent);
                calls++;
            };

            info.NotifyOccupantChanged(null, null);
            info.NotifyOccupantChanged(null, currentNative);

            Assert.Equal(1, calls);
        }
        finally
        {
            Player.All.Remove(current);
        }
    }

    private static PropertyInfo GetProperty(string name) =>
        typeof(VehicleSeatInfo).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!;

    private static bool ExposesNativeVehicleType(MemberInfo member)
    {
        IEnumerable<Type> types = member switch
        {
            PropertyInfo property => new[] { property.PropertyType },
            EventInfo eventInfo when eventInfo.EventHandlerType != null =>
                new[] { eventInfo.EventHandlerType },
            MethodInfo method => new[] { method.ReturnType }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType)),
            _ => Array.Empty<Type>()
        };

        return types
            .SelectMany(ExpandType)
            .Any(type => type.Namespace?.Contains(
                "ScheduleOne.Vehicles",
                StringComparison.Ordinal) == true);
    }

    private static IEnumerable<Type> ExpandType(Type root)
    {
        yield return root;

        if (root.HasElementType && root.GetElementType() is Type elementType)
        {
            foreach (Type nested in ExpandType(elementType))
                yield return nested;
        }

        foreach (Type argument in root.GetGenericArguments())
        {
            foreach (Type nested in ExpandType(argument))
                yield return nested;
        }
    }
}

internal static class VehicleSeatInfoApiCompileFixture
{
    internal static void Observe(LandVehicle vehicle, VehicleSeatInfo seat)
    {
        IReadOnlyList<VehicleSeatInfo> seats = vehicle.Seats;
        int index = seat.Index;
        bool isDriverSeat = seat.IsDriverSeat;
        bool isOccupied = seat.IsOccupied;
        Player? occupant = seat.Occupant;
        Action<Player?, Player?> handler = (_, _) => { };

        seat.OccupantChanged += handler;
        seat.OccupantChanged -= handler;

        _ = seats;
        _ = index;
        _ = isDriverSeat;
        _ = isOccupied;
        _ = occupant;
    }
}
