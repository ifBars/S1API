#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using NativeRelationshipUnlockedAction = Il2CppSystem.Action<Il2CppScheduleOne.NPCs.Relation.NPCRelationData.EUnlockType, bool>;
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif MONOMELON
using NativeRelationshipUnlockedAction = System.Action<ScheduleOne.NPCs.Relation.NPCRelationData.EUnlockType, bool>;
using S1Relation = ScheduleOne.NPCs.Relation;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Entities.Relation;
using S1API.Logging;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing wrapper for an NPC's relationship data. Provides safe access to relationship values, unlock state, connections,
    /// and convenience helpers which bridge to the base game's NPCRelationData. Relationship configuration must be done in <see cref="NPC.ConfigurePrefab"/>.
    /// </summary>
    /// <remarks>
    /// Use this to manage NPC social connections, unlock states, and relationship levels with the player and other NPCs.
    /// Subscribe to <see cref="OnChanged"/> and <see cref="OnUnlocked"/> events for dynamic relationship interactions.
    /// </remarks>
    public sealed class NPCRelationship
    {
        private static readonly Log Logger = new Log("NPCRelationship");

        #region Types

        /// <summary>
        /// Unlock types mirrored from the base game.
        /// </summary>
        public enum UnlockType
        {
            Recommendation = 0,
            DirectApproach = 1
        }

        #endregion

        #region Construction

        /// <summary>
        /// INTERNAL: Reference to the owning API NPC.
        /// </summary>
        internal readonly NPC NPC;
        private readonly Dictionary<Action<float>, Delegate> _relationshipChangedHandlers = new Dictionary<Action<float>, Delegate>();
        private Action<UnlockType, bool>? _relationshipUnlockedHandlers;
        private S1Relation.NPCRelationData? _subscribedRelationship;
        private NativeRelationshipUnlockedAction? _nativeRelationshipUnlockedDispatcher;
        private static readonly MemberInfo? RelationshipChangedMember =
            ResolveNativeEventMember("OnRelationshipChange", "onRelationshipChange");
        internal NPCRelationship(NPC npc)
        {
            NPC = npc;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Current relationship delta, clamped to [0, 5].
        /// Setting invokes the game's SetRelationship.
        /// </summary>
        public float Delta
        {
            get => Component != null ? Component.RelationDelta : 0f;
            set { Component?.SetRelationship(value); }
        }

        /// <summary>
        /// Normalized relationship delta in [0, 1].
        /// </summary>
        public float Normalized =>
            Component != null ? Component.NormalizedRelationDelta : 0f;

        /// <summary>
        /// Adds to the relationship delta (optionally networked).
        /// </summary>
        public void Add(float delta, bool network = true) =>
            Component?.ChangeRelationship(delta, network);

        /// <summary>
        /// True if the NPC has been unlocked (known) to the player systems.
        /// </summary>
        public bool IsUnlocked =>
            Component != null && Component.Unlocked;

        /// <summary>
        /// The way this NPC was unlocked.
        /// Setting attempts to assign the base game's UnlockType via reflection without unlocking.
        /// </summary>
        public UnlockType Type
        {
            get => Component != null ? FromS1(Component.UnlockType) : UnlockType.DirectApproach;
            set => SetUnlockType(value);
        }

        /// <summary>
        /// Unlocks this NPC with the specified type.
        /// </summary>
        public void Unlock(UnlockType type = UnlockType.DirectApproach, bool notify = true) =>
            Component?.Unlock(ToS1(type), notify);

        /// <summary>
        /// Sets the underlying unlock type without changing locked state.
        /// Use <see cref="Unlock(UnlockType, bool)"/> if you intend to unlock as well.
        /// </summary>
        public void SetUnlockType(UnlockType type)
        {
            if (Component == null)
                return;
            try
            {
                var prop = typeof(S1Relation.NPCRelationData).GetProperty("UnlockType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var setter = prop?.GetSetMethod(true);
                setter?.Invoke(Component, new object[] { ToS1(type) });
            }
            catch { }
        }

        /// <summary>
        /// Unlocks all connected NPCs (recommendation unlocking).
        /// </summary>
        public void UnlockConnections() =>
            Component?.UnlockConnections();

        /// <summary>
        /// True if the NPC is known to the player (directly or via mutual connections).
        /// </summary>
        public bool IsKnown =>
            Component != null && Component.IsKnown();

        /// <summary>
        /// True if the NPC is known via mutual connections even if not directly unlocked.
        /// </summary>
        public bool IsMutuallyKnown =>
            Component != null && Component.IsMutuallyKnown();

        /// <summary>
        /// Returns the IDs of current connection NPCs (safe across Mono and IL2CPP).
        /// </summary>
        public List<string> ConnectionIDs
        {
            get
            {
                var ids = new List<string>();
                var comp = Component;
                if (comp == null || comp.Connections == null)
                    return ids;

                try
                {
                    object listObj = comp.Connections;
                    int count = GetListCount(listObj);
                    for (int i = 0; i < count; i++)
                    {
                    S1NPCs.NPC? other = GetListItem(listObj, i);
                        if (other != null && other.ID != null)
                            ids.Add(other.ID);
                    }
                }
                catch { }

                return ids;
            }
        }

        /// <summary>
        /// Subscribes to relationship change events. Callback receives the change amount.
        /// Best-effort under IL2CPP; silently no-ops if delegate bridging is unavailable.
        /// </summary>
        public event Action<float> OnChanged
        {
            add
            {
                if (value == null || Component == null)
                    return;

                if (_relationshipChangedHandlers.ContainsKey(value))
                    return;

                try
                {
                    MemberInfo? member = RelationshipChangedMember;
                    if (member == null)
                        return;

                    object? existing = GetNativeEventValue(member, Component);
#if IL2CPPMELON
                    System.Action<float> wrapped = new System.Action<float>(d => { try { value(d); } catch { } });
                    var combined = (Il2CppSystem.Delegate)Il2CppSystem.Delegate.Combine(existing as Il2CppSystem.Delegate, (Il2CppSystem.Delegate)(object)wrapped);
                    SetNativeEventValue(member, Component, combined);
                    _relationshipChangedHandlers[value] = wrapped;
#else
                    Action<float> wrapped = d => { try { value(d); } catch { } };
                    var combined = Delegate.Combine(existing as Delegate, wrapped);
                    SetNativeEventValue(member, Component, combined);
                    _relationshipChangedHandlers[value] = wrapped;
#endif
                }
                catch { }
            }
            remove
            {
                if (value == null || Component == null)
                    return;

                if (!_relationshipChangedHandlers.TryGetValue(value, out var wrapped))
                    return;

                _relationshipChangedHandlers.Remove(value);
                try
                {
                    MemberInfo? member = RelationshipChangedMember;
                    if (member == null)
                        return;

#if IL2CPPMELON
                    var existing = GetNativeEventValue(member, Component);
                    var remaining = existing != null
                        ? Il2CppSystem.Delegate.Remove(existing as Il2CppSystem.Delegate, (Il2CppSystem.Delegate)(object)wrapped)
                        : null;
                    SetNativeEventValue(member, Component, remaining);
#else
                    var existing = GetNativeEventValue(member, Component);
                    var remaining = existing != null
                        ? Delegate.Remove(existing as Delegate, (Delegate)wrapped)
                        : null;
                    SetNativeEventValue(member, Component, remaining);
#endif
                }
                catch { }
            }
        }

        /// <summary>
        /// Subscribes to unlocked events. Callback receives unlock type and notify flag.
        /// The wrapper retains a native dispatcher and bridges it explicitly under IL2CPP.
        /// </summary>
        public event Action<UnlockType, bool> OnUnlocked
        {
            add
            {
                if (value == null)
                    return;

                if (_relationshipUnlockedHandlers != null &&
                    Array.IndexOf(
                        _relationshipUnlockedHandlers.GetInvocationList(),
                        value) >= 0)
                {
                    return;
                }

                _relationshipUnlockedHandlers += value;
                EnsureUnlockedHook();
            }
            remove
            {
                if (value == null)
                    return;

                _relationshipUnlockedHandlers -= value;
                if (_relationshipUnlockedHandlers == null)
                    RemoveUnlockedHook();
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// INTERNAL: Direct access to the underlying base-game relation data.
        /// </summary>
        internal S1Relation.NPCRelationData? Component => NPC?.S1NPC?.RelationData;

        #endregion

        #region Private Helpers

        internal void EnsureUnlockedHook()
        {
            if (_relationshipUnlockedHandlers == null)
                return;

            S1Relation.NPCRelationData? relationship = Component;
            if (relationship == null || ReferenceEquals(relationship, _subscribedRelationship))
                return;

            try
            {
                RemoveUnlockedHook();
                NativeRelationshipUnlockedAction dispatcher =
                    GetOrCreateNativeUnlockedDispatcher();

#if IL2CPPMELON
                relationship.OnUnlocked = relationship.OnUnlocked == null
                    ? dispatcher
                    : Il2CppSystem.Delegate.Combine(
                            relationship.OnUnlocked,
                            dispatcher)
                        .Cast<NativeRelationshipUnlockedAction>();
#else
                relationship.OnUnlocked += dispatcher;
#endif
                _subscribedRelationship = relationship;
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"Could not attach the native relationship-unlocked hook: {ex}");
            }
        }

        private NativeRelationshipUnlockedAction GetOrCreateNativeUnlockedDispatcher()
        {
            if (_nativeRelationshipUnlockedDispatcher != null)
                return _nativeRelationshipUnlockedDispatcher;

#if IL2CPPMELON
            _nativeRelationshipUnlockedDispatcher =
                DelegateSupport.ConvertDelegate<NativeRelationshipUnlockedAction>(
                    new Action<S1Relation.NPCRelationData.EUnlockType, bool>(
                        DispatchUnlocked))
                ?? throw new InvalidOperationException(
                    "Could not create the native relationship-unlocked dispatcher.");
#else
            _nativeRelationshipUnlockedDispatcher = DispatchUnlocked;
#endif
            return _nativeRelationshipUnlockedDispatcher;
        }

        private void RemoveUnlockedHook()
        {
            if (_subscribedRelationship == null ||
                _nativeRelationshipUnlockedDispatcher == null)
            {
                _subscribedRelationship = null;
                return;
            }

            try
            {
#if IL2CPPMELON
                Il2CppSystem.Delegate? remaining = Il2CppSystem.Delegate.Remove(
                    _subscribedRelationship.OnUnlocked,
                    _nativeRelationshipUnlockedDispatcher);
                _subscribedRelationship.OnUnlocked =
                    remaining?.Cast<NativeRelationshipUnlockedAction>();
#else
                _subscribedRelationship.OnUnlocked -=
                    _nativeRelationshipUnlockedDispatcher;
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"Could not remove the native relationship-unlocked hook: {ex}");
            }
            finally
            {
                _subscribedRelationship = null;
            }
        }

        private void DispatchUnlocked(
            S1Relation.NPCRelationData.EUnlockType type,
            bool notify)
        {
            Action<UnlockType, bool>? handlers = _relationshipUnlockedHandlers;
            if (handlers == null)
                return;

            foreach (Action<UnlockType, bool> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(FromS1(type), notify);
                }
                catch (Exception ex)
                {
                    Logger.Warning(
                        $"NPCRelationship.OnUnlocked subscriber " +
                        $"'{handler.Method.DeclaringType?.FullName}.{handler.Method.Name}' " +
                        $"failed: {ex}");
                }
            }
        }

        internal static MemberInfo? ResolveNativeEventMember(
            string canonicalName,
            string legacyName)
        {
            return ResolveNativeEventMember(
                typeof(S1Relation.NPCRelationData),
                canonicalName,
                legacyName);
        }

        internal static MemberInfo? ResolveNativeEventMember(
            Type relationType,
            string canonicalName,
            string legacyName)
        {
            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;
            return (MemberInfo?)relationType.GetField(canonicalName, flags) ??
                   (MemberInfo?)relationType.GetProperty(canonicalName, flags) ??
                   (MemberInfo?)relationType.GetField(legacyName, flags) ??
                   (MemberInfo?)relationType.GetProperty(legacyName, flags);
        }

        private static object? GetNativeEventValue(
            MemberInfo member,
            object target) =>
            member is FieldInfo field
                ? field.GetValue(target)
                : ((PropertyInfo)member).GetValue(target);

        private static void SetNativeEventValue(
            MemberInfo member,
            object target,
            object? value)
        {
            if (member is FieldInfo field)
                field.SetValue(target, value);
            else
                ((PropertyInfo)member).SetValue(target, value);
        }

        private static UnlockType FromS1(S1Relation.NPCRelationData.EUnlockType t) =>
            t == S1Relation.NPCRelationData.EUnlockType.Recommendation ? UnlockType.Recommendation : UnlockType.DirectApproach;

        private static S1Relation.NPCRelationData.EUnlockType ToS1(UnlockType t) =>
            t == UnlockType.Recommendation ? S1Relation.NPCRelationData.EUnlockType.Recommendation : S1Relation.NPCRelationData.EUnlockType.DirectApproach;

        private static int GetListCount(object listObj)
        {
            if (listObj == null)
                return 0;
            var prop = listObj.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            return prop != null ? Convert.ToInt32(prop.GetValue(listObj)) : 0;
        }

        private static S1NPCs.NPC? GetListItem(object? listObj, int index)
        {
            if (listObj == null)
                return null;
            var indexer = listObj.GetType().GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            return indexer?.GetValue(listObj, new object[] { index }) as S1NPCs.NPC;
        }

        #endregion
    }
}


