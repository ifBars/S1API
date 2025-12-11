#if (IL2CPPMELON)
using S1Storage = Il2CppScheduleOne.Storage;
using S1AccessSettings = Il2CppScheduleOne.Storage.StorageEntity.EAccessSettings;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Storage = ScheduleOne.Storage;
using S1AccessSettings = ScheduleOne.Storage.StorageEntity.EAccessSettings;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Abstraction;
using S1API.Items;

namespace S1API.Storages
{
    /// <summary>
    /// Access settings for storage containers.
    /// </summary>
    public enum StorageAccessSettings
    {
        /// <summary>
        /// Storage is closed and cannot be accessed.
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Storage can only be accessed by one player at a time.
        /// </summary>
        SinglePlayerOnly = 1,

        /// <summary>
        /// Storage can be accessed by all players.
        /// </summary>
        Full = 2
    }

    /// <summary>
    /// Represents a storage container in-game.
    /// </summary>
    public class StorageInstance
    {
        /// <summary>
        /// INTERNAL: The in-game storage instance.
        /// </summary>
        internal readonly S1Storage.StorageEntity S1Storage;

        /// <summary>
        /// Creates a storage instance from the in-game storage instance.
        /// </summary>
        /// <param name="storage"></param>
        internal StorageInstance(S1Storage.StorageEntity storage) => S1Storage = storage;

        // ====== Metadata Properties ======

        /// <summary>
        /// The display name of this storage container.
        /// </summary>
        public string Name =>
            S1Storage.StorageEntityName;

        /// <summary>
        /// The subtitle of this storage container.
        /// </summary>
        public string Subtitle =>
            S1Storage.StorageEntitySubtitle;

        /// <summary>
        /// The total number of slots available in this storage container.
        /// </summary>
        public int SlotCount =>
            S1Storage.SlotCount;

        /// <summary>
        /// The current number of items stored in this storage container.
        /// </summary>
        public int ItemCount =>
            S1Storage.ItemCount;

        /// <summary>
        /// The access settings for this storage container.
        /// </summary>
        public StorageAccessSettings AccessSettings {
            get {
                return (StorageAccessSettings)S1Storage.AccessSettings;
            }
            set {
                S1Storage.AccessSettings = (S1AccessSettings)value;
            }
        }

        /// <summary>
        /// Whether this storage container is currently opened by a player.
        /// </summary>
        public bool IsOpened =>
            S1Storage.IsOpened;

        // ====== Slot and Contents Access ======

        /// <summary>
        /// An array of all slots available on the storage container.
        /// </summary>
        public ItemSlotInstance[] Slots =>
            S1Storage.ItemSlots.ToArray()
                .Select(itemSlot => new ItemSlotInstance(itemSlot)).ToArray();

        /// <summary>
        /// Gets all item instances currently stored in this storage container.
        /// </summary>
        /// <returns>An array of item instances.</returns>
        public ItemInstance[] GetItems()
        {
            var items = S1Storage.GetAllItems();
            if (items == null || items.Count == 0)
            {
                return Array.Empty<ItemInstance>();
            }

            var wrapped = new ItemInstance[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                wrapped[i] = new ItemInstance(items[i]);
            }

            return wrapped;
        }

        /// <summary>
        /// Gets a dictionary mapping item instances to their quantities in this storage container.
        /// </summary>
        /// <returns>A dictionary where keys are item instances and values are quantities.</returns>
        public Dictionary<ItemInstance, int> GetContentsDictionary()
        {
            var contents = S1Storage.GetContentsDictionary();
            var result = new Dictionary<ItemInstance, int>();

            foreach (var kvp in contents)
            {
                if (kvp.Key != null)
                {
                    result[new ItemInstance(kvp.Key)] = kvp.Value;
                }
            }

            return result;
        }

        // ====== Item Addition ======

        /// <summary>
        /// Whether an item can fit inside this storage container or not.
        /// </summary>
        /// <param name="itemInstance">The item instance you want to store.</param>
        /// <param name="quantity">The quantity of item you want to store.</param>
        /// <returns>Whether the item will fit or not.</returns>
        public bool CanItemFit(ItemInstance itemInstance, int quantity = 1) =>
            S1Storage.CanItemFit(itemInstance.S1ItemInstance, quantity);

        /// <summary>
        /// Adds an item instance to this storage container.
        /// </summary>
        /// <param name="itemInstance">The item instance you want to store.</param>
        public void AddItem(ItemInstance itemInstance) =>
            S1Storage.InsertItem(itemInstance.S1ItemInstance);

        // ====== Item Removal ======

        /// <summary>
        /// Removes a specific item instance from this storage container.
        /// This performs a soft removal (decrements quantity) and does not spawn world items.
        /// </summary>
        /// <param name="itemInstance">The item instance to remove.</param>
        /// <returns>The quantity that was removed.</returns>
        public int RemoveItem(ItemInstance itemInstance)
        {
            if (itemInstance == null)
                return 0;

            int removed = 0;
            var slots = S1Storage.ItemSlots;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.ItemInstance != null && slot.ItemInstance == itemInstance.S1ItemInstance)
                {
                    int quantityToRemove = slot.Quantity;
                    slot.ClearStoredInstance();
                    removed += quantityToRemove;
                    break;
                }
            }

            return removed;
        }

        /// <summary>
        /// Attempts to remove a specific quantity of items matching the given item definition ID.
        /// This performs a soft removal (decrements quantity) and does not spawn world items.
        /// </summary>
        /// <param name="itemDefinitionId">The ID of the item definition to remove.</param>
        /// <param name="quantity">The quantity to remove.</param>
        /// <returns>The actual quantity that was removed (may be less than requested if insufficient items exist).</returns>
        public int TryRemoveQuantity(string itemDefinitionId, int quantity)
        {
            if (string.IsNullOrEmpty(itemDefinitionId) || quantity <= 0)
                return 0;

            int remaining = quantity;
            var slots = S1Storage.ItemSlots;

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.ItemInstance != null && slot.ItemInstance.ID == itemDefinitionId)
                {
                    int available = slot.Quantity;
                    int toRemove = Math.Min(remaining, available);
                    remaining -= toRemove;

                    if (toRemove >= available)
                    {
                        slot.ClearStoredInstance();
                    }
                    else
                    {
                        slot.ChangeQuantity(-toRemove);
                    }
                }
            }

            return quantity - remaining;
        }

        /// <summary>
        /// Removes all items matching the given item definition ID from this storage container.
        /// This performs a soft removal (decrements quantity) and does not spawn world items.
        /// </summary>
        /// <param name="itemDefinitionId">The ID of the item definition to remove.</param>
        /// <returns>The total quantity that was removed.</returns>
        public int RemoveAllOfDefinition(string itemDefinitionId)
        {
            if (string.IsNullOrEmpty(itemDefinitionId))
                return 0;

            int removed = 0;
            var slots = S1Storage.ItemSlots;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.ItemInstance != null && slot.ItemInstance.ID == itemDefinitionId)
                {
                    removed += slot.Quantity;
                    slot.ClearStoredInstance();
                }
            }

            return removed;
        }

        // ====== Events ======

        /// <summary>
        /// An action fired when the storage container is opened by the player.
        /// </summary>
        public event Action OnOpened
        {
            add => EventHelper.AddListener(value, h => S1Storage.onOpened += h);
            remove => EventHelper.RemoveListener(value, h => S1Storage.onOpened -= h);
        }

        /// <summary>
        /// An action fired when the storage container is closed by the player.
        /// </summary>
        public event Action OnClosed
        {
            add => EventHelper.AddListener(value, h => S1Storage.onClosed += h);
            remove => EventHelper.RemoveListener(value, h => S1Storage.onClosed -= h);
        }

        /// <summary>
        /// An action fired when the contents of the storage container change (items added or removed).
        /// </summary>
        public event Action OnContentsChanged
        {
            add => EventHelper.AddListener(value, h => S1Storage.onContentsChanged += h);
            remove => EventHelper.RemoveListener(value, h => S1Storage.onContentsChanged -= h);
        }
    }
}
