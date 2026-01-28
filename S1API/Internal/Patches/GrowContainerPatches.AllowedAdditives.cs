#if (IL2CPPMELON)
using S1Growing = Il2CppScheduleOne.Growing;
using S1Grid = Il2CppScheduleOne.Tiles.Grid;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Growing = ScheduleOne.Growing;
using S1Grid = ScheduleOne.Tiles.Grid;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using HarmonyLib;
using S1API.Growing;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Injects S1API-registered allowed additives into <c>GrowContainer.AllowedAdditives</c>.
    /// </summary>
    [HarmonyPatch]
    internal static class GrowContainerPatches
    {
        private static readonly Log Logger = new Log("GrowContainerPatches");

        [HarmonyPatch(typeof(S1Growing.GrowContainer), nameof(S1Growing.GrowContainer.InitializeGridItem),
            new Type[] { typeof(S1ItemFramework.ItemInstance), typeof(S1Grid), typeof(Vector2), typeof(int), typeof(string) })]
        [HarmonyPrefix]
        private static void GrowContainer_InitializeGridItem_Prefix(S1Growing.GrowContainer __instance)
        {
            try
            {
                ApplyRegisteredAllowedAdditives(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to apply registered allowed additives: {ex.Message}");
            }
        }

        internal static void ApplyRegisteredAllowedAdditives(S1Growing.GrowContainer container)
        {
            if (container == null)
                return;

            var ids = GrowContainerAdditives.GetAllowedAdditiveIdsInternal();
            if (ids == null || ids.Count == 0)
                return;

            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                S1ItemFramework.AdditiveDefinition additive;
                try
                {
                    var item = S1Registry.GetItem(id);
                    if (item == null || !CrossType.Is(item, out S1ItemFramework.AdditiveDefinition add))
                    {
                        GrowContainerAdditives.WarnMissing(
                            id,
                            $"[S1API] GrowContainerAdditives: additive '{id}' was not found or is not an AdditiveDefinition; skipping.");
                        continue;
                    }

                    additive = add;
                }
                catch (Exception ex)
                {
                    GrowContainerAdditives.WarnMissing(
                        id,
                        $"[S1API] GrowContainerAdditives: could not resolve additive '{id}' (registry unavailable yet?): {ex.Message}");
                    continue;
                }

                if (ContainsAdditive(container, id))
                    continue;

                AppendAdditive(container, additive);
            }
        }

        private static bool ContainsAdditive(S1Growing.GrowContainer container, string additiveId)
        {
#if (IL2CPPMELON)
            var current = container.AllowedAdditives;
            var len = current?.Length ?? 0;
            for (int i = 0; i < len; i++)
            {
                var existing = current![i];
                if (existing != null && string.Equals(existing.ID, additiveId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
#else
            var current = container.AllowedAdditives;
            if (current == null)
                return false;

            for (int i = 0; i < current.Length; i++)
            {
                var existing = current[i];
                if (existing != null && string.Equals(existing.ID, additiveId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
#endif
        }

        private static void AppendAdditive(S1Growing.GrowContainer container, S1ItemFramework.AdditiveDefinition additive)
        {
            if (additive == null)
                return;

#if (IL2CPPMELON)
            var current = container.AllowedAdditives;
            var len = current?.Length ?? 0;

            var managed = new S1ItemFramework.AdditiveDefinition[len + 1];
            for (int i = 0; i < len; i++)
            {
                managed[i] = current![i];
            }

            managed[len] = additive;
            container.AllowedAdditives = new Il2CppReferenceArray<S1ItemFramework.AdditiveDefinition>(managed);
#else
            var current = container.AllowedAdditives ?? Array.Empty<S1ItemFramework.AdditiveDefinition>();
            var next = new S1ItemFramework.AdditiveDefinition[current.Length + 1];
            current.CopyTo(next, 0);
            next[next.Length - 1] = additive;
            container.AllowedAdditives = next;
#endif
        }
    }
}

