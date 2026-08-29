#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
#endif

using System;
using System.Collections.Generic;
using S1API.Property;
using S1API.Shops;
using S1API.Vehicles;

namespace S1API.Deliveries
{
    /// <summary>
    /// Provides read-only access to a currently active in-game delivery.
    /// </summary>
    /// <remarks>
    /// Scalar properties reflect the live delivery state. <see cref="Items"/> and
    /// <see cref="ToReceipt"/> return immutable snapshots.
    /// </remarks>
    public sealed class Delivery
    {
        internal Delivery(S1Delivery.DeliveryInstance delivery)
        {
            NativeDelivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
        }

        /// <summary>
        /// INTERNAL: Gets the native delivery represented by this wrapper.
        /// </summary>
        internal S1Delivery.DeliveryInstance NativeDelivery { get; }

        /// <summary>
        /// Gets the delivery's unique identifier.
        /// </summary>
        public string Id => NativeDelivery.DeliveryID ?? string.Empty;

        /// <summary>
        /// Gets the stable shop name that accepted the order.
        /// </summary>
        public string StoreName => NativeDelivery.StoreName ?? string.Empty;

        /// <summary>
        /// Gets the property code of the delivery destination.
        /// </summary>
        public string DestinationCode => NativeDelivery.DestinationCode ?? string.Empty;

        /// <summary>
        /// Gets the zero-based loading-dock index selected for the order.
        /// </summary>
        public int LoadingDockIndex => NativeDelivery.LoadingDockIndex;

        /// <summary>
        /// Gets the selected destination loading dock, or <see langword="null"/> while it is unavailable.
        /// </summary>
        public LoadingDock? LoadingDock
        {
            get
            {
                try
                {
                    var loadingDock = NativeDelivery.LoadingDock;
                    return loadingDock == null
                        ? null
                        : global::S1API.Deliveries.LoadingDock.Wrap(loadingDock);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the wrapped destination property, or <see langword="null"/> while it is unavailable.
        /// </summary>
        public PropertyWrapper? Destination
        {
            get
            {
                try
                {
                    var destination = NativeDelivery.Destination;
                    return destination == null ? null : new PropertyWrapper(destination);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the delivery's current lifecycle state.
        /// </summary>
        public DeliveryStatus Status => (DeliveryStatus)(int)NativeDelivery.Status;

        /// <summary>
        /// Gets the remaining in-game minutes before arrival.
        /// </summary>
        public int MinutesUntilArrival => NativeDelivery.TimeUntilArrival;

        /// <summary>
        /// Gets an immutable snapshot of the currently ordered items.
        /// </summary>
        public IReadOnlyList<DeliveryItem> Items
            => DeliveryItem.Snapshot(NativeDelivery.Items);

        /// <summary>
        /// Gets the shop associated with this delivery, or <see langword="null"/> while it is unavailable.
        /// </summary>
        public Shop? Shop => DeliveryRegistry.ResolveShop(StoreName);

        /// <summary>
        /// Gets the active delivery vehicle, or <see langword="null"/> until the delivery has arrived.
        /// </summary>
        public LandVehicle? ActiveVehicle
        {
            get
            {
                var nativeVehicle = NativeDelivery.ActiveVehicle?.Vehicle;
                return nativeVehicle == null ? null : new LandVehicle(nativeVehicle);
            }
        }

        /// <summary>
        /// Creates an immutable receipt snapshot of the current delivery.
        /// </summary>
        public DeliveryReceipt ToReceipt() => new DeliveryReceipt(NativeDelivery);
    }
}
