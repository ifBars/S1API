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
using S1API.Economy;
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
        private static readonly Logging.Log Logger = new Logging.Log("NPCCustomer");

        internal NPCCustomer(NPC npc)
        {
            NPC = npc;
            // Do not assume Customer exists; prefab may omit it by design
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
                Logger.Warning($"Customer component not present on NPC prefab for {NPC.ID}. Add it via NPC.ConfigurePrefab(builder.EnsureCustomer()).");
                return;
            }
            
            try
            {
                EnsureCustomerData(Component);
                
                S1Economy.CustomerData verifyData = null;
#if MONOMELON
                verifyData = (S1Economy.CustomerData)customerDataField?.GetValue(Component);
#else
                verifyData = Component.CustomerData;
#endif
                WireCoreReferences(Component);
                InitializeRuntimeState(Component);
                EnsureUnityEvents(Component);
                TryNetworkInitialize(Component);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in EnsureCustomer for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
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
                relation.Unlock(S1NPCs.Relation.NPCRelationData.EUnlockType.DirectApproach);
            }
        }

        /// <summary>
        /// Forces the NPC to generate and offer a random contract based on their Customer data and relationship with the player.
        /// 
        /// The generated contract is based on:
        /// - Available products listed for sale in ProductManagerApp
        /// - NPC's drug type affinities and preferences
        /// - NPC's spending budget (adjusted by relationship level)
        /// - NPC's quality standards
        /// - Current addiction level
        /// 
        /// For custom contracts with specific products/prices, use <see cref="OfferContract(ContractInfo)"/> instead.
        /// </summary>
        /// <returns>True if a contract was generated and offered, false if generation failed.</returns>
        public bool ForceDealOffer()
        {
            if (Component == null)
            {
                Logger.Warning($"Cannot force deal offer for {NPC.ID}: Customer component is null");
                return false;
            }
            
            try
            {
                // Store the original state to check if a contract was actually generated
                var originalOfferedContract = Component.OfferedContractInfo;
                
                Component.ForceDealOffer();
                
                // Check if a new contract was generated
                var newOfferedContract = Component.OfferedContractInfo;
                bool contractGenerated = newOfferedContract != null && newOfferedContract != originalOfferedContract;
                
                if (contractGenerated)
                {
                    Logger.Msg($"Successfully generated contract for {NPC.ID}");
                }
                else
                {
                    Logger.Warning($"Failed to generate contract for {NPC.ID}. Check if products are listed for sale and NPC meets order conditions.");
                }
                
                return contractGenerated;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in ForceDealOffer for {NPC.ID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Offers a custom contract to this customer using an API-friendly <see cref="ContractInfo"/>.
        /// This method allows you to specify exactly what products, quantities, prices, and delivery details the contract should have.
        /// For automatic contract generation based on NPC preferences, use <see cref="ForceDealOffer()"/> instead.
        /// </summary>
        /// <param name="info">The contract info containing the specific products, quantities, prices, and delivery details.</param>
        /// <returns>True if the contract was successfully offered, false otherwise.</returns>
        public bool OfferContract(ContractInfo info)
        {
            if (Component == null)
            {
                Logger.Warning($"Cannot offer contract to {NPC.ID}: Customer component is null");
                return false;
            }
            
            if (info == null)
            {
                Logger.Warning($"Cannot offer contract to {NPC.ID}: ContractInfo is null");
                return false;
            }

            // Convert API model to game model and invoke game logic
            var internalInfo = info.ToInternal();
            try
            {
                Component.OfferContract(internalInfo);
                Logger.Msg($"Successfully offered custom contract to {NPC.ID}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"OfferContract failed for {NPC.ID}: {ex.Message}");
                return false;
            }
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
            catch (Exception ex)
            {
                Logger.Warning($"Exception in TryNetworkInitialize for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Wires basic references Customer expects when added at runtime.
        /// </summary>
        private void WireCoreReferences(S1Economy.Customer customer)
        {
            try
            {
                var npcProp = typeof(S1Economy.Customer).GetProperty("NPC", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var setMethod = npcProp?.GetSetMethod(true);
                if (setMethod != null)
                {
                    setMethod.Invoke(customer, new object[] { NPC.S1NPC });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in WireCoreReferences for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
        }

        private void EnsureCustomerData(S1Economy.Customer customer)
        {
            try
            {
                var dataViaProperty = customer.CustomerData;
                
                S1Economy.CustomerData data = null;
#if MONOMELON
                
                if (customerDataField == null)
                {
                    Logger.Warning($"customerDataField is null, attempting to find field again for {NPC.ID}");
                    customerDataField = typeof(S1Economy.Customer).GetField("customerData",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
                
                data = (S1Economy.CustomerData)customerDataField?.GetValue(customer);
#else
                data = customer.CustomerData;
#endif
                
                // Use property result if reflection failed
                if (data == null && dataViaProperty != null)
                {
                    data = dataViaProperty;
                }
                
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

                    
#if MONOMELON
                    // Try reflection first
                    if (customerDataField != null)
                    {
                        customerDataField.SetValue(customer, data);
                    }
                    else
                    {
                        Logger.Warning($"Attempting manual field setting");
                        customerDataField.SetValue(customer, data);
                        if (currentAffinityDataField != null)
                            currentAffinityDataField.SetValue(customer, data.DefaultAffinityData);
                    }
#else
                    // Use property assignment for IL2CPP
                    customer.customerData = data;
                    customer.currentAffinityData = data.DefaultAffinityData;
#endif
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in EnsureCustomerData for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
        }

        private void InitializeRuntimeState(S1Economy.Customer customer)
        {
            try
            {
                S1Economy.CustomerData data;
#if MONOMELON
                data = (S1Economy.CustomerData)customerDataField?.GetValue(customer);
#else
                data = customer.CustomerData;
#endif
                if (data == null)
                {
                    EnsureCustomerData(customer);
#if MONOMELON
                    data = (S1Economy.CustomerData)customerDataField?.GetValue(customer);
#else
                    data = customer.CustomerData;
#endif
                }

                // currentAffinityData = new CustomerAffinityData(); data.DefaultAffinityData.CopyTo(currentAffinityData)
#if MONOMELON
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
#else
                var currentAffinity = customer.currentAffinityData;
                if (currentAffinity == null)
                {
                    currentAffinity = new S1Economy.CustomerAffinityData();
                    // Copy default affinities if present
                    if (data != null && data.DefaultAffinityData != null)
                    {
                        data.DefaultAffinityData.CopyTo(currentAffinity);
                    }
                    customer.currentAffinityData = currentAffinity;
                }
#endif

                // Set starting addiction from data.BaseAddiction
#if MONOMELON
                var currentAddictionField = typeof(S1Economy.Customer).GetField("CurrentAddiction", BindingFlags.Public | BindingFlags.Instance);
                currentAddictionField?.SetValue(customer, (float)(data?.BaseAddiction ?? 0f));
#else
                customer.CurrentAddiction = (float)(data?.BaseAddiction ?? 0f);
#endif

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
#if MONOMELON
                    var dealSignalField = typeof(S1Economy.Customer).GetField("DealSignal", BindingFlags.Public | BindingFlags.Instance);
#else
                    var dealSignalField = typeof(S1Economy.Customer).GetProperty("DealSignal", BindingFlags.Public | BindingFlags.Instance);
#endif
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
#if MONOMELON
                var onDealCompletedField = typeof(S1Economy.Customer).GetField("onDealCompleted", BindingFlags.Public | BindingFlags.Instance);
                var onUnlockedField = typeof(S1Economy.Customer).GetField("onUnlocked", BindingFlags.Public | BindingFlags.Instance);
                var onContractAssignedField = typeof(S1Economy.Customer).GetField("onContractAssigned", BindingFlags.Public | BindingFlags.Instance);
#else
                var onDealCompletedField = typeof(S1Economy.Customer).GetProperty("onDealCompleted", BindingFlags.Public | BindingFlags.Instance);
                var onUnlockedField = typeof(S1Economy.Customer).GetProperty("onUnlocked", BindingFlags.Public | BindingFlags.Instance);
                var onContractAssignedField = typeof(S1Economy.Customer).GetProperty("onContractAssigned", BindingFlags.Public | BindingFlags.Instance);
#endif
                if (onUnlockedField != null && onUnlockedField.GetValue(customer) == null)
                {
                    onUnlockedField.SetValue(customer, new UnityEvent());
                }

                // onDealCompleted
                if (onDealCompletedField != null && onDealCompletedField.GetValue(customer) == null)
                {
                    onDealCompletedField.SetValue(customer, new UnityEvent());
                }

                // onContractAssigned (UnityEvent<Contract>)
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

#if MONOMELON
        private FieldInfo customerDataField = typeof(S1Economy.Customer).GetField("customerData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
#else
        // In IL2CPP, customerData is a property, not a field
#endif
#if MONOMELON
        private FieldInfo currentAffinityDataField = typeof(S1Economy.Customer).GetField("currentAffinityData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
#else
        // In IL2CPP, currentAffinityData is a property, not a field
#endif
        private MethodInfo setupDialogueMethod = typeof(S1Economy.Customer).GetMethod("SetUpDialogue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        private static void SetNonPublicInstanceField(object target, string fieldName, object value)
        {
            try
            {
                if (target == null || string.IsNullOrEmpty(fieldName)) return;
                var type = target.GetType();
                FieldInfo field = null;
                while (type != null && field == null)
                {
                    field = type.GetField(fieldName, BindingFlags.Instance | System.Reflection.BindingFlags.Public | BindingFlags.NonPublic);
                    type = type.BaseType;
                }
                field?.SetValue(target, value);
            }
            catch (Exception) { }
        }
    }
}


