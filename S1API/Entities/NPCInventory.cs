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
using System.Collections.Generic;
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
        /// This method properly initializes slots without duplication, handling the fact that
        /// <see cref="S1Items.ItemSlot.SetSlotOwner"/> automatically adds slots to the owner's ItemSlots list.
        /// </summary>
        public void EnsureInitialized()
        {
            string npcId = NPC?.S1NPC?.ID ?? "<null>";
            var inv = Component;
            if (inv == null)
            {
                // Attach inventory if missing
                var comp = NPC.gameObject.GetComponent<S1NPCs.NPCInventory>() ?? NPC.gameObject.AddComponent<S1NPCs.NPCInventory>();
                inv = comp;
            }

            // Ensure ItemSlots list exists
            if (inv.ItemSlots == null)
            {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                inv.ItemSlots = new Il2CppSystem.Collections.Generic.List<S1Items.ItemSlot>();
#else
                inv.ItemSlots = new System.Collections.Generic.List<S1Items.ItemSlot>();
#endif
            }

            // Remove duplicate slots (same instance appearing multiple times)
            int duplicatesRemoved = DeduplicateSlots(inv);

            // Ensure we have the correct number of slots
            int currentCount = inv.ItemSlots.Count;
            int targetCount = inv.SlotCount;

            // Remove excess slots if we have too many
            if (currentCount > targetCount)
            {
                int trimmed = currentCount - targetCount;
                for (int i = currentCount - 1; i >= targetCount; i--)
                {
                    var slot = inv.ItemSlots[i];
                    if (slot != null)
                    {
                        try { slot.ClearStoredInstance(true); } catch { }
                    }
                    inv.ItemSlots.RemoveAt(i);
                }
                currentCount = inv.ItemSlots.Count;
            }

            // CRITICAL: Unlock all existing slots before creating new ones
            // Base game NPCs may have locked slots, which prevents AddCash/InsertItem from working
            for (int i = 0; i < inv.ItemSlots.Count; i++)
            {
                var slot = inv.ItemSlots[i];
                if (slot != null)
                {
                    try 
                    { 
                        ReflectionUtils.TrySetFieldOrProperty(slot, "ActiveLock", null); 
                    } 
                    catch { }
                    try 
                    { 
                        ReflectionUtils.TrySetFieldOrProperty(slot, "IsAddLocked", false); 
                    } 
                    catch { }
                }
            }

            // Create missing slots if we have too few
            // Note: SetSlotOwner automatically adds the slot to ItemSlots, so we don't call Add() manually
            if (currentCount < targetCount)
            {
                int slotsToCreate = targetCount - currentCount;
                for (int i = 0; i < slotsToCreate; i++)
                {
                    var slot = new S1Items.ItemSlot();
                    
                    // Set up event handler before SetSlotOwner (which adds to list)
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

                    // SetSlotOwner automatically adds slot to inv.ItemSlots - DO NOT call Add() manually
#if MONOMELON
                    slot.SetSlotOwner(inv);
#else
                    slot.SetSlotOwner(inv.Cast<S1Items.IItemSlotOwner>());
#endif

                    // Ensure slot is unlocked
                    try { ReflectionUtils.TrySetFieldOrProperty(slot, "ActiveLock", null); } catch { }
                    try { ReflectionUtils.TrySetFieldOrProperty(slot, "IsAddLocked", false); } catch { }
                }
            }

            if (inv.PickpocketIntObj == null)
            {
                var talk = NPC.GetType().GetMethod("GetPrimaryInteractable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var primary = talk?.Invoke(NPC, null) as S1Interaction.InteractableObject;
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

            try
            {
                ReflectionUtils.TrySetFieldOrProperty(inv, "npc", NPC.S1NPC);

                // NOTE: Do NOT add listeners here - the game's NPCInventory.Awake() already adds
                // onHovered and onInteractStart listeners. Adding them again causes duplicate
                // event firing which breaks the pickpocket screen.
            }
            catch { }

            try
            {
                var contentsChanged = ReflectionUtils.TryGetFieldOrProperty(inv, "onContentsChanged") as UnityEvent;
                if (contentsChanged == null)
                {
                    ReflectionUtils.TrySetFieldOrProperty(inv, "onContentsChanged", new UnityEvent());
                }
            }
            catch { }

            try { inv.NetworkInitializeIfDisabled(); } catch (Exception ex) { Logger.Warning($"[NPCInventory] EnsureInitialized: NetworkInitializeIfDisabled threw for '{npcId}': {ex.Message}"); }
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

        /// <summary>
        /// Removes duplicate slot instances from the inventory's ItemSlots list.
        /// Duplicates can occur if slots are added manually after SetSlotOwner (which already adds them).
        /// </summary>
        private int DeduplicateSlots(S1NPCs.NPCInventory inv)
        {
            if (inv?.ItemSlots == null)
                return 0;

            try
            {
                // Use a set to track seen slot instances (by reference equality)
                var seen = new HashSet<object>();
                var toRemove = new List<int>();

                for (int i = 0; i < inv.ItemSlots.Count; i++)
                {
                    var slot = inv.ItemSlots[i];
                    if (slot == null)
                    {
                        toRemove.Add(i);
                        continue;
                    }

                    // Check if we've seen this exact slot instance before
                    if (seen.Contains(slot))
                    {
                        // This is a duplicate - clear it and mark for removal
                        try { slot.ClearStoredInstance(true); } catch { }
                        toRemove.Add(i);
                    }
                    else
                    {
                        seen.Add(slot);
                    }
                }

                // Remove duplicates from the end to preserve indices
                for (int i = toRemove.Count - 1; i >= 0; i--)
                {
                    inv.ItemSlots.RemoveAt(toRemove[i]);
                }

                return toRemove.Count;
            }
            catch (Exception ex)
            {
                // If deduplication fails, continue - better to have duplicates than crash
                Logger.Warning($"[NPCInventory] DeduplicateSlots: Failed with exception: {ex.Message}");
                Logger.Warning($"[NPCInventory] DeduplicateSlots: Stack trace: {ex.StackTrace}");
                return 0;
            }
        }
    }
}