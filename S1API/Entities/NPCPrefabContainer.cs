using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Manages NPC prefab containers to keep the scene hierarchy organized and persistent
    /// across scene loads. Prefabs are parented under a dedicated root that is marked as
    /// DontDestroyOnLoad so host and clients share the same configured prefabs before the
    /// gameplay scene initializes.
    /// </summary>
    internal static class NPCPrefabContainer
    {
        private const string RootName = "@S1API_PersistentPrefabs";
        private static GameObject _persistentRoot;

        /// <summary>
        /// Gets or creates the persistent prefab root that survives scene loads.
        /// </summary>
        /// <returns>The root prefabs container GameObject.</returns>
        /// <remarks>
        /// This method ensures the persistent prefab root exists and is marked as
        /// DontDestroyOnLoad so configured prefabs remain available across scenes.
        /// </remarks>
        public static GameObject GetOrCreatePrefabsContainer()
        {
            if (_persistentRoot != null)
                return _persistentRoot;

            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                _persistentRoot = existing;
            }
            else
            {
                _persistentRoot = new GameObject(RootName);
            }

            Object.DontDestroyOnLoad(_persistentRoot);
            return _persistentRoot;
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

            var npcContainer = prefabsRoot.transform.Find(npcTypeName);
            if (npcContainer == null)
            {
                var containerGO = new GameObject(npcTypeName);
                containerGO.transform.SetParent(prefabsRoot.transform, false);
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
                prefab.transform.SetParent(container.transform, false);
                prefab.SetActive(false); // Keep prefabs inactive
            }

            return container;
        }
    }
}
