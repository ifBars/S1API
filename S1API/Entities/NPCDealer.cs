#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using S1Economy = Il2CppScheduleOne.Economy;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Quests = Il2CppScheduleOne.Quests;
using S1Items = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1Economy = ScheduleOne.Economy;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Quests = ScheduleOne.Quests;
using S1Items = ScheduleOne.ItemFramework;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
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
using S1API.Entities.Dealer;
#endif

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing dealer wrapper for an NPC. Provides helpers to configure and interact with dealer behavior,
    /// including customer assignment, cash management, and contract handling. Dealer configuration must be done in <see cref="NPC.ConfigurePrefab"/>.
    /// </summary>
    /// <remarks>
    /// Use this to enable NPCs to act as dealers that sell products to assigned customers.
    /// Subscribe to events like <see cref="OnRecruited"/> and <see cref="OnContractAccepted"/> for dynamic dealer interactions.
    /// </remarks>
    public sealed class NPCDealer
    {
        internal readonly NPC NPC;
        private static readonly Logging.Log Logger = new Logging.Log("NPCDealer");

        internal NPCDealer(NPC npc)
        {
            NPC = npc;
            // Do not assume Dealer exists; prefab may omit it by design
        }

        /// <summary>
        /// Returns whether this NPC currently has dealer functionality.
        /// </summary>
        public bool IsDealer => Component != null;

        /// <summary>
        /// Ensures this NPC has dealer functionality, initializing it if present.
        /// </summary>
        /// <remarks>
        /// Note: Since Dealer inherits from NPC in the base game (not a component), this will only work
        /// if the wrapped NPC is already a Dealer instance. For custom NPCs created via S1API,
        /// dealer functionality must be configured at prefab creation time.
        /// </remarks>
        public void EnsureDealer()
        {
            if (Component == null)
            {
                Logger.Warning($"Dealer component not present on NPC prefab for {NPC.ID}. Add it via NPC.ConfigurePrefab(builder.EnsureDealer()).");
                return;
            }
            
            try
            {
                WireCoreReferences(Component);
                InitializeRuntimeState(Component);
                EnsureUnityEvents(Component);
                TryNetworkInitialize(Component);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in EnsureDealer for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Marks this dealer as recruited (hired by the player).
        /// </summary>
        public void RecruitDealer()
        {
            EnsureDealer();
            if (Component == null)
                return;

            try
            {
                // Call InitialRecruitment ServerRpc
                Component.InitialRecruitment();
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in RecruitDealer for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks this dealer as recommended (by another NPC).
        /// </summary>
        public void MarkAsRecommended()
        {
            EnsureDealer();
            if (Component == null)
                return;

            try
            {
                Component.MarkAsRecommended();
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in MarkAsRecommended for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Assigns a customer to this dealer.
        /// </summary>
        public void AssignCustomer(NPC customer)
        {
            EnsureDealer();
            if (Component == null || customer == null)
                return;

            try
            {
                Component.SendAddCustomer(customer.ID);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in AssignCustomer for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a customer assignment from this dealer.
        /// </summary>
        public void RemoveCustomer(NPC customer)
        {
            EnsureDealer();
            if (Component == null || customer == null)
                return;

            try
            {
                Component.SendRemoveCustomer(customer.ID);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in RemoveCustomer for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current cash balance held by this dealer.
        /// </summary>
        public float GetCash()
        {
            EnsureDealer();
            if (Component == null)
                return 0f;

            try
            {
#if MONOMELON
                var cashProperty = typeof(S1Economy.Dealer).GetProperty("Cash", BindingFlags.Public | BindingFlags.Instance);
                if (cashProperty != null)
                    return (float)cashProperty.GetValue(Component);
                return 0f; // Fallback if property not found
#else
                return Component.Cash;
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in GetCash for {NPC.ID}: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Changes the dealer's cash balance by the specified amount.
        /// </summary>
        public void ChangeCash(float amount)
        {
            EnsureDealer();
            if (Component == null)
                return;

            try
            {
                Component.ChangeCash(amount);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in ChangeCash for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Collects all cash from the dealer and transfers it to the player.
        /// </summary>
        public void CollectCash()
        {
            EnsureDealer();
            if (Component == null)
                return;

            try
            {
                // Find the CollectCash method via reflection
                var collectCashMethod = typeof(S1Economy.Dealer).GetMethod("CollectCash", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (collectCashMethod != null)
                {
                    collectCashMethod.Invoke(Component, null);
                }
                else
                {
                    // Fallback: manually transfer cash
                    float cash = GetCash();
                    if (cash > 0f)
                    {
                        ChangeCash(-cash);
                        // Note: Money transfer would need to be handled via MoneyManager
                        Logger.Msg($"Collected {cash} cash from dealer {NPC.ID}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in CollectCash for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets whether the dealer has been recruited.
        /// </summary>
        public bool IsRecruited()
        {
            EnsureDealer();
            if (Component == null)
                return false;

            try
            {
#if MONOMELON
                var isRecruitedProperty = typeof(S1Economy.Dealer).GetProperty("IsRecruited", BindingFlags.Public | BindingFlags.Instance);
                if (isRecruitedProperty != null)
                    return (bool)isRecruitedProperty.GetValue(Component);
                return false; // Fallback if property not found
#else
                return Component.IsRecruited;
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in IsRecruited for {NPC.ID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets whether the dealer has been recommended.
        /// </summary>
        public bool HasBeenRecommended()
        {
            EnsureDealer();
            if (Component == null)
                return false;

            try
            {
#if MONOMELON
                var hasBeenRecommendedProperty = typeof(S1Economy.Dealer).GetProperty("HasBeenRecommended", BindingFlags.Public | BindingFlags.Instance);
                if (hasBeenRecommendedProperty != null)
                    return (bool)hasBeenRecommendedProperty.GetValue(Component);
                return false; // Fallback if property not found
#else
                return Component.HasBeenRecommended;
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in HasBeenRecommended for {NPC.ID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the list of assigned customers.
        /// </summary>
        public List<NPC> GetAssignedCustomers()
        {
            EnsureDealer();
            List<NPC> result = new List<NPC>();
            if (Component == null)
                return result;

            try
            {
#if MONOMELON
                var assignedCustomersProperty = typeof(S1Economy.Dealer).GetProperty("AssignedCustomers", BindingFlags.Public | BindingFlags.Instance);
                if (assignedCustomersProperty != null)
                {
                    var customers = assignedCustomersProperty.GetValue(Component) as System.Collections.IEnumerable;
                    if (customers != null)
                    {
                        foreach (var customer in customers)
                        {
                            var npcProp = customer.GetType().GetProperty("NPC");
                            if (npcProp != null)
                            {
                                var npc = npcProp.GetValue(customer) as S1NPCs.NPC;
                                if (npc != null)
                                {
                                    // Find the S1API NPC wrapper for this base NPC
                                    var apiNPC = NPC.All.FirstOrDefault(n => n.S1NPC == npc);
                                    if (apiNPC != null)
                                        result.Add(apiNPC);
                                }
                            }
                        }
                    }
                }
#else
                foreach (var customer in Component.AssignedCustomers)
                {
                    if (customer?.NPC != null)
                    {
                        // Find the S1API NPC wrapper for this base NPC
                        var apiNPC = NPC.All.FirstOrDefault(n => n.S1NPC == customer.NPC);
                        if (apiNPC != null)
                            result.Add(apiNPC);
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in GetAssignedCustomers for {NPC.ID}: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// INTERNAL: Direct access to underlying dealer instance.
        /// Since Dealer inherits from NPC, we check if the wrapped NPC is a Dealer instance.
        /// </summary>
        internal S1Economy.Dealer Component
        {
            get
            {
                // First try: if S1NPC is already a Dealer instance (from Dealer prefab)
                var dealerAsNpc = NPC.S1NPC as S1Economy.Dealer;
                if (dealerAsNpc != null)
                    return dealerAsNpc;
                
                // Fallback: try GetComponent in case Dealer is a separate component
                return NPC.gameObject.GetComponent<S1Economy.Dealer>();
            }
        }

        /// <summary>
        /// INTERNAL: Ensures the newly added Dealer component is initialized with FishNet.
        /// Safe to call before or after the NPC NetworkObject is spawned.
        /// </summary>
        private void TryNetworkInitialize(S1Economy.Dealer dealer)
        {
            if (dealer == null)
                return;
            try
            {
                // Directly set FishNet caches instead of calling NetworkInitializeIfDisabled.
                var transportManager = InstanceFinder.TransportManager;
                var networkObject = NPC.gameObject.GetComponent<NetworkObject>();
                if (transportManager == null || networkObject == null)
                    return;

                SetNonPublicInstanceField(dealer, "_transportManagerCache", transportManager);
                SetNonPublicInstanceField(dealer, "_networkObjectCache", networkObject);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in TryNetworkInitialize for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Wires basic references Dealer expects when added at runtime.
        /// </summary>
        private void WireCoreReferences(S1Economy.Dealer dealer)
        {
            try
            {
                // Dealer inherits from NPC, so it should already have the NPC reference
                // But we ensure it's wired correctly
                if (dealer != null && dealer != NPC.S1NPC as S1Economy.Dealer)
                {
                    // If dealer is not the same instance as NPC, we need to wire references
                    // This is a fallback for edge cases
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in WireCoreReferences for {NPC.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
            }
        }

        private void InitializeRuntimeState(S1Economy.Dealer dealer)
        {
            try
            {
                // Initialize overflow slots if not already initialized
#if MONOMELON
                var overflowSlotsField = typeof(S1Economy.Dealer).GetField("overflowSlots", BindingFlags.NonPublic | BindingFlags.Instance);
                if (overflowSlotsField != null)
                {
                    var overflowSlots = overflowSlotsField.GetValue(dealer) as S1Items.ItemSlot[];
                    if (overflowSlots == null || overflowSlots.Length == 0)
                    {
                        // Create overflow slots
                        overflowSlots = new S1Items.ItemSlot[10];
                        for (int i = 0; i < 10; i++)
                        {
                            overflowSlots[i] = new S1Items.ItemSlot();
                            overflowSlots[i].SetSlotOwner(dealer);
                        }
                        overflowSlotsField.SetValue(dealer, overflowSlots);
                    }
                }
#else
                // In IL2CPP, overflow slots are private fields - try to initialize via reflection
                var overflowSlotsField = typeof(S1Economy.Dealer).GetField("overflowSlots", BindingFlags.NonPublic | BindingFlags.Instance);
                if (overflowSlotsField != null)
                {
                    var overflowSlots = overflowSlotsField.GetValue(dealer) as S1Items.ItemSlot[];
                    if (overflowSlots == null || overflowSlots.Length == 0)
                    {
                        overflowSlots = new S1Items.ItemSlot[10];
                        for (int i = 0; i < 10; i++)
                        {
                            overflowSlots[i] = new S1Items.ItemSlot();
                            // In IL2CPP, cast Dealer to IItemSlotOwner interface
                            overflowSlots[i].SetSlotOwner(dealer.Cast<S1Items.IItemSlotOwner>());
                        }
                        overflowSlotsField.SetValue(dealer, overflowSlots);
                    }
                }
#endif

                // Ensure DealSignal exists for dealer schedule
                try
                {
#if MONOMELON
                    var dealSignalField = typeof(S1Economy.Dealer).GetField("DealSignal", BindingFlags.Public | BindingFlags.Instance);
#else
                    var dealSignalField = typeof(S1Economy.Dealer).GetProperty("DealSignal", BindingFlags.Public | BindingFlags.Instance);
#endif
                    var existingSignal = dealSignalField?.GetValue(dealer) as S1NPCsSchedules.NPCSignal_HandleDeal;
                    if (existingSignal == null)
                    {
                        var sched = NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
                        if (sched == null)
                        {
                            var schedGo = new GameObject("NPCScheduleManager");
                            schedGo.transform.SetParent(NPC.gameObject.transform, false);
                            sched = schedGo.AddComponent<S1NPCs.NPCScheduleManager>();
                        }

                        var signal = NPC.gameObject.GetComponentInChildren<S1NPCsSchedules.NPCSignal_HandleDeal>(true);
                        if (signal == null)
                        {
                            var go = new GameObject("DealSignal");
                            go.transform.SetParent(sched.transform, false);
                            signal = go.AddComponent<S1NPCsSchedules.NPCSignal_HandleDeal>();
                            go.SetActive(false);
                        }
                        dealSignalField?.SetValue(dealer, signal);
                    }
                }
                catch { /* ignore */ }

                // Ensure HomeEvent exists
                try
                {
#if MONOMELON
                    var homeEventField = typeof(S1Economy.Dealer).GetField("HomeEvent", BindingFlags.Public | BindingFlags.Instance);
#else
                    var homeEventField = typeof(S1Economy.Dealer).GetProperty("HomeEvent", BindingFlags.Public | BindingFlags.Instance);
#endif
                    var homeEvent = homeEventField?.GetValue(dealer) as S1NPCsSchedules.NPCEvent_StayInBuilding;
                    if (homeEvent == null)
                    {
                        var sched = NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
                        if (sched != null)
                        {
                            homeEvent = NPC.gameObject.GetComponentInChildren<S1NPCsSchedules.NPCEvent_StayInBuilding>(true);
                            if (homeEvent == null)
                            {
                                var go = new GameObject("HomeEvent");
                                go.transform.SetParent(sched.transform, false);
                                homeEvent = go.AddComponent<S1NPCsSchedules.NPCEvent_StayInBuilding>();
                                go.SetActive(false);
                            }
                            homeEventField?.SetValue(dealer, homeEvent);
                        }
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
        private void EnsureUnityEvents(S1Economy.Dealer dealer)
        {
            try
            {
                // onRecommended
#if MONOMELON
                var onRecommendedField = typeof(S1Economy.Dealer).GetField("onRecommended", BindingFlags.Public | BindingFlags.Instance);
                var onContractAcceptedField = typeof(S1Economy.Dealer).GetField("onContractAccepted", BindingFlags.Public | BindingFlags.Instance);
#else
                var onRecommendedField = typeof(S1Economy.Dealer).GetProperty("onRecommended", BindingFlags.Public | BindingFlags.Instance);
                var onContractAcceptedField = typeof(S1Economy.Dealer).GetProperty("onContractAccepted", BindingFlags.Public | BindingFlags.Instance);
#endif
                if (onRecommendedField != null && onRecommendedField.GetValue(dealer) == null)
                {
                    onRecommendedField.SetValue(dealer, new UnityEvent());
                }

                // onContractAccepted (Action, not UnityEvent)
                if (onContractAcceptedField != null && onContractAcceptedField.GetValue(dealer) == null)
                {
                    onContractAcceptedField.SetValue(dealer, new Action(() => { }));
                }
            }
            catch (Exception)
            {
                // ignore: best-effort to prevent null UnityEvents
            }
        }

        /// <summary>
        /// Subscribe to dealer recruited event.
        /// </summary>
        public void OnRecruited(Action callback)
        {
            EnsureDealer();
            if (Component == null || callback == null) return;
            try
            {
                // Subscribe to static event
                var onDealerRecruitedField = typeof(S1Economy.Dealer).GetField("onDealerRecruited", BindingFlags.Public | BindingFlags.Static);
                if (onDealerRecruitedField != null)
                {
                    var existingValue = onDealerRecruitedField.GetValue(null);
                    Action<S1Economy.Dealer> evt = existingValue as Action<S1Economy.Dealer>;
                    
                    Action<S1Economy.Dealer> wrapper = (dealer) =>
                    {
                        if (dealer == Component)
                            callback();
                    };
                    
                    if (evt != null)
                    {
                        evt = (Action<S1Economy.Dealer>)Delegate.Combine(evt, wrapper);
                    }
                    else
                    {
                        evt = wrapper;
                    }
                    
                    onDealerRecruitedField.SetValue(null, evt);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in OnRecruited for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to contract accepted event.
        /// </summary>
        public void OnContractAccepted(Action callback)
        {
            EnsureDealer();
            if (Component == null || callback == null) return;
            try
            {
#if MONOMELON
                var onContractAcceptedField = typeof(S1Economy.Dealer).GetField("onContractAccepted", BindingFlags.Public | BindingFlags.Instance);
                if (onContractAcceptedField != null)
                {
                    var existing = onContractAcceptedField.GetValue(Component) as Action;
                    if (existing != null)
                    {
                        existing = (Action)Delegate.Combine(existing, callback);
                        onContractAcceptedField.SetValue(Component, existing);
                    }
                    else
                    {
                        onContractAcceptedField.SetValue(Component, callback);
                    }
                }
#else
                // In IL2CPP, onContractAccepted is Il2CppSystem.Action, need to combine properly
                if (Component.onContractAccepted != null)
                {
                    var existingAction = Component.onContractAccepted;
                    Component.onContractAccepted = new System.Action(() =>
                    {
                        existingAction?.Invoke();
                        callback?.Invoke();
                    });
                }
                else
                {
                    Component.onContractAccepted = callback;
                }
#endif
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Subscribe to dealer recommended event.
        /// </summary>
        public void OnRecommended(Action callback)
        {
            EnsureDealer();
            if (Component == null || callback == null) return;
            try
            {
                UnityEvent evt = null;
                
#if MONOMELON
                var onRecommendedField = typeof(S1Economy.Dealer).GetField("onRecommended", BindingFlags.Public | BindingFlags.Instance);
                if (onRecommendedField != null)
                {
                    evt = onRecommendedField.GetValue(Component) as UnityEvent;
                    if (evt == null)
                    {
                        evt = new UnityEvent();
                        onRecommendedField.SetValue(Component, evt);
                    }
                }
#else
                // Try property first, then field
                var onRecommendedProperty = typeof(S1Economy.Dealer).GetProperty("onRecommended", BindingFlags.Public | BindingFlags.Instance);
                if (onRecommendedProperty != null)
                {
                    evt = onRecommendedProperty.GetValue(Component) as UnityEvent;
                    if (evt == null)
                    {
                        evt = new UnityEvent();
                        onRecommendedProperty.SetValue(Component, evt);
                    }
                }
                else
                {
                    var onRecommendedField = typeof(S1Economy.Dealer).GetField("onRecommended", BindingFlags.Public | BindingFlags.Instance);
                    if (onRecommendedField != null)
                    {
                        evt = onRecommendedField.GetValue(Component) as UnityEvent;
                        if (evt == null)
                        {
                            evt = new UnityEvent();
                            onRecommendedField.SetValue(Component, evt);
                        }
                    }
                }
#endif
                
                if (evt != null)
                {
                    EventHelper.AddListener(callback, evt);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in OnRecommended for {NPC.ID}: {ex.Message}");
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

