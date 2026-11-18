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
        private static bool _lifecycleHooked;

        internal static int DestinationCount => Destinations.Count;
        internal static int DockCount => Docks.Count;
        internal static int HoldCount => Holds.Count;

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
                Held[id] = new HeldArrival(delivery, delivery.Status);
                string line = $"Delivery {id} to '{delivery.DestinationCode}' dock #{delivery.LoadingDockIndex + 1} held: {reason}.";
                if (missingDock) Warn(line);
                else Info(line);
            }

            delivery.Status = S1Delivery.EDeliveryStatus.Arrived;
            return true;
        }

        /// <summary>DeliveryManager.OnTimePass prefix: retries held arrivals and takes the rest out of the tick.</summary>
        internal static void BeforeTimePass(S1Delivery.DeliveryManager? manager)
        {
            RestorePulled(manager);
            if (Held.Count == 0 || manager?.Deliveries == null)
                return;

            RetryHeld("game minute");
            foreach (HeldArrival held in Held.Values.ToArray())
            {
                if (manager.Deliveries.Contains(held.Delivery))
                {
                    manager.Deliveries.Remove(held.Delivery);
                    PulledFromTick.Add(held.Delivery);
                }
            }
        }

        /// <summary>DeliveryManager.OnTimePass postfix: puts the held arrivals back.</summary>
        internal static void AfterTimePass(S1Delivery.DeliveryManager? manager) => RestorePulled(manager);

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

            return null;
        }

        private static void RetryHeld(string why)
        {
            if (Held.Count == 0)
                return;

            foreach (KeyValuePair<string, HeldArrival> pair in Held.ToArray())
            {
                S1Delivery.DeliveryInstance delivery = pair.Value.Delivery;
                if (!IsActive(delivery))
                {
                    Held.Remove(pair.Key);
                    Info($"Held delivery {pair.Key} is no longer active; it is no longer held.");
                    continue;
                }

                AttachDocksFor(delivery.DestinationCode ?? string.Empty);
                if (HoldReason(delivery, out _) != null)
                    continue;

                Held.Remove(pair.Key);
                try
                {
                    // Back to the status before the hold, so the status event reports the real change.
                    delivery.Status = pair.Value.PreviousStatus;
                    delivery.SetStatus(S1Delivery.EDeliveryStatus.Arrived);
                    Info($"Held delivery {pair.Key} to '{delivery.DestinationCode}' arrives now ({why}).");
                }
                catch (Exception ex)
                {
                    Fail($"Held delivery {pair.Key} could not arrive ({why}). {ex}");
                }
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

        private static void RestorePulled(S1Delivery.DeliveryManager? manager)
        {
            if (PulledFromTick.Count == 0)
                return;
            if (manager?.Deliveries == null)
            {
                Warn($"{PulledFromTick.Count} held delivery(ies) could not be put back: the delivery manager is gone.");
                PulledFromTick.Clear();
                return;
            }

            foreach (S1Delivery.DeliveryInstance delivery in PulledFromTick)
            {
                if (!manager.Deliveries.Contains(delivery))
                    manager.Deliveries.Add(delivery);
            }

            PulledFromTick.Clear();
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

        /// <summary>Asks the provider and appends its dock to the property. True when a dock was attached now.</summary>
        private static bool Attach(DockEntry entry)
        {
            S1Property.Property? property = entry.Resolve();
            if (property == null)
            {
                Debug($"Loading dock for '{entry.Code}' not attached: the property is not loaded.");
                return false;
            }

            if (entry.Attached != null && IndexOf(property, entry.Attached) >= 0)
                return false;

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

            if (transform == null)
                return false;

            S1Delivery.LoadingDock? dock = transform.GetComponent<S1Delivery.LoadingDock>();
            if (dock == null)
                dock = transform.GetComponentInChildren<S1Delivery.LoadingDock>(true);
            if (dock == null)
            {
                WarnOnce("nodock:" + entry.Code + ":" + transform.GetInstanceID(),
                    $"Loading dock provider for '{entry.Code}' from {entry.Owner} returned '{transform.name}', which has no LoadingDock; nothing attached.");
                return false;
            }

            entry.Attached = dock;
            int existing = IndexOf(property, dock);
            if (existing >= 0)
            {
                Debug($"Loading dock '{dock.name}' is already dock #{existing + 1} of '{entry.Code}'.");
                return false;
            }

            List<S1Delivery.LoadingDock> docks = CopyDocks(property);
            docks.Add(dock);
            dock.ParentProperty = property;
            property.LoadingDocks = docks.ToArray();
            Info($"Loading dock '{dock.name}' attached to '{entry.Code}' as dock #{docks.Count} (from {entry.Owner}).");
            return true;
        }

        private static void Detach(DockEntry entry)
        {
            S1Delivery.LoadingDock? dock = entry.Attached;
            entry.Attached = null;
            S1Property.Property? property = entry.Resolve();
            if (dock == null || property == null)
                return;

            int index = IndexOf(property, dock);
            if (index < 0)
                return;

            List<S1Delivery.LoadingDock> docks = CopyDocks(property);
            docks.RemoveAt(index);
            property.LoadingDocks = docks.ToArray();
            if (index < docks.Count)
                Warn($"Loading dock #{index + 1} of '{entry.Code}' removed; the docks after it moved up one index.");
            else
                Info($"Loading dock #{index + 1} of '{entry.Code}' removed.");
        }

        /// <summary>Copies the dock array, null entries included, so existing indexes stay valid.</summary>
        private static List<S1Delivery.LoadingDock> CopyDocks(S1Property.Property property)
        {
            var docks = new List<S1Delivery.LoadingDock>();
            if (property.LoadingDocks == null)
                return docks;
            for (int i = 0; i < property.LoadingDocks.Length; i++)
                docks.Add(property.LoadingDocks[i]);
            return docks;
        }

        private static int IndexOf(S1Property.Property property, S1Delivery.LoadingDock dock)
        {
            if (property.LoadingDocks == null)
                return -1;
            for (int i = 0; i < property.LoadingDocks.Length; i++)
            {
                if (property.LoadingDocks[i] == dock)
                    return i;
            }

            return -1;
        }

        private static bool DockExists(S1Property.Property property, int index) =>
            !IsDockMissing(DockLength(property), index) && property.LoadingDocks[index] != null;

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
            foreach (DockEntry entry in Docks)
                entry.Attached = null;
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
        }

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
            internal S1Delivery.LoadingDock? Attached { get; set; }
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

        private sealed class HeldArrival
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
