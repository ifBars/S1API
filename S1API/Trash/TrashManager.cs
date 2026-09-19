using System;
using System.Collections.Generic;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Internal.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

#if (IL2CPPMELON)
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1Trash = Il2CppScheduleOne.Trash;
#elif MONOMELON
using S1Trash = ScheduleOne.Trash;
#endif

namespace S1API.Trash
{
    /// <summary>
    /// Provides management over trash items in the game.
    /// </summary>
    public static class TrashManager
    {
        private static readonly Log Logger = new Log("TrashManager");
        private static readonly object RegistrationGate = new object();
        private static readonly Dictionary<string, S1Trash.TrashItem>
            RegisteredPrefabs =
                new Dictionary<string, S1Trash.TrashItem>(StringComparer.OrdinalIgnoreCase);
        private static GameObject? _prefabRoot;
        private static bool _lifecycleSubscribed;

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
            var manager = S1Trash.TrashManager.Instance;
            var nativePrefab = manager != null
                ? manager.GetTrashPrefab(id)
                : null;
            if (nativePrefab != null)
                return nativePrefab.gameObject;

            lock (RegistrationGate)
            {
                return RegisteredPrefabs.TryGetValue(id, out var registered) &&
                    registered != null
                    ? registered.gameObject
                    : null;
            }
        }

        /// <summary>
        /// Registers a stable trash prefab that can be spawned by ID and referenced by station items.
        /// </summary>
        /// <remarks>
        /// The supplied prefab is cloned under an inactive, persistent cache root. Registrations are
        /// reapplied after game loads so network trash spawning can resolve the same ID on every client.
        /// All clients must register the same ID and prefab behavior for multiplayer consistency.
        /// </remarks>
        /// <param name="id">Stable trash ID used for spawning and persistence.</param>
        /// <param name="trashPrefab">Prefab containing a native TrashItem component.</param>
        /// <param name="replaceExisting">Whether an existing registration with the same ID may be replaced.</param>
        /// <returns>The cached registered prefab.</returns>
        public static GameObject RegisterTrashPrefab(
            string id,
            GameObject trashPrefab,
            bool replaceExisting = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Trash ID is required.", nameof(id));
            if (trashPrefab == null)
                throw new ArgumentNullException(nameof(trashPrefab));

            var source = trashPrefab.GetComponent<S1Trash.TrashItem>();
            if (source == null)
            {
                throw new ArgumentException(
                    "Trash prefab must have a TrashItem component.",
                    nameof(trashPrefab));
            }

            S1Trash.TrashItem cached;
            lock (RegistrationGate)
            {
                if (RegisteredPrefabs.TryGetValue(id, out var existing) &&
                    existing != null)
                {
                    if (!replaceExisting)
                    {
                        Logger.Warning(
                            $"Trash ID '{id}' is already registered. " +
                            $"Keeping '{existing.gameObject.name}' and ignoring " +
                            $"'{trashPrefab.name}'. Set replaceExisting to true " +
                            "to replace the existing prefab.");
                        return existing.gameObject;
                    }

                    Object.Destroy(existing.gameObject);
                }

                var root = GetOrCreatePrefabRoot();
                cached = Object.Instantiate(source, root.transform, false);
                cached.name = $"S1API_Trash_{id}";
                ReflectionUtils.TrySetFieldOrProperty(cached, "Id", id);
                cached.gameObject.hideFlags = HideFlags.HideAndDontSave;
                cached.gameObject.SetActive(true);
                RegisteredPrefabs[id] = cached;
                EnsureLifecycleSubscription();
            }

            ApplyRegistration(id, cached, replaceExisting: true);
            return cached.gameObject;
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

        private static void EnsureLifecycleSubscription()
        {
            if (_lifecycleSubscribed)
                return;

            GameLifecycle.OnPreLoad += ApplyAllRegistrations;
            GameLifecycle.OnLoadComplete += ApplyAllRegistrations;
            _lifecycleSubscribed = true;
        }

        private static void ApplyAllRegistrations()
        {
            KeyValuePair<string, S1Trash.TrashItem>[] registrations;
            lock (RegistrationGate)
            {
                registrations =
                    new KeyValuePair<string, S1Trash.TrashItem>[RegisteredPrefabs.Count];
                int index = 0;
                foreach (var registration in RegisteredPrefabs)
                    registrations[index++] = registration;
            }

            foreach (var registration in registrations)
            {
                if (registration.Value != null)
                {
                    ApplyRegistration(
                        registration.Key,
                        registration.Value,
                        replaceExisting: true);
                }
            }
        }

        private static void ApplyRegistration(
            string id,
            S1Trash.TrashItem prefab,
            bool replaceExisting)
        {
            var manager = S1Trash.TrashManager.Instance;
            if (manager == null)
                return;

#if (IL2CPPMELON)
            Il2CppReferenceArray<S1Trash.TrashItem>? prefabs = manager.TrashPrefabs;
            int count = prefabs?.Length ?? 0;
            for (int index = 0; index < count; index++)
            {
                var existing = prefabs![index];
                if (!string.Equals(existing?.ID, id, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (replaceExisting)
                {
                    prefabs[index] = prefab;
                    manager.TrashPrefabs = prefabs;
                }
                return;
            }

            var expanded = new Il2CppReferenceArray<S1Trash.TrashItem>(count + 1);
            for (int index = 0; index < count; index++)
                expanded[index] = prefabs![index];
            expanded[count] = prefab;
            manager.TrashPrefabs = expanded;
#else
            S1Trash.TrashItem[] prefabs =
                manager.TrashPrefabs ?? Array.Empty<S1Trash.TrashItem>();
            for (int index = 0; index < prefabs.Length; index++)
            {
                var existing = prefabs[index];
                if (!string.Equals(existing?.ID, id, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (replaceExisting)
                {
                    prefabs[index] = prefab;
                    manager.TrashPrefabs = prefabs;
                }
                return;
            }

            Array.Resize(ref prefabs, prefabs.Length + 1);
            prefabs[prefabs.Length - 1] = prefab;
            manager.TrashPrefabs = prefabs;
#endif
        }

        private static GameObject GetOrCreatePrefabRoot()
        {
            if (_prefabRoot != null)
                return _prefabRoot;

            _prefabRoot = new GameObject("S1API_TrashPrefabs")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _prefabRoot.SetActive(false);
            Object.DontDestroyOnLoad(_prefabRoot);
            return _prefabRoot;
        }
    }
}
