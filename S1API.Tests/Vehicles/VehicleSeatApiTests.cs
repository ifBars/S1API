using System.Reflection;
using S1API.Entities;
using S1API.Internal.Patches;
using S1API.Internal.Vehicles;
using S1API.Vehicles;

namespace S1API.Tests.Vehicles;

public sealed class VehicleSeatApiTests
{
    [Fact]
    public void AssignmentHookSelectsTheGeneratedMutationInBothRuntimes()
    {
        var target = Assert.IsAssignableFrom<MethodInfo>(VehicleSeatPatches.TargetMethod());
        Assert.StartsWith("RpcLogic___SetSeatOccupant_", target.Name);
        Assert.Equal(typeof(int), target.GetParameters()[1].ParameterType);
        Assert.Equal(3, target.GetParameters().Length);
    }

    [Fact]
    public void SeatSurfaceIsReadOnlyAndUsesManagedTypes()
    {
        Assert.True(typeof(VehicleSeatInfo).IsSealed);
        Assert.Empty(typeof(VehicleSeatInfo).GetConstructors());
        var expected = new Dictionary<string, Type>
        {
            [nameof(VehicleSeatInfo.Index)] = typeof(int),
            [nameof(VehicleSeatInfo.IsDriverSeat)] = typeof(bool),
            [nameof(VehicleSeatInfo.IsOccupied)] = typeof(bool),
            [nameof(VehicleSeatInfo.Occupant)] = typeof(Player)
        };
        foreach (var property in typeof(VehicleSeatInfo).GetProperties())
        {
            Assert.Equal(expected[property.Name], property.PropertyType);
            Assert.Null(property.SetMethod);
        }
        Assert.Equal(typeof(Action<Player?, Player?>),
            typeof(VehicleSeatInfo).GetEvent(nameof(VehicleSeatInfo.OccupantChanged))!.EventHandlerType);
        Assert.Equal(typeof(IReadOnlyList<VehicleSeatInfo>),
            typeof(LandVehicle).GetProperty(nameof(LandVehicle.Seats))!.PropertyType);
        // The legacy writable property is a public compatibility contract.
        Assert.True(typeof(LandVehicle).GetProperty(nameof(LandVehicle.IsOccupied))!.CanWrite);
    }

    [Fact]
    public void MissingSeatRetainsIndexAndDoesNotEmitNoOpNotifications()
    {
        var seat = new VehicleSeatInfo(3, null);
        int calls = 0;
        seat.OccupantChanged += (_, _) => calls++;
        seat.Refresh();
        seat.Refresh();
        Assert.Equal(3, seat.Index);
        Assert.False(seat.IsDriverSeat);
        Assert.False(seat.IsOccupied);
        Assert.Null(seat.Occupant);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void UnavailableWrappersDoNotSuppressNativeTransitionNotifications()
    {
        var seat = new VehicleSeatInfo(0, null);
        int calls = 0;
        seat.OccupantChanged += (_, _) => throw new InvalidOperationException("expected subscriber failure");
        seat.OccupantChanged += (previous, current) =>
        {
            Assert.Null(previous);
            Assert.Null(current);
            calls++;
        };
        seat.NotifyOccupantChanged(null, null);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void InvalidationReleasesSubscribersAndIsIdempotent()
    {
        var seat = new VehicleSeatInfo(2, null);
        int calls = 0;
        Action<Player?, Player?> handler = (_, _) => calls++;
        seat.OccupantChanged += handler;
        seat.OccupantChanged -= handler;
        seat.NotifyOccupantChanged(null, null);
        Assert.Equal(0, calls);
        seat.OccupantChanged += handler;
        seat.Invalidate();
        seat.Invalidate();
        seat.NotifyOccupantChanged(null, null);
        Assert.Equal(0, calls);
        Assert.Equal(2, seat.Index);
        Assert.False(seat.IsOccupied);
        Assert.Null(seat.Occupant);
    }

    [Fact]
    public void UnavailableVehicleReturnsAnImmutableEmptyCollection()
    {
        var seats = VehicleSeats.Get(null);
        Assert.Empty(seats);
        Assert.True(Assert.IsAssignableFrom<ICollection<VehicleSeatInfo>>(seats).IsReadOnly);
    }
}
