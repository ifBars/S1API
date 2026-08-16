#if IL2CPPMELON
using Il2CppInterop.Runtime;
using NativeTrashAddedAction = UnityEngine.Events.UnityAction<string>;
using NativeTrashLevelChangedAction = UnityEngine.Events.UnityAction;
using S1InstanceFinder = Il2CppFishNet.InstanceFinder;
using S1Trash = Il2CppScheduleOne.Trash;
#elif MONOMELON
using NativeTrashAddedAction = System.Action<string>;
using NativeTrashLevelChangedAction = System.Action;
using S1InstanceFinder = FishNet.InstanceFinder;
using S1Trash = ScheduleOne.Trash;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
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
        private static readonly Dictionary<int, TrashAddedRegistrationState> TrashAddedRegistrations =
            new Dictionary<int, TrashAddedRegistrationState>();

        private static readonly Dictionary<int, TrashLevelChangedRegistrationState> TrashLevelChangedRegistrations =
            new Dictionary<int, TrashLevelChangedRegistrationState>();

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

            PruneDestroyedRegistrationStates();
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
            PruneDestroyedRegistrationStates();
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
        /// <remarks>
        /// A handler may be removed through any wrapper for the same native container.
        /// Duplicate subscriptions are removed one at a time.
        /// </remarks>
        public event Action<string> OnTrashAdded
        {
            add
            {
                if (value == null)
                    return;

                NativeTrashAddedAction nativeHandler = CreateNativeTrashAddedHandler(value);
                SubscribeTrashAdded(nativeHandler);
                GetTrashAddedRegistrationState().Registrations.Add(value, nativeHandler);
            }
            remove
            {
                if (value == null || !TryTakeTrashAddedRegistration(
                        value,
                        out TrashAddedRegistrationState state,
                        out NativeTrashAddedAction nativeHandler))
                    return;

                try
                {
                    UnsubscribeTrashAdded(nativeHandler);
                }
                catch
                {
                    state.Registrations.Add(value, nativeHandler);
                    throw;
                }

                if (state.Registrations.IsEmpty)
                    TrashAddedRegistrations.Remove(S1TrashContainer.GetInstanceID());
            }
        }

        /// <summary>
        /// Occurs after the native container level changes.
        /// </summary>
        /// <remarks>
        /// A handler may be removed through any wrapper for the same native container.
        /// Duplicate subscriptions are removed one at a time.
        /// </remarks>
        public event Action OnTrashLevelChanged
        {
            add
            {
                if (value == null)
                    return;

                NativeTrashLevelChangedAction nativeHandler = CreateNativeTrashLevelChangedHandler(value);
                SubscribeTrashLevelChanged(nativeHandler);
                GetTrashLevelChangedRegistrationState().Registrations.Add(value, nativeHandler);
            }
            remove
            {
                if (value == null || !TryTakeTrashLevelChangedRegistration(
                        value,
                        out TrashLevelChangedRegistrationState state,
                        out NativeTrashLevelChangedAction nativeHandler))
                    return;

                try
                {
                    UnsubscribeTrashLevelChanged(nativeHandler);
                }
                catch
                {
                    state.Registrations.Add(value, nativeHandler);
                    throw;
                }

                if (state.Registrations.IsEmpty)
                    TrashLevelChangedRegistrations.Remove(S1TrashContainer.GetInstanceID());
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

        private static NativeTrashAddedAction CreateNativeTrashAddedHandler(Action<string> handler)
        {
#if IL2CPPMELON
            return DelegateSupport.ConvertDelegate<NativeTrashAddedAction>(handler)
                ?? throw new InvalidOperationException("Could not create the native trash-added delegate.");
#else
            return trashId => handler(trashId);
#endif
        }

        private static NativeTrashLevelChangedAction CreateNativeTrashLevelChangedHandler(Action handler)
        {
#if IL2CPPMELON
            return DelegateSupport.ConvertDelegate<NativeTrashLevelChangedAction>(handler)
                ?? throw new InvalidOperationException("Could not create the native trash-level delegate.");
#else
            return () => handler();
#endif
        }

        private void SubscribeTrashAdded(NativeTrashAddedAction handler)
        {
#if IL2CPPMELON
            S1TrashContainer.onTrashAdded.AddListener(handler);
#else
            global::S1API.Utils.EventHelper.AddListener(handler, S1TrashContainer.onTrashAdded);
#endif
        }

        private void UnsubscribeTrashAdded(NativeTrashAddedAction handler)
        {
#if IL2CPPMELON
            S1TrashContainer.onTrashAdded.RemoveListener(handler);
#else
            global::S1API.Utils.EventHelper.RemoveListener(handler, S1TrashContainer.onTrashAdded);
#endif
        }

        private void SubscribeTrashLevelChanged(NativeTrashLevelChangedAction handler)
        {
#if IL2CPPMELON
            S1TrashContainer.onTrashLevelChanged.AddListener(handler);
#else
            global::S1API.Utils.EventHelper.AddListener(handler, S1TrashContainer.onTrashLevelChanged);
#endif
        }

        private void UnsubscribeTrashLevelChanged(NativeTrashLevelChangedAction handler)
        {
#if IL2CPPMELON
            S1TrashContainer.onTrashLevelChanged.RemoveListener(handler);
#else
            global::S1API.Utils.EventHelper.RemoveListener(handler, S1TrashContainer.onTrashLevelChanged);
#endif
        }

        private TrashAddedRegistrationState GetTrashAddedRegistrationState()
        {
            PruneDestroyedRegistrationStates();
            int instanceId = S1TrashContainer.GetInstanceID();
            if (TrashAddedRegistrations.TryGetValue(instanceId, out TrashAddedRegistrationState? state))
                return state;

            state = new TrashAddedRegistrationState(S1TrashContainer);
            TrashAddedRegistrations.Add(instanceId, state);
            return state;
        }

        private bool TryTakeTrashAddedRegistration(
            Action<string> managedHandler,
            out TrashAddedRegistrationState state,
            out NativeTrashAddedAction nativeHandler)
        {
            PruneDestroyedRegistrationStates();
            if (TrashAddedRegistrations.TryGetValue(
                    S1TrashContainer.GetInstanceID(),
                    out TrashAddedRegistrationState? registrationState)
                && registrationState.Registrations.TryTakeLast(managedHandler, out nativeHandler))
            {
                state = registrationState;
                return true;
            }

            state = null!;
            nativeHandler = null!;
            return false;
        }

        private TrashLevelChangedRegistrationState GetTrashLevelChangedRegistrationState()
        {
            PruneDestroyedRegistrationStates();
            int instanceId = S1TrashContainer.GetInstanceID();
            if (TrashLevelChangedRegistrations.TryGetValue(
                    instanceId,
                    out TrashLevelChangedRegistrationState? state))
                return state;

            state = new TrashLevelChangedRegistrationState(S1TrashContainer);
            TrashLevelChangedRegistrations.Add(instanceId, state);
            return state;
        }

        private bool TryTakeTrashLevelChangedRegistration(
            Action managedHandler,
            out TrashLevelChangedRegistrationState state,
            out NativeTrashLevelChangedAction nativeHandler)
        {
            PruneDestroyedRegistrationStates();
            if (TrashLevelChangedRegistrations.TryGetValue(
                    S1TrashContainer.GetInstanceID(),
                    out TrashLevelChangedRegistrationState? registrationState)
                && registrationState.Registrations.TryTakeLast(managedHandler, out nativeHandler))
            {
                state = registrationState;
                return true;
            }

            state = null!;
            nativeHandler = null!;
            return false;
        }

        private static void PruneDestroyedRegistrationStates()
        {
            PruneDestroyedRegistrationStates(TrashAddedRegistrations);
            PruneDestroyedRegistrationStates(TrashLevelChangedRegistrations);
        }

        private static void PruneDestroyedRegistrationStates<TState>(Dictionary<int, TState> registrations)
            where TState : TrashContainerRegistrationState
        {
            List<int>? destroyedIds = null;
            foreach (KeyValuePair<int, TState> registration in registrations)
            {
                if (registration.Value.S1TrashContainer != null)
                    continue;

                destroyedIds ??= new List<int>();
                destroyedIds.Add(registration.Key);
            }

            if (destroyedIds == null)
                return;

            foreach (int destroyedId in destroyedIds)
                registrations.Remove(destroyedId);
        }

        private abstract class TrashContainerRegistrationState
        {
            internal S1Trash.TrashContainer S1TrashContainer { get; }

            protected TrashContainerRegistrationState(S1Trash.TrashContainer trashContainer)
            {
                S1TrashContainer = trashContainer;
            }
        }

        private sealed class TrashAddedRegistrationState : TrashContainerRegistrationState
        {
            internal ManagedEventRegistrationTracker<NativeTrashAddedAction> Registrations { get; } =
                new ManagedEventRegistrationTracker<NativeTrashAddedAction>();

            internal TrashAddedRegistrationState(S1Trash.TrashContainer trashContainer)
                : base(trashContainer)
            {
            }
        }

        private sealed class TrashLevelChangedRegistrationState : TrashContainerRegistrationState
        {
            internal ManagedEventRegistrationTracker<NativeTrashLevelChangedAction> Registrations { get; } =
                new ManagedEventRegistrationTracker<NativeTrashLevelChangedAction>();

            internal TrashLevelChangedRegistrationState(S1Trash.TrashContainer trashContainer)
                : base(trashContainer)
            {
            }
        }
    }
}
