using System;
using UnityEngine;

#if (IL2CPPMELON)
using S1Trash = Il2CppScheduleOne.Trash;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Trash = ScheduleOne.Trash;
#endif

namespace S1API.Trash
{
    /// <summary>
    /// Provides management over trash items in the game.
    /// </summary>
    public static class TrashManager
    {
        /// <summary>
        /// Maximum number of trash items allowed in the world (2000).
        /// </summary>
        public const int TrashItemLimit = 2000;

        /// <summary>
        /// Creates a trash item at the specified position.
        /// </summary>
        /// <param name="id">The ID of the trash item to create.</param>
        /// <param name="position">The position to create the trash at.</param>
        /// <param name="rotation">The rotation of the trash item.</param>
        /// <param name="initialVelocity">Optional initial velocity.</param>
        /// <param name="guid">Optional GUID (auto-generated if empty).</param>
        /// <returns>The created trash item GameObject, or null if creation failed.</returns>
        public static GameObject? CreateTrashItem(string id, Vector3 position, Quaternion rotation, 
            Vector3 initialVelocity = default, string guid = "")
        {
            var trashItem = S1Trash.TrashManager.Instance.CreateTrashItem(id, position, rotation, initialVelocity, guid);
            return trashItem?.gameObject;
        }

        /// <summary>
        /// Destroys all trash items in the world.
        /// Only works if called on the server/host.
        /// </summary>
        public static void DestroyAllTrash() => S1Trash.TrashManager.Instance.DestroyAllTrash();

        /// <summary>
        /// Gets a trash prefab by its ID.
        /// </summary>
        /// <param name="id">The ID of the trash prefab.</param>
        /// <returns>The trash prefab GameObject, or null if not found.</returns>
        public static GameObject? GetTrashPrefab(string id)
        {
            var prefab = S1Trash.TrashManager.Instance.GetTrashPrefab(id);
            return prefab?.gameObject;
        }

        /// <summary>
        /// Gets a random trash prefab based on generation chances.
        /// </summary>
        /// <returns>A random trash prefab GameObject.</returns>
        public static GameObject? GetRandomTrashPrefab()
        {
            var prefab = S1Trash.TrashManager.Instance.GetRandomGeneratableTrashPrefab();
            return prefab?.gameObject;
        }
    }
}
