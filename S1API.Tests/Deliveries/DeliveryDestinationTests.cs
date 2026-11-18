using System.Reflection;
using S1API.Deliveries;
using S1API.Internal.Deliveries;
using UnityEngine;

#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using S1Property = ScheduleOne.Property;
#endif

namespace S1API.Tests.Deliveries;

[Collection("DeliveryDestinations")]
public sealed class DeliveryDestinationTests : IDisposable
{
    private readonly List<string> _log = new();

    public DeliveryDestinationTests()
    {
        DeliveryDestinationRuntime.ResetForTesting();
        DeliveryDestinationRuntime.LogSinkForTesting = _log.Add;
    }

    public void Dispose()
    {
        DeliveryDestinationRuntime.ResetForTesting();
        DeliveryDestinationRuntime.LogSinkForTesting = null;
    }

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.NotNull(typeof(S1DeliveryUi.DeliveryShop).GetMethod("GetPotentialDestinations", flags));
        Assert.NotNull(typeof(S1Delivery.DeliveryManager).GetMethod("OnTimePass", flags));
        MethodInfo? bay = typeof(S1Delivery.DeliveryManager).GetMethod("IsLoadingBayFree", flags);
        Assert.NotNull(bay);
        Assert.Equal(
            new[] { typeof(S1Property.Property), typeof(int) },
            Array.ConvertAll(bay!.GetParameters(), p => p.ParameterType));
        Assert.Equal(typeof(bool), bay.ReturnType);
    }

    [Fact]
    public void RegistryExposesTheDestinationMethods()
    {
        Type registry = typeof(DeliveryRegistry);

        Assert.Equal(typeof(IDisposable), registry.GetMethod(nameof(DeliveryRegistry.AddDestination))!.ReturnType);
        Assert.Equal(typeof(IDisposable), registry.GetMethod(nameof(DeliveryRegistry.AddLoadingDock))!.ReturnType);
        Assert.Equal(typeof(IDisposable), registry.GetMethod(nameof(DeliveryRegistry.AddArrivalHold))!.ReturnType);
        Assert.NotNull(registry.GetMethod(nameof(DeliveryRegistry.RefreshLoadingDocks)));
        Assert.Equal(
            typeof(Func<Transform?>),
            registry.GetMethod(nameof(DeliveryRegistry.AddLoadingDock))!.GetParameters()[1].ParameterType);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(3, 2, false)]
    [InlineData(3, -1, true)]
    public void DockIsMissingOutsideTheArray(int count, int index, bool missing)
    {
        Assert.Equal(missing, DeliveryDestinationRuntime.IsDockMissing(count, index));
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => DeliveryRegistry.AddDestination(null!, () => true));
        Assert.Throws<ArgumentNullException>(() => DeliveryRegistry.AddLoadingDock(null!, () => null));
        Assert.Throws<ArgumentNullException>(() => DeliveryRegistry.AddArrivalHold(null!));
    }

    [Fact]
    public void ArrivalHoldIsRemovedOnceOnDispose()
    {
        IDisposable first = DeliveryRegistry.AddArrivalHold(_ => true);
        IDisposable second = DeliveryRegistry.AddArrivalHold(_ => false);
        Assert.Equal(2, DeliveryDestinationRuntime.HoldCount);

        first.Dispose();
        first.Dispose();

        Assert.Equal(1, DeliveryDestinationRuntime.HoldCount);
        Assert.Single(_log, line => line.Contains("arrival hold from") && line.Contains("removed"));
        second.Dispose();
        Assert.Equal(0, DeliveryDestinationRuntime.HoldCount);
    }

    [Fact]
    public void HoldNamesTheRegisteringAssembly()
    {
        using IDisposable hold = DeliveryRegistry.AddArrivalHold(_ => false);

        string owner = typeof(DeliveryDestinationTests).Assembly.GetName().Name!;
        Assert.Contains(_log, line => line == $"info: Delivery arrival hold registered by {owner}.");
    }

    [Fact]
    public void RefreshWithoutRegistrationsOnlyLogs()
    {
        DeliveryRegistry.RefreshLoadingDocks();

        Assert.Contains(_log, line => line.StartsWith("debug: RefreshLoadingDocks: 0 loading dock(s) attached"));
    }

    [Fact]
    public void ArrivalWithoutDeliveryIsNotHeld()
    {
        Assert.False(DeliveryDestinationRuntime.TryHold(null, default));
    }
}
