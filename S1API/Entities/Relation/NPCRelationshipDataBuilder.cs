#if (IL2CPPMELON)
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Relation = ScheduleOne.NPCs.Relation;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using S1API.Entities;

namespace S1API.Entities.Relation
{
    /// <summary>
    /// Builder for configuring an NPC's relationship data from code.
    /// Supports setting relationship delta, unlock state/type, and connections by ID or wrapper.
    /// </summary>
    public sealed class NPCRelationshipDataBuilder
    {
        private float? _relationDelta;
        private bool? _unlocked;
        private NPCRelationship.UnlockType? _unlockType;
        private readonly List<string> _connectionIDs = new List<string>();

        /// <summary>
        /// Sets the relationship delta in [0, 5].
        /// </summary>
        public NPCRelationshipDataBuilder WithDelta(float delta)
        {
            _relationDelta = Mathf.Clamp(delta, 0f, 5f);
            return this;
        }

        /// <summary>
        /// Sets the relationship delta using a normalized [0..1] value.
        /// </summary>
        public NPCRelationshipDataBuilder WithNormalized(float normalized)
        {
            var clamped = Mathf.Clamp01(normalized);
            _relationDelta = clamped * 5f;
            return this;
        }

        /// <summary>
        /// Sets whether the NPC is unlocked.
        /// </summary>
        public NPCRelationshipDataBuilder SetUnlocked(bool unlocked)
        {
            _unlocked = unlocked;
            return this;
        }

        /// <summary>
        /// Sets unlock type by enum.
        /// </summary>
        public NPCRelationshipDataBuilder SetUnlockType(NPCRelationship.UnlockType type)
        {
            _unlockType = type;
            return this;
        }

        /// <summary>
        /// Sets unlock type by name ("Recommendation", "DirectApproach").
        /// </summary>
        public NPCRelationshipDataBuilder SetUnlockType(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName) && Enum.TryParse(typeName, true, out NPCRelationship.UnlockType parsed))
                _unlockType = parsed;
            return this;
        }

        /// <summary>
        /// Replaces the connections list with the given NPC IDs (case-insensitive).
        /// </summary>
        public NPCRelationshipDataBuilder WithConnectionsById(IEnumerable<string> ids)
        {
            _connectionIDs.Clear();
            if (ids == null)
                return this;
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!_connectionIDs.Contains(id, StringComparer.OrdinalIgnoreCase))
                    _connectionIDs.Add(id);
            }
            return this;
        }

        /// <summary>
        /// Replaces the connections list with the given NPC IDs (case-insensitive).
        /// </summary>
        public NPCRelationshipDataBuilder WithConnectionsById(params string[] ids) =>
            WithConnectionsById((IEnumerable<string>)ids);

        /// <summary>
        /// Replaces the connections list using API NPC wrappers. Nulls are ignored.
        /// </summary>
        public NPCRelationshipDataBuilder WithConnections(IEnumerable<NPC> npcs)
        {
            _connectionIDs.Clear();
            if (npcs == null)
                return this;
            foreach (var npc in npcs)
            {
                var id = npc?.ID;
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!_connectionIDs.Contains(id, StringComparer.OrdinalIgnoreCase))
                    _connectionIDs.Add(id);
            }
            return this;
        }

        /// <summary>
        /// Replaces the connections list using API NPC wrappers. Nulls are ignored.
        /// </summary>
        public NPCRelationshipDataBuilder WithConnections(params NPC[] npcs) =>
            WithConnections((IEnumerable<NPC>)npcs);

        /// <summary>
        /// INTERNAL: Applies the configured values to a relation data instance.
        /// </summary>
        public void ApplyTo(S1Relation.NPCRelationData relationData, S1NPCs.NPC owner)
        {
            if (relationData == null)
                return;

            try
            {
                if (_relationDelta.HasValue)
                    relationData.SetRelationship(_relationDelta.Value);
            }
            catch { }

            try
            {
                if (_connectionIDs.Count > 0)
                {
                    var registry = S1NPCs.NPCManager.NPCRegistry;
                    var targetList = relationData.Connections;
                    if (targetList != null)
                    {
                        targetList.Clear();
                        for (int i = 0; i < _connectionIDs.Count; i++)
                        {
                            var id = _connectionIDs[i];
                            var other = registry.FirstOrDefault(n => n != null && !ReferenceEquals(n, owner) && string.Equals(n.ID, id, StringComparison.OrdinalIgnoreCase));
                            if (other != null && !targetList.Contains(other))
                                targetList.Add(other);
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (_unlocked.HasValue)
                {
                    if (_unlocked.Value)
                    {
                        var type = _unlockType.HasValue ? _unlockType.Value : NPCRelationship.UnlockType.DirectApproach;
                        relationData.Unlock(ToS1(type), notify: false);
                    }
                    else if (_unlockType.HasValue)
                    {
                        // Set unlock type without unlocking, matching wrapper logic.
                        var prop = typeof(S1Relation.NPCRelationData).GetProperty("UnlockType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var setter = prop?.GetSetMethod(true);
                        setter?.Invoke(relationData, new object[] { ToS1(_unlockType.Value) });
                    }
                }
                else if (_unlockType.HasValue)
                {
                    var prop = typeof(S1Relation.NPCRelationData).GetProperty("UnlockType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var setter = prop?.GetSetMethod(true);
                    setter?.Invoke(relationData, new object[] { ToS1(_unlockType.Value) });
                }
            }
            catch { }
        }

        private static S1Relation.NPCRelationData.EUnlockType ToS1(NPCRelationship.UnlockType t) =>
            t == NPCRelationship.UnlockType.Recommendation
                ? S1Relation.NPCRelationData.EUnlockType.Recommendation
                : S1Relation.NPCRelationData.EUnlockType.DirectApproach;
    }
}


