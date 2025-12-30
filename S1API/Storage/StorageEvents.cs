using System;
using S1API.Logging;

namespace S1API.Storage
{
    /// <summary>
    /// Event arguments for storage-specific events.
    /// </summary>
    public class StorageEventArgs
    {
        /// <summary>
        /// The storage entity involved in the event.
        /// </summary>
        public StorageEntity Storage { get; internal set; }

        /// <summary>
        /// The item ID of the storage container.
        /// Convenience property for filtering by storage type.
        /// </summary>
        public string ItemId => Storage?.ItemId;

        /// <summary>
        /// INTERNAL: Constructor used by S1API patches.
        /// </summary>
        internal StorageEventArgs(StorageEntity storage)
        {
            Storage = storage;
        }
    }

    /// <summary>
    /// Event arguments for storage loading operations.
    /// Raised when storage is being loaded from a save file.
    /// </summary>
    public class StorageLoadingEventArgs : StorageEventArgs
    {
        /// <summary>
        /// Number of items being loaded into this storage.
        /// Use this to determine if slot expansion is needed.
        /// </summary>
        public int ItemCountBeingLoaded { get; internal set; }

        /// <summary>
        /// Current slot count before loading.
        /// </summary>
        public int CurrentSlotCount => Storage?.SlotCount ?? 0;

        /// <summary>
        /// Whether additional slots are needed to fit all items.
        /// </summary>
        public bool NeedsMoreSlots => ItemCountBeingLoaded > CurrentSlotCount;

        /// <summary>
        /// Number of additional slots needed to fit all items.
        /// Returns 0 if no additional slots are needed.
        /// </summary>
        public int AdditionalSlotsNeeded =>
            Math.Max(0, ItemCountBeingLoaded - CurrentSlotCount);

        /// <summary>
        /// INTERNAL: Constructor used by S1API patches.
        /// </summary>
        internal StorageLoadingEventArgs(StorageEntity storage, int itemCount) : base(storage)
        {
            ItemCountBeingLoaded = itemCount;
        }
    }

    /// <summary>
    /// Provides events for the storage system.
    /// Subscribe to these events instead of patching PlaceableStorageEntity or ItemSet.
    /// </summary>
    /// <example>
    /// <code>
    /// // Expand storage slots when placed
    /// StorageEvents.OnStorageCreated += (args) =>
    /// {
    ///     if (args.ItemId == "my_custom_storage")
    ///     {
    ///         args.Storage.AddSlots(10);
    ///     }
    /// };
    ///
    /// // Expand storage slots when loading from save
    /// StorageEvents.OnStorageLoading += (args) =>
    /// {
    ///     if (args.ItemId == "my_custom_storage" && args.NeedsMoreSlots)
    ///     {
    ///         args.Storage.AddSlots(args.AdditionalSlotsNeeded);
    ///     }
    /// };
    /// </code>
    /// </example>
    public static class StorageEvents
    {
        private static readonly Log Logger = new Log("StorageEvents");

        /// <summary>
        /// Event raised after a storage entity is created and initialized in the world.
        /// This event fires when storage items are placed by the player.
        /// </summary>
        /// <remarks>
        /// Use this event to customize storage properties when items are placed.
        /// This is the primary event for expanding storage slots on placement.
        /// </remarks>
        public static event Action<StorageEventArgs> OnStorageCreated;

        /// <summary>
        /// Event raised before items are loaded into storage from a save file.
        /// Use this event to expand storage slots to accommodate saved items.
        /// </summary>
        /// <remarks>
        /// This event is critical for save compatibility when you've expanded storage slots.
        /// Check args.NeedsMoreSlots to determine if expansion is required.
        /// </remarks>
        public static event Action<StorageLoadingEventArgs> OnStorageLoading;

        /// <summary>
        /// Event raised just before the storage menu opens for a storage entity.
        /// Use this event to update the display name or perform other pre-open actions.
        /// </summary>
        /// <remarks>
        /// This event is useful for syncing custom names (set via clipboard) to the display name.
        /// The base game's StorageMenu uses StorageEntityName for display, but custom names
        /// are stored separately in Configuration.Name. Subscribe to this event and call
        /// <see cref="StorageEntity.SyncCustomNameToDisplayName"/> to ensure custom names are shown.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Ensure custom names are displayed when opening storage
        /// StorageEvents.OnStorageOpening += (args) =>
        /// {
        ///     args.Storage.SyncCustomNameToDisplayName();
        /// };
        /// </code>
        /// </example>
        public static event Action<StorageEventArgs> OnStorageOpening;

        /// <summary>
        /// INTERNAL: Raises the OnStorageCreated event.
        /// Called by Harmony patches in S1API.Internal.Patches.StoragePatches.
        /// </summary>
        internal static void RaiseStorageCreated(StorageEventArgs args)
        {
            if (args == null)
                return;

            try
            {
                OnStorageCreated?.Invoke(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in OnStorageCreated handler for storage '{args.ItemId}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Raises the OnStorageLoading event.
        /// Called by Harmony patches in S1API.Internal.Patches.StoragePatches.
        /// </summary>
        internal static void RaiseStorageLoading(StorageLoadingEventArgs args)
        {
            if (args == null)
                return;

            try
            {
                OnStorageLoading?.Invoke(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in OnStorageLoading handler for storage '{args.ItemId}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Raises the OnStorageOpening event.
        /// Called by Harmony patches in S1API.Internal.Patches.StoragePatches.
        /// </summary>
        internal static void RaiseStorageOpening(StorageEventArgs args)
        {
            if (args == null)
                return;

            try
            {
                OnStorageOpening?.Invoke(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in OnStorageOpening handler for storage '{args.ItemId}': {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
