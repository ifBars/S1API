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
using S1API.Logging;
using S1API.Utils;
namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing inventory wrapper for an <see cref="NPC"/>.
    /// Provides helpers to query capacity and insert items safely.
    /// </summary>
    public sealed class NPCInventory
    {
        private static readonly Logging.Log Logger = new Logging.Log("NPCInventory");
        private readonly NPC NPC;

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
            if (!CanItemFitInternal(temp))
                return false;
            InsertItemInternal(temp, network);
            return true;
        }

        /// <summary>
        /// Ensures the underlying <see cref="S1NPCs.NPCInventory"/> exists and has slots.
        /// Some custom NPCs added at runtime may not have had their slots populated yet.
        /// This method properly initializes slots without duplication, handling the fact that
        /// <see cref="S1Items.ItemSlot.SetSlotOwner"/> automatically adds slots to the owner's ItemSlots list.
        /// </summary>
        public void EnsureInitialized()
        {
            var npcId = NPC?.ID ?? "<null>";
            var inv = Component;
            if (inv == null && NPC != null)
            {
                var comp = NPC.gameObject.GetComponent<S1NPCs.NPCInventory>() ?? NPC.gameObject.AddComponent<S1NPCs.NPCInventory>();
                inv = comp;
            }

            if (inv == null)
                return;
            
            if (inv.ItemSlots == null)
            {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                inv.ItemSlots = new Il2CppSystem.Collections.Generic.List<S1Items.ItemSlot>();
#else
                inv.ItemSlots = new System.Collections.Generic.List<S1Items.ItemSlot>();
#endif
            }
            
            var currentCount = inv.ItemSlots.Count;
            var targetCount = GetSlotCount(inv, currentCount);
            
            if (currentCount > targetCount)
            {
                for (var i = currentCount - 1; i >= targetCount; i--)
                {
                    var slot = inv.ItemSlots[i];
                    if (slot != null)
                    {
                        try { slot.ClearStoredInstance(true); }
                        catch
                        {
                            // ignored
                        }
                    }
                    inv.ItemSlots.RemoveAt(i);
                }
                currentCount = inv.ItemSlots.Count;
            }
            
            foreach (var slot in inv.ItemSlots)
            {
                if (slot == null) continue;
                try 
                { 
                    ReflectionUtils.TrySetFieldOrProperty(slot, "ActiveLock", null); 
                }
                catch
                {
                    // ignored
                }

                try 
                { 
                    ReflectionUtils.TrySetFieldOrProperty(slot, "IsAddLocked", false); 
                }
                catch
                {
                    // ignored
                }
            }

            // Create missing slots if we have too few
            // Note: SetSlotOwner automatically adds the slot to ItemSlots, so we don't call Add() manually
            if (currentCount < targetCount)
            {
                var slotsToCreate = targetCount - currentCount;
                for (var i = 0; i < slotsToCreate; i++)
                {
                    var slot = new S1Items.ItemSlot();
                    
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    var handler = new System.Action(() =>
                    {
                        try { TryInvokeContentsChanged(inv); }
                        catch
                        {
                            // ignored
                        }
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
                            try { TryInvokeContentsChanged(inv); }
                            catch
                            {
                                // ignored
                            }
                        })
                    );
#endif
                    
#if MONOMELON
                    slot.SetSlotOwner(inv);
#else
                    slot.SetSlotOwner(inv.Cast<S1Items.IItemSlotOwner>());
#endif

                    // Ensure slot is unlocked
                    try { ReflectionUtils.TrySetFieldOrProperty(slot, "ActiveLock", null); }
                    catch
                    {
                        // ignored
                    }

                    try { ReflectionUtils.TrySetFieldOrProperty(slot, "IsAddLocked", false); }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (GetPickpocketInteractable(inv) == null && NPC != null)
            {
                var talk = NPC.GetType().GetMethod("GetPrimaryInteractable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var primary = talk?.Invoke(NPC, null) as S1Interaction.InteractableObject;
                var interactables = NPC.gameObject.GetComponentsInChildren<S1Interaction.InteractableObject>(true);
                S1Interaction.InteractableObject? pick = null;
                for (var i = 0; i < interactables.Length; i++)
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
                SetPickpocketInteractable(inv, pick);
            }

            try
            {
                ReflectionUtils.TrySetFieldOrProperty(inv, "npc", NPC?.S1NPC);
            }
            catch
            {
                // ignored
            }

            try
            {
                var contentsChanged = ReflectionUtils.TryGetFieldOrProperty(inv, "onContentsChanged") as UnityEvent;
                if (contentsChanged == null)
                {
                    ReflectionUtils.TrySetFieldOrProperty(inv, "onContentsChanged", new UnityEvent());
                }
            }
            catch
            {
                // ignored
            }

            try { inv.NetworkInitializeIfDisabled(); } catch (Exception ex) { Logger.Warning($"[NPCInventory] EnsureInitialized: NetworkInitializeIfDisabled threw for '{npcId}': {ex.Message}"); }
        }

        private static int GetSlotCount(S1NPCs.NPCInventory inv, int fallback)
        {
            var value = ReflectionUtils.TryGetFieldOrProperty(inv, "SlotCount");
            return value is int slotCount && slotCount >= 0
                ? slotCount
                : fallback;
        }

        private static void TryInvokeContentsChanged(S1NPCs.NPCInventory inv)
        {
            var evt = ReflectionUtils.TryGetFieldOrProperty(inv, "onContentsChanged") as UnityEvent;
            evt?.Invoke();
        }

        private static S1Interaction.InteractableObject? GetPickpocketInteractable(S1NPCs.NPCInventory inv)
        {
            return ReflectionUtils.TryGetFieldOrProperty(inv, "PickpocketIntObj") as S1Interaction.InteractableObject;
        }

        private static void SetPickpocketInteractable(S1NPCs.NPCInventory inv, S1Interaction.InteractableObject interactable)
        {
            ReflectionUtils.TrySetFieldOrProperty(inv, "PickpocketIntObj", interactable);
        }

        private S1NPCs.NPCInventory Component => NPC.gameObject.GetComponent<S1NPCs.NPCInventory>();

        private S1Items.ItemInstance? BuildTempItem(string itemId, int quantity)
        {
            try
            {
                var def = S1Registry.GetItem(itemId);
                if (def == null)
                    return null;
                var inst = def.GetDefaultInstance();
                var copy = inst.GetCopy(quantity);
                return copy ?? inst;
            }
            catch
            {
                return null;
            }
        }

        private bool CanItemFitInternal(S1Items.ItemInstance item)
        {
            EnsureInitialized();
            var inv = Component;
            return inv != null && inv.CanItemFit(item);
        }

        private int GetCapacityForItemInternal(S1Items.ItemInstance item)
        {
            EnsureInitialized();
            var inv = Component;
            return inv != null ? inv.GetCapacityForItem(item) : 0;
        }

        private void InsertItemInternal(S1Items.ItemInstance item, bool network = true)
        {
            EnsureInitialized();
            Component?.InsertItem(item, network);
        }
    }
}
