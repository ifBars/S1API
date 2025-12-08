#if (IL2CPPMELON)
using S1Storage = Il2CppScheduleOne.Storage;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Storage = ScheduleOne.Storage;
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Storages
{
    /// <summary>
    /// Provides methods for managing and retrieving storage containers within the game.
    /// </summary>
    public static class StorageManager
    {
        /// <summary>
        /// Gets all world storage entities currently in the game.
        /// </summary>
        /// <returns>An array of storage instances representing all world storage entities.</returns>
        public static StorageInstance[] GetAll()
        {
            var storages = new List<StorageInstance>();

            if (S1Storage.WorldStorageEntity.All == null)
                return storages.ToArray();

            foreach (var storage in S1Storage.WorldStorageEntity.All)
            {
                if (storage != null)
                {
                    storages.Add(new StorageInstance(storage));
                }
            }

            return storages.ToArray();
        }

        /// <summary>
        /// Finds a storage entity by its display name.
        /// </summary>
        /// <param name="name">The display name of the storage entity to find.</param>
        /// <returns>A storage instance if found; otherwise, null.</returns>
        public static StorageInstance? FindByName(string name)
        {
            if (string.IsNullOrEmpty(name) || S1Storage.WorldStorageEntity.All == null)
                return null;

            foreach (var storage in S1Storage.WorldStorageEntity.All)
            {
                if (storage != null && storage.StorageEntityName == name)
                {
                    return new StorageInstance(storage);
                }
            }

            return null;
        }

        /// <summary>
        /// Finds storage entities matching a given predicate.
        /// </summary>
        /// <param name="predicate">A function to test each storage entity for a condition.</param>
        /// <returns>An array of storage instances that match the predicate.</returns>
        public static StorageInstance[] FindByPredicate(Func<StorageInstance, bool> predicate)
        {
            if (predicate == null || S1Storage.WorldStorageEntity.All == null)
                return Array.Empty<StorageInstance>();

            var results = new List<StorageInstance>();

            foreach (var storage in S1Storage.WorldStorageEntity.All)
            {
                if (storage != null)
                {
                    var instance = new StorageInstance(storage);
                    if (predicate(instance))
                    {
                        results.Add(instance);
                    }
                }
            }

            return results.ToArray();
        }
    }
}

