#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1Vehicles = ScheduleOne.Vehicles;
#endif

using S1API.Deliveries;
using S1API.Vehicles;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Converts native loading-dock state transitions into managed wrapper events.
    /// </summary>
    internal static class LoadingDockEventBridge
    {
        internal static void NotifyDynamicOccupantChanged(
            S1Delivery.LoadingDock native,
            S1Vehicles.LandVehicle? previous,
            S1Vehicles.LandVehicle? current)
        {
            if (previous == current)
                return;

            LoadingDock.Wrap(native).NotifyDynamicOccupantChanged(
                VehicleRegistry.Wrap(previous),
                VehicleRegistry.Wrap(current));
        }

        internal static void NotifyStaticOccupantChanged(
            S1Delivery.LoadingDock native,
            S1Vehicles.LandVehicle? previous,
            S1Vehicles.LandVehicle? current)
        {
            if (previous == current)
                return;

            LoadingDock.Wrap(native).NotifyStaticOccupantChanged(
                VehicleRegistry.Wrap(previous),
                VehicleRegistry.Wrap(current));
        }

        internal static void NotifyAcceptingItemsChanged(
            S1Delivery.LoadingDock native,
            bool previous,
            bool current)
        {
            if (previous == current)
                return;

            LoadingDock.Wrap(native).NotifyAcceptingItemsChanged(previous, current);
        }
    }
}
