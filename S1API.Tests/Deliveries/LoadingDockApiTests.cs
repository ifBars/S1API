using System.Reflection;
using S1API.Deliveries;
using S1API.Items;
using S1API.Property;
using S1API.Vehicles;

#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
#endif

namespace S1API.Tests.Deliveries;

public sealed class LoadingDockApiTests
{
    [Fact]
    public void NativeTransitionPatchPointsExistInTargetRuntime()
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        Assert.NotNull(typeof(S1Delivery.LoadingDock).GetMethod(
            "SetOccupant",
            flags));
        Assert.NotNull(typeof(S1Delivery.LoadingDock).GetMethod(
            nameof(S1Delivery.LoadingDock.SetStaticOccupant),
            flags));
        Assert.NotNull(typeof(S1Delivery.LoadingDock).GetMethod(
            "set_IsAcceptingItems",
            flags));
    }

    [Fact]
    public void WrapperSurfaceIsReadOnlyAndManaged()
    {
        Assert.Empty(typeof(LoadingDock).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance));
        Assert.All(
            typeof(LoadingDock).GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.Null(property.SetMethod));

        Assert.Equal(typeof(string), GetProperty(nameof(LoadingDock.GUID)).PropertyType);
        Assert.Equal(typeof(string), GetProperty(nameof(LoadingDock.Name)).PropertyType);
        Assert.Equal(typeof(PropertyWrapper), GetProperty(nameof(LoadingDock.Property)).PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<ItemSlotInstance>),
            GetProperty(nameof(LoadingDock.InputSlots)).PropertyType);
        Assert.Equal(
            typeof(IReadOnlyList<ItemSlotInstance>),
            GetProperty(nameof(LoadingDock.OutputSlots)).PropertyType);
        Assert.Equal(typeof(LandVehicle), GetProperty(nameof(LoadingDock.DynamicOccupant)).PropertyType);
        Assert.Equal(typeof(LandVehicle), GetProperty(nameof(LoadingDock.StaticOccupant)).PropertyType);

        Assert.DoesNotContain(
            typeof(LoadingDock).GetMembers(BindingFlags.Public | BindingFlags.Instance),
            ExposesNativeDeliveryType);
    }

    [Fact]
    public void PropertyAndDeliveryExposeLoadingDockNavigation()
    {
        Assert.Equal(
            typeof(IReadOnlyList<LoadingDock>),
            typeof(PropertyWrapper).GetProperty(nameof(PropertyWrapper.LoadingDocks))!.PropertyType);
        Assert.Equal(
            typeof(LoadingDock),
            typeof(Delivery).GetProperty(nameof(Delivery.LoadingDock))!.PropertyType);
    }

    [Fact]
    public void EventsExposeManagedPreviousAndCurrentValues()
    {
        Assert.Equal(
            typeof(Action<LandVehicle?, LandVehicle?>),
            GetEvent(nameof(LoadingDock.DynamicOccupantChanged)).EventHandlerType);
        Assert.Equal(
            typeof(Action<LandVehicle?, LandVehicle?>),
            GetEvent(nameof(LoadingDock.StaticOccupantChanged)).EventHandlerType);
        Assert.Equal(
            typeof(Action<bool, bool>),
            GetEvent(nameof(LoadingDock.AcceptingItemsChanged)).EventHandlerType);
    }

    [Fact]
    public void ManagedNotificationsSuppressNoOpsAndIsolateSubscribers()
    {
        var nativeDock = TestObjectFactory.CreateUninitialized<S1Delivery.LoadingDock>();
        var dock = new LoadingDock(nativeDock);
        var previous = TestObjectFactory.CreateUninitialized<LandVehicle>();
        var current = TestObjectFactory.CreateUninitialized<LandVehicle>();
        int dynamicCalls = 0;
        int staticCalls = 0;
        int acceptingCalls = 0;

        dock.DynamicOccupantChanged += (_, _) => throw new InvalidOperationException("expected");
        dock.DynamicOccupantChanged += (observedPrevious, observedCurrent) =>
        {
            Assert.Same(previous, observedPrevious);
            Assert.Same(current, observedCurrent);
            dynamicCalls++;
        };
        dock.StaticOccupantChanged += (_, _) => staticCalls++;
        dock.AcceptingItemsChanged += (observedPrevious, observedCurrent) =>
        {
            Assert.False(observedPrevious);
            Assert.True(observedCurrent);
            acceptingCalls++;
        };

        dock.NotifyDynamicOccupantChanged(previous, previous);
        dock.NotifyDynamicOccupantChanged(previous, current);
        dock.NotifyStaticOccupantChanged(current, current);
        dock.NotifyStaticOccupantChanged(previous, current);
        dock.NotifyAcceptingItemsChanged(false, false);
        dock.NotifyAcceptingItemsChanged(false, true);

        Assert.Equal(1, dynamicCalls);
        Assert.Equal(1, staticCalls);
        Assert.Equal(1, acceptingCalls);
    }

    private static PropertyInfo GetProperty(string name) =>
        typeof(LoadingDock).GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!;

    private static EventInfo GetEvent(string name) =>
        typeof(LoadingDock).GetEvent(name, BindingFlags.Public | BindingFlags.Instance)!;

    private static bool ExposesNativeDeliveryType(MemberInfo member)
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
                "ScheduleOne.Delivery",
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

internal static class LoadingDockApiCompileFixture
{
    internal static void Observe(
        PropertyWrapper property,
        Delivery delivery,
        LoadingDock dock)
    {
        IReadOnlyList<LoadingDock> propertyDocks = property.LoadingDocks;
        LoadingDock? selectedDock = delivery.LoadingDock;
        IReadOnlyList<ItemSlotInstance> inputSlots = dock.InputSlots;
        IReadOnlyList<ItemSlotInstance> outputSlots = dock.OutputSlots;
        LandVehicle? dynamicOccupant = dock.DynamicOccupant;
        LandVehicle? staticOccupant = dock.StaticOccupant;

        Action<LandVehicle?, LandVehicle?> vehicleHandler = (_, _) => { };
        Action<bool, bool> acceptingHandler = (_, _) => { };
        dock.DynamicOccupantChanged += vehicleHandler;
        dock.StaticOccupantChanged += vehicleHandler;
        dock.AcceptingItemsChanged += acceptingHandler;
        dock.DynamicOccupantChanged -= vehicleHandler;
        dock.StaticOccupantChanged -= vehicleHandler;
        dock.AcceptingItemsChanged -= acceptingHandler;

        _ = propertyDocks;
        _ = selectedDock;
        _ = inputSlots;
        _ = outputSlots;
        _ = dynamicOccupant;
        _ = staticOccupant;
    }
}
