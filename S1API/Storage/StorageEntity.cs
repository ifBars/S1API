#if (IL2CPPMELON)
using S1Storage = Il2CppScheduleOne.Storage;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using Il2CppInterop.Runtime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Storage = ScheduleOne.Storage;
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
#endif

using S1API.Items;
using S1API.Logging;
using System;

namespace S1API.Storage
{
    /// <summary>
    /// Represents a storage entity in the game world.
    /// Provides cross-runtime safe manipulation of storage slots and properties.
    /// </summary>
    /// <remarks>
    /// Use this wrapper to modify storage containers without dealing with Il2Cpp/Mono differences.
    /// All slot manipulation is handled safely with automatic UI updates.
    /// </remarks>
    public sealed class StorageEntity
    {
        private static readonly Log Logger = new Log("StorageEntity");

        /// <summary>
        /// INTERNAL: Reference to the native storage entity.
        /// </summary>
        internal readonly S1Storage.StorageEntity S1StorageEntity;

        /// <summary>
        /// INTERNAL: Reference to the placeable storage entity (if this is placeable storage).
        /// </summary>
        internal readonly S1ObjectScripts.PlaceableStorageEntity S1PlaceableStorageEntity;

        /// <summary>
        /// INTERNAL: Constructor for wrapping storage entities.
        /// </summary>
        internal StorageEntity(S1Storage.StorageEntity storageEntity, S1ObjectScripts.PlaceableStorageEntity placeableStorage = null)
        {
            S1StorageEntity = storageEntity ?? throw new ArgumentNullException(nameof(storageEntity));
            S1PlaceableStorageEntity = placeableStorage;
        }

        // ====== Properties ======

        /// <summary>
        /// Current number of slots in this storage container.
        /// Setting this value will add or remove slots as needed.
        /// </summary>
        public int SlotCount
        {
            get => S1StorageEntity.SlotCount;
            set
            {
                if (value < 0)
                {
                    Logger.Warning("Cannot set SlotCount to negative value");
                    return;
                }

                if (value > MaxSlots)
                {
                    Logger.Warning($"Cannot set SlotCount to {value}, maximum is {MaxSlots}");
                    return;
                }

                int currentCount = S1StorageEntity.ItemSlots.Count;
                if (value > currentCount)
                {
                    AddSlots(value - currentCount);
                }
                else if (value < currentCount)
                {
                    RemoveSlots(currentCount - value);
                }
            }
        }

        /// <summary>
        /// Maximum number of slots this storage can have.
        /// Default is 20 for most storage types.
        /// </summary>
        public int MaxSlots { get; set; } = 20;

        /// <summary>
        /// Number of rows to display in the storage UI.
        /// Automatically set to 2 when SlotCount exceeds 6.
        /// </summary>
        public int DisplayRowCount
        {
            get => S1StorageEntity.DisplayRowCount;
            set => S1StorageEntity.DisplayRowCount = value;
        }

        /// <summary>
        /// Whether slots in this storage are filterable (can restrict item types).
        /// </summary>
        public bool SlotsAreFilterable =>
            S1StorageEntity.SlotsAreFilterable;

        /// <summary>
        /// The item instance this storage is part of (e.g., the storage rack item).
        /// Returns null if this storage is not placeable.
        /// </summary>
        public ItemInstance ItemInstance
        {
            get
            {
                if (S1PlaceableStorageEntity?.ItemInstance == null)
                    return null;

                return new ItemInstance(S1PlaceableStorageEntity.ItemInstance);
            }
        }

        /// <summary>
        /// The item ID of the storage container.
        /// Convenience property for filtering by storage type.
        /// Returns null if this storage is not placeable.
        /// </summary>
        public string ItemId =>
            S1PlaceableStorageEntity?.ItemInstance?.Definition?.ID;

        /// <summary>
        /// Whether this storage entity is placeable in the world.
        /// </summary>
        public bool IsPlaceable =>
            S1PlaceableStorageEntity != null;

        // ====== Slot Manipulation Methods ======

        /// <summary>
        /// Adds the specified number of slots to this storage container.
        /// Automatically updates input/output slots for placeable storage.
        /// </summary>
        /// <param name="count">Number of slots to add (must be positive)</param>
        /// <returns>True if slots were added successfully</returns>
        /// <remarks>
        /// This method handles all runtime-specific logic internally.
        /// Display row count is automatically adjusted for > 6 slots.
        /// </remarks>
        public bool AddSlots(int count)
        {
            if (count <= 0)
            {
                Logger.Warning("AddSlots called with non-positive count");
                return false;
            }

            int currentCount = S1StorageEntity.ItemSlots.Count;
            int newTotal = currentCount + count;

            if (newTotal > MaxSlots)
            {
                Logger.Warning($"Cannot add {count} slots, would exceed MaxSlots ({MaxSlots})");
                return false;
            }

            try
            {
                // Create new slots with runtime-appropriate owner casting
                for (int i = 0; i < count; i++)
                {
                    var slot = new S1ItemFramework.ItemSlot(S1StorageEntity.SlotsAreFilterable);

#if (IL2CPPMELON || IL2CPPBEPINEX)
                    // Il2Cpp requires explicit interface casting
                    slot.SetSlotOwner(S1StorageEntity.Cast<S1ItemFramework.IItemSlotOwner>());
#else
                    // Mono can use implicit casting
                    slot.SetSlotOwner(S1StorageEntity);
#endif
                }

                // Update slot count
                S1StorageEntity.SlotCount = newTotal;

                // Update input/output slots for placeable storage
                if (S1PlaceableStorageEntity != null)
                {
                    int newSlotCount = S1StorageEntity.ItemSlots.Count;
                    for (int i = currentCount; i < newSlotCount; i++)
                    {
                        var itemSlot = S1StorageEntity.ItemSlots[i];
                        S1PlaceableStorageEntity.InputSlots.Add(itemSlot);
                        S1PlaceableStorageEntity.OutputSlots.Add(itemSlot);
                    }
                }

                // Auto-adjust display rows for larger storages
                if (newTotal > 6 && DisplayRowCount < 2)
                {
                    DisplayRowCount = 2;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to add slots: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes the specified number of slots from this storage container.
        /// Only removes empty slots from the end.
        /// </summary>
        /// <param name="count">Number of slots to remove (must be positive)</param>
        /// <returns>True if slots were removed successfully</returns>
        /// <remarks>
        /// This method will fail if any of the slots to be removed contain items.
        /// Always check slot contents before removing.
        /// </remarks>
        public bool RemoveSlots(int count)
        {
            if (count <= 0)
            {
                Logger.Warning("RemoveSlots called with non-positive count");
                return false;
            }

            int currentCount = S1StorageEntity.ItemSlots.Count;
            if (count > currentCount)
            {
                Logger.Warning($"Cannot remove {count} slots, only {currentCount} exist");
                return false;
            }

            try
            {
                // Check if slots to remove are empty
                for (int i = currentCount - count; i < currentCount; i++)
                {
                    if (S1StorageEntity.ItemSlots[i].ItemInstance != null)
                    {
                        Logger.Warning($"Cannot remove slot {i}, it contains an item");
                        return false;
                    }
                }

                // Remove slots from the end
                for (int i = 0; i < count; i++)
                {
                    int lastIndex = S1StorageEntity.ItemSlots.Count - 1;
                    var slot = S1StorageEntity.ItemSlots[lastIndex];

                    // Remove from placeable storage lists
                    if (S1PlaceableStorageEntity != null)
                    {
                        S1PlaceableStorageEntity.InputSlots.Remove(slot);
                        S1PlaceableStorageEntity.OutputSlots.Remove(slot);
                    }

                    // Remove the slot itself
                    S1StorageEntity.ItemSlots.RemoveAt(lastIndex);
                }

                // Update slot count
                S1StorageEntity.SlotCount = currentCount - count;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to remove slots: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the total number of slots, expanding or contracting as needed.
        /// </summary>
        /// <param name="targetSlotCount">Target number of slots</param>
        /// <returns>True if operation was successful</returns>
        public bool SetSlotCount(int targetSlotCount)
        {
            if (targetSlotCount < 0)
            {
                Logger.Warning("Cannot set negative slot count");
                return false;
            }

            int currentCount = S1StorageEntity.ItemSlots.Count;
            int difference = targetSlotCount - currentCount;

            if (difference > 0)
            {
                return AddSlots(difference);
            }
            else if (difference < 0)
            {
                return RemoveSlots(-difference);
            }

            return true; // No change needed
        }

        // ====== Helper Methods ======

        /// <summary>
        /// Gets the number of empty slots in this storage.
        /// </summary>
        public int GetEmptySlotCount()
        {
            int emptyCount = 0;
            foreach (var slot in S1StorageEntity.ItemSlots)
            {
                if (slot.ItemInstance == null)
                    emptyCount++;
            }
            return emptyCount;
        }

        /// <summary>
        /// Gets the number of occupied slots in this storage.
        /// </summary>
        public int GetOccupiedSlotCount() =>
            S1StorageEntity.ItemSlots.Count - GetEmptySlotCount();

        /// <summary>
        /// Checks if this storage has any items in it.
        /// </summary>
        public bool HasItems()
        {
            foreach (var slot in S1StorageEntity.ItemSlots)
            {
                if (slot.ItemInstance != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if this storage is completely empty.
        /// </summary>
        public bool IsEmpty() =>
            !HasItems();
    }
}
