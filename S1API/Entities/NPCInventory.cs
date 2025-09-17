#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1Items = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
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

            // Create slots if none exist
            if (inv.ItemSlots == null || inv.ItemSlots.Count == 0)
            {
                int slotCount = inv.SlotCount;
                for (int i = 0; i < slotCount; i++)
                {
                    var slot = new S1Items.ItemSlot();
                    slot.SetSlotOwner(inv);
                    inv.ItemSlots.Add(slot);
                }
            }
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
    }
}


