#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1Property = Il2CppScheduleOne.Property;
using S1PropertyList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Property.Property>;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using Il2CppFishNet;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Persistence = ScheduleOne.Persistence;
using S1Property = ScheduleOne.Property;
using S1PropertyList = System.Collections.Generic.List<ScheduleOne.Property.Property>;
using S1Vehicles = ScheduleOne.Vehicles;
using FishNet;
using HarmonyLib;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        private static readonly List<S1Delivery.DeliveryInstance> HeldDuringTick = new List<S1Delivery.DeliveryInstance>();
        private static bool _lifecycleHooked;
        private static int _tickDepth;
        private static int _dockUiRefreshDepth;
        // Instance IDs of the dock dropdowns that already carry the S1API listener.
        private static readonly HashSet<int> ListenedDropdowns = new HashSet<int>();
        // Arrival already checked by the SetDeliveryState prefix; the SetStatus prefix of the same call skips the holds.
        private static string? _checkedArrivalId;
        // Dock dropdown options as last shown, per delivery shop: option i (1-based) stands for dock index Docks[i - 1].
        private static readonly Dictionary<int, ShownDockList> ShownDocks = new Dictionary<int, ShownDockList>();
        // Held deliveries a retry is deciding right now; a nested retry from a provider or hold callback skips them.
        private static readonly HashSet<string> InRetry = new HashSet<string>(StringComparer.Ordinal);
        // Dock registrations whose provider is running right now; a nested refresh does not ask them again.
        private static readonly HashSet<object> ProvidersRunning = new HashSet<object>();
        // A host release found its delivery taken out of the delivery tick; the tick's finalizer retries it.
        private static bool _retryAfterTick;
        // DockSlotBook.Moves when the slot moves were last taken.
        private static int _seenMoves;
        // Deliveries that follow a moved dock slot, kept across scene reloads of the same save.
        private static readonly DockMoveFollower Followed = new DockMoveFollower();
        // True from the load-complete event of a save until the next scene change; saved deliveries exist then.
        private static bool _saveLoaded;
        // Counts scene changes; a co-op client's save key changes with it, so a hosted save never inherits its slots.
        private static int _sceneGeneration;
        // Delivery whose release a retry decided; its arrival does not ask the holds a second time.
        private static string? _releasingId;
        // Destination codes the delivery app listed in its last destination refresh, per delivery shop.
        private static readonly Dictionary<int, HashSet<string>> ShownDestinations = new Dictionary<int, HashSet<string>>();
        private static int _destinationUiRefreshDepth;
#if MONOMELON
        private static readonly MethodInfo? SetDeliveryStateMethod =
            AccessTools.Method(typeof(S1Delivery.DeliveryManager), "SetDeliveryState");
        private static readonly FieldInfo? ShopDestinationField =
            AccessTools.Field(typeof(S1DeliveryUi.DeliveryShop), "destinationProperty");
        private static readonly FieldInfo? ShopDockIndexField =
            AccessTools.Field(typeof(S1DeliveryUi.DeliveryShop), "loadingDockIndex");
        private static readonly MethodInfo? ActiveVehicleSetter =
            AccessTools.PropertySetter(typeof(S1Delivery.DeliveryInstance), nameof(S1Delivery.DeliveryInstance.ActiveVehicle));
#endif

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

        /// <summary>DeliveryShop.RefreshDestinationUI prefix: the destination list built now is the one the player sees.</summary>
        internal static void BeginDestinationUiRefresh() => _destinationUiRefreshDepth++;

        /// <summary>DeliveryShop.RefreshDestinationUI finalizer.</summary>
        internal static void EndDestinationUiRefresh() => _destinationUiRefreshDepth = Math.Max(0, _destinationUiRefreshDepth - 1);

        /// <summary>
        /// DeliveryShop.GetPotentialDestinations postfix: appends the available registered destinations. Inside a
        /// destination refresh the availability checks run and their answer is kept for <paramref name="shop"/>; a call
        /// outside it (vanilla maps a picked option through a second list) reuses that answer, so an availability
        /// change between the two lists does not move the pick to another property.
        /// </summary>
        internal static void AppendDestinations(S1PropertyList? list, S1DeliveryUi.DeliveryShop? shop = null)
        {
            if (Destinations.Count == 0)
                return;
            if (list == null)
            {
                Warn("GetPotentialDestinations returned no list; registered delivery destinations were not added.");
                return;
            }

            int shopId = shop == null ? 0 : shop.GetInstanceID();
            HashSet<string>? shown = null;
            HashSet<string>? record = null;
            if (shop != null)
            {
                if (_destinationUiRefreshDepth > 0)
                {
                    record = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    ShownDestinations[shopId] = record;
                }
                else
                {
                    ShownDestinations.TryGetValue(shopId, out shown);
                }
            }

            foreach (DestinationEntry entry in Destinations.ToArray())
            {
                S1Property.Property? property = entry.Resolve();
                if (property == null)
                {
                    WarnOnce("destination:" + entry.Code, $"Delivery destination '{entry.Code}' is registered but its property is not loaded; it is not offered.");
                    continue;
                }

                if (shown != null)
                {
                    // The list the player picked from: the same registered destinations, without asking again.
                    if (shown.Contains(entry.Code) && !Contains(list, entry.Code))
                        list.Add(property);
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
                record?.Add(entry.Code);
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
            int moves = Slots.Moves;
            AttachDocksFor(code);
            if (Slots.Moves != moves)
            {
                // The delivery asking may just have followed its slot to another index; the tick still holds the old one.
                Info($"Loading dock slots of '{code}' moved during the bay check of dock #{index + 1}; the bay counts as busy this minute.");
                return true;
            }

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

        // --- delivery app dock list ----------------------------------------------------------------------
        // Reserved slots hold placeholders so later dock numbers do not move. The delivery app must not offer them:
        // a delivery ordered to one would be paid and then wait at a dock that does not exist. The dock dropdown
        // therefore lists only live docks, labelled with their real dock numbers, and maps its options back.

        /// <summary>DeliveryShop.RefreshLoadingDockUI prefix: vanilla fills the dropdown 1:1, so selections pass through.</summary>
        internal static void BeginDockUiRefresh() => _dockUiRefreshDepth++;

        /// <summary>
        /// DeliveryShop.RefreshLoadingDockUI finalizer: when the destination has reserved docks, rebuilds the dock
        /// dropdown with the live docks only and moves a selection off a reserved dock.
        /// </summary>
        internal static void EndDockUiRefresh(S1DeliveryUi.DeliveryShop? shop)
        {
            _dockUiRefreshDepth = Math.Max(0, _dockUiRefreshDepth - 1);
            if (shop == null)
            {
                Warn("The delivery app dock list was refreshed without a shop; reserved docks are not filtered.");
                return;
            }

            S1Property.Property? property = ShopDestination(shop);
            if (property == null)
            {
                ShownDocks.Remove(shop.GetInstanceID());
                return;
            }

            UnityEngine.UI.Dropdown? dropdown = shop.LoadingDockDropdown;
            if (dropdown == null)
            {
                WarnOnce("shop:nodropdown", "The delivery app has no loading dock dropdown; reserved docks are not filtered.");
                return;
            }
            ListenToDropdown(shop, dropdown);
            ShowDocks(shop, property, dropdown, rebuild: false);
        }

        /// <summary>
        /// Stores the docks the dropdown shows for <paramref name="shop"/>. When a dock is reserved, or when
        /// <paramref name="rebuild"/> is set, fills the dropdown with the live docks only; otherwise vanilla's 1:1
        /// options stand.
        /// </summary>
        private static void ShowDocks(S1DeliveryUi.DeliveryShop shop, S1Property.Property property, UnityEngine.UI.Dropdown dropdown, bool rebuild)
        {
            List<int> live = LiveDocks(property);
            int length = DockLength(property);
            var objects = new S1Delivery.LoadingDock?[live.Count];
            for (int i = 0; i < live.Count; i++)
                objects[i] = property.LoadingDocks[live[i]];
            ShownDocks[shop.GetInstanceID()] = new ShownDockList(property.PropertyCode ?? string.Empty, live.ToArray(), objects);
            if (live.Count == length && !rebuild)
                return;

            int current = ShopDockNumber(shop);
            int option = OptionOfDock(live, current);
            if (option == 0 && current != 0 && live.Count > 0)
                option = 1;

            dropdown.ClearOptions();
            dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData("-"));
            foreach (int index in live)
                dropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData((index + 1).ToString()));
            dropdown.SetValueWithoutNotify(option);
            dropdown.RefreshShownValue();
            SetShopDockNumber(shop, DockOfOption(live, option));
            Debug($"Delivery app offers {live.Count} of {length} loading dock(s) of '{property.PropertyCode}'; reserved docks are hidden.");
        }

        // Vanilla adds its own listener in DeliveryShop.Initialize, before the first dock refresh, so this one runs
        // after it and overwrites the option index vanilla stored with the dock the option stands for.
        private static void ListenToDropdown(S1DeliveryUi.DeliveryShop shop, UnityEngine.UI.Dropdown dropdown)
        {
            if (!ListenedDropdowns.Add(dropdown.GetInstanceID()))
                return;
            dropdown.onValueChanged.AddListener((UnityEngine.Events.UnityAction<int>)delegate (int option)
            {
                try
                {
                    SelectDockOption(shop, option);
                }
                catch (Exception ex)
                {
                    Fail($"Delivery app dock option {option} could not be mapped to a loading dock; the order uses the option as vanilla does. {ex}");
                }
            });
            Debug("Delivery app dock dropdown listener added.");
        }

        /// <summary>
        /// Dock dropdown listener: maps the picked option through the docks the dropdown showed, because the event
        /// names an option, not a dock. When that dock is gone since, the choice is reset and the list rebuilt instead
        /// of picking another dock. False when the dropdown was never shown by S1API and the vanilla value stands.
        /// </summary>
        internal static bool SelectDockOption(S1DeliveryUi.DeliveryShop? shop, int option)
        {
            if (_dockUiRefreshDepth > 0 || shop == null)
                return false;
            if (!ShownDocks.TryGetValue(shop.GetInstanceID(), out ShownDockList? shown))
            {
                Debug($"Delivery app dock option {option} picked before S1API saw the dock list; the vanilla value stands.");
                return false;
            }

            S1Property.Property? property = ShopDestination(shop);
            if (property == null || !string.Equals(property.PropertyCode, shown.Code, StringComparison.OrdinalIgnoreCase))
            {
                Debug($"Delivery app dock option {option} belongs to another destination than the one now selected; the vanilla value stands.");
                return false;
            }

            // The dock counts as gone when its number now holds another dock object than the one the player saw.
            S1Delivery.LoadingDock? seen = option >= 1 && option <= shown.Objects.Length ? shown.Objects[option - 1] : null;
            int number = PickedDock(shown.Docks, option, index => DockExists(property, index) && IsSameDock(property, index, seen), out bool gone);
            SetShopDockNumber(shop, number);
            if (!gone)
                return true;

            Info($"Delivery app: dock #{DockOfOption(shown.Docks, option)} of '{shown.Code}' is gone since the list was shown; the dock choice is reset.");
            UnityEngine.UI.Dropdown? dropdown = shop.LoadingDockDropdown;
            if (dropdown == null)
            {
                WarnOnce("shop:nodropdown", "The delivery app has no loading dock dropdown; reserved docks are not filtered.");
                return true;
            }

            _dockUiRefreshDepth++;
            try
            {
                ShowDocks(shop, property, dropdown, rebuild: true);
            }
            finally
            {
                _dockUiRefreshDepth = Math.Max(0, _dockUiRefreshDepth - 1);
            }

            return true;
        }

        /// <summary>
        /// The dock number (1-based, 0 = none) of dropdown option <paramref name="option"/> in the list the player saw.
        /// <paramref name="gone"/> is true when that dock is no longer live; the result is then 0.
        /// </summary>
        internal static int PickedDock(IReadOnlyList<int> shown, int option, Func<int, bool> isLive, out bool gone)
        {
            int number = DockOfOption(shown, option);
            gone = number > 0 && !isLive(number - 1);
            return gone ? 0 : number;
        }

        /// <summary>
        /// DeliveryShop.SubmitOrder prefix: true when the selected dock is a reserved or missing dock and the order
        /// must not go out. Asks the dock providers first, so a dock whose provider returns null now counts as missing.
        /// Covers reorders of old receipts, which set the dock without the dropdown.
        /// </summary>
        internal static bool BlockOrder(S1DeliveryUi.DeliveryShop? shop)
        {
            if (shop == null)
                return false;
            S1Property.Property? property = ShopDestination(shop);
            int number = ShopDockNumber(shop);
            if (property == null || number <= 0)
                return false;
            AttachDocksFor(property.PropertyCode ?? string.Empty);
            if (!DockExists(property, number - 1))
            {
                Warn($"Delivery order to '{property.PropertyCode}' dock #{number} blocked: that dock is reserved or missing, so the order is not placed and not paid.");
                return true;
            }

            // A dock picked from the dropdown must still be the dock the player saw under that number.
            if (ShownDocks.TryGetValue(shop.GetInstanceID(), out ShownDockList? shown)
                && string.Equals(shown.Code, property.PropertyCode, StringComparison.OrdinalIgnoreCase))
            {
                int position = Array.IndexOf(shown.Docks, number - 1);
                if (position >= 0 && !IsSameDock(property, number - 1, shown.Objects[position]))
                {
                    Warn($"Delivery order to '{property.PropertyCode}' dock #{number} blocked: another dock took that number since the list was shown, so the order is not placed and not paid.");
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameDock(S1Property.Property property, int index, S1Delivery.LoadingDock? seen)
        {
            if (seen == null || IsDockMissing(DockLength(property), index))
                return false;
            S1Delivery.LoadingDock now = property.LoadingDocks[index];
            return now != null && SameDock(now, seen);
        }

        /// <summary>The live dock indexes of <paramref name="property"/>, in dock order.</summary>
        private static List<int> LiveDocks(S1Property.Property property)
        {
            int length = DockLength(property);
            var live = new List<int>(length);
            for (int i = 0; i < length; i++)
            {
                if (DockExists(property, i))
                    live.Add(i);
            }

            return live;
        }

        /// <summary>The dropdown option (1-based, 0 = none) that shows dock number <paramref name="dockNumber"/>.</summary>
        internal static int OptionOfDock(IReadOnlyList<int> offered, int dockNumber)
        {
            for (int i = 0; i < offered.Count; i++)
            {
                if (offered[i] + 1 == dockNumber)
                    return i + 1;
            }

            return 0;
        }

        /// <summary>The dock number (1-based, 0 = none) that dropdown option <paramref name="option"/> shows.</summary>
        internal static int DockOfOption(IReadOnlyList<int> offered, int option) =>
            option >= 1 && option <= offered.Count ? offered[option - 1] + 1 : 0;

        private static S1Property.Property? ShopDestination(S1DeliveryUi.DeliveryShop shop)
        {
#if IL2CPPMELON
            return shop.destinationProperty;
#else
            if (ShopDestinationField == null)
            {
                WarnOnce("shop:nodestfield", "DeliveryShop.destinationProperty was not found; the delivery app offers reserved docks.");
                return null;
            }

            return ShopDestinationField.GetValue(shop) as S1Property.Property;
#endif
        }

        private static int ShopDockNumber(S1DeliveryUi.DeliveryShop shop)
        {
#if IL2CPPMELON
            return shop.loadingDockIndex;
#else
            return ShopDockIndexField?.GetValue(shop) is int number ? number : 0;
#endif
        }

        private static void SetShopDockNumber(S1DeliveryUi.DeliveryShop shop, int number)
        {
#if IL2CPPMELON
            shop.loadingDockIndex = number;
#else
            if (ShopDockIndexField == null)
            {
                WarnOnce("shop:nodockfield", "DeliveryShop.loadingDockIndex was not found; the dock selection cannot skip reserved docks.");
                return;
            }

            ShopDockIndexField.SetValue(shop, number);
#endif
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

            if (_releasingId != null && string.Equals(_releasingId, id, StringComparison.Ordinal))
            {
                // A retry asked the holds a moment ago and released this arrival; asking again could give another answer.
                if (Held.Remove(id))
                    Info($"Delivery {id} to '{delivery.DestinationCode}' is no longer held.");
                Debug($"Delivery {id} arrives after its retry released it; the arrival holds are not asked again.");
                return false;
            }

            if (_checkedArrivalId != null && string.Equals(_checkedArrivalId, id, StringComparison.Ordinal))
            {
                // The SetDeliveryState prefix ran the holds for this arrival a moment ago and let it through.
                _checkedArrivalId = null;
                if (Held.Remove(id))
                    Info($"Delivery {id} to '{delivery.DestinationCode}' is no longer held.");
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

            if (_tickDepth > 0)
            {
                // Inside DeliveryManager.OnTimePass an arrived delivery has its van read in the same loop step; the
                // status turns "arrived" when the tick has finished (SettleHeldDuringTick).
                if (!HeldDuringTick.Contains(delivery))
                    HeldDuringTick.Add(delivery);
                Debug($"Delivery {id} held inside the delivery tick; its status turns arrived after the tick.");
            }
            else
            {
                delivery.Status = S1Delivery.EDeliveryStatus.Arrived;
            }

            return true;
        }

        /// <summary>
        /// DeliveryManager.SetDeliveryState prefix (host only, the method is an observers RPC that runs locally):
        /// true when the arrival is held and vanilla must not run. Skipping vanilla keeps the RPC to the clients,
        /// the "Delivery Arrived" notification and the van activation back while the arrival is held.
        /// </summary>
        internal static bool HoldStateChange(S1Delivery.DeliveryManager? manager, string? id, S1Delivery.EDeliveryStatus status)
        {
            _checkedArrivalId = null;
            if (status != S1Delivery.EDeliveryStatus.Arrived)
                return false;

            S1Delivery.DeliveryInstance? delivery = FindDelivery(manager, id);
            if (delivery == null)
            {
                Debug($"Delivery {id} turns arrived but is not in the active delivery list; vanilla handles it.");
                return false;
            }

            if (!TryHold(delivery, status))
            {
                _checkedArrivalId = id;
                return false;
            }

            if (manager != null)
                manager.HasChanged = true;
            Info($"Delivery {id} arrival held before its state change: no van, no arrival notification, clients keep the previous status.");
            return true;
        }

        /// <summary>DeliveryManager.SetDeliveryState finalizer: forgets the arrival check of that call.</summary>
        internal static void AfterStateChange() => _checkedArrivalId = null;

        // --- van contents of held arrivals in the save -----------------------------------------------------
        // A held arrival already has its items in the shop's van (the tick fills it before the state change), but the
        // van is not its ActiveVehicle, and DeliveryManager.GetSaveString saves only the vans of deliveries that have
        // one. For the save the van is lent to the held delivery, so its contents are written and loaded again.

        /// <summary>
        /// DeliveryManager.GetSaveString prefix: sets the shop van as ActiveVehicle of every held arrival that has
        /// none. Returns the deliveries changed, for <see cref="ReturnVansAfterSave"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static List<S1Delivery.DeliveryInstance>? LendVansForSave(S1Delivery.DeliveryManager? manager)
        {
            if (Held.Count == 0)
                return null;
            if (manager == null)
            {
                Warn($"The delivery save ran without a delivery manager; the vans of {Held.Count} held arrival(s) are not saved.");
                return null;
            }

            var lent = new List<S1Delivery.DeliveryInstance>();
            foreach (HeldArrival held in Held.Values)
            {
                S1Delivery.DeliveryInstance delivery = held.Delivery;
                if (delivery == null || delivery.ActiveVehicle != null)
                    continue;
                if (FindDelivery(manager, delivery.DeliveryID) == null)
                {
                    Debug($"Held delivery {delivery.DeliveryID} is not in the delivery list; its van is not saved.");
                    continue;
                }

                var shop = manager.GetShopInterface(delivery.StoreName);
                S1Delivery.DeliveryVehicle? van = shop == null ? null : shop.DeliveryVehicle;
                if (van == null)
                {
                    Warn($"Held delivery {delivery.DeliveryID} from '{delivery.StoreName}' has no shop van; its items are not saved.");
                    continue;
                }

                if (!SetActiveVehicle(delivery, van))
                    continue;
                lent.Add(delivery);
            }

            if (lent.Count > 0)
                Debug($"{lent.Count} held arrival van(s) lent to the delivery save.");
            return lent;
        }

        /// <summary>DeliveryManager.GetSaveString finalizer: takes the lent vans back, also when the save threw.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ReturnVansAfterSave(List<S1Delivery.DeliveryInstance>? lent)
        {
            if (lent == null)
                return;
            foreach (S1Delivery.DeliveryInstance delivery in lent)
            {
                if (delivery != null)
                    SetActiveVehicle(delivery, null);
            }
        }

        /// <summary>
        /// LandVehicle.Load postfix: a delivery van saved while its arrival was held was saved at the hidden van
        /// position. When the loaded delivery arrived at once, its van was parked before the van data moved it there;
        /// it is parked at the dock again.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AfterVehicleLoad(S1Vehicles.LandVehicle? vehicle)
        {
            if (vehicle == null)
                return;
            S1Delivery.DeliveryVehicle? van = vehicle.GetComponent<S1Delivery.DeliveryVehicle>();
            if (van == null)
                return;
            S1Delivery.DeliveryInstance? delivery = van.ActiveDelivery;
            if (delivery == null || delivery.Status != S1Delivery.EDeliveryStatus.Arrived)
                return;
            if (delivery.ActiveVehicle == null || delivery.ActiveVehicle != van)
                return;
            if (vehicle.transform.position.y > HiddenVanHeight)
                return;

            van.Activate(delivery);
            Info($"Delivery van of {delivery.DeliveryID} was saved while its arrival was held; it is parked at dock #{delivery.LoadingDockIndex + 1} again.");
        }

        // DeliveryVehicle.Deactivate moves an unused van to y = -100.
        private const float HiddenVanHeight = -50f;

        private static bool SetActiveVehicle(S1Delivery.DeliveryInstance delivery, S1Delivery.DeliveryVehicle? van)
        {
#if IL2CPPMELON
            delivery._ActiveVehicle_k__BackingField = van;
            return true;
#else
            if (ActiveVehicleSetter == null)
            {
                WarnOnce("activevehicle", "DeliveryInstance.ActiveVehicle has no setter; the vans of held arrivals are not saved.");
                return false;
            }

            ActiveVehicleSetter.Invoke(delivery, new object?[] { van });
            return true;
#endif
        }

        /// <summary>
        /// DeliveryManager.OnTimePass prefix: retries held arrivals, takes the rest out of the tick and marks the
        /// tick as running. Fills <paramref name="state"/> for <see cref="AfterTimePass"/>.
        /// </summary>
        internal static void BeforeTimePass(S1Delivery.DeliveryManager? manager, TickState state)
        {
            if (Held.Count > 0)
            {
                if (manager?.Deliveries == null)
                {
                    WarnOnce("tick:nomanager", $"The delivery tick ran without a delivery list; {Held.Count} held arrival(s) are not retried this minute.");
                }
                else
                {
                    // A failed retry must not keep the held deliveries in the tick, which would read their vans.
                    try
                    {
                        RetryHeld("game minute");
                    }
                    catch (Exception ex)
                    {
                        Fail($"Held delivery arrivals could not be retried this minute; they stay held. {ex}");
                    }

                    PullHeld(manager, state);
                }
            }

            state.Entered = true;
            _tickDepth++;
        }

        private static void PullHeld(S1Delivery.DeliveryManager manager, TickState state)
        {
            var deliveries = manager.Deliveries;
            // Walk backwards, so each index is the delivery's position in the list before the tick.
            for (int i = deliveries.Count - 1; i >= 0; i--)
            {
                S1Delivery.DeliveryInstance delivery = deliveries[i];
                if (delivery == null || !Held.ContainsKey(delivery.DeliveryID ?? string.Empty))
                    continue;
                deliveries.RemoveAt(i);
                PulledFromTick.Add(delivery);
                state.Pulled.Insert(0, delivery);
                state.Indexes.Insert(0, i);
            }

            if (state.Pulled.Count > 0)
                Debug($"{state.Pulled.Count} held delivery(ies) taken out of the delivery tick.");
        }

        /// <summary>
        /// DeliveryManager.OnTimePass finalizer: ends the tick, gives arrivals held inside it their "arrived" status
        /// and puts back exactly the deliveries <see cref="BeforeTimePass"/> took out in the same call, at their old
        /// positions, also when the tick threw.
        /// </summary>
        internal static void AfterTimePass(S1Delivery.DeliveryManager? manager, TickState? state, Exception? thrown)
        {
            if (state == null)
            {
                Warn("The delivery tick ended without its prefix state; held arrivals of this tick are settled at the next tick.");
                return;
            }

            if (state.Entered)
            {
                _tickDepth = Math.Max(0, _tickDepth - 1);
                if (_tickDepth == 0)
                    SettleHeldDuringTick();
            }

            List<S1Delivery.DeliveryInstance> pulled = state.Pulled;
            if (pulled.Count == 0)
                return;

            foreach (S1Delivery.DeliveryInstance delivery in pulled)
                PulledFromTick.Remove(delivery);

            var deliveries = manager?.Deliveries;
            if (deliveries == null)
            {
                Fail($"{pulled.Count} held delivery(ies) could not be put back after the delivery tick: the delivery manager is gone.");
                return;
            }

            int restored = PutBack(pulled, state.Indexes, deliveries.Contains, deliveries.Insert, () => deliveries.Count);
            if (thrown != null)
                Warn($"{restored} held delivery(ies) put back after the delivery tick threw {thrown.GetType().Name}.");
            else
                Debug($"{restored} held delivery(ies) put back after the delivery tick.");

            // A release that came while the tick had the delivery out of the list (for example a refresh from another
            // mod's OnTimePass postfix) waited; now SetDeliveryState can find it and tell the clients.
            if (_retryAfterTick && _tickDepth == 0)
            {
                _retryAfterTick = false;
                RetryHeld("after the delivery tick");
            }
        }

        /// <summary>
        /// Inserts every pulled item that is not in the list yet at its old index (clamped to the list length), in
        /// ascending index order, so the list keeps its order. Returns how many were inserted.
        /// </summary>
        internal static int PutBack<T>(IReadOnlyList<T> pulled, IReadOnlyList<int> indexes, Func<T, bool> contains, Action<int, T> insert, Func<int> count)
        {
            int restored = 0;
            for (int i = 0; i < pulled.Count; i++)
            {
                T item = pulled[i];
                if (contains(item))
                    continue;
                int index = i < indexes.Count ? indexes[i] : int.MaxValue;
                insert(Math.Max(0, Math.Min(index, count())), item);
                restored++;
            }

            return restored;
        }

        /// <summary>Gives every arrival held inside the tick that is still held the status "arrived".</summary>
        private static void SettleHeldDuringTick()
        {
            if (HeldDuringTick.Count == 0)
                return;

            foreach (S1Delivery.DeliveryInstance delivery in HeldDuringTick.ToArray())
            {
                string id = delivery.DeliveryID ?? string.Empty;
                if (Held.TryGetValue(id, out HeldArrival? held) && ReferenceEquals(held.Delivery, delivery))
                {
                    RestoreArrivedStatus(delivery);
                    Info($"Delivery {id} was held during the delivery tick; it waits as arrived without a van.");
                }
                else
                {
                    Debug($"Delivery {id} was held during the delivery tick and released before the tick ended.");
                }
            }

            HeldDuringTick.Clear();
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

            string? held = AskHolds(new Delivery(delivery), delivery.DeliveryID);
            if (held != null)
                return held;

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

        /// <summary>
        /// Asks the registered arrival holds in registration order. A hold that an earlier callback disposed is
        /// skipped, and the answer of a hold that disposed itself while it ran does not count. Returns the hold reason,
        /// or null when no hold applies.
        /// </summary>
        internal static string? AskHolds(Delivery wrapped, string? id)
        {
            foreach (HoldEntry hold in Holds.ToArray())
            {
                if (!Holds.Contains(hold))
                {
                    Debug($"Arrival hold from {hold.Owner} was removed by an earlier hold; it is not asked for delivery {id}.");
                    continue;
                }

                try
                {
                    if (!hold.Hold(wrapped))
                        continue;
                    if (!Holds.Contains(hold))
                    {
                        Debug($"Arrival hold from {hold.Owner} was removed while it ran; its answer for delivery {id} does not count.");
                        continue;
                    }

                    return "held by " + hold.Owner;
                }
                catch (Exception ex)
                {
                    Fail($"Arrival hold from {hold.Owner} threw for delivery {id}; that hold is skipped. {ex}");
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
                // A nested retry from a callback may already have released or replaced this entry.
                if (!IsCurrent(pair.Key, pair.Value))
                    continue;

                // A provider or hold callback that refreshes the docks starts a nested retry while this one is still
                // deciding the entry; that nested retry leaves it to this one.
                if (!InRetry.Add(pair.Key))
                {
                    Debug($"Held delivery {pair.Key} is still being checked by an outer retry; this retry ({why}) skips it.");
                    continue;
                }

                try
                {
                    RetryOne(pair.Key, pair.Value, why);
                }
                finally
                {
                    InRetry.Remove(pair.Key);
                }
            }
        }

        private static void RetryOne(string id, HeldArrival held, string why)
        {
            S1Delivery.DeliveryInstance delivery = held.Delivery;
            string? reason;
            if (RetryReasonForTesting != null)
            {
                reason = RetryReasonForTesting(id);
            }
            else
            {
                if (!IsActive(delivery))
                {
                    if (Claim(id, held))
                        Info($"Held delivery {id} is no longer active; it is no longer held.");
                    return;
                }

                AttachDocksFor(delivery.DestinationCode ?? string.Empty);
                reason = HoldReason(delivery, out _);
            }

            if (reason != null)
                return;

            // The host releases through SetDeliveryState, which looks the delivery up in the active list. While the
            // delivery tick has it out of that list the release waits for the tick's finalizer.
            if (PulledFromTick.Contains(delivery) && IsHost())
            {
                _retryAfterTick = true;
                Debug($"Held delivery {id} is released while the delivery tick has it out of the list; it arrives once the tick puts it back.");
                return;
            }

            if (!Claim(id, held))
            {
                Debug($"Held delivery {id} was handled by a nested retry; this retry skips it.");
                return;
            }

            string? outerRelease = _releasingId;
            try
            {
                // Back to the status before the hold, so the status event reports the real change.
                delivery.Status = held.PreviousStatus;
                _releasingId = id;
                try
                {
                    Arrive(delivery);
                }
                finally
                {
                    _releasingId = outerRelease;
                }

                if (Held.ContainsKey(id))
                    Info($"Held delivery {id} to '{delivery.DestinationCode}' was held again during its arrival ({why}).");
                else
                    Info($"Held delivery {id} to '{delivery.DestinationCode}' arrives now ({why}).");
            }
            catch (Exception ex)
            {
                bool arrived = ArrivalWentThrough(delivery);
                if (AfterFailedArrival(id, held, arrived, why, ex))
                    RestoreArrivedStatus(delivery);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsHost() => InstanceFinder.IsServer;

        /// <summary>
        /// Lets a released arrival go through. The host uses DeliveryManager.SetDeliveryState, the networked path
        /// vanilla uses, so clients see the arrival and the notification goes out; a client, or a delivery that is
        /// not in the active list, only changes its local status.
        /// </summary>
        private static void Arrive(S1Delivery.DeliveryInstance delivery)
        {
            S1Delivery.DeliveryManager? manager = HostManagerFor(delivery);
            if (manager == null)
            {
                delivery.SetStatus(S1Delivery.EDeliveryStatus.Arrived);
                return;
            }

#if IL2CPPMELON
            manager.SetDeliveryState(delivery.DeliveryID, S1Delivery.EDeliveryStatus.Arrived);
#else
            if (SetDeliveryStateMethod == null)
            {
                WarnOnce("statemethod", "DeliveryManager.SetDeliveryState was not found; released arrivals only change the host's status and clients are not told.");
                delivery.SetStatus(S1Delivery.EDeliveryStatus.Arrived);
                return;
            }

            try
            {
                SetDeliveryStateMethod.Invoke(manager, new object[] { delivery.DeliveryID, S1Delivery.EDeliveryStatus.Arrived });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
#endif
        }

        /// <summary>The delivery manager when this side is the host and the delivery is in its active list.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static S1Delivery.DeliveryManager? HostManagerFor(S1Delivery.DeliveryInstance delivery)
        {
            if (!InstanceFinder.IsServer)
            {
                Debug($"Held delivery {delivery.DeliveryID} is released on a client; only its local status changes.");
                return null;
            }

            if (!S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.InstanceExists)
            {
                Warn($"Held delivery {delivery.DeliveryID} is released without a delivery manager; only its local status changes.");
                return null;
            }

            S1Delivery.DeliveryManager manager = S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.Instance;
            if (FindDelivery(manager, delivery.DeliveryID) == null)
            {
                Warn($"Held delivery {delivery.DeliveryID} is released while it is out of the active list; only the host's status changes and clients are not told.");
                return null;
            }

            return manager;
        }

        private static S1Delivery.DeliveryInstance? FindDelivery(S1Delivery.DeliveryManager? manager, string? id)
        {
            var deliveries = manager?.Deliveries;
            if (deliveries == null || string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < deliveries.Count; i++)
            {
                S1Delivery.DeliveryInstance delivery = deliveries[i];
                if (delivery != null && string.Equals(delivery.DeliveryID, id, StringComparison.Ordinal))
                    return delivery;
            }

            return null;
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
            if (string.IsNullOrEmpty(code) || (Docks.Count == 0 && !Slots.HasSlots(code)))
                return;

            bool asked = false;
            bool attached = false;
            foreach (DockEntry entry in Docks.ToArray())
            {
                if (!string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase))
                    continue;
                asked = true;
                if (Attach(entry))
                    attached = true;
            }

            if (!asked)
                ReserveSlots(code);
            if (attached)
                Debug($"Loading docks of '{code}' refreshed.");
        }

        /// <summary>
        /// Puts the reserved slots of a property whose providers are all disposed back into its dock list, so a
        /// collision with a dock S1API did not add is noticed before a saved delivery reaches that index.
        /// </summary>
        private static void ReserveSlots(string code)
        {
            if (!Slots.HasSlots(code))
                return;
            S1Property.Property? property = FindProperty(code);
            if (property == null)
            {
                Debug($"Reserved loading dock slots of '{code}' not restored: the property is not loaded.");
                return;
            }

            UseCurrentSave();
            List<S1Delivery.LoadingDock?> docks = CopyDocks(property);
            if (Slots.Reserve(code, docks, index => CreatePlaceholder(property, code, index)))
            {
                property.LoadingDocks = docks.ToArray();
                Debug($"Reserved loading dock slots of '{code}' restored without a registered provider.");
            }

            AfterReserve("reserved slots restored");
        }

        /// <summary>Moves saved deliveries after their slots when <see cref="DockSlotBook{TDock}.Reserve"/> moved one.</summary>
        private static void AfterReserve(string why)
        {
            if (Slots.Moves == _seenMoves)
                return;
            _seenMoves = Slots.Moves;
            Followed.Add(Slots.TakeMoves());
            MoveSavedDeliveries(why);
        }

        /// <summary>
        /// Tells the slot book which save is loaded. When it is another save, the deliveries that followed a moved slot
        /// of the previous save are forgotten with the slots.
        /// </summary>
        private static void UseCurrentSave()
        {
            if (!Slots.UseSave(CurrentSave()))
                return;
            if (Followed.HasWork)
                Info($"Another save is loaded: {Followed.FollowedCount} delivery(ies) that followed a moved loading dock slot are forgotten.");
            Followed.Clear();
            _seenMoves = Slots.Moves;
        }

        /// <summary>
        /// Points every delivery that targeted the old index of a moved slot when the move happened at the slot's new
        /// index, so it goes to the dock it was ordered to and not to the dock S1API did not add that now sits at the
        /// old index. While that dock does not exist, the delivery waits like any delivery to a missing dock. A move
        /// that happens before the save has loaded applies when it has; deliveries ordered after a move are not
        /// redirected. The deliveries that followed are kept by ID, so a reload of an older save of the same game
        /// redirects them again. Host only; the host's delivery data is what the clients receive when they join.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MoveSavedDeliveries(string why)
        {
            if (!Followed.HasWork)
                return;
            if (!IsHost())
            {
                if (Followed.PendingCount > 0)
                    Debug($"Loading dock slots moved ({why}); only the host moves saved deliveries after them.");
                Followed.Clear();
                return;
            }

            if (!IsSaveLoaded())
            {
                Debug($"Loading dock slots moved ({why}) before the save has loaded; saved deliveries follow when it has.");
                return;
            }

            if (!S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.InstanceExists)
            {
                Warn($"Loading dock slots moved ({why}) but the delivery manager does not exist; saved deliveries do not follow yet.");
                return;
            }

            S1Delivery.DeliveryManager manager = S1DevUtilities.NetworkSingleton<S1Delivery.DeliveryManager>.Instance;
            var candidates = new List<S1Delivery.DeliveryInstance>(PulledFromTick);
            var deliveries = manager.Deliveries;
            if (deliveries != null)
            {
                for (int i = 0; i < deliveries.Count; i++)
                {
                    if (deliveries[i] != null && !candidates.Contains(deliveries[i]))
                        candidates.Add(deliveries[i]);
                }
            }

            var targets = new List<DockTarget>(candidates.Count);
            foreach (S1Delivery.DeliveryInstance delivery in candidates)
                targets.Add(new DockTarget(delivery.DeliveryID ?? string.Empty, delivery.DestinationCode ?? string.Empty, delivery.LoadingDockIndex));

            List<DockFollow> changes = Followed.Apply(targets);
            foreach (DockFollow change in changes)
            {
                S1Delivery.DeliveryInstance delivery = candidates[change.Position];
                delivery.LoadingDockIndex = change.To;
                Info($"Delivery {delivery.DeliveryID} to '{delivery.DestinationCode}' follows its loading dock from dock #{change.From + 1} to dock #{change.To + 1} ({why}).");
            }

            if (changes.Count > 0)
                manager.HasChanged = true;
        }

        private static void OnSaveLoaded()
        {
            _saveLoaded = true;
            MoveSavedDeliveries("save loaded");
        }

        /// <summary>True once the save's deliveries are loaded: after the load-complete event, or for a mod that
        /// registered later, while the game reports a finished load.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsSaveLoaded()
        {
            if (_saveLoaded)
                return true;
            S1Persistence.LoadManager? manager = S1Persistence.LoadManager.Instance;
            return manager != null && manager.IsGameLoaded && !manager.IsLoading;
        }

        /// <summary>
        /// Runs a dock provider unless the same registration's provider is running already: a provider that calls
        /// RefreshLoadingDocks is not asked again inside that call, its own answer counts once it returns. False when
        /// the provider was skipped or threw.
        /// </summary>
        internal static bool AskProvider(object key, Func<Transform?> provider, string code, string owner, out Transform? transform)
        {
            transform = null;
            if (!ProvidersRunning.Add(key))
            {
                Debug($"Loading dock provider for '{code}' from {owner} is still running; the nested refresh does not ask it again.");
                return false;
            }

            try
            {
                transform = provider();
                return true;
            }
            catch (Exception ex)
            {
                Fail($"Loading dock provider for '{code}' from {owner} threw. {ex}");
                return false;
            }
            finally
            {
                ProvidersRunning.Remove(key);
            }
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

            if (!AskProvider(entry, entry.Provider, entry.Code, entry.Owner, out Transform? transform))
                return false;

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

            UseCurrentSave();
            List<S1Delivery.LoadingDock?> docks = CopyDocks(property);
            Func<int, S1Delivery.LoadingDock> placeholder = index => CreatePlaceholder(property, entry.Code, index);
            bool changed = Slots.Reserve(entry.Code, docks, placeholder);
            DockSlotChange change = Slots.Apply(entry.Code, entry, dock, dock == null ? null : IdentityOf(dock, entry.Owner), docks, placeholder, out int slot);
            if (change == DockSlotChange.Attached || change == DockSlotChange.Released)
                changed = true;
            if (changed)
            {
                if (dock != null && change == DockSlotChange.Attached)
                    dock.ParentProperty = property;
                property.LoadingDocks = docks.ToArray();
            }

            AfterReserve("loading dock attached");
            switch (change)
            {
                case DockSlotChange.Attached:
                    // A null provider result can attach another registration's dock that shares the slot.
                    S1Delivery.LoadingDock? attached = dock ?? (slot >= 0 && slot < docks.Count ? docks[slot] : null);
                    Info($"Loading dock '{(attached == null ? "unknown" : attached.name)}' attached to '{entry.Code}' as dock #{slot + 1} (from {entry.Owner}).");
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
                UseCurrentSave();
                docks = CopyDocks(property);
                changed = Slots.Reserve(entry.Code, docks, placeholder);
            }

            DockSlotRelease release = Slots.Release(entry.Code, entry, docks, placeholder, out int slot);
            if (property != null && docks != null && (changed || release == DockSlotRelease.Reserved || release == DockSlotRelease.StillUsed))
                property.LoadingDocks = docks.ToArray();
            AfterReserve("loading dock removed");

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

        /// <summary>
        /// The key a reserved slot recognizes this dock by after a reload: the registering mod plus the dock's GUID,
        /// or its hierarchy path when it has no GUID. The mod name keeps clones of one vanilla dock, which share the
        /// template's GUID, apart between mods.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string IdentityOf(S1Delivery.LoadingDock dock, string owner)
        {
            string? guid = null;
            try
            {
                guid = dock.GUID.ToString();
                if (string.IsNullOrEmpty(guid) || guid == Guid.Empty.ToString())
                    guid = null;
            }
            catch (Exception ex)
            {
                Debug($"Loading dock '{dock.name}' has no readable GUID; its slot is matched by its hierarchy path. {ex.Message}");
            }

            return guid != null ? owner + "|guid:" + guid : owner + "|path:" + PathOf(dock.transform);
        }

        private static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (Transform? current = transform; current != null; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>
        /// The key of the loaded game: the save folder plus the save's creation time, so a new game in the same slot
        /// is another save. A co-op client has no save folder; its key changes with every scene load, so the slots of a
        /// joined game never reach a save the player hosts later. Empty when no game is loaded or the key cannot be read.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string CurrentSave()
        {
            try
            {
                S1Persistence.LoadManager? manager = S1Persistence.LoadManager.Instance;
                if (manager == null)
                    return string.Empty;
                string folder = manager.LoadedGameFolderPath ?? string.Empty;
                if (folder.Length > 0)
                {
                    S1Persistence.SaveInfo? info = manager.ActiveSaveInfo;
                    return info == null ? folder : folder + "|" + info.DateCreated.Ticks;
                }

                return InstanceFinder.IsClient && !InstanceFinder.IsServer ? "client#" + _sceneGeneration : string.Empty;
            }
            catch (Exception ex)
            {
                Debug($"The loaded save folder could not be read; loading dock slots stay as they are. {ex.Message}");
                return string.Empty;
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
            GameLifecycle.OnLoadComplete += OnSaveLoaded;
            _lifecycleHooked = true;
        }

        private static void ResetSceneState()
        {
            if (Held.Count > 0)
                Info($"Scene change: {Held.Count} held delivery arrival(s) dropped; the save keeps them as arrived.");
            Held.Clear();
            PulledFromTick.Clear();
            HeldDuringTick.Clear();
            Offered.Clear();
            Warned.Clear();
            _tickDepth = 0;
            _dockUiRefreshDepth = 0;
            _destinationUiRefreshDepth = 0;
            // ListenedDropdowns stays: instance IDs are unique for the whole game process, and a dropdown that survives
            // the scene change keeps its listener.
            ShownDocks.Clear();
            ShownDestinations.Clear();
            _checkedArrivalId = null;
            _releasingId = null;
            _retryAfterTick = false;
            _saveLoaded = false;
            _sceneGeneration++;
            // Slot indexes and the deliveries that followed a moved slot stay: saved deliveries keep pointing at them
            // after the reload. Another save drops them at its first dock attach (UseCurrentSave).
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
            HeldDuringTick.Clear();
            Offered.Clear();
            Warned.Clear();
            Slots.Clear();
            _tickDepth = 0;
            _dockUiRefreshDepth = 0;
            ListenedDropdowns.Clear();
            ShownDocks.Clear();
            ShownDestinations.Clear();
            InRetry.Clear();
            ProvidersRunning.Clear();
            Followed.Clear();
            _checkedArrivalId = null;
            _releasingId = null;
            _retryAfterTick = false;
            _seenMoves = 0;
            _saveLoaded = false;
            _destinationUiRefreshDepth = 0;
            RetryReasonForTesting = null;
            if (_lifecycleHooked)
            {
                GameLifecycle.OnPreSceneChange -= ResetSceneState;
                GameLifecycle.OnLoadComplete -= OnSaveLoaded;
                _lifecycleHooked = false;
            }
        }

        /// <summary>
        /// INTERNAL: Replaces the activity, dock and hold checks of a held arrival in a retry with this answer (the
        /// hold reason, or null to release). Only for tests.
        /// </summary>
        internal static Func<string, string?>? RetryReasonForTesting { get; set; }

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

        /// <summary>What one DeliveryManager.OnTimePass prefix did, for its finalizer.</summary>
        internal sealed class TickState
        {
            /// <summary>True when the prefix counted the tick as running.</summary>
            internal bool Entered { get; set; }

            /// <summary>Held deliveries taken out of the list, in ascending order of <see cref="Indexes"/>.</summary>
            internal List<S1Delivery.DeliveryInstance> Pulled { get; } = new List<S1Delivery.DeliveryInstance>();

            /// <summary>The list position of each pulled delivery before the tick.</summary>
            internal List<int> Indexes { get; } = new List<int>();
        }

        /// <summary>The docks a delivery app dropdown showed: option i (1-based) is dock index Docks[i - 1].</summary>
        private sealed class ShownDockList
        {
            internal ShownDockList(string code, int[] docks, S1Delivery.LoadingDock?[] objects)
            {
                Code = code;
                Docks = docks;
                Objects = objects;
            }

            internal string Code { get; }
            internal int[] Docks { get; }

            /// <summary>The dock object each option showed, in option order.</summary>
            internal S1Delivery.LoadingDock?[] Objects { get; }
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
