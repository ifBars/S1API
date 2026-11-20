#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1UIShop = Il2CppScheduleOne.UI.Shop;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1UIShop = ScheduleOne.UI.Shop;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Logging;
using S1API.Shops;

namespace S1API.Deliveries
{
    /// <summary>
    /// Discovers active deliveries and immutable delivery-history snapshots.
    /// </summary>
    /// <remarks>
    /// This API intentionally does not create, mutate, or complete deliveries. Those operations
    /// are server-authoritative native flows and are not exposed until they can be represented safely.
    /// </remarks>
    public static partial class DeliveryRegistry
    {
        private static readonly Log Logger = new Log("DeliveryRegistry");
        private static readonly IReadOnlyList<Delivery> EmptyDeliveries =
            new ReadOnlyCollection<Delivery>(Array.Empty<Delivery>());

        private static readonly IReadOnlyList<DeliveryReceipt> EmptyReceipts =
            new ReadOnlyCollection<DeliveryReceipt>(Array.Empty<DeliveryReceipt>());

        /// <summary>
        /// Raised after an active delivery is received by the game.
        /// </summary>
        public static event Action<Delivery>? Created;

        /// <summary>
        /// Raised after an active delivery changes lifecycle state.
        /// The second and third callback values are the previous and current states.
        /// </summary>
        public static event Action<Delivery, DeliveryStatus, DeliveryStatus>? StatusChanged;

        /// <summary>
        /// Raised after a delivery's native status becomes <see cref="DeliveryStatus.Completed"/>.
        /// </summary>
        public static event Action<Delivery>? Completed;

        /// <summary>
        /// Gets an immutable snapshot of all active deliveries.
        /// </summary>
        public static IReadOnlyList<Delivery> GetAll()
        {
            S1Delivery.DeliveryManager? manager = GetNativeManager();
            if (manager?.Deliveries == null)
                return EmptyDeliveries;

            var deliveries = new List<Delivery>();
            foreach (S1Delivery.DeliveryInstance nativeDelivery in manager.Deliveries)
            {
                if (nativeDelivery != null)
                    deliveries.Add(new Delivery(nativeDelivery));
            }

            return deliveries.Count == 0
                ? EmptyDeliveries
                : new ReadOnlyCollection<Delivery>(deliveries);
        }

        /// <summary>
        /// Finds an active delivery by its unique identifier.
        /// </summary>
        /// <param name="id">The delivery identifier to find.</param>
        /// <returns>The delivery wrapper, or <see langword="null"/> when no active delivery matches.</returns>
        public static Delivery? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            S1Delivery.DeliveryManager? manager = GetNativeManager();
            if (manager?.Deliveries == null)
                return null;

            foreach (S1Delivery.DeliveryInstance nativeDelivery in manager.Deliveries)
            {
                if (nativeDelivery != null
                    && string.Equals(nativeDelivery.DeliveryID, id, StringComparison.Ordinal))
                {
                    return new Delivery(nativeDelivery);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets an immutable snapshot of active deliveries placed with a shop.
        /// </summary>
        /// <param name="shop">The wrapped shop whose deliveries should be returned.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="shop"/> is null.</exception>
        public static IReadOnlyList<Delivery> GetForShop(Shop shop)
        {
            if (shop == null)
                throw new ArgumentNullException(nameof(shop));

            return GetForShop(shop.Name);
        }

        /// <summary>
        /// Gets an immutable snapshot of active deliveries placed with a shop name.
        /// </summary>
        /// <param name="shopName">The stable native shop name stored on delivery records.</param>
        public static IReadOnlyList<Delivery> GetForShop(string shopName)
        {
            if (string.IsNullOrWhiteSpace(shopName))
                return EmptyDeliveries;

            S1Delivery.DeliveryManager? manager = GetNativeManager();
            if (manager?.Deliveries == null)
                return EmptyDeliveries;

            var deliveries = new List<Delivery>();
            foreach (S1Delivery.DeliveryInstance nativeDelivery in manager.Deliveries)
            {
                if (nativeDelivery != null
                    && string.Equals(nativeDelivery.StoreName, shopName, StringComparison.Ordinal))
                {
                    deliveries.Add(new Delivery(nativeDelivery));
                }
            }

            return deliveries.Count == 0
                ? EmptyDeliveries
                : new ReadOnlyCollection<Delivery>(deliveries);
        }

        /// <summary>
        /// Gets an immutable snapshot of the delivery receipts visible in the in-game order history.
        /// </summary>
        public static IReadOnlyList<DeliveryReceipt> GetHistory()
        {
            S1Delivery.DeliveryManager? manager = GetNativeManager();
            if (manager == null)
                return EmptyReceipts;

            var nativeHistory = manager.DisplayedDeliveryHistory;
            if (nativeHistory == null)
                return EmptyReceipts;

            var receipts = new List<DeliveryReceipt>();
            foreach (S1Delivery.DeliveryReceipt nativeReceipt in nativeHistory)
            {
                if (nativeReceipt != null)
                    receipts.Add(new DeliveryReceipt(nativeReceipt));
            }

            return receipts.Count == 0
                ? EmptyReceipts
                : new ReadOnlyCollection<DeliveryReceipt>(receipts);
        }

        internal static Shop? ResolveShop(string shopName)
        {
            if (string.IsNullOrWhiteSpace(shopName))
                return null;

            try
            {
                if (S1UIShop.ShopInterface.AllShops == null)
                    return null;

                foreach (S1UIShop.ShopInterface nativeShop in S1UIShop.ShopInterface.AllShops)
                {
                    if (nativeShop != null
                        && string.Equals(nativeShop.ShopName, shopName, StringComparison.Ordinal))
                    {
                        return new Shop(nativeShop);
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        internal static void NotifyCreated(S1Delivery.DeliveryInstance delivery)
        {
            if (delivery == null || Created == null)
                return;

            var wrapped = new Delivery(delivery);
            foreach (Action<Delivery> handler in Created.GetInvocationList())
            {
                try
                {
                    handler(wrapped);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"A DeliveryRegistry.Created subscriber failed: {ex.Message}");
                }
            }
        }

        internal static void NotifyStatusChanged(
            S1Delivery.DeliveryInstance delivery,
            DeliveryStatus previousStatus,
            DeliveryStatus currentStatus)
        {
            if (delivery == null || previousStatus == currentStatus || StatusChanged == null)
                return;

            var wrapped = new Delivery(delivery);
            foreach (Action<Delivery, DeliveryStatus, DeliveryStatus> handler in StatusChanged.GetInvocationList())
            {
                try
                {
                    handler(wrapped, previousStatus, currentStatus);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"A DeliveryRegistry.StatusChanged subscriber failed: {ex.Message}");
                }
            }
        }

        internal static void NotifyCompleted(S1Delivery.DeliveryInstance delivery)
        {
            if (delivery == null || Completed == null)
                return;

            var wrapped = new Delivery(delivery);
            foreach (Action<Delivery> handler in Completed.GetInvocationList())
            {
                try
                {
                    handler(wrapped);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"A DeliveryRegistry.Completed subscriber failed: {ex.Message}");
                }
            }
        }

        private static S1Delivery.DeliveryManager? GetNativeManager()
        {
            try
            {
                if (!S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.InstanceExists)
                    return null;

                return S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.Instance;
            }
            catch
            {
                return null;
            }
        }
    }
}
