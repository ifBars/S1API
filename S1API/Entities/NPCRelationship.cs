#if (IL2CPPMELON)
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Relation = ScheduleOne.NPCs.Relation;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Entities.Relation;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing wrapper for an NPC's relationship data.
    /// Provides safe access to relationship values, unlock state, connections,
    /// and convenience helpers which bridge to the base game's NPCRelationData.
    /// </summary>
    public sealed class NPCRelationship
    {
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
                        S1NPCs.NPC other = GetListItem(listObj, i);
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
        public void OnChanged(Action<float> callback)
        {
            if (callback == null || Component == null)
                return;
            try
            {
                FieldInfo field = typeof(S1Relation.NPCRelationData).GetField("onRelationshipChange", BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                    return;

                object existing = field.GetValue(Component);
#if (IL2CPPMELON || IL2CPPBEPINEX)
                System.Action<float> wrapped = new System.Action<float>(d => { try { callback(d); } catch { } });
                var combined = (Il2CppSystem.Delegate)Il2CppSystem.Delegate.Combine(existing as Il2CppSystem.Delegate, (Il2CppSystem.Delegate)(object)wrapped);
                field.SetValue(Component, combined);
#else
                Action<float> wrapped = d => { try { callback(d); } catch { } };
                var combined = Delegate.Combine(existing as Delegate, wrapped);
                field.SetValue(Component, combined);
#endif
            }
            catch { }
        }

        /// <summary>
        /// Subscribes to unlocked events. Callback receives unlock type and notify flag.
        /// Best-effort under IL2CPP; silently no-ops if delegate bridging is unavailable.
        /// </summary>
        public void OnUnlocked(Action<UnlockType, bool> callback)
        {
            if (callback == null || Component == null)
                return;
            try
            {
                FieldInfo field = typeof(S1Relation.NPCRelationData).GetField("onUnlocked", BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                    return;

                object existing = field.GetValue(Component);
#if (IL2CPPMELON || IL2CPPBEPINEX)
                System.Action<S1Relation.NPCRelationData.EUnlockType, bool> wrapped = new System.Action<S1Relation.NPCRelationData.EUnlockType, bool>((t, notify) =>
                {
                    try { callback(FromS1(t), notify); } catch { }
                });
                var combined = (Il2CppSystem.Delegate)Il2CppSystem.Delegate.Combine(existing as Il2CppSystem.Delegate, (Il2CppSystem.Delegate)(object)wrapped);
                field.SetValue(Component, combined);
#else
                Action<S1Relation.NPCRelationData.EUnlockType, bool> wrapped = (t, notify) =>
                {
                    try { callback(FromS1(t), notify); } catch { }
                };
                var combined = Delegate.Combine(existing as Delegate, wrapped);
                field.SetValue(Component, combined);
#endif
            }
            catch { }
        }

        /// <summary>
        /// Deprecated: Declare defaults in NPC.ConfigurePrefab via NPCPrefabBuilder.WithRelationshipDefaults.
        /// Runtime mutation is no longer supported to preserve save/load consistency.
        /// </summary>
        [Obsolete("Declare defaults in NPC.ConfigurePrefab via NPCPrefabBuilder.WithRelationshipDefaults. Runtime mutation is disabled.")]
        public void BuildAndSetRelationshipData(Action<NPCRelationshipDataBuilder> configure)
        {
            // Keep a no-op to avoid breaking mods at runtime but guide them to the new API
        }

        #endregion

        #region Internal

        /// <summary>
        /// INTERNAL: Direct access to the underlying base-game relation data.
        /// </summary>
        internal S1Relation.NPCRelationData Component => NPC?.S1NPC?.RelationData;

        #endregion

        #region Private Helpers

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

        private static S1NPCs.NPC GetListItem(object listObj, int index)
        {
            if (listObj == null)
                return null;
            var indexer = listObj.GetType().GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            return indexer != null ? (S1NPCs.NPC)indexer.GetValue(listObj, new object[] { index }) : null;
        }

        #endregion
    }
}


