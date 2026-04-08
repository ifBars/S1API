#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Entities;
using S1API.Logging;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Shared helpers for resolving S1API NPC wrapper types and IDs.
    /// </summary>
    internal static class NPCTypeUtils
    {
        private static readonly Log Logger = new Log("NPCTypeUtils");

        internal static string TryGetStaticNPCId(Type npcType)
        {
            if (npcType == null)
                return null;

            try
            {
                var npcIdProperty = npcType.GetProperty(
                    "NPCId",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (npcIdProperty?.PropertyType == typeof(string))
                    return npcIdProperty.GetValue(null) as string;
            }
            catch (Exception ex)
            {
                Logger.Warning($"TryGetStaticNPCId: Failed reading static NPCId for '{npcType.Name}': {ex.Message}");
            }

            return null;
        }

        internal static string TryResolveNPCIdFromType(Type npcType)
        {
            if (npcType == null)
                return null;

            try
            {
                var registry = S1NPCs.NPCManager.NPCRegistry;
                if (registry == null)
                    return null;

                var idCandidates = new List<string> { npcType.Name.ToLowerInvariant() };
                var snakeCase = System.Text.RegularExpressions.Regex
                    .Replace(npcType.Name, "([a-z])([A-Z])", "$1_$2")
                    .ToLowerInvariant();

                if (!idCandidates.Contains(snakeCase))
                    idCandidates.Add(snakeCase);

                foreach (var candidateId in idCandidates)
                {
                    foreach (var npc in registry)
                    {
                        if (npc != null && string.Equals(npc.ID, candidateId, StringComparison.OrdinalIgnoreCase))
                            return npc.ID;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"TryResolveNPCIdFromType: Failed resolving ID for '{npcType.Name}': {ex.Message}");
            }

            return null;
        }

        internal static Type TryResolveBuiltInNPCType(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return null;

            foreach (var npcType in ReflectionUtils.GetDerivedClasses<NPC>())
            {
                if (npcType == null || npcType.IsAbstract || npcType.Assembly != typeof(NPC).Assembly)
                    continue;

                var typeNpcId = TryGetStaticNPCId(npcType);
                if (string.Equals(typeNpcId, npcId, StringComparison.OrdinalIgnoreCase))
                    return npcType;
            }

            return null;
        }
    }
}
