#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Property = Il2CppScheduleOne.Property;
using S1PropertyList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Property.Property>;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Property = ScheduleOne.Property;
using S1PropertyList = System.Collections.Generic.List<ScheduleOne.Property.Property>;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using S1API.Deliveries;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Property;
using UnityEngine;

namespace S1API.Internal.Deliveries
{
    /// <summary>
    /// Mod-registered delivery destinations, loading docks and arrival holds.
    /// </summary>
    /// <remarks>
    /// Vanilla builds the destination list from owned properties, indexes <c>Property.LoadingDocks</c> without a
    /// bounds check in the bay check and on arrival, and activates the delivery van as soon as the status turns
    /// "arrived". A held arrival keeps that status but leaves the van inactive; it is taken out of
    /// <c>DeliveryManager.OnTimePass</c> while held, because that tick reads the van of every arrived delivery.
    /// Mod docks keep their index for the session through <see cref="DockSlotBook{TDock}"/>, because deliveries and
    /// saves store the dock as an index.
    /// </remarks>
    internal static class DeliveryDestinationRuntime
    {
        private static readonly Log Logger = new Log("DeliveryDestinations");
        private static readonly List<DestinationEntry> Destinations = new List<DestinationEntry>();
        private static readonly List<DockEntry> Docks = new List<DockEntry>();
        private static readonly List<HoldEntry> Holds = new List<HoldEntry>();
        private static readonly Dictionary<string, HeldArrival> Held = new Dictionary<string, HeldArrival>(StringComparer.Ordinal);
        private static readonly List<S1Delivery.DeliveryInstance> PulledFromTick = new List<S1Delivery.DeliveryInstance>();
        private static readonly HashSet<string> Offered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);
        private static readonly DockSlotBook<S1Delivery.LoadingDock> Slots =
            new DockSlotBook<S1Delivery.LoadingDock>(SameDock, IsAlive, message => Info(message), message => Warn(message));
        private static bool _lifecycleHooked;

        internal static int DestinationCount => Destinations.Count;
        internal static int DockCount => Docks.Count;
        internal static int HoldCount => Holds.Count;
        internal static int HeldCount => Held.Count;
        internal static bool LifecycleHookedForTesting => _lifecycleHooked;

        // --- registration ---------------------------------------------------------------------------------

        internal static IDisposable AddDestination(PropertyWrapper property, Func<bool> available)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (available == null) throw new ArgumentNullException(nameof(available));

            var entry = new DestinationEntry(property.InnerProperty, property.PropertyCode, available, OwnerOf(available));
            Destinations.Add(entry);
            EnsureLifecycleHook();
            Info($"Delivery destination '{entry.Code}' registered by {entry.Owner}.");
            return new Registration(() =>
            {
                if (!Destinations.Remove(entry))
                    return;
                Offered.Remove(entry.Code);
                Info($"Delivery destination '{entry.Code}' from {entry.Owner} removed.");
            });
        }

        internal static IDisposable AddLoadingDock(PropertyWrapper property, Func<Transform?> dock)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (dock == null) throw new ArgumentNullException(nameof(dock));

            var entry = new DockEntry(property.InnerProperty, property.PropertyCode, dock, OwnerOf(dock));
            Docks.Add(entry);
            EnsureLifecycleHook();
            Info($"Loading dock provider for '{entry.Code}' registered by {entry.Owner}.");
            if (Attach(entry))
                RetryHeld("a loading dock was attached");
            return new Registration(() =>
            {
                if (!Docks.Remove(entry))
                    return;
                Detach(entry);
                Info($"Loading dock provider for '{entry.Code}' from {entry.Owner} removed.");
            });
        }

        internal static IDisposable AddArrivalHold(Func<Delivery, bool> hold)
        {
            if (hold == null) throw new ArgumentNullException(nameof(hold));

            var entry = new HoldEntry(hold, OwnerOf(hold));
            Holds.Add(entry);
            EnsureLifecycleHook();
            Info($"Delivery arrival hold registered by {entry.Owner}.");
            return new Registration(() =>
            {
                if (!Holds.Remove(entry))
                    return;
                Info($"Delivery arrival hold from {entry.Owner} removed.");
                RetryHeld("an arrival hold was removed");
            });
        }

        internal static void Refresh(string why)
        {
            int attached = 0;
            foreach (DockEntry entry in Docks.ToArray())
            {
                if (Attach(entry))
                    attached++;
            }

            Debug($"{why}: {attached} loading dock(s) attached, {Held.Count} held arrival(s).");
            RetryHeld(why);
        }

        // --- vanilla hooks --------------------------------------------------------------------------------

        /// <summary>DeliveryShop.GetPotentialDestinations postfix: appends the available registered destinations.</summary>
        internal static void AppendDestinations(S1PropertyList? list)
        {
            if (Destinations.Count == 0)
                return;
            if (list == null)
            {
                Warn("GetPotentialDestinations returned no list; registered delivery destinations were not added.");
                return;
            }

            foreach (DestinationEntry entry in Destinations.ToArray())
            {
                S1Property.Property? property = entry.Resolve();
                if (property == null)
                {
                    WarnOnce("destination:" + entry.Code, $"Delivery destination '{entry.Code}' is registered but its property is not loaded; it is not offered.");
                    continue;
                }

                bool available;
                try
                {
                    available = entry.Available();
                }
                catch (Exception ex)
                {
                    Fail($"Availability check of delivery destination '{entry.Code}' from {entry.Owner} threw; it is not offered. {ex}");
                    continue;
                }

                if (!available)
                {
                    if (Offered.Remove(entry.Code))
                        Info($"Delivery destination '{entry.Code}' is no longer offered.");
                    continue;
                }

                AttachDocksFor(entry.Code);
                // The availability check and the dock providers may have disposed this registration.
                if (!Destinations.Contains(entry))
                {
                    Info($"Delivery destination '{entry.Code}' from {entry.Owner} was removed during its availability check; it is not offered.");
                    continue;
                }

                if (!Contains(list, entry.Code))
                    list.Add(property);
                if (Offered.Add(entry.Code))
                    Info($"Delivery destination '{entry.Code}' offered with {DockLength(property)} loading dock(s).");
            }
        }

        /// <summary>DeliveryManager.IsLoadingBayFree prefix: true when the bay must count as busy instead of being read.</summary>
        internal static bool BlockBay(S1Property.Property? destination, int index)
        {
            if (destination == null)
            {
                WarnOnce("bay:null", "The bay check ran without a destination property; the bay counts as busy.");
                return true;
            }

            string code = destination.PropertyCode ?? string.Empty;
            AttachDocksFor(code);
            string key = "bay:" + code + ":" + index;
            if (DockExists(destination, index))
            {
                if (Warned.Remove(key))
                    Info($"Loading dock #{index + 1} of '{code}' exists now.");
                return IsBayHeld(code, index);
            }

            WarnOnce(key, $"Loading dock #{index + 1} of '{code}' does not exist; the bay counts as busy until it does.");
            return true;
        }

        /// <summary>DeliveryInstance.SetStatus prefix: true when the arrival is held and vanilla must not run.</summary>
        internal static bool TryHold(S1Delivery.DeliveryInstance? delivery, S1Delivery.EDeliveryStatus status)
        {
            if (delivery == null)
                return false;

            string id = delivery.DeliveryID ?? string.Empty;
            if (status != S1Delivery.EDeliveryStatus.Arrived)
            {
                if (Held.Remove(id))
                    Info($"Held delivery {id} moved to {status}; it is no longer held.");
                return false;
            }

            AttachDocksFor(delivery.DestinationCode ?? string.Empty);
            string? reason = HoldReason(delivery, out bool missingDock);
            if (reason == null)
            {
                if (Held.Remove(id))
                    Info($"Delivery {id} to '{delivery.DestinationCode}' is no longer held.");
                return false;
            }

            if (!Held.ContainsKey(id))
            {
                Remember(id, new HeldArrival(delivery, delivery.Status));
                string line = $"Delivery {id} to '{delivery.DestinationCode}' dock #{delivery.LoadingDockIndex + 1} held: {reason}.";
                if (missingDock) Warn(line);
                else Info(line);
            }

            delivery.Status = S1Delivery.EDeliveryStatus.Arrived;
            return true;
        }

        /// <summary>
        /// DeliveryManager.OnTimePass prefix: retries held arrivals and takes the rest out of the tick.
        /// Returns the deliveries this call took out, or null.
        /// </summary>
        internal static List<S1Delivery.DeliveryInstance>? BeforeTimePass(S1Delivery.DeliveryManager? manager)
        {
            if (Held.Count == 0)
                return null;
            if (manager?.Deliveries == null)
            {
                WarnOnce("tick:nomanager", $"The delivery tick ran without a delivery list; {Held.Count} held arrival(s) are not retried this minute.");
                return null;
            }

            RetryHeld("game minute");
            List<S1Delivery.DeliveryInstance>? pulled = null;
            foreach (HeldArrival held in Held.Values.ToArray())
            {
                if (manager.Deliveries.Contains(held.Delivery))
                {
                    manager.Deliveries.Remove(held.Delivery);
                    PulledFromTick.Add(held.Delivery);
                    (pulled ??= new List<S1Delivery.DeliveryInstance>()).Add(held.Delivery);
                }
            }

            if (pulled != null)
                Debug($"{pulled.Count} held delivery(ies) taken out of the delivery tick.");
            return pulled;
        }

        /// <summary>
        /// DeliveryManager.OnTimePass finalizer: puts back exactly the deliveries <see cref="BeforeTimePass"/> took
        /// out in the same call, also when the tick threw.
        /// </summary>
        internal static void AfterTimePass(S1Delivery.DeliveryManager? manager, List<S1Delivery.DeliveryInstance>? pulled, Exception? thrown)
        {
            if (pulled == null || pulled.Count == 0)
                return;

            foreach (S1Delivery.DeliveryInstance delivery in pulled)
                PulledFromTick.Remove(delivery);

            var deliveries = manager?.Deliveries;
            if (deliveries == null)
            {
                Fail($"{pulled.Count} held delivery(ies) could not be put back after the delivery tick: the delivery manager is gone.");
                return;
            }

            int restored = PutBack(pulled, deliveries.Contains, deliveries.Add);
            if (thrown != null)
                Warn($"{restored} held delivery(ies) put back after the delivery tick threw {thrown.GetType().Name}.");
            else
                Debug($"{restored} held delivery(ies) put back after the delivery tick.");
        }

        /// <summary>Adds every pulled item that is not in the list yet. Returns how many were added.</summary>
        internal static int PutBack<T>(IReadOnlyList<T> pulled, Func<T, bool> contains, Action<T> add)
        {
            int restored = 0;
            foreach (T item in pulled)
            {
                if (contains(item))
                    continue;
                add(item);
                restored++;
            }

            return restored;
        }

        // --- internals ------------------------------------------------------------------------------------

        private static string? HoldReason(S1Delivery.DeliveryInstance delivery, out bool missingDock)
        {
            missingDock = false;
            S1Property.Property? destination = FindProperty(delivery.DestinationCode);
            if (destination == null)
            {
                missingDock = true;
                return "its destination property is not loaded";
            }

            if (!DockExists(destination, delivery.LoadingDockIndex))
            {
                missingDock = true;
                return "its loading dock does not exist yet";
            }

            if (Holds.Count == 0)
                return null;

            var wrapped = new Delivery(delivery);
            foreach (HoldEntry hold in Holds.ToArray())
            {
                try
                {
                    if (hold.Hold(wrapped))
                        return "held by " + hold.Owner;
                }
                catch (Exception ex)
                {
                    Fail($"Arrival hold from {hold.Owner} threw for delivery {delivery.DeliveryID}; that hold is skipped. {ex}");
                }
            }

            // A hold callback may have disposed the dock or unloaded the destination while it ran.
            destination = FindProperty(delivery.DestinationCode);
            if (destination == null)
            {
                missingDock = true;
                return "its destination property is not loaded";
            }

            if (!DockExists(destination, delivery.LoadingDockIndex))
            {
                missingDock = true;
                return "its loading dock does not exist yet";
            }

            return null;
        }

        private static void RetryHeld(string why)
        {
            if (Held.Count == 0)
                return;

            foreach (KeyValuePair<string, HeldArrival> pair in Held.ToArray())
            {
                // A nested retry from a callback may already have released or replaced this entry.
                if (!IsCurrent(pair.Key, pair.Value))
                    continue;

                S1Delivery.DeliveryInstance delivery = pair.Value.Delivery;
                if (!IsActive(delivery))
                {
                    if (Claim(pair.Key, pair.Value))
                        Info($"Held delivery {pair.Key} is no longer active; it is no longer held.");
                    continue;
                }

                AttachDocksFor(delivery.DestinationCode ?? string.Empty);
                if (HoldReason(delivery, out _) != null)
                    continue;

                if (!Claim(pair.Key, pair.Value))
                {
                    Debug($"Held delivery {pair.Key} was handled by a nested retry; this retry skips it.");
                    continue;
                }

                try
                {
                    // Back to the status before the hold, so the status event reports the real change.
                    delivery.Status = pair.Value.PreviousStatus;
                    delivery.SetStatus(S1Delivery.EDeliveryStatus.Arrived);
                    if (Held.ContainsKey(pair.Key))
                        Info($"Held delivery {pair.Key} to '{delivery.DestinationCode}' was held again during its arrival ({why}).");
                    else
                        Info($"Held delivery {pair.Key} to '{delivery.DestinationCode}' arrives now ({why}).");
                }
                catch (Exception ex)
                {
                    bool arrived = ArrivalWentThrough(delivery);
                    if (AfterFailedArrival(pair.Key, pair.Value, arrived, why, ex))
                        RestoreArrivedStatus(delivery);
                }
            }
        }

        /// <summary>True while <paramref name="arrival"/> is still the held entry for <paramref name="id"/>.</summary>
        internal static bool IsCurrent(string id, HeldArrival arrival) =>
            Held.TryGetValue(id, out HeldArrival? current) && ReferenceEquals(current, arrival);

        /// <summary>Removes the held entry only when it is still <paramref name="arrival"/>. True when this call removed it.</summary>
        internal static bool Claim(string id, HeldArrival arrival) =>
            IsCurrent(id, arrival) && Held.Remove(id);

        /// <summary>
        /// Handles a SetStatus call that threw for a claimed arrival. Returns true when the arrival is held again
        /// and will be retried; false when it already went through or another call holds it.
        /// </summary>
        internal static bool AfterFailedArrival(string id, HeldArrival arrival, bool arrived, string why, Exception ex)
        {
            if (Held.ContainsKey(id))
            {
                Fail($"Held delivery {id} threw while it was held again ({why}); it stays held. {ex}");
                return false;
            }

            if (arrived)
            {
                Fail($"Held delivery {id} arrived, but a later step threw ({why}); it is not retried. {ex}");
                return false;
            }

            Remember(id, arrival);
            Fail($"Held delivery {id} could not arrive ({why}); it stays held and is retried. {ex}");
            return true;
        }

        /// <summary>Stores a held arrival and makes sure a scene change clears it.</summary>
        internal static void Remember(string id, HeldArrival arrival)
        {
            EnsureLifecycleHook();
            Held[id] = arrival;
        }

        /// <summary>Vanilla assigns the delivery van before it activates it, so a set van means the arrival ran.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ArrivalWentThrough(S1Delivery.DeliveryInstance delivery)
        {
            try
            {
                return delivery.ActiveVehicle != null;
            }
            catch (Exception ex)
            {
                Warn($"Could not read the delivery van of {delivery.DeliveryID}; the arrival counts as not done. {ex.Message}");
                return false;
            }
        }

        /// <summary>A held arrival keeps the status "arrived", so the save and the tick treat it as held.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RestoreArrivedStatus(S1Delivery.DeliveryInstance delivery)
        {
            try
            {
                delivery.Status = S1Delivery.EDeliveryStatus.Arrived;
            }
            catch (Exception ex)
            {
                Fail($"Status of held delivery {delivery.DeliveryID} could not be set back to arrived. {ex}");
            }
        }

        private static bool IsActive(S1Delivery.DeliveryInstance delivery)
        {
            if (PulledFromTick.Contains(delivery))
                return true;
            try
            {
                if (!S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.InstanceExists)
                    return false;
                var manager = S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.Instance;
                return manager?.Deliveries != null && manager.Deliveries.Contains(delivery);
            }
            catch (Exception ex)
            {
                Warn($"Could not read the active deliveries; delivery {delivery.DeliveryID} stays held. {ex.Message}");
                return true;
            }
        }

        private static bool IsBayHeld(string code, int index)
        {
            foreach (HeldArrival held in Held.Values)
            {
                if (string.Equals(held.Delivery.DestinationCode, code, StringComparison.OrdinalIgnoreCase)
                    && held.Delivery.LoadingDockIndex == index)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AttachDocksFor(string code)
        {
            if (Docks.Count == 0 || string.IsNullOrEmpty(code))
                return;

            bool attached = false;
            foreach (DockEntry entry in Docks.ToArray())
            {
                if (string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase) && Attach(entry))
                    attached = true;
            }

            if (attached)
                Debug($"Loading docks of '{code}' refreshed.");
        }

        /// <summary>
        /// Asks the provider and reconciles its answer with the registration's dock slot: the same dock keeps the
        /// slot, a new dock replaces it, null leaves a placeholder. True when a dock was attached now.
        /// </summary>
        private static bool Attach(DockEntry entry)
        {
            S1Property.Property? property = entry.Resolve();
            if (property == null)
            {
                Debug($"Loading dock for '{entry.Code}' not attached: the property is not loaded.");
                return false;
            }

            Transform? transform;
            try
            {
                transform = entry.Provider();
            }
            catch (Exception ex)
            {
                Fail($"Loading dock provider for '{entry.Code}' from {entry.Owner} threw. {ex}");
                return false;
            }

            // The provider may have disposed its own registration while it ran.
            if (!Docks.Contains(entry))
            {
                Info($"Loading dock provider for '{entry.Code}' from {entry.Owner} was removed while it ran; its dock is not attached.");
                return false;
            }

            S1Delivery.LoadingDock? dock = null;
            if (transform != null)
            {
                dock = FindDock(transform);
                if (dock == null)
                {
                    WarnOnce("nodock:" + entry.Code + ":" + transform.GetInstanceID(),
                        $"Loading dock provider for '{entry.Code}' from {entry.Owner} returned '{transform.name}', which has no LoadingDock; nothing attached.");
                    return false;
                }
            }

            List<S1Delivery.LoadingDock?> docks = CopyDocks(property);
            Func<int, S1Delivery.LoadingDock> placeholder = index => CreatePlaceholder(property, entry.Code, index);
            bool changed = Slots.Reserve(entry.Code, docks, placeholder);
            DockSlotChange change = Slots.Apply(entry.Code, entry, dock, dock == null ? null : IdentityOf(dock), docks, placeholder, out int slot);
            if (change == DockSlotChange.Attached || change == DockSlotChange.Released)
                changed = true;
            if (changed)
            {
                if (dock != null && change == DockSlotChange.Attached)
                    dock.ParentProperty = property;
                property.LoadingDocks = docks.ToArray();
            }

            switch (change)
            {
                case DockSlotChange.Attached:
                    Info($"Loading dock '{dock?.name}' attached to '{entry.Code}' as dock #{slot + 1} (from {entry.Owner}).");
                    return true;
                case DockSlotChange.Released:
                    Info($"Loading dock #{slot + 1} of '{entry.Code}' from {entry.Owner} is unavailable; its slot stays reserved.");
                    return false;
                case DockSlotChange.Shared:
                    Debug($"Loading dock '{dock?.name}' is already dock #{slot + 1} of '{entry.Code}'; {entry.Owner} uses it without owning it.");
                    return false;
                case DockSlotChange.Conflict:
                    WarnOnce("conflict:" + entry.Code + ":" + slot,
                        $"Loading dock provider for '{entry.Code}' from {entry.Owner} returned '{dock?.name}', which already is another dock of the property; it keeps dock #{slot + 1}.");
                    return false;
                default:
                    return false;
            }
        }

        private static void Detach(DockEntry entry)
        {
            S1Property.Property? property = entry.Resolve();
            List<S1Delivery.LoadingDock?>? docks = null;
            Func<int, S1Delivery.LoadingDock> placeholder = index => CreatePlaceholder(property!, entry.Code, index);
            bool changed = false;
            if (property != null)
            {
                docks = CopyDocks(property);
                changed = Slots.Reserve(entry.Code, docks, placeholder);
            }

            DockSlotRelease release = Slots.Release(entry.Code, entry, docks, placeholder, out int slot);
            if (property != null && docks != null && (changed || release == DockSlotRelease.Reserved || release == DockSlotRelease.StillUsed))
                property.LoadingDocks = docks.ToArray();

            switch (release)
            {
                case DockSlotRelease.Reserved:
                    Info($"Loading dock #{slot + 1} of '{entry.Code}' from {entry.Owner} removed; the slot stays reserved so later dock numbers do not move.");
                    break;
                case DockSlotRelease.StillUsed:
                    Info($"Loading dock #{slot + 1} of '{entry.Code}' from {entry.Owner} removed; another registration still uses that dock.");
                    break;
                case DockSlotRelease.NotOwned:
                    Info($"Loading dock #{slot + 1} of '{entry.Code}' from {entry.Owner} removed; S1API did not add that dock, so it stays.");
                    break;
                default:
                    Debug($"Loading dock provider for '{entry.Code}' from {entry.Owner} never had a dock slot.");
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static S1Delivery.LoadingDock? FindDock(Transform transform)
        {
            S1Delivery.LoadingDock? dock = transform.GetComponent<S1Delivery.LoadingDock>();
            if (dock == null)
                dock = transform.GetComponentInChildren<S1Delivery.LoadingDock>(true);
            return dock == null ? null : dock;
        }

        /// <summary>The dock's GUID as text, or null when it has none, so a reserved slot can recognize it.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string? IdentityOf(S1Delivery.LoadingDock dock)
        {
            try
            {
                string guid = dock.GUID.ToString();
                return string.IsNullOrEmpty(guid) || guid == Guid.Empty.ToString() ? null : guid;
            }
            catch (Exception ex)
            {
                Debug($"Loading dock '{dock.name}' has no readable GUID; its slot is matched by object only. {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds the stand-in for an empty dock slot: an inactive GameObject under the property with a LoadingDock
        /// that never runs Awake. Vanilla can read it like a dock (Name, IsInUse, VehicleDetector.Clear); the bay
        /// check and the arrival hold treat it as a missing dock, so no delivery is sent to it.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static S1Delivery.LoadingDock CreatePlaceholder(S1Property.Property property, string code, int index)
        {
            var go = new GameObject($"S1API reserved loading dock #{index + 1}");
            go.SetActive(false);
            go.transform.SetParent(property.transform, false);
            S1Delivery.LoadingDock dock = go.AddComponent<S1Delivery.LoadingDock>();
            dock.ParentProperty = property;
            dock.VehicleDetector = go.AddComponent<S1DevUtilities.VehicleDetector>();
            Debug($"Placeholder created for reserved loading dock #{index + 1} of '{code}'.");
            return dock;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SameDock(S1Delivery.LoadingDock a, S1Delivery.LoadingDock b) => a == b;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsAlive(S1Delivery.LoadingDock dock) => dock != null;

        /// <summary>Copies the dock array, null entries included, so existing indexes stay valid.</summary>
        private static List<S1Delivery.LoadingDock?> CopyDocks(S1Property.Property property)
        {
            var docks = new List<S1Delivery.LoadingDock?>();
            if (property.LoadingDocks == null)
                return docks;
            for (int i = 0; i < property.LoadingDocks.Length; i++)
                docks.Add(property.LoadingDocks[i]);
            return docks;
        }

        private static bool DockExists(S1Property.Property property, int index)
        {
            if (IsDockMissing(DockLength(property), index))
                return false;
            S1Delivery.LoadingDock dock = property.LoadingDocks[index];
            return IsAlive(dock) && !Slots.IsPlaceholder(dock);
        }

        private static int DockLength(S1Property.Property property) => property.LoadingDocks?.Length ?? 0;

        internal static bool IsDockMissing(int dockCount, int index) => index < 0 || index >= dockCount;

        private static bool Contains(S1PropertyList list, string code)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].PropertyCode, code, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        internal static S1Property.Property? FindProperty(string? code)
        {
            if (string.IsNullOrEmpty(code) || S1Property.Property.Properties == null)
                return null;
            var properties = S1Property.Property.Properties;
            for (int i = 0; i < properties.Count; i++)
            {
                S1Property.Property property = properties[i];
                if (property != null && string.Equals(property.PropertyCode, code, StringComparison.OrdinalIgnoreCase))
                    return property;
            }

            return null;
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;
            GameLifecycle.OnPreSceneChange += ResetSceneState;
            _lifecycleHooked = true;
        }

        private static void ResetSceneState()
        {
            if (Held.Count > 0)
                Info($"Scene change: {Held.Count} held delivery arrival(s) dropped; the save keeps them as arrived.");
            Held.Clear();
            PulledFromTick.Clear();
            Offered.Clear();
            Warned.Clear();
            // Slot indexes stay: saved deliveries keep pointing at them after the reload.
            Slots.ResetScene();
        }

        internal static string OwnerOf(Delegate handler)
        {
            Assembly? assembly = handler.Method?.DeclaringType?.Assembly;
            return assembly?.GetName().Name ?? "unknown mod";
        }

        // --- logging and test hooks -----------------------------------------------------------------------

        /// <summary>INTERNAL: Removes every registration and all held state. Only for tests.</summary>
        internal static void ResetForTesting()
        {
            Destinations.Clear();
            Docks.Clear();
            Holds.Clear();
            Held.Clear();
            PulledFromTick.Clear();
            Offered.Clear();
            Warned.Clear();
            Slots.Clear();
            if (_lifecycleHooked)
            {
                GameLifecycle.OnPreSceneChange -= ResetSceneState;
                _lifecycleHooked = false;
            }
        }

        /// <summary>INTERNAL: Runs the scene change cleanup. Only for tests.</summary>
        internal static void SceneChangeForTesting() => ResetSceneState();

        /// <summary>INTERNAL: Receives log lines instead of MelonLoader while set. Only for tests.</summary>
        internal static Action<string>? LogSinkForTesting { get; set; }

        private static void WarnOnce(string key, string message)
        {
            if (Warned.Add(key))
                Warn(message);
        }

        private static void Debug(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("debug: " + message);
            else Logger.Debug(message);
        }

        private static void Info(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("info: " + message);
            else Logger.Msg(message);
        }

        private static void Warn(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("warn: " + message);
            else Logger.Warning(message);
        }

        private static void Fail(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("error: " + message);
            else Logger.Error(message);
        }

        // --- entries --------------------------------------------------------------------------------------

        private abstract class PropertyEntry
        {
            private S1Property.Property? _property;

            protected PropertyEntry(S1Property.Property property, string code, string owner)
            {
                _property = property;
                Code = code ?? string.Empty;
                Owner = owner;
            }

            internal string Code { get; }
            internal string Owner { get; }

            /// <summary>The registered property, or the loaded one with the same code after a reload.</summary>
            internal S1Property.Property? Resolve()
            {
                if (_property != null)
                    return _property;
                _property = FindProperty(Code);
                return _property;
            }
        }

        private sealed class DestinationEntry : PropertyEntry
        {
            internal DestinationEntry(S1Property.Property property, string code, Func<bool> available, string owner)
                : base(property, code, owner)
            {
                Available = available;
            }

            internal Func<bool> Available { get; }
        }

        private sealed class DockEntry : PropertyEntry
        {
            internal DockEntry(S1Property.Property property, string code, Func<Transform?> provider, string owner)
                : base(property, code, owner)
            {
                Provider = provider;
            }

            internal Func<Transform?> Provider { get; }
        }

        private sealed class HoldEntry
        {
            internal HoldEntry(Func<Delivery, bool> hold, string owner)
            {
                Hold = hold;
                Owner = owner;
            }

            internal Func<Delivery, bool> Hold { get; }
            internal string Owner { get; }
        }

        internal sealed class HeldArrival
        {
            internal HeldArrival(S1Delivery.DeliveryInstance delivery, S1Delivery.EDeliveryStatus previousStatus)
            {
                Delivery = delivery;
                PreviousStatus = previousStatus;
            }

            internal S1Delivery.DeliveryInstance Delivery { get; }
            internal S1Delivery.EDeliveryStatus PreviousStatus { get; }
        }

        private sealed class Registration : IDisposable
        {
            private Action? _dispose;

            internal Registration(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Action? dispose = _dispose;
                _dispose = null;
                dispose?.Invoke();
            }
        }
    }
}
