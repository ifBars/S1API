#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Interaction = Il2CppScheduleOne.Interaction;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1Items = ScheduleOne.ItemFramework;
using S1Interaction = ScheduleOne.Interaction;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing inventory wrapper for an <see cref="NPC"/>.
    /// Provides helpers to query capacity and insert items safely.
    /// </summary>
    public sealed class NPCInventory
    {
        internal readonly NPC NPC;

        internal NPCInventory(NPC npc)
        {
            NPC = npc;
        }

        /// <summary>
        /// Returns true if an item with the given ID and quantity can fit in this NPC's inventory.
        /// </summary>
        public bool CanItemFit(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;
            var temp = BuildTempItem(itemId, quantity);
            if (temp == null)
                return false;
            return CanItemFitInternal(temp);
        }

        /// <summary>
        /// Returns how many units of the given item ID could fit right now.
        /// </summary>
        public int GetCapacityForItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return 0;
            var temp = BuildTempItem(itemId, quantity);
            if (temp == null)
                return 0;
            return GetCapacityForItemInternal(temp);
        }

        /// <summary>
        /// Attempts to insert an item created from the ID and quantity.
        /// Returns true if insertion was performed (fit was sufficient).
        /// </summary>
        public bool TryInsert(string itemId, int quantity = 1, bool network = true)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;
            var temp = BuildTempItem(itemId, quantity);
            if (temp == null)
                return false;
            if (!CanItemFitInternal(temp))
                return false;
            InsertItemInternal(temp, network);
            return true;
        }

        /// <summary>
        /// Ensures the underlying <see cref="S1NPCs.NPCInventory"/> exists and has slots.
        /// Some custom NPCs added at runtime may not have had their slots populated yet.
        /// </summary>
        public void EnsureInitialized()
        {
            var inv = Component;
            if (inv == null)
            {
                // Attach inventory if missing
                var comp = NPC.gameObject.GetComponent<S1NPCs.NPCInventory>() ?? NPC.gameObject.AddComponent<S1NPCs.NPCInventory>();
                inv = comp;
            }

            // Create slots if none exist, or top-up to SlotCount
            if (inv.ItemSlots == null)
            {
                // Ensure list instance exists
                var listProp = typeof(S1NPCs.NPCInventory).GetProperty("ItemSlots", BindingFlags.Public | BindingFlags.Instance);
                listProp?.SetValue(inv, new System.Collections.Generic.List<S1Items.ItemSlot>());
            }
            if (inv.ItemSlots == null || inv.ItemSlots.Count < inv.SlotCount)
            {
                int existing = inv.ItemSlots?.Count ?? 0;
                int toCreate = Mathf.Max(0, inv.SlotCount - existing);
                for (int i = 0; i < toCreate; i++)
                {
                    var slot = new S1Items.ItemSlot();
#if MONOMELON
                    slot.SetSlotOwner(inv);
#else
                    slot.SetSlotOwner(inv.Cast<S1Items.IItemSlotOwner>());
#endif
                    try
                    {
                        var activeLockProp = typeof(S1Items.ItemSlot).GetProperty("ActiveLock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var setActiveLock = activeLockProp?.GetSetMethod(true);
                        setActiveLock?.Invoke(slot, new object[] { null });
                    }
                    catch { }
                    try
                    {
                        var isAddLockedProp = typeof(S1Items.ItemSlot).GetProperty("IsAddLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var setIsAddLocked = isAddLockedProp?.GetSetMethod(true);
                        setIsAddLocked?.Invoke(slot, new object[] { false });
                    }
                    catch { }
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    System.Action handler = new System.Action(() =>
                    {
                        try { inv.onContentsChanged?.Invoke(); } catch { }
                    });
                    slot.onItemDataChanged = (Il2CppSystem.Action)Il2CppSystem.Delegate.Combine(
                        slot.onItemDataChanged,
                        (Il2CppSystem.Action)handler
                    );
#else
                    slot.onItemDataChanged = (Action)Delegate.Combine(
                        slot.onItemDataChanged,
                        new Action(() =>
                        {
                            try { inv.onContentsChanged?.Invoke(); }
                            catch
                            {
                                try
                                {
                                    var evtField = typeof(S1NPCs.NPCInventory).GetField("onContentsChanged", BindingFlags.Public | BindingFlags.Instance);
                                    var evt = evtField?.GetValue(inv) as UnityEvent;
                                    evt?.Invoke();
                                }
                                catch { }
                            }
                        })
                    );
#endif
                    inv.ItemSlots.Add(slot);
                }
            }

            // Ensure all slots are unlocked and owned properly
            try
            {
                for (int i = 0; i < inv.ItemSlots.Count; i++)
                {
                    var s = inv.ItemSlots[i];
                    if (s == null)
                        continue;
#if MONOMELON
                    s.SetSlotOwner(inv);
#else
                    s.SetSlotOwner(inv.Cast<S1Items.IItemSlotOwner>());
#endif
                    try
                    {
                        var activeLockProp = typeof(S1Items.ItemSlot).GetProperty("ActiveLock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var setActiveLock = activeLockProp?.GetSetMethod(true);
                        setActiveLock?.Invoke(s, new object[] { null });
                    }
                    catch { }
                    try
                    {
                        var isAddLockedProp = typeof(S1Items.ItemSlot).GetProperty("IsAddLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var setIsAddLocked = isAddLockedProp?.GetSetMethod(true);
                        setIsAddLocked?.Invoke(s, new object[] { false });
                    }
                    catch { }
                }
            }
            catch { }

            // Ensure Pickpocket interactable exists to avoid UI NREs when opening the pickpocket screen
            if (inv.PickpocketIntObj == null)
            {
                var talk = NPC.GetType().GetMethod("GetPrimaryInteractable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var primary = talk?.Invoke(NPC, null) as S1Interaction.InteractableObject; // safe no-op if null under symbols
                var interactables = NPC.gameObject.GetComponentsInChildren<S1Interaction.InteractableObject>(true);
                S1Interaction.InteractableObject pick = null;
                for (int i = 0; i < interactables.Length; i++)
                {
                    if (interactables[i] != null && interactables[i] != primary)
                    {
                        pick = interactables[i];
                        break;
                    }
                }
                if (pick == null)
                {
                    pick = NPC.gameObject.AddComponent< S1Interaction.InteractableObject >();
                }
                inv.PickpocketIntObj = pick;
            }

            // Base Awake sets private 'npc' and wires Pickpocket listeners; simulate that here
            try
            {
                FieldInfo npcField = typeof(S1NPCs.NPCInventory).GetField("npc", BindingFlags.NonPublic | BindingFlags.Instance);
                npcField?.SetValue(inv, NPC.S1NPC);

                if (inv.PickpocketIntObj != null)
                {
                    MethodInfo hoveredMi = typeof(S1NPCs.NPCInventory).GetMethod("Hovered", BindingFlags.Public | BindingFlags.Instance);
                    MethodInfo interactedMi = typeof(S1NPCs.NPCInventory).GetMethod("Interacted", BindingFlags.Public | BindingFlags.Instance);
                    if (hoveredMi != null)
                    {
                        UnityAction hovered = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), inv, hoveredMi);
                        inv.PickpocketIntObj.onHovered?.AddListener(hovered);
                    }
                    if (interactedMi != null)
                    {
                        UnityAction interacted = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), inv, interactedMi);
                        inv.PickpocketIntObj.onInteractStart?.AddListener(interacted);
                    }
                }
            }
            catch { }

            // Ensure UnityEvent exists
            try
            {
                FieldInfo onChangedField = typeof(S1NPCs.NPCInventory).GetField("onContentsChanged", BindingFlags.Public | BindingFlags.Instance);
                if (onChangedField != null && onChangedField.GetValue(inv) == null)
                {
                    onChangedField.SetValue(inv, new UnityEvent());
                }
            }
            catch { }

            // Make sure FishNet is initialized for the component
            try { inv.NetworkInitializeIfDisabled(); } catch { }
        }

        internal S1NPCs.NPCInventory Component => NPC.gameObject.GetComponent<S1NPCs.NPCInventory>();

        private S1Items.ItemInstance BuildTempItem(string itemId, int quantity)
        {
            try
            {
                var def = S1Registry.GetItem(itemId);
                if (def == null)
                    return null;
                var inst = def.GetDefaultInstance();
                // Prefer GetCopy to set quantity if available
                var copy = inst.GetCopy(quantity);
                return copy ?? inst;
            }
            catch
            {
                return null;
            }
        }

        internal bool CanItemFitInternal(S1Items.ItemInstance item)
        {
            if (item == null) return false;
            EnsureInitialized();
            var inv = Component;
            return inv != null && inv.CanItemFit(item);
        }

        internal int GetCapacityForItemInternal(S1Items.ItemInstance item)
        {
            if (item == null) return 0;
            EnsureInitialized();
            var inv = Component;
            return inv != null ? inv.GetCapacityForItem(item) : 0;
        }

        internal void InsertItemInternal(S1Items.ItemInstance item, bool network = true)
        {
            if (item == null) return;
            EnsureInitialized();
            Component?.InsertItem(item, network);
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
                    field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                    type = type.BaseType;
                }
                field?.SetValue(target, value);
            }
            catch { }
        }
    }
}


