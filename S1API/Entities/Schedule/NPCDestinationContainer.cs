using UnityEngine;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Manages destination marker containers for NPCs to keep the scene hierarchy organized.
    /// Creates and maintains a container hierarchy under "@Managers/@NPCs/NPC Containers/" 
    /// for each NPC's destination markers.
    /// </summary>
    internal static class NPCDestinationContainer
    {
        private const string NPC_CONTAINERS_PATH = "@Managers/@NPCs/NPC Containers";

        /// <summary>
        /// Gets or creates a container GameObject for the specified NPC's destination markers.
        /// </summary>
        /// <param name="npcName">The name of the NPC.</param>
        /// <returns>The container GameObject for this NPC's destination markers.</returns>
        /// <remarks>
        /// This method ensures that each NPC has its own container under the NPC Containers hierarchy.
        /// The container will be created if it doesn't exist, and the hierarchy will be maintained
        /// as: "@Managers/@NPCs/NPC Containers/{npcName}/".
        /// </remarks>
        public static GameObject GetOrCreateContainer(string npcName)
        {
            if (string.IsNullOrEmpty(npcName))
                return null;

            // Find or create the root NPC Containers object
            var containersRoot = GameObject.Find(NPC_CONTAINERS_PATH);
            if (containersRoot == null)
            {
                // Create the hierarchy if it doesn't exist
                var managers = GameObject.Find("@Managers");
                if (managers == null)
                {
                    managers = new GameObject("@Managers");
                }

                var npcs = managers.transform.Find("@NPCs");
                if (npcs == null)
                {
                    npcs = new GameObject("@NPCs").transform;
                    npcs.SetParent(managers.transform);
                }

                containersRoot = new GameObject("NPC Containers");
                containersRoot.transform.SetParent(npcs);
            }

            // Find or create the NPC-specific container
            var npcContainer = containersRoot.transform.Find(npcName);
            if (npcContainer == null)
            {
                var containerGO = new GameObject(npcName);
                containerGO.transform.SetParent(containersRoot.transform);
                return containerGO;
            }

            return npcContainer.gameObject;
        }

        /// <summary>
        /// Creates a destination marker GameObject as a child of the NPC's container.
        /// </summary>
        /// <param name="npcName">The name of the NPC.</param>
        /// <param name="markerName">The name for the marker GameObject.</param>
        /// <param name="position">The world position for the marker.</param>
        /// <param name="forward">The optional forward direction for the marker.</param>
        /// <returns>A transform representing the destination marker.</returns>
        /// <remarks>
        /// This method creates a destination marker GameObject and places it in the NPC's
        /// dedicated container to keep the scene hierarchy organized. If no forward direction
        /// is specified, the marker will face forward (0,0,1).
        /// </remarks>
        public static Transform CreateDestinationMarker(string npcName, string markerName, Vector3 position, Vector3? forward = null)
        {
            var container = GetOrCreateContainer(npcName);
            if (container == null)
                return null;

            var markerGO = new GameObject(markerName);
            markerGO.transform.SetParent(container.transform);
            markerGO.transform.position = position;
            
            if (forward.HasValue && forward.Value.sqrMagnitude > 0.001f)
                markerGO.transform.forward = forward.Value.normalized;
            else
                markerGO.transform.forward = Vector3.forward;

            return markerGO.transform;
        }
    }
}
