#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1ItemSlotList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.ItemFramework.ItemSlot>;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1ItemSlotList = System.Collections.Generic.List<ScheduleOne.ItemFramework.ItemSlot>;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Items;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Property;
using S1API.Vehicles;

namespace S1API.Deliveries
{
    /// <summary>
    /// Provides read-only access to a property's native loading dock.
    /// </summary>
    /// <remarks>
    /// Scalar properties reflect the live dock state. Slot collections are immutable
    /// snapshots containing live <see cref="ItemSlotInstance"/> wrappers. Instances are
    /// cached for the loaded scene so property and delivery lookups share event subscriptions.
    /// Resolve the dock again after a scene or save transition. Events report state observed
    /// by the local peer and do not add network replication.
    /// </remarks>
    public sealed class LoadingDock
    {
        private static readonly Log Logger = new Log("LoadingDock");
        private static readonly Dictionary<int, LoadingDock> Cache = new Dictionary<int, LoadingDock>();
        private static readonly IReadOnlyList<ItemSlotInstance> EmptySlots =
            new ReadOnlyCollection<ItemSlotInstance>(Array.Empty<ItemSlotInstance>());
        private static bool _lifecycleHooked;

        internal LoadingDock(S1Delivery.LoadingDock loadingDock)
        {
            S1LoadingDock = loadingDock ?? throw new ArgumentNullException(nameof(loadingDock));
        }

        /// <summary>
        /// INTERNAL: Gets the native loading dock represented by this wrapper.
        /// </summary>
        internal S1Delivery.LoadingDock S1LoadingDock { get; }

        /// <summary>
        /// Gets the stable GUID assigned to this loading dock.
        /// </summary>
        public string GUID => S1LoadingDock.GUID.ToString();

        /// <summary>
        /// Gets the display name assigned by the owning property.
        /// </summary>
        public string Name
        {
            get
            {
                try
                {
                    return S1LoadingDock.Name ?? string.Empty;
                }
                catch
                {
                    return S1LoadingDock.gameObject?.name ?? string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the owning property, or <see langword="null"/> while it is unavailable.
        /// </summary>
        public PropertyWrapper? Property
        {
            get
            {
                try
                {
                    var property = S1LoadingDock.ParentProperty;
                    return property == null ? null : new PropertyWrapper(property);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets an immutable snapshot of the dock's input slots.
        /// </summary>
        /// <remarks>
        /// The collection cannot be modified, but each contained slot retains the behavior
        /// of the existing <see cref="ItemSlotInstance"/> API.
        /// </remarks>
        public IReadOnlyList<ItemSlotInstance> InputSlots =>
            SnapshotSlots(S1LoadingDock.InputSlots);

        /// <summary>
        /// Gets an immutable snapshot of the dock's output slots.
        /// </summary>
        /// <remarks>
        /// The collection cannot be modified, but each contained slot retains the behavior
        /// of the existing <see cref="ItemSlotInstance"/> API.
        /// </remarks>
        public IReadOnlyList<ItemSlotInstance> OutputSlots =>
            SnapshotSlots(S1LoadingDock.OutputSlots);

        /// <summary>
        /// Gets whether the dock currently accepts incoming transit items.
        /// </summary>
        public bool IsAcceptingItems => S1LoadingDock.IsAcceptingItems;

        /// <summary>
        /// Gets whether the dock has been removed from transit routing.
        /// </summary>
        public bool IsDestroyed => S1LoadingDock.IsDestroyed;

        /// <summary>
        /// Gets whether a dynamic or static vehicle currently occupies the dock.
        /// </summary>
        public bool IsInUse => S1LoadingDock.IsInUse;

        /// <summary>
        /// Gets the vehicle detected in the dock, or <see langword="null"/> when none is present.
        /// </summary>
        public LandVehicle? DynamicOccupant =>
            VehicleRegistry.Wrap(S1LoadingDock.DynamicOccupant);

        /// <summary>
        /// Gets the delivery vehicle assigned to the dock, or <see langword="null"/> when none is assigned.
        /// </summary>
        public LandVehicle? StaticOccupant =>
            VehicleRegistry.Wrap(S1LoadingDock.StaticOccupant);

        /// <summary>
        /// Raised after the dynamically detected vehicle changes.
        /// </summary>
        public event Action<LandVehicle?, LandVehicle?>? DynamicOccupantChanged;

        /// <summary>
        /// Raised after the assigned delivery vehicle changes.
        /// </summary>
        public event Action<LandVehicle?, LandVehicle?>? StaticOccupantChanged;

        /// <summary>
        /// Raised after the accepting-items state changes.
        /// </summary>
        public event Action<bool, bool>? AcceptingItemsChanged;

        internal static LoadingDock Wrap(S1Delivery.LoadingDock native)
        {
            EnsureLifecycleHook();
            int key = native.GetInstanceID();
            if (!Cache.TryGetValue(key, out LoadingDock? loadingDock)
                || loadingDock.S1LoadingDock != native)
            {
                loadingDock = new LoadingDock(native);
                Cache[key] = loadingDock;
            }

            return loadingDock;
        }

        internal void NotifyDynamicOccupantChanged(LandVehicle? previous, LandVehicle? current)
        {
            if (ReferenceEquals(previous, current))
                return;

            Invoke(DynamicOccupantChanged, previous, current, nameof(DynamicOccupantChanged));
        }

        internal void NotifyStaticOccupantChanged(LandVehicle? previous, LandVehicle? current)
        {
            if (ReferenceEquals(previous, current))
                return;

            Invoke(StaticOccupantChanged, previous, current, nameof(StaticOccupantChanged));
        }

        internal void NotifyAcceptingItemsChanged(bool previous, bool current)
        {
            if (previous == current)
                return;

            Invoke(AcceptingItemsChanged, previous, current, nameof(AcceptingItemsChanged));
        }

        private static IReadOnlyList<ItemSlotInstance> SnapshotSlots(S1ItemSlotList? nativeSlots)
        {
            if (nativeSlots == null || nativeSlots.Count == 0)
                return EmptySlots;

            var slots = new List<ItemSlotInstance>(nativeSlots.Count);
            for (int i = 0; i < nativeSlots.Count; i++)
            {
                if (nativeSlots[i] != null)
                    slots.Add(new ItemSlotInstance(nativeSlots[i]));
            }

            return slots.Count == 0
                ? EmptySlots
                : new ReadOnlyCollection<ItemSlotInstance>(slots);
        }

        private static void Invoke<T>(
            Action<T, T>? handlers,
            T previous,
            T current,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T, T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(previous, current);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Logger.Warning($"A {eventName} subscriber failed: {ex.Message}");
                    }
                    catch
                    {
                        // Logging must not prevent the remaining subscribers from running.
                    }
                }
            }
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;

            GameLifecycle.OnPreSceneChange += Cache.Clear;
            _lifecycleHooked = true;
        }
    }
}
