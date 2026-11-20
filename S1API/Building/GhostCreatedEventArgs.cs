using UnityEngine;

namespace S1API.Building
{
    /// <summary>
    /// Event arguments for <see cref="BuildEvents.OnGhostCreated"/>.
    /// The game builds a placement ghost from the item definition, so there is no item instance.
    /// </summary>
    public class GhostCreatedEventArgs
    {
        /// <summary>
        /// The placement ghost shown while the player places the item.
        /// </summary>
        public GameObject GameObject { get; }

        /// <summary>
        /// The definition ID of the item being placed.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// INTERNAL: Constructor used by S1API patches.
        /// </summary>
        internal GhostCreatedEventArgs(string itemId, GameObject ghost)
        {
            ItemId = itemId;
            GameObject = ghost;
        }
    }
}
