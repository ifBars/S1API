#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using S1Quests = Il2CppScheduleOne.Quests;
using S1Economy = Il2CppScheduleOne.Economy;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1UIPhoneMessages = Il2CppScheduleOne.UI.Phone.Messages;
using S1Money = Il2CppScheduleOne.Money;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
using S1NPCs = ScheduleOne.NPCs;
using S1Economy = ScheduleOne.Economy;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = ScheduleOne.NPCs.Behaviour;
using S1Items = ScheduleOne.ItemFramework;
using S1Messaging = ScheduleOne.Messaging;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1UIPhoneMessages = ScheduleOne.UI.Phone.Messages;
using S1Money = ScheduleOne.Money;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MelonLoader;
using S1API.Economy;
using S1API.Internal.Abstraction;
using S1API.Map;
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
        private static readonly FieldInfo DealerRecruitedField = typeof(S1Economy.Dealer).GetField("onDealerRecruited", BindingFlags.Public | BindingFlags.Static);

        private readonly Dictionary<Action, Action<S1Economy.Dealer>> _dealerRecruitedHandlers = new Dictionary<Action, Action<S1Economy.Dealer>>();
        private Action _contractAcceptedHandlers;
        private bool _contractAcceptedHooked;

        internal NPCDealer(NPC npc)
        {
            NPC = npc;
            // Do not assume Dealer exists; prefab may omit it by design
        }

        /// <summary>
        /// Clears stale delegates from the static Dealer.onDealerRecruited field.
        /// Must be called on scene change to prevent dead wrapper delegates from accumulating.
        /// </summary>
        internal static void ClearStaticDelegates()
        {
            try
            {
                if (DealerRecruitedField != null)
                    DealerRecruitedField.SetValue(null, null);
            }
            catch { }
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
        /// dealer functionality must be configured at prefab creation time using <see cref="NPCPrefabBuilder.EnsureDealer"/>.
        /// This method is called automatically when the NPC spawns if <see cref="NPCPrefabBuilder.EnsureDealer"/> was used.
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
                // Ensure Dealer category is set for messaging app
                EnsureDealerCategory();
                
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
        /// Ensures the NPC has the Dealer category set for the messaging app.
        /// Removes Customer category if present, as dealers should only show as dealers.
        /// </summary>
        private void EnsureDealerCategory()
        {
            try
            {
                var categoriesObj = Utils.ReflectionUtils.TryGetFieldOrProperty(NPC.S1NPC, "ConversationCategories");
                
#if (IL2CPPMELON || IL2CPPBEPINEX)
                var categories = categoriesObj as Il2CppSystem.Collections.Generic.List<S1Messaging.EConversationCategory>;
                if (categories == null)
                {
                    categories = new Il2CppSystem.Collections.Generic.List<S1Messaging.EConversationCategory>();
                    Utils.ReflectionUtils.TrySetFieldOrProperty(NPC.S1NPC, "ConversationCategories", categories);
                }
                
                bool changed = false;
                
                // Log current contents
                try
                {
                    string before = string.Join(",", Enumerable.Range(0, categories.Count).Select(i => categories[i].ToString()))
                        + $" | first={ (categories.Count>0? categories[0].ToString():"<none>") }";
                }
                catch { }
                
                // Remove Customer category if present (dealers should only be dealers)
                for (int i = categories.Count - 1; i >= 0; i--)
                {
                    if (categories[i] == S1Messaging.EConversationCategory.Customer)
                    {
                        categories.RemoveAt(i);
                        changed = true;
                    }
                }
                
                // Check if Dealer category is already present
                bool hasDealer = false;
                for (int i = 0; i < categories.Count; i++)
                {
                    if (categories[i] == S1Messaging.EConversationCategory.Dealer)
                    {
                        hasDealer = true;
                        break;
                    }
                }
                
                if (!hasDealer)
                {
                    categories.Add(S1Messaging.EConversationCategory.Dealer);
                    changed = true;
                }
                
                // Log after contents
                try
                {
                    string after = string.Join(",", Enumerable.Range(0, categories.Count).Select(i => categories[i].ToString()))
                        + $" | first={ (categories.Count>0? categories[0].ToString():"<none>") }";
                }
                catch { }
                
                // Update the MSGConversation if it already exists and we made changes
                if (changed && NPC.S1NPC.MSGConversation != null)
                {
                    NPC.S1NPC.MSGConversation.SetCategories(categories);
                    
                    // Force UI creation if not already created, so badge exists to refresh
                    NPC.S1NPC.MSGConversation.EnsureUIExists();
                    
                    TryHookConversationUIRefresh(NPC.S1NPC.MSGConversation);
                    RefreshDealerCategoryBadge();
                }
 #else
                var categories = categoriesObj as System.Collections.Generic.List<S1Messaging.EConversationCategory>;
                if (categories == null)
                {
                    categories = new System.Collections.Generic.List<S1Messaging.EConversationCategory>();
                    Utils.ReflectionUtils.TrySetFieldOrProperty(NPC.S1NPC, "ConversationCategories", categories);
                }
                
                bool changed = false;
                
                
                // Remove Customer category if present (dealers should only be dealers)
                if (categories.Remove(S1Messaging.EConversationCategory.Customer))
                {
                    changed = true;
                }
                
                if (!categories.Contains(S1Messaging.EConversationCategory.Dealer))
                {
                    categories.Add(S1Messaging.EConversationCategory.Dealer);
                    changed = true;
                }
                
                
                // Update the MSGConversation if it already exists and we made changes
                if (changed && NPC.S1NPC.MSGConversation != null)
                {
                    NPC.S1NPC.MSGConversation.SetCategories(categories);
                    
                    // Force UI creation if not already created, so badge exists to refresh
                    NPC.S1NPC.MSGConversation.EnsureUIExists();
                    
                    TryHookConversationUIRefresh(NPC.S1NPC.MSGConversation);
                    RefreshDealerCategoryBadge();
                }
 #endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in EnsureDealerCategory for {NPC.ID}: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensure we retry the badge refresh after the conversation UI definitely exists.
        /// The key issue: MSGConversation.CreateUI() is called lazily (on first message/load/etc),
        /// and it bakes the category badge from Categories[0] at creation time.
        /// We need to detect when UI is created and immediately refresh the badge.
        /// </summary>
        private void TryHookConversationUIRefresh(object convoObj)
        {
            try
            {
                var convo = convoObj as S1Messaging.MSGConversation;
                if (convo == null) return;

                // Check if UI already exists (uiCreated field)
                var uiCreatedField = typeof(S1Messaging.MSGConversation).GetField("uiCreated", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (uiCreatedField != null)
                {
                    var uiCreated = uiCreatedField.GetValue(convo);
                    
                    if (uiCreated != null && uiCreated is bool created && created)
                    {
                        // UI already exists, refresh immediately
                        RefreshDealerCategoryBadge();
                    }
                }

                // Hook onLoaded (called after UI is loaded from save)
                var prevLoaded = convo.onLoaded;
                convo.onLoaded = new System.Action(() =>
                {
                    try { prevLoaded?.Invoke(); } catch { }
                    RefreshDealerCategoryBadge();
                });

                // Hook onConversationOpened (called when player opens conversation)
                var prevOpened = convo.onConversationOpened;
                convo.onConversationOpened = new System.Action(() =>
                {
                    try { prevOpened?.Invoke(); } catch { }
                    RefreshDealerCategoryBadge();
                });
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in TryHookConversationUIRefresh: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh the category badge on the existing conversation entry to ensure Dealer icon/label/color are shown.
        /// Mirrors MessagesApp.CreateConversationUI category setup.
        /// </summary>
        private void RefreshDealerCategoryBadge()
        {
            try
            {
                var convo = NPC.S1NPC.MSGConversation;
                if (convo == null)
                {
                    return;
                }
                var entry = Utils.ReflectionUtils.TryGetFieldOrProperty(convo, "entry") as RectTransform;
                if (entry == null)
                {
                    return;
                }

                // Resolve category info from MessagesApp for Dealer
                var app = S1DevUtilities.PlayerSingleton<S1UIPhoneMessages.MessagesApp>.Instance;
                if (app == null)
                {
                    return;
                }
                var info = app.GetCategoryInfo(S1Messaging.EConversationCategory.Dealer);
                if (info == null)
                {
                    return;
                }

                var categoryRect = entry.Find("Category") as RectTransform;
                if (categoryRect == null)
                {
                    try
                    {
                        for (int i = 0; i < entry.childCount; i++)
                        {
                            var child = entry.GetChild(i);
                            if (child.name != null && child.name.Contains("Category"))
                            {
                                categoryRect = child as RectTransform;
                            }
                        }
                    }
                    catch { }
                    if (categoryRect == null)
                        return;
                }
                var label = categoryRect.Find("Label")?.GetComponent<Text>();
                var image = categoryRect.GetComponent<Image>();
                var nameText = entry.Find("Name")?.GetComponent<Text>();
                if (label == null || image == null || nameText == null)
                    return;

                label.text = info.Name != null && info.Name.Length > 0 ? info.Name[0].ToString() : "D";
                LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
                image.color = info.Color;
                categoryRect.anchoredPosition = new Vector2(225f + nameText.preferredWidth, categoryRect.anchoredPosition.y);
                categoryRect.gameObject.SetActive(true);
            }
            catch
            {
                // best-effort UI refresh; avoid throwing from API layer
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
                Component.AddCustomer_Server(customer.ID);

                // Best-effort local wiring so both sides have AssignedDealer set immediately
                try
                {
                    var custComponent = customer.gameObject.GetComponent<S1Economy.Customer>();
                    if (custComponent != null && custComponent.AssignedDealer == null)
                    {
                        Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(custComponent, "AssignedDealer", Component);
                    }
                }
                catch { }
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
                var cash = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(Component, "Cash");
                if (cash != null && cash is float cashValue)
                    return cashValue;
                return 0f;
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
                var recruited = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(Component, "IsRecruited");
                if (recruited != null && recruited is bool recruitedValue)
                    return recruitedValue;
                return false;
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
                var recommended = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(Component, "HasBeenRecommended");
                if (recommended != null && recommended is bool recommendedValue)
                    return recommendedValue;
                return false;
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
        /// Gets or sets the home building for this dealer.
        /// </summary>
        public Map.Building? Home
        {
            get
            {
                EnsureDealer();
                if (Component == null)
                    return null;

                try
                {
                    var homeBuilding = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(Component, "Home");
                    if (homeBuilding == null)
                        return null;

                    // Get the building name from the NPCEnterableBuilding
                    var buildingName = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(homeBuilding, "BuildingName") as string;
                    if (string.IsNullOrEmpty(buildingName))
                        return null;

                    // Find the Building wrapper by name
                    return Map.Building.GetByName(buildingName);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception in Home getter for {NPC.ID}: {ex.Message}");
                    return null;
                }
            }
            set
            {
                EnsureDealer();
                if (Component == null)
                {
                    Logger.Warning($"Cannot set Home for {NPC.ID}: Dealer component not available.");
                    return;
                }

                try
                {
                    object homeBuilding = null;
                    if (value != null)
                    {
                        // Resolve the underlying game building object
                        homeBuilding = value.ResolveGameBuilding();
                        if (homeBuilding == null)
                        {
                            Logger.Warning($"Cannot set Home for {NPC.ID}: Building '{value.Name}' could not be resolved.");
                            return;
                        }
                    }

                    // Set the Home field/property on the Dealer component
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(Component, "Home", homeBuilding);

                    // Sync HomeEvent.Building — Dealer.Awake() does this but runs before
                    // Home is set for S1API NPCs, so we must do it here too.
                    try
                    {
#if MONOMELON
                        var homeEventField = typeof(S1Economy.Dealer).GetField("HomeEvent", BindingFlags.Public | BindingFlags.Instance);
#else
                        var homeEventField = typeof(S1Economy.Dealer).GetProperty("HomeEvent", BindingFlags.Public | BindingFlags.Instance);
#endif
                        var homeEvent = homeEventField?.GetValue(Component);
                        if (homeEvent != null)
                        {
                            Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(homeEvent, "Building", homeBuilding);
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception in Home setter for {NPC.ID}: {ex.Message}");
                }
            }
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
                // Ensure contract/assignment lists exist before any contract RPCs run
                try
                {
                    var assignedCustomersObj = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(dealer, "AssignedCustomers");
                    if (assignedCustomersObj == null)
                    {
                        Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(dealer, "AssignedCustomers",
#if IL2CPPMELON || IL2CPPBEPINEX
                            new Il2CppSystem.Collections.Generic.List<S1Economy.Customer>()
#else
                            new System.Collections.Generic.List<S1Economy.Customer>()
#endif
                        );
                    }

                    var activeContractsObj = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(dealer, "ActiveContracts");
                    if (activeContractsObj == null)
                    {
                        Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(dealer, "ActiveContracts",
#if IL2CPPMELON || IL2CPPBEPINEX
                            new Il2CppSystem.Collections.Generic.List<S1Quests.Contract>()
#else
                            new System.Collections.Generic.List<S1Quests.Contract>()
#endif
                        );
                    }
                }
                catch { /* best effort to prevent null lists */ }

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

                // Ensure DealerAttendDealBehaviour exists (replaced NPCSignal_HandleDeal in v0.4.2f4)
                try
                {
                    var attendDealField = typeof(S1Economy.Dealer).GetField("_attendDealBehaviour", BindingFlags.NonPublic | BindingFlags.Instance);
                    var existingBehaviour = attendDealField?.GetValue(dealer) as S1NPCsBehaviour.DealerAttendDealBehaviour;
                    if (existingBehaviour == null)
                    {
                        // Get or create NPCBehaviour manager
                        var npcBehaviour = NPC.gameObject.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
                        if (npcBehaviour == null)
                        {
                            var behGo = new GameObject("NPCBehaviour");
                            behGo.transform.SetParent(NPC.gameObject.transform, false);
                            npcBehaviour = behGo.AddComponent<S1NPCsBehaviour.NPCBehaviour>();
                        }

                        var behaviour = NPC.gameObject.GetComponentInChildren<S1NPCsBehaviour.DealerAttendDealBehaviour>(true);
                        if (behaviour == null)
                        {
                            var go = new GameObject("DealerAttendDealBehaviour");
                            go.transform.SetParent(npcBehaviour.transform, false);
                            behaviour = go.AddComponent<S1NPCsBehaviour.DealerAttendDealBehaviour>();
                            go.SetActive(false);
                        }
                        attendDealField?.SetValue(dealer, behaviour);
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
        public event Action OnRecruited
        {
            add
            {
                EnsureDealer();
                if (Component == null || value == null || DealerRecruitedField == null) return;
                if (_dealerRecruitedHandlers.ContainsKey(value))
                    return;

                try
                {
                    Action<S1Economy.Dealer> wrapper = dealer =>
                    {
                        if (dealer != Component)
                            return;
                        try { value(); }
                        catch (Exception ex) { Logger.Warning($"Exception in OnRecruited handler for {NPC.ID}: {ex.Message}"); }
                    };

                    var existingValue = DealerRecruitedField.GetValue(null) as Action<S1Economy.Dealer>;
                    var combined = existingValue != null
                        ? (Action<S1Economy.Dealer>)Delegate.Combine(existingValue, wrapper)
                        : wrapper;
                    DealerRecruitedField.SetValue(null, combined);
                    _dealerRecruitedHandlers[value] = wrapper;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception wiring OnRecruited for {NPC.ID}: {ex.Message}");
                }
            }
            remove
            {
                if (value == null || DealerRecruitedField == null)
                    return;

                if (!_dealerRecruitedHandlers.TryGetValue(value, out var wrapper))
                    return;

                _dealerRecruitedHandlers.Remove(value);
                try
                {
                    var existingValue = DealerRecruitedField.GetValue(null) as Action<S1Economy.Dealer>;
                    if (existingValue == null)
                        return;

                    var remaining = (Action<S1Economy.Dealer>)Delegate.Remove(existingValue, wrapper);
                    DealerRecruitedField.SetValue(null, remaining);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception removing OnRecruited handler for {NPC.ID}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Subscribe to contract accepted event.
        /// </summary>
        public event Action OnContractAccepted
        {
            add
            {
                EnsureDealer();
                if (Component == null || value == null) return;
                EnsureContractAcceptedHook();
                _contractAcceptedHandlers += value;
            }
            remove
            {
                if (value == null) return;
                _contractAcceptedHandlers -= value;
            }
        }

        private void EnsureContractAcceptedHook()
        {
            if (_contractAcceptedHooked || Component == null)
                return;

            try
            {
#if MONOMELON
                var onContractAcceptedField = typeof(S1Economy.Dealer).GetField("onContractAccepted", BindingFlags.Public | BindingFlags.Instance);
                if (onContractAcceptedField == null)
                    return;

                var existing = onContractAcceptedField.GetValue(Component) as Action;
                Action dispatch = DispatchContractAccepted;
                var combined = existing != null ? (Action)Delegate.Combine(existing, dispatch) : dispatch;
                onContractAcceptedField.SetValue(Component, combined);
#else
                var existing = Component.onContractAccepted;
                System.Action dispatch = DispatchContractAccepted;
                Component.onContractAccepted = existing != null
                    ? new System.Action(() =>
                    {
                        existing?.Invoke();
                        dispatch();
                    })
                    : dispatch;
#endif
                _contractAcceptedHooked = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception wiring OnContractAccepted for {NPC.ID}: {ex.Message}");
            }
        }

        private void DispatchContractAccepted()
        {
            var handlers = _contractAcceptedHandlers;
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception in OnContractAccepted handler for {NPC.ID}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Subscribe to dealer recommended event.
        /// </summary>
        public event Action OnRecommended
        {
            add
            {
                EnsureDealer();
                if (Component == null || value == null) return;
                try
                {
                    var evt = GetRecommendedUnityEvent(true);
                    if (evt == null) return;
                    EventHelper.AddListener(value, evt);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception in OnRecommended for {NPC.ID}: {ex.Message}");
                }
            }
            remove
            {
                if (Component == null || value == null) return;
                try
                {
                    var evt = GetRecommendedUnityEvent(false);
                    if (evt == null) return;
                    EventHelper.RemoveListener(value, evt);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Exception while removing OnRecommended handler for {NPC.ID}: {ex.Message}");
                }
            }
        }

        private UnityEvent GetRecommendedUnityEvent(bool createIfMissing)
        {
            if (Component == null)
                return null;

#if MONOMELON
            var onRecommendedField = typeof(S1Economy.Dealer).GetField("onRecommended", BindingFlags.Public | BindingFlags.Instance);
            if (onRecommendedField == null)
                return null;
            var evt = onRecommendedField.GetValue(Component) as UnityEvent;
            if (evt == null && createIfMissing)
            {
                evt = new UnityEvent();
                onRecommendedField.SetValue(Component, evt);
            }
            return evt;
#else
            var onRecommendedProperty = typeof(S1Economy.Dealer).GetProperty("onRecommended", BindingFlags.Public | BindingFlags.Instance);
            if (onRecommendedProperty != null)
            {
                var evt = onRecommendedProperty.GetValue(Component) as UnityEvent;
                if (evt == null && createIfMissing)
                {
                    evt = new UnityEvent();
                    onRecommendedProperty.SetValue(Component, evt);
                }
                return evt;
            }

            var onRecommendedField = typeof(S1Economy.Dealer).GetField("onRecommended", BindingFlags.Public | BindingFlags.Instance);
            if (onRecommendedField == null)
                return null;

            var fieldEvent = onRecommendedField.GetValue(Component) as UnityEvent;
            if (fieldEvent == null && createIfMissing)
            {
                fieldEvent = new UnityEvent();
                onRecommendedField.SetValue(Component, fieldEvent);
            }

            return fieldEvent;
#endif
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
