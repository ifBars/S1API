#if (IL2CPPMELON)
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1NPCs = Il2CppScheduleOne.NPCs;
using Il2CppInterop.Runtime.Attributes;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Relation = ScheduleOne.NPCs.Relation;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using S1API.Entities;
using S1API.Logging;

namespace S1API.Entities.Relation
{
    /// <summary>
    /// Builder for configuring an NPC's relationship data from code.
    /// Supports setting relationship delta, unlock state/type, and connections by ID or wrapper.
    /// </summary>
    public sealed class NPCRelationshipDataBuilder
    {
        private static readonly Log Logger = new Log("NPCRelationshipDataBuilder");
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
            {
                return this;
            }
            
            int addedCount = 0;
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id))
                    continue;
                if (!_connectionIDs.Contains(id, StringComparer.OrdinalIgnoreCase))
                {
                    _connectionIDs.Add(id);
                    addedCount++;
                }
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
        /// For NPCs that aren't spawned yet (during prefab configuration), this will attempt to extract IDs
        /// from the NPC type's static NPCId property or by looking them up in the base game's NPC registry.
        /// Prefer <see cref="WithConnections(System.Type[])"/> or the generic overloads when you only have types available.
        /// </summary>
        [Obsolete("Use WithConnections<T1, T2, ...>() or WithConnectionsById instead. NPC instances are not available during prefab configuration.")]
        public NPCRelationshipDataBuilder WithConnections(IEnumerable<NPC> npcs)
        {
            Logger.Warning("[Relationship Data] WithConnections(NPC[]) is obsolete. Use WithConnections<T1, T2, ...>() or WithConnectionsById instead to resolve IDs in Menu scene.");
            // Preserve behavior by forwarding to type-based resolution, which pulls IDs from static NPCId/registry.
            return WithConnections(npcs?.Select(n => n?.GetType()).ToArray());
        }
        
        /// <summary>
        /// INTERNAL: Attempts to get the static NPCId property value from an NPC type.
        /// </summary>
        private static string TryGetStaticNPCId(System.Type npcType)
        {
            if (npcType == null)
                return null;
            
            try
            {
                // Use reflection to get static NPCId property
                var npcIdProperty = npcType.GetProperty("NPCId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                if (npcIdProperty != null && npcIdProperty.PropertyType == typeof(string))
                {
                    var staticId = npcIdProperty.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(staticId))
                    {
                        return staticId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[Relationship Data] TryGetStaticNPCId: Exception reading static NPCId property for type '{npcType.Name}': {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// INTERNAL: Attempts to resolve an NPC ID from a built-in NPC wrapper type by looking it up in the base game's NPC registry.
        /// This works for built-in NPCs that have their IDs hardcoded in their constructors.
        /// </summary>
        private static string TryResolveNPCIdFromType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            
            try
            {
                // For built-in NPC wrappers, try to find the ID by looking up the type name pattern
                // Built-in NPCs typically have IDs that match their type name (e.g., KyleCooley -> "kyle_cooley")
                string typeName = npcType.Name;
                
                // Try common ID patterns for built-in NPCs
                // Convert PascalCase to snake_case (e.g., "KyleCooley" -> "kyle_cooley")
                var idCandidates = new List<string>();
                
                // Direct lowercase conversion
                idCandidates.Add(typeName.ToLowerInvariant());
                
                // Snake case conversion (insert underscores before capitals)
                var snakeCase = System.Text.RegularExpressions.Regex.Replace(typeName, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
                if (snakeCase != typeName.ToLowerInvariant())
                    idCandidates.Add(snakeCase);
                
                // Check base game NPC registry for matching IDs
                var registry = S1NPCs.NPCManager.NPCRegistry;
                if (registry != null)
                {
                    foreach (var candidateId in idCandidates)
                    {
                        foreach (var baseNpc in registry)
                        {
                            if (baseNpc != null && string.Equals(baseNpc.ID, candidateId, StringComparison.OrdinalIgnoreCase))
                            {
                                return candidateId;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[Relationship Data] TryResolveNPCIdFromType: Exception resolving ID for NPC type '{npcType?.Name ?? "<null>"}': {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Replaces the connections list using API NPC wrappers. Nulls are ignored.
        /// </summary>
        [Obsolete("Use WithConnections<T1, T2, ...>() or WithConnectionsById instead. NPC instances are not available during prefab configuration.")]
        public NPCRelationshipDataBuilder WithConnections(params NPC[] npcs) =>
            WithConnections((IEnumerable<NPC>)npcs);

        /// <summary>
        /// Replaces the connections list using NPC types. This overload works during prefab configuration
        /// when NPC instances are not yet available. IDs are resolved from the static NPCId property.
        /// </summary>
        /// <typeparam name="T1">First NPC type</typeparam>
        /// <typeparam name="T2">Second NPC type</typeparam>
        public NPCRelationshipDataBuilder WithConnections<T1, T2>() where T1 : NPC where T2 : NPC =>
            WithConnections(typeof(T1), typeof(T2));

        /// <summary>
        /// Replaces the connections list using NPC types. This overload works during prefab configuration
        /// when NPC instances are not yet available. IDs are resolved from the static NPCId property.
        /// </summary>
        /// <typeparam name="T1">First NPC type</typeparam>
        /// <typeparam name="T2">Second NPC type</typeparam>
        /// <typeparam name="T3">Third NPC type</typeparam>
        public NPCRelationshipDataBuilder WithConnections<T1, T2, T3>() where T1 : NPC where T2 : NPC where T3 : NPC =>
            WithConnections(typeof(T1), typeof(T2), typeof(T3));

        /// <summary>
        /// Replaces the connections list using NPC types. This overload works during prefab configuration
        /// when NPC instances are not yet available. IDs are resolved from the static NPCId property.
        /// </summary>
        public NPCRelationshipDataBuilder WithConnections(params System.Type[] npcTypes)
        {
            _connectionIDs.Clear();
            if (npcTypes == null || npcTypes.Length == 0)
            {
                return this;
            }

            int addedCount = 0;
            int nullCount = 0;
            int emptyIdCount = 0;
            int resolvedFromTypeCount = 0;

            foreach (var npcType in npcTypes)
            {
                if (npcType == null)
                {
                    nullCount++;
                    Logger.Warning("[Relationship Data] WithConnections: Received null NPC type");
                    continue;
                }

                if (!typeof(NPC).IsAssignableFrom(npcType))
                {
                    Logger.Warning($"[Relationship Data] WithConnections: Type '{npcType.Name}' is not an NPC type");
                    continue;
                }

                // Try to get ID from static NPCId property
                string id = TryGetStaticNPCId(npcType);

                // Try to resolve from base game registry if needed
                if (string.IsNullOrEmpty(id) && npcType.Assembly == typeof(NPCRelationshipDataBuilder).Assembly)
                {
                    id = TryResolveNPCIdFromType(npcType);
                    if (!string.IsNullOrEmpty(id))
                        resolvedFromTypeCount++;
                }

                if (string.IsNullOrEmpty(id))
                {
                    emptyIdCount++;
                    Logger.Warning($"[Relationship Data] WithConnections: Could not resolve ID for NPC type '{npcType.Name}'. Ensure the NPC class has a static NPCId property (e.g., public new static string NPCId => \"npc_id\";)");
                    continue;
                }

                if (!_connectionIDs.Contains(id, StringComparer.OrdinalIgnoreCase))
                {
                    _connectionIDs.Add(id);
                    addedCount++;
                }
            }
            
            return this;
        }

        /// <summary>
        /// INTERNAL: Captures the configured values for Il2Cpp-safe transfer without reflection.
        /// </summary>
#if IL2CPPMELON
        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
#endif
        internal RelationshipDefaultsData CaptureData()
        {
            return new RelationshipDefaultsData
            {
                RelationDelta = _relationDelta,
                Unlocked = _unlocked,
                UnlockType = _unlockType,
                ConnectionIDs = _connectionIDs.Count > 0 ? new List<string>(_connectionIDs) : null
            };
        }

        /// <summary>
        /// INTERNAL: Applies the configured values to a relation data instance.
        /// </summary>
        /// <param name="preserveUnlockState">If true, will not modify unlock state if the NPC is already unlocked (preserves save data).</param>
        public void ApplyTo(S1Relation.NPCRelationData relationData, S1NPCs.NPC owner, bool preserveUnlockState = false)
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
                        int previousCount = targetList.Count;
                        targetList.Clear();
                        int foundCount = 0;
                        int notFoundCount = 0;
                        
                        for (int i = 0; i < _connectionIDs.Count; i++)
                        {
                            var id = _connectionIDs[i];
                            S1NPCs.NPC other = null;
                            
                            // Manual search instead of FirstOrDefault
                            foreach (var n in registry)
                            {
                                if (n != null && !ReferenceEquals(n, owner) && string.Equals(n.ID, id, StringComparison.OrdinalIgnoreCase))
                                {
                                    other = n;
                                    break;
                                }
                            }
                            
                            if (other != null)
                            {
                                if (!targetList.Contains(other))
                                {
                                    targetList.Add(other);
                                    foundCount++;
                                }
                                else
                                {
                                    foundCount++;
                                }
                            }
                            else
                            {
                                notFoundCount++;
                                Logger.Warning($"[Relationship Data] ApplyTo: Connection ID '{id}' not found in NPC registry for NPC '{owner?.ID ?? "<null>"}'");
                            }
                        }
                    }
                    else
                    {
                        Logger.Warning($"[Relationship Data] ApplyTo: Connections list is null for NPC '{owner?.ID ?? "<null>"}'");
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error($"[Relationship Data] ApplyTo: Exception applying connections to NPC '{owner?.ID ?? "<null>"}': {ex.Message}");
            }

            try
            {
                // Only apply unlock state if not preserving (i.e., not loaded from save)
                if (!preserveUnlockState && _unlocked.HasValue)
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
                else if (_unlockType.HasValue && !preserveUnlockState)
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

        internal sealed class RelationshipDefaultsData
        {
            public float? RelationDelta;
            public bool? Unlocked;
            public NPCRelationship.UnlockType? UnlockType;
            public List<string> ConnectionIDs;
        }
    }
}



