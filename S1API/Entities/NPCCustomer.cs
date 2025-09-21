#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
using S1Player = Il2CppScheduleOne.PlayerScripts.Player;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Product = Il2CppScheduleOne.Product;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1Map = Il2CppScheduleOne.Map;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Schedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Quests = Il2CppScheduleOne.Quests;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1Economy = ScheduleOne.Economy;
using S1Player = ScheduleOne.PlayerScripts.Player;
using S1NPCsActions = ScheduleOne.NPCs.Actions;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Product = ScheduleOne.Product;
using S1Messaging = ScheduleOne.Messaging;
using S1Map = ScheduleOne.Map;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Schedules = ScheduleOne.NPCs.Schedules;
using S1Quests = ScheduleOne.Quests;
#endif

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using MelonLoader;
using S1API.Entities.Customer;
using S1API.Internal.Abstraction;
#if (IL2CPPMELON)
using Il2CppFishNet;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using ScheduleOne.Economy;
using S1API.Entities.Customer;
#endif

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing customer wrapper for an <see cref="NPC"/>.
    /// Provides helpers to configure and interact with the base game's Customer behaviour.
    /// </summary>
    public sealed class NPCCustomer
    {
        internal readonly NPC NPC;

        internal NPCCustomer(NPC npc)
        {
            NPC = npc;
        }

        /// <summary>
        /// Returns whether this NPC currently has a Customer component.
        /// </summary>
        public bool IsCustomer => Component != null;

        /// <summary>
        /// Ensures this NPC has a Customer component, creating one if absent.
        /// </summary>
        public void EnsureCustomer()
        {
            if (Component == null)
            {
                var c = NPC.gameObject.GetComponentInChildren<S1Economy.Customer>() ?? NPC.gameObject.AddComponent<S1Economy.Customer>();
                EnsureCustomerData(c);
                WireCoreReferences(c);
                InitializeRuntimeState(c);
                EnsureUnityEvents(c);
                c.enabled = true;
                c.InitializeSaveable();
                TryNetworkInitialize(c);
            }
            else
            {
                EnsureCustomerData(Component);
                WireCoreReferences(Component);
                InitializeRuntimeState(Component);
                EnsureUnityEvents(Component);
                TryNetworkInitialize(Component);
            }
        }

        /// <summary>
        /// Builds a CustomerData object from code and applies it to this NPC's customer component.
        /// </summary>
        public void BuildAndSetCustomerData(Action<CustomerDataBuilder> configure)
        {
            if (configure == null)
                return;
            EnsureCustomer();
            if (Component == null)
                return;
            var builder = new CustomerDataBuilder();
            configure(builder);
            var data = builder.BuildInternal();
            customerDataField?.SetValue(Component, data);
            // Refresh runtime caches and defaults now that data is present
            InitializeRuntimeState(Component);
        }

        /// <summary>
        /// Marks this customer as unlocked (visible to the player systems).
        /// </summary>
        public void Unlock()
        {
            EnsureCustomer();
            if (Component == null)
                return;

            var relation = NPC.S1NPC.RelationData;
            if (relation != null && !relation.Unlocked)
            {
                relation.Unlock(S1NPCs.Relation.NPCRelationData.EUnlockType.DirectApproach, false);
            }
        }

        /// <summary>
        /// Forces a deal offer attempt (based on the base game's heuristics).
        /// </summary>
        public void ForceDealOffer()
        {
            if (Component == null)
                return;
            Component.ForceDealOffer();
        }

        /// <summary>
        /// Requests a product from the specified player (or local player if null).
        /// </summary>
        public void RequestProduct(Player player = null)
        {
            if (Component == null)
                return;
            if (player == null)
            {
                Component.RequestProduct(S1Player.Local);
            }
            else
            {
                Component.RequestProduct(player.S1Player);
            }
        }
        
        /// <summary>
        /// Sets whether the customer is awaiting delivery.
        /// </summary>
        public void SetAwaitingDelivery(bool awaiting)
        {
            if (Component == null)
                return;
            Component.SetIsAwaitingDelivery(awaiting);
        }

        /// <summary>
        /// Sets up customer dialogue
        /// </summary>
        public void SetupDialog() => setupDialogueMethod?.Invoke(Component, null);

        /// <summary>
        /// INTERNAL: Direct access to underlying customer component.
        /// </summary>
        internal S1Economy.Customer Component => NPC.gameObject.GetComponent<S1Economy.Customer>();
        
        

        /// <summary>
        /// INTERNAL: Ensures the newly added Customer component is initialized with FishNet.
        /// Safe to call before or after the NPC NetworkObject is spawned.
        /// </summary>
        private void TryNetworkInitialize(S1Economy.Customer customer)
        {
            if (customer == null)
                return;
            try
            {
                // Directly set FishNet caches instead of calling NetworkInitializeIfDisabled.
                var transportManager = InstanceFinder.TransportManager;
                var networkObject = NPC.gameObject.GetComponent<NetworkObject>();
                if (transportManager == null || networkObject == null)
                    return;

                SetNonPublicInstanceField(customer, "_transportManagerCache", transportManager);
                SetNonPublicInstanceField(customer, "_networkObjectCache", networkObject);
            }
            catch (Exception)
            {
                // no-op: avoid crashing mods if FishNet state isn't ready yet
            }
        }

        /// <summary>
        /// INTERNAL: Wires basic references Customer expects when added at runtime.
        /// </summary>
        private void WireCoreReferences(S1Economy.Customer customer)
        {
            try
            {
                // Set protected NPC property via reflection so Customer has a back-reference immediately
                var npcProp = typeof(S1Economy.Customer).GetProperty("NPC", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var setMethod = npcProp?.GetSetMethod(true);
                setMethod?.Invoke(customer, new object[] { NPC.S1NPC });
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void EnsureCustomerData(S1Economy.Customer customer)
        {
            try
            {
                var data = (S1Economy.CustomerData)customerDataField?.GetValue(customer);
                if (data == null)
                {
                    // Create a minimal, safe CustomerData so base game logic has sane defaults
                    data = ScriptableObject.CreateInstance<S1Economy.CustomerData>();
                    data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
                    // Seed affinities for all product types with neutral preference
                    Array drugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
                    foreach (var dt in drugTypes)
                    {
                        data.DefaultAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                        {
                            DrugType = (S1Product.EDrugType)dt,
                            Affinity = 0f
                        });
                    }
                    // Reasonable defaults
                    data.MinWeeklySpend = 100f;
                    data.MaxWeeklySpend = 400f;
                    data.MinOrdersPerWeek = 1;
                    data.MaxOrdersPerWeek = 3;
                    data.OrderTime = 1200;
                    data.CanBeDirectlyApproached = true;
                    data.DependenceMultiplier = 1f;
                    data.BaseAddiction = 0f;

                    customerDataField?.SetValue(customer, data);
                }
            }
            catch (Exception)
            {
                // ignore; best-effort defaults
            }
        }

        private void InitializeRuntimeState(S1Economy.Customer customer)
        {
            try
            {
                var data = (S1Economy.CustomerData)customerDataField?.GetValue(customer);
                if (data == null)
                {
                    EnsureCustomerData(customer);
                    data = (S1Economy.CustomerData)customerDataField?.GetValue(customer);
                }

                // currentAffinityData = new CustomerAffinityData(); data.DefaultAffinityData.CopyTo(currentAffinityData)
                var currentAffinityField = typeof(S1Economy.Customer).GetField("currentAffinityData", BindingFlags.NonPublic | BindingFlags.Instance);
                var currentAffinity = currentAffinityField?.GetValue(customer) as S1Economy.CustomerAffinityData;
                if (currentAffinity == null)
                {
                    currentAffinity = new S1Economy.CustomerAffinityData();
                    // Copy default affinities if present
                    if (data != null && data.DefaultAffinityData != null)
                    {
                        data.DefaultAffinityData.CopyTo(currentAffinity);
                    }
                    currentAffinityField?.SetValue(customer, currentAffinity);
                }

                // Set starting addiction from data.BaseAddiction
                var currentAddictionProp = typeof(S1Economy.Customer).GetProperty("CurrentAddiction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                currentAddictionProp?.SetValue(customer, (float)(data?.BaseAddiction ?? 0f));

                // Ensure a valid default delivery location to avoid nulls during contract creation
                try
                {
                    if (customer.DefaultDeliveryLocation == null)
                    {
                        var map = S1DevUtilities.Singleton<S1Map.Map>.Instance;
                        if (map != null)
                        {
                            var regionData = map.GetRegionData(NPC.S1NPC.Region);
                            var loc = (regionData != null) ? regionData.GetRandomUnscheduledDeliveryLocation() : null;
                            if (loc != null)
                            {
                                customer.DefaultDeliveryLocation = loc;
                            }
                        }
                    }
                }
                catch { /* ignore */ }

                // Ensure DealSignal exists and is wired to the customer's schedule manager
                try
                {
                    var dealSignalField = typeof(S1Economy.Customer).GetField("DealSignal", BindingFlags.Public | BindingFlags.Instance);
                    var existingSignal = dealSignalField?.GetValue(customer) as S1Schedules.NPCSignal_WaitForDelivery;
                    if (existingSignal == null)
                    {
                        var sched = NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
                        if (sched == null)
                        {
                            var schedGo = new GameObject("NPCScheduleManager");
                            schedGo.transform.SetParent(NPC.gameObject.transform, false);
                            sched = schedGo.AddComponent<S1NPCs.NPCScheduleManager>();
                        }

                        var signal = NPC.gameObject.GetComponentInChildren<S1Schedules.NPCSignal_WaitForDelivery>(true);
                        if (signal == null)
                        {
                            var go = new GameObject("DealSignal");
                            go.transform.SetParent(sched.transform, false);
                            signal = go.AddComponent<S1Schedules.NPCSignal_WaitForDelivery>();
                            go.SetActive(false);
                        }
                        dealSignalField?.SetValue(customer, signal);
                    }
                }
                catch { /* ignore */ }
            }
            catch (Exception)
            {
                // ignore; best-effort runtime init
            }
        }

        /// <summary>
        /// INTERNAL: Ensure UnityEvents are allocated so runtime wiring does not NRE.
        /// </summary>
        private void EnsureUnityEvents(S1Economy.Customer customer)
        {
            try
            {
                // onUnlocked
                var onUnlockedField = typeof(S1Economy.Customer).GetField("onUnlocked", BindingFlags.Public | BindingFlags.Instance);
                if (onUnlockedField != null && onUnlockedField.GetValue(customer) == null)
                {
                    onUnlockedField.SetValue(customer, new UnityEvent());
                }

                // onDealCompleted
                var onDealCompletedField = typeof(S1Economy.Customer).GetField("onDealCompleted", BindingFlags.Public | BindingFlags.Instance);
                if (onDealCompletedField != null && onDealCompletedField.GetValue(customer) == null)
                {
                    onDealCompletedField.SetValue(customer, new UnityEvent());
                }

                // onContractAssigned (UnityEvent<Contract>)
                var onContractAssignedField = typeof(S1Economy.Customer).GetField("onContractAssigned", BindingFlags.Public | BindingFlags.Instance);
                if (onContractAssignedField != null && onContractAssignedField.GetValue(customer) == null)
                {
                    // Create a UnityEvent dynamically for generic arg if missing
                    var genericEventType = typeof(UnityEvent<>).MakeGenericType(typeof(S1Quests.Contract));
                    var evt = Activator.CreateInstance(genericEventType);
                    onContractAssignedField.SetValue(customer, evt);
                }
            }
            catch (Exception)
            {
                // ignore: best-effort to prevent null UnityEvents
            }
        }

        /// <summary>
        /// Subscribe to customer unlocked event.
        /// </summary>
        public void OnUnlocked(Action callback)
        {
            EnsureCustomer();
            if (Component == null || callback == null) return;
            try
            {
                var onUnlockedField = typeof(S1Economy.Customer).GetField("onUnlocked", BindingFlags.Public | BindingFlags.Instance);
                if (onUnlockedField?.GetValue(Component) is UnityEvent evt)
                {
                    EventHelper.AddListener(callback, evt);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Subscribe to deal completed event.
        /// </summary>
        public void OnDealCompleted(Action callback)
        {
            EnsureCustomer();
            if (Component == null || callback == null) return;
            try
            {
                var onDealCompletedField = typeof(S1Economy.Customer).GetField("onDealCompleted", BindingFlags.Public | BindingFlags.Instance);
                if (onDealCompletedField?.GetValue(Component) is UnityEvent evt)
                {
                    EventHelper.AddListener(callback, evt);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Subscribe to contract assigned event. Provides payment, product count, and delivery window via callback.
        /// </summary>
        public void OnContractAssigned(Action<float, int, int, int> callback)
        {
            EnsureCustomer();
            if (Component == null || callback == null) return;
            try
            {
                var onContractAssignedField = typeof(S1Economy.Customer).GetField("onContractAssigned", BindingFlags.Public | BindingFlags.Instance);
                var val = onContractAssignedField?.GetValue(Component);
                if (val == null) return;

                // Build a UnityAction<Contract> that maps to a safe callback signature
                var contractType = typeof(S1Quests.Contract);
                var unityActionType = typeof(UnityAction<>).MakeGenericType(contractType);
                var method = GetType().GetMethod(nameof(HandleContractAssigned), BindingFlags.NonPublic | BindingFlags.Instance);
                var del = Delegate.CreateDelegate(unityActionType, this, method);

                // Store user callback for use in handler
                _onContractAssigned = callback;

                // AddListener via reflection: ((UnityEvent<Contract>)val).AddListener(del)
                var addListener = val.GetType().GetMethod("AddListener", new[] { unityActionType });
                addListener?.Invoke(val, new object[] { del });
            }
            catch (Exception) { }
        }

        private Action<float, int, int, int> _onContractAssigned;

        // Maps Contract to safe primitives for modders
        private void HandleContractAssigned(object contract)
        {
            try
            {
                if (_onContractAssigned == null || contract == null) return;
                // Read minimal bits via reflection: payment, total qty, window start/end
                float payment = 0f;
                int totalQty = 0;
                int winStart = 0;
                int winEnd = 0;

                var contractType = contract.GetType();
                var paymentProp = contractType.GetProperty("Payment", BindingFlags.Public | BindingFlags.Instance);
                if (paymentProp != null)
                    payment = Convert.ToSingle(paymentProp.GetValue(contract));

                var productListProp = contractType.GetProperty("ProductList", BindingFlags.Public | BindingFlags.Instance);
                var productList = productListProp?.GetValue(contract);
                if (productList != null)
                {
                    var entriesField = productList.GetType().GetField("entries", BindingFlags.Public | BindingFlags.Instance);
                    var entries = entriesField?.GetValue(productList) as System.Collections.IEnumerable;
                    if (entries != null)
                    {
                        foreach (var e in entries)
                        {
                            var qtyField = e.GetType().GetField("Quantity", BindingFlags.Public | BindingFlags.Instance);
                            if (qtyField != null)
                                totalQty += Convert.ToInt32(qtyField.GetValue(e));
                        }
                    }
                }

                var windowProp = contractType.GetProperty("DeliveryWindow", BindingFlags.Public | BindingFlags.Instance);
                var window = windowProp?.GetValue(contract);
                if (window != null)
                {
                    var startField = window.GetType().GetField("WindowStartTime", BindingFlags.Public | BindingFlags.Instance);
                    var endField = window.GetType().GetField("WindowEndTime", BindingFlags.Public | BindingFlags.Instance);
                    if (startField != null) winStart = Convert.ToInt32(startField.GetValue(window));
                    if (endField != null) winEnd = Convert.ToInt32(endField.GetValue(window));
                }

                _onContractAssigned?.Invoke(payment, totalQty, winStart, winEnd);
            }
            catch (Exception) { }
        }

        private FieldInfo customerDataField = typeof(S1Economy.Customer).GetField("customerData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private MethodInfo setupDialogueMethod = typeof(S1Economy.Customer).GetMethod("SetUpDialogue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static void SetNonPublicInstanceField(object target, string fieldName, object value)
        {
            try
            {
                if (target == null || string.IsNullOrEmpty(fieldName)) return;
                var type = target.GetType();
                FieldInfo field = null;
                while (type != null && field == null)
                {
                    field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                    type = type.BaseType;
                }
                field?.SetValue(target, value);
            }
            catch (Exception) { }
        }
    }
}


