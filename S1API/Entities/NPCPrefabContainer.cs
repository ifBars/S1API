using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Manages NPC prefab containers to keep the scene hierarchy organized.
    /// Creates and maintains a container hierarchy under "@Managers/@Prefabs/" 
    /// for NPC prefabs to avoid cluttering the main scene.
    /// </summary>
    internal static class NPCPrefabContainer
    {
        private const string PREFABS_PATH = "@Managers/@Prefabs";

        /// <summary>
        /// Gets or creates the root prefabs container under @Managers.
        /// </summary>
        /// <returns>The root prefabs container GameObject.</returns>
        /// <remarks>
        /// This method ensures that the "@Managers/@Prefabs" hierarchy exists
        /// for organizing NPC prefabs. If the hierarchy doesn't exist, it will be created.
        /// </remarks>
        public static GameObject GetOrCreatePrefabsContainer()
        {
            // Find or create the root prefabs container
            var prefabsContainer = GameObject.Find(PREFABS_PATH);
            if (prefabsContainer == null)
            {
                // Find or create the @Managers root
                var managers = GameObject.Find("@Managers");
                if (managers == null)
                {
                    managers = new GameObject("@Managers");
                }

                // Create the @Prefabs container
                prefabsContainer = new GameObject("@Prefabs");
                prefabsContainer.transform.SetParent(managers.transform);
            }

            return prefabsContainer;
        }

        /// <summary>
        /// Gets or creates a specific NPC prefab container.
        /// </summary>
        /// <param name="npcTypeName">The name of the NPC type (e.g., "MyCustomNPC").</param>
        /// <returns>The NPC prefab container GameObject.</returns>
        /// <remarks>
        /// This method ensures that each NPC type has its own container under the prefabs hierarchy.
        /// The hierarchy will be maintained as: "@Managers/@Prefabs/{npcTypeName}/".
        /// </remarks>
        public static GameObject GetOrCreateNPCPrefabContainer(string npcTypeName)
        {
            if (string.IsNullOrEmpty(npcTypeName))
                return null;

            var prefabsRoot = GetOrCreatePrefabsContainer();
            if (prefabsRoot == null)
                return null;

            // Find or create the NPC-specific container
            var npcContainer = prefabsRoot.transform.Find(npcTypeName);
            if (npcContainer == null)
            {
                var containerGO = new GameObject(npcTypeName);
                containerGO.transform.SetParent(prefabsRoot.transform);
                return containerGO;
            }

            return npcContainer.gameObject;
        }

        /// <summary>
        /// Places a prefab GameObject in the appropriate NPC prefab container.
        /// </summary>
        /// <param name="prefab">The prefab GameObject to organize.</param>
        /// <param name="npcTypeName">The name of the NPC type.</param>
        /// <returns>The container where the prefab was placed.</returns>
        /// <remarks>
        /// This method moves the prefab to the organized hierarchy under the NPC's container.
        /// The prefab will be inactive by default to keep it out of the main scene.
        /// </remarks>
        public static GameObject OrganizePrefab(GameObject prefab, string npcTypeName)
        {
            if (prefab == null || string.IsNullOrEmpty(npcTypeName))
                return null;

            var container = GetOrCreateNPCPrefabContainer(npcTypeName);
            if (container != null)
            {
                prefab.transform.SetParent(container.transform);
                prefab.SetActive(false); // Keep prefabs inactive
            }

            return container;
        }
    }
}
