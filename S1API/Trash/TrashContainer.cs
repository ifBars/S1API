#if IL2CPPMELON
using S1InstanceFinder = Il2CppFishNet.InstanceFinder;
using S1Trash = Il2CppScheduleOne.Trash;
#elif MONOMELON
using S1InstanceFinder = FishNet.InstanceFinder;
using S1Trash = ScheduleOne.Trash;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Trash
{
    /// <summary>
    /// Provides managed access to an existing native trash container.
    /// </summary>
    /// <remarks>
    /// This wrapper does not add persistence or replacement replication for custom containers.
    /// </remarks>
    public sealed class TrashContainer
    {
        private Action<string>? _trashAdded;
        private Action? _trashLevelChanged;
        private bool _trashAddedSubscribed;
        private bool _trashLevelChangedSubscribed;

        /// <summary>
        /// INTERNAL: The native trash container.
        /// </summary>
        internal readonly S1Trash.TrashContainer S1TrashContainer;

        /// <summary>
        /// INTERNAL: Creates a wrapper around a native trash container.
        /// </summary>
        /// <param name="trashContainer">The native trash container.</param>
        internal TrashContainer(S1Trash.TrashContainer trashContainer)
        {
            S1TrashContainer = trashContainer;
        }

        /// <summary>
        /// Gets a trash container attached directly to a game object.
        /// </summary>
        /// <param name="gameObject">The game object to inspect.</param>
        /// <returns>A trash-container wrapper, or <c>null</c> when the game object has no container component.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameObject"/> is <c>null</c> or destroyed.</exception>
        public static TrashContainer? FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            S1Trash.TrashContainer? trashContainer =
                gameObject.GetComponent<S1Trash.TrashContainer>();
            return trashContainer == null ? null : new TrashContainer(trashContainer);
        }

        /// <summary>
        /// Finds native trash containers in the loaded scene.
        /// </summary>
        /// <param name="includeInactive">Whether to include containers on inactive game objects.</param>
        /// <returns>A snapshot of the trash containers found in the scene.</returns>
        public static TrashContainer[] FindInScene(bool includeInactive = false)
        {
            var nativeContainers =
                Object.FindObjectsOfType<S1Trash.TrashContainer>(includeInactive);
            if (nativeContainers == null || nativeContainers.Length == 0)
                return Array.Empty<TrashContainer>();

            var containers = new List<TrashContainer>(nativeContainers.Length);
            for (int index = 0; index < nativeContainers.Length; index++)
            {
                S1Trash.TrashContainer nativeContainer = nativeContainers[index];
                if (nativeContainer != null)
                    containers.Add(new TrashContainer(nativeContainer));
            }

            return containers.ToArray();
        }

        /// <summary>
        /// Gets the game object that owns the native trash container.
        /// </summary>
        public GameObject? GameObject =>
            S1TrashContainer?.gameObject;

        /// <summary>
        /// Gets the maximum number of capacity units the container can hold.
        /// </summary>
        public int Capacity =>
            S1TrashContainer.TrashCapacity;

        /// <summary>
        /// Gets the current number of capacity units used by the container contents.
        /// </summary>
        public int Level =>
            S1TrashContainer.TrashLevel;

        /// <summary>
        /// Gets the current level divided by the container capacity.
        /// </summary>
        public float NormalizedLevel =>
            S1TrashContainer.NormalizedTrashLevel;

        /// <summary>
        /// Gets an immutable managed snapshot of the current content entries.
        /// </summary>
        public IReadOnlyList<TrashContentEntry> Contents
        {
            get
            {
                var entries = S1TrashContainer.Content?.Entries;
                if (entries == null || entries.Count == 0)
                    return Array.Empty<TrashContentEntry>();

                var snapshot = new List<TrashContentEntry>(entries.Count);
                for (int index = 0; index < entries.Count; index++)
                {
                    S1Trash.TrashContent.Entry entry = entries[index];
                    if (entry == null)
                        continue;

                    snapshot.Add(new TrashContentEntry(
                        entry.TrashID,
                        entry.Quantity,
                        entry.UnitSize,
                        entry.UnitValue));
                }

                return snapshot.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets whether the native container currently has enough contents to create a trash bag.
        /// </summary>
        public bool CanBeBagged =>
            S1TrashContainer.CanBeBagged();

        /// <summary>
        /// Occurs after the native container adds trash.
        /// </summary>
        public event Action<string> OnTrashAdded
        {
            add
            {
                if (value == null)
                    return;

                if (!_trashAddedSubscribed)
                {
                    global::S1API.Utils.EventHelper.AddListener(
                        HandleTrashAdded,
                        S1TrashContainer.onTrashAdded);
                    _trashAddedSubscribed = true;
                }

                _trashAdded += value;
            }
            remove
            {
                if (value == null)
                    return;

                _trashAdded -= value;
                if (_trashAdded != null || !_trashAddedSubscribed)
                    return;

                global::S1API.Utils.EventHelper.RemoveListener(
                    HandleTrashAdded,
                    S1TrashContainer.onTrashAdded);
                _trashAddedSubscribed = false;
            }
        }

        /// <summary>
        /// Occurs after the native container level changes.
        /// </summary>
        public event Action OnTrashLevelChanged
        {
            add
            {
                if (value == null)
                    return;

                if (!_trashLevelChangedSubscribed)
                {
                    global::S1API.Utils.EventHelper.AddListener(
                        HandleTrashLevelChanged,
                        S1TrashContainer.onTrashLevelChanged);
                    _trashLevelChangedSubscribed = true;
                }

                _trashLevelChanged += value;
            }
            remove
            {
                if (value == null)
                    return;

                _trashLevelChanged -= value;
                if (_trashLevelChanged != null || !_trashLevelChangedSubscribed)
                    return;

                global::S1API.Utils.EventHelper.RemoveListener(
                    HandleTrashLevelChanged,
                    S1TrashContainer.onTrashLevelChanged);
                _trashLevelChangedSubscribed = false;
            }
        }

        /// <summary>
        /// Bags the current contents through the native server-authoritative path when eligible.
        /// </summary>
        /// <returns><c>true</c> when native bagging was invoked; otherwise, <c>false</c>.</returns>
        public bool TryBagTrash()
        {
            if (!S1InstanceFinder.IsServer || !S1TrashContainer.CanBeBagged())
                return false;

            S1TrashContainer.BagTrash();
            return true;
        }

        private void HandleTrashAdded(string trashId) =>
            _trashAdded?.Invoke(trashId);

        private void HandleTrashLevelChanged() =>
            _trashLevelChanged?.Invoke();
    }
}
