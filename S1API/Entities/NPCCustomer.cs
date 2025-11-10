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
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1UI = Il2CppScheduleOne.UI;
using S1VoiceOver = Il2CppScheduleOne.VoiceOver;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
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
using S1Dialogue = ScheduleOne.Dialogue;
using S1UI = ScheduleOne.UI;
using S1VoiceOver = ScheduleOne.VoiceOver;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using MelonLoader;
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
    /// Modder-facing customer wrapper for an NPC. Provides helpers to configure and interact with customer behavior,
    /// including deal offers, contracts, and customer events. Customer configuration must be done in <see cref="NPC.ConfigurePrefab"/>.
    /// </summary>
    /// <remarks>
    /// Use this to enable NPCs to act as business customers that buy products from the player.
    /// Subscribe to events like <see cref="OnDealCompleted"/> and <see cref="OnUnlocked"/> for dynamic customer interactions.
    /// </remarks>
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
            
            // Ensure currentAffinityData is initialized before any RPC calls
            EnsureCurrentAffinityDataInitialized(Component);
            
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

            // Ensure currentAffinityData is initialized before any RPC calls
            EnsureCurrentAffinityDataInitialized(Component);

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
            
            // Ensure currentAffinityData is initialized before any RPC calls
            EnsureCurrentAffinityDataInitialized(Component);
            
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
                    }
                    
                    // Initialize currentAffinityData immediately when creating CustomerData
                    if (currentAffinityDataField == null)
                    {
                        currentAffinityDataField = typeof(S1Economy.Customer).GetField("currentAffinityData",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    }
                    
                    var newAffinity = new S1Economy.CustomerAffinityData();
                    if (data.DefaultAffinityData != null)
                    {
                        data.DefaultAffinityData.CopyTo(newAffinity);
                    }
                    // Ensure ProductAffinities list is populated
                    if (newAffinity.ProductAffinities == null || newAffinity.ProductAffinities.Count == 0)
                    {
                        Array drugTypesForAffinity = Enum.GetValues(typeof(S1Product.EDrugType));
                        foreach (var dt in drugTypesForAffinity)
                        {
                            newAffinity.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                            {
                                DrugType = (S1Product.EDrugType)dt,
                                Affinity = 0f
                            });
                        }
                    }
                    currentAffinityDataField?.SetValue(customer, newAffinity);
#else
                    // Use property assignment for IL2CPP
                    customer.customerData = data;
                    
                    // Initialize currentAffinityData immediately when creating CustomerData
                    var newAffinity = new S1Economy.CustomerAffinityData();
                    if (data.DefaultAffinityData != null)
                    {
                        data.DefaultAffinityData.CopyTo(newAffinity);
                    }
                    // Ensure ProductAffinities list is populated
                    if (newAffinity.ProductAffinities == null || newAffinity.ProductAffinities.Count == 0)
                    {
                        Array drugTypesForAffinity = Enum.GetValues(typeof(S1Product.EDrugType));
                        foreach (var dt in drugTypesForAffinity)
                        {
                            newAffinity.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                            {
                                DrugType = (S1Product.EDrugType)dt,
                                Affinity = 0f
                            });
                        }
                    }
                    customer.currentAffinityData = newAffinity;
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

                // Initialize currentAffinityData - this is critical for AdjustAffinity RPC calls
#if MONOMELON
                // Use cached field if available, otherwise get it fresh
                if (currentAffinityDataField == null)
                {
                    currentAffinityDataField = typeof(S1Economy.Customer).GetField("currentAffinityData",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
                
                var currentAffinity = currentAffinityDataField?.GetValue(customer) as S1Economy.CustomerAffinityData;
                if (currentAffinity == null)
                {
                    currentAffinity = new S1Economy.CustomerAffinityData();
                    // Copy default affinities if present
                    if (data != null && data.DefaultAffinityData != null)
                    {
                        data.DefaultAffinityData.CopyTo(currentAffinity);
                    }
                    // Ensure ProductAffinities list is populated even if CopyTo didn't work
                    if (currentAffinity.ProductAffinities == null || currentAffinity.ProductAffinities.Count == 0)
                    {
                        // Initialize with all drug types at neutral affinity
                        Array drugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
                        foreach (var dt in drugTypes)
                        {
                            currentAffinity.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                            {
                                DrugType = (S1Product.EDrugType)dt,
                                Affinity = 0f
                            });
                        }
                    }
                    currentAffinityDataField?.SetValue(customer, currentAffinity);
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
                    // Ensure ProductAffinities list is populated even if CopyTo didn't work
                    if (currentAffinity.ProductAffinities == null || currentAffinity.ProductAffinities.Count == 0)
                    {
                        // Initialize with all drug types at neutral affinity
                        Array drugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
                        foreach (var dt in drugTypes)
                        {
                            currentAffinity.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                            {
                                DrugType = (S1Product.EDrugType)dt,
                                Affinity = 0f
                            });
                        }
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
                if (Utils.ReflectionUtils.TryGetFieldOrProperty(customer, "onUnlocked") == null)
                {
                    Utils.ReflectionUtils.TrySetFieldOrProperty(customer, "onUnlocked", new UnityEvent());
                }

                // onDealCompleted
                if (Utils.ReflectionUtils.TryGetFieldOrProperty(customer, "onDealCompleted") == null)
                {
                    Utils.ReflectionUtils.TrySetFieldOrProperty(customer, "onDealCompleted", new UnityEvent());
                }

                // onContractAssigned (UnityEvent<Contract>)
                if (Utils.ReflectionUtils.TryGetFieldOrProperty(customer, "onContractAssigned") == null)
                {
                    // Create a UnityEvent dynamically for generic arg if missing
                    var genericEventType = typeof(UnityEvent<>).MakeGenericType(typeof(S1Quests.Contract));
                    var evt = Activator.CreateInstance(genericEventType);
                    Utils.ReflectionUtils.TrySetFieldOrProperty(customer, "onContractAssigned", evt);
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
        public event Action OnUnlocked
        {
            add
            {
                EnsureCustomer();
                if (Component == null || value == null) return;

                try
                {
                    var evt = GetCustomerUnityEvent("onUnlocked", true);
                    if (evt == null) return;
                    EventHelper.AddListener(value, evt);
                }
                catch (Exception) { }
            }
            remove
            {
                if (Component == null || value == null) return;

                try
                {
                    var evt = GetCustomerUnityEvent("onUnlocked", false);
                    if (evt == null) return;
                    EventHelper.RemoveListener(value, evt);
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Subscribe to deal completed event.
        /// </summary>
        public event Action OnDealCompleted
        {
            add
            {
                EnsureCustomer();
                if (Component == null || value == null) return;

                try
                {
                    var evt = GetCustomerUnityEvent("onDealCompleted", true);
                    if (evt == null) return;
                    EventHelper.AddListener(value, evt);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception in OnDealCompleted for {NPC.ID}: {ex.Message}");
                    Logger.Warning($"Stack trace: {ex.StackTrace}");
                }
            }
            remove
            {
                if (Component == null || value == null) return;

                try
                {
                    var evt = GetCustomerUnityEvent("onDealCompleted", false);
                    if (evt == null) return;
                    EventHelper.RemoveListener(value, evt);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception while removing OnDealCompleted listener for {NPC.ID}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Subscribe to contract assigned event. Provides payment, product count, and delivery window via callback.
        /// </summary>
        public event Action<float, int, int, int> OnContractAssigned
        {
            add
            {
                EnsureCustomer();
                if (Component == null || value == null) return;
                if (!EnsureContractAssignedHook())
                    return;

                _onContractAssigned += value;
            }
            remove
            {
                if (value == null) return;
                _onContractAssigned -= value;
                if (_onContractAssigned == null)
                {
                    TryUnhookContractAssignedEvent();
                }
            }
        }

        private UnityEvent GetCustomerUnityEvent(string memberName, bool createIfMissing)
        {
            if (Component == null)
                return null;

            var evt = Utils.ReflectionUtils.TryGetFieldOrProperty(Component, memberName) as UnityEvent;
            if (evt == null && createIfMissing)
            {
                evt = new UnityEvent();
                Utils.ReflectionUtils.TrySetFieldOrProperty(Component, memberName, evt);
            }

            return evt;
        }

        private bool EnsureContractAssignedHook()
        {
            if (_contractAssignedBridge != null || Component == null)
                return _contractAssignedBridge != null;

            try
            {
                var onContractAssignedField = typeof(S1Economy.Customer).GetField("onContractAssigned", BindingFlags.Public | BindingFlags.Instance);
                var evt = onContractAssignedField?.GetValue(Component);
                if (evt == null)
                    return false;

                var contractType = typeof(S1Quests.Contract);
                var unityActionType = typeof(UnityAction<>).MakeGenericType(contractType);
                var method = GetType().GetMethod(nameof(HandleContractAssigned), BindingFlags.NonPublic | BindingFlags.Instance);
                var del = Delegate.CreateDelegate(unityActionType, this, method);
                var addListener = evt.GetType().GetMethod("AddListener", new[] { unityActionType });
                addListener?.Invoke(evt, new object[] { del });
                _contractAssignedBridge = del;
                _contractAssignedUnityEvent = evt;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception wiring OnContractAssigned for {NPC.ID}: {ex.Message}");
                return false;
            }
        }

        private void TryUnhookContractAssignedEvent()
        {
            if (_contractAssignedBridge == null || _contractAssignedUnityEvent == null)
                return;

            try
            {
                var unityActionType = _contractAssignedBridge.GetType();
                var removeListener = _contractAssignedUnityEvent.GetType().GetMethod("RemoveListener", new[] { unityActionType });
                removeListener?.Invoke(_contractAssignedUnityEvent, new object[] { _contractAssignedBridge });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception removing OnContractAssigned for {NPC.ID}: {ex.Message}");
            }
            finally
            {
                _contractAssignedBridge = null;
                _contractAssignedUnityEvent = null;
            }
        }

        private Action<float, int, int, int> _onContractAssigned;
        private Delegate _contractAssignedBridge;
        private object _contractAssignedUnityEvent;

        // Maps Contract to safe primitives for modders
        private void HandleContractAssigned(object contract)
        {
            try
            {
                var handlers = _onContractAssigned;
                if (handlers == null || contract == null) return;

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

                foreach (Action<float, int, int, int> handler in handlers.GetInvocationList())
                {
                    try
                    {
                        handler(payment, totalQty, winStart, winEnd);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Exception in OnContractAssigned handler for {NPC.ID}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception while processing OnContractAssigned for {NPC.ID}: {ex.Message}");
            }
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

        /// <summary>
        /// INTERNAL: Ensures currentAffinityData is initialized before operations that might trigger RPCs.
        /// Also ensures all drug types are present to prevent null reference exceptions in AdjustAffinity RPC.
        /// </summary>
        private void EnsureCurrentAffinityDataInitialized(S1Economy.Customer customer)
        {
            if (customer == null) return;
            
            try
            {
#if MONOMELON
                if (currentAffinityDataField == null)
                {
                    currentAffinityDataField = typeof(S1Economy.Customer).GetField("currentAffinityData",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
                
                var currentAffinity = currentAffinityDataField?.GetValue(customer) as S1Economy.CustomerAffinityData;
                if (currentAffinity == null)
                {
                    // Re-initialize if somehow it became null
                    InitializeRuntimeState(customer);
                    currentAffinity = currentAffinityDataField?.GetValue(customer) as S1Economy.CustomerAffinityData;
                }
                
                // Ensure all drug types are present
                if (currentAffinity != null && currentAffinity.ProductAffinities != null)
                {
                    Array allDrugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
                    foreach (var dt in allDrugTypes)
                    {
                        var drugType = (S1Product.EDrugType)dt;
                        S1Economy.ProductTypeAffinity existing = null;
                        foreach (var item in currentAffinity.ProductAffinities)
                        {
                            if (item != null && item.DrugType == drugType)
                            {
                                existing = item;
                                break;
                            }
                        }
                        if (existing == null)
                        {
                            // Add missing drug type with neutral affinity
                            currentAffinity.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                            {
                                DrugType = drugType,
                                Affinity = 0f
                            });
                        }
                    }
                }
#else
                if (customer.currentAffinityData == null)
                {
                    // Re-initialize if somehow it became null
                    InitializeRuntimeState(customer);
                }
                
                // Ensure all drug types are present
                if (customer.currentAffinityData != null && customer.currentAffinityData.ProductAffinities != null)
                {
                    Array allDrugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
                    foreach (var dt in allDrugTypes)
                    {
                        var drugType = (S1Product.EDrugType)dt;
                        S1Economy.ProductTypeAffinity existing = null;
                        foreach (var item in customer.currentAffinityData.ProductAffinities)
                        {
                            if (item != null && item.DrugType == drugType)
                            {
                                existing = item;
                                break;
                            }
                        }
                        if (existing == null)
                        {
                            // Add missing drug type with neutral affinity
                            customer.currentAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                            {
                                DrugType = drugType,
                                Affinity = 0f
                            });
                        }
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception ensuring currentAffinityData initialized for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Recommends a dealer to the player. This marks the dealer as recommended and shows UI feedback.
        /// </summary>
        /// <param name="dealer">The dealer NPC to recommend.</param>
        public void RecommendDealer(NPCDealer dealer)
        {
            if (dealer == null)
            {
                Logger.Warning($"Cannot recommend dealer: dealer is null");
                return;
            }

            if (Component == null)
            {
                Logger.Warning($"Cannot recommend dealer for {NPC.ID}: Customer component is null");
                return;
            }

            // Ensure currentAffinityData is initialized before any RPC calls
            EnsureCurrentAffinityDataInitialized(Component);

            try
            {
                Logger.Msg($"Customer {NPC.FullName} recommended dealer {dealer.NPC.FullName} to player");
                
                // Mark the dealer as recommended
                dealer.MarkAsRecommended();
                
                // Show hint to player
                var hintDisplay = S1DevUtilities.Singleton<S1UI.HintDisplay>.Instance;
                if (hintDisplay != null)
                {
                    hintDisplay.ShowHint_20s($"You can now hire <h1>{dealer.NPC.FullName}</h> as a dealer.");
                }
                
                // Show dialogue if player is nearby
                var closestPlayer = S1PlayerScripts.Player.GetClosestPlayer(NPC.gameObject.transform.position, out var distance);
                if (closestPlayer == S1PlayerScripts.Player.Local && distance < 6f)
                {
                    // Get dialogue database from the NPC's dialogue handler
                    var dialogueHandler = NPC.S1NPC?.DialogueHandler;
                    if (dialogueHandler != null && dialogueHandler.Database != null)
                    {
                        // Get dialogue line
                        var dialogueLine = dialogueHandler.Database.GetLine(
                            S1Dialogue.EDialogueModule.Customer, 
                            "post_deal_recommend_dealer"
                        );
                        
                        if (!string.IsNullOrEmpty(dialogueLine))
                        {
                            // Replace placeholder with dealer name
                            dialogueLine = dialogueLine.Replace("<NAME>", dealer.NPC.FullName);
                            
                            // Create dialogue container
                            var container = ScriptableObject.CreateInstance<S1Dialogue.DialogueContainer>();
                            var nodeData = new S1Dialogue.DialogueNodeData
                            {
                                DialogueText = dialogueLine,
                                choices = new S1Dialogue.DialogueChoiceData[0],
                                DialogueNodeLabel = "ENTRY",
                                VoiceLine = S1VoiceOver.EVOLineType.Thanks
                            };
                            
                            // Create list and convert to Il2Cpp list if needed
                            var nodeDataList = new List<S1Dialogue.DialogueNodeData> { nodeData };
#if IL2CPPMELON
                            var il2cppList = new Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueNodeData>();
                            foreach (var node in nodeDataList)
                                il2cppList.Add(node);
                            container.DialogueNodeData = il2cppList;
#else
                            container.DialogueNodeData = nodeDataList;
#endif
                            
                            // Start coroutine using MelonCoroutines
                            MelonCoroutines.Start(WaitAndShowDialogue(container, dialogueHandler));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in RecommendDealer for {NPC.ID}: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator WaitAndShowDialogue(S1Dialogue.DialogueContainer container, S1Dialogue.DialogueHandler handler)
        {
            yield return new WaitForSeconds(0.1f);
            if (handler != null && container != null)
            {
                handler.InitializeDialogue(container);
            }
        }

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
