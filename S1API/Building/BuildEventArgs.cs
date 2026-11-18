using UnityEngine;
using S1API.Items;
using S1API.Storage;

namespace S1API.Building
{
    /// <summary>
    /// Event arguments for building-related events.
    /// Provides access to the item being built and the resulting GameObject.
    /// </summary>
    public class BuildEventArgs
    {
        /// <summary>
        /// The item instance that was built.
        /// </summary>
        /// <remarks>
        /// Null for <see cref="BuildEvents.OnGhostCreated"/>: the game builds the ghost from the item definition.
        /// Use <see cref="ItemId"/> there.
        /// </remarks>
        public ItemInstance ItemInstance { get; internal set; }

        /// <summary>
        /// True when <see cref="GameObject"/> is the placement ghost shown while the player places the item.
        /// </summary>
        public bool IsGhost { get; internal set; }

        private readonly string? _itemId;

        /// <summary>
        /// The GameObject created in the world.
        /// Use this to customize appearance, add components, or modify materials.
        /// </summary>
        public GameObject GameObject { get; internal set; }

        /// <summary>
        /// The item definition ID.
        /// Convenience property for filtering events by item type.
        /// </summary>
        public string? ItemId => ItemInstance?.Definition?.ID ?? _itemId;

        /// <summary>
        /// The storage entity if this item is a storage container.
        /// Returns null if the item is not a storage container.
        /// </summary>
        /// <remarks>
        /// Use this to customize storage properties when items are placed.
        /// Example: args.Storage?.AddSlots(5);
        /// </remarks>
        public StorageEntity? Storage { get; internal set; }

        /// <summary>
        /// INTERNAL: Constructor used by S1API patches.
        /// </summary>
        internal BuildEventArgs(ItemInstance itemInstance, GameObject gameObject, StorageEntity? storage = null)
        {
            ItemInstance = itemInstance;
            GameObject = gameObject;
            Storage = storage;
        }

        /// <summary>
        /// INTERNAL: Constructor for a placement ghost, which has a definition but no item instance.
        /// </summary>
        internal BuildEventArgs(string itemId, GameObject ghost)
        {
            ItemInstance = null!;
            GameObject = ghost;
            IsGhost = true;
            _itemId = itemId;
        }
    }
}
