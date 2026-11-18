#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON)
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Entities;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Raises <see cref="NPC.OnInitializing"/> before the game initializes an NPC.
    /// </summary>
    [HarmonyPatch]
    internal static class NpcInitializePatches
    {
        /// <summary>
        /// Raises the event before the game applies the NPC data, then writes a changed mugshot back into the data.
        /// </summary>
        [HarmonyPatch(typeof(S1NPCs.NPC), nameof(S1NPCs.NPC.InitializeNPC))]
        [HarmonyPrefix]
        private static void InitializeNpcPrefix(S1NPCs.Framework.NPCData data)
        {
            try
            {
                if (data == null)
                {
                    NpcAttackEvents.Warn("InitializeNPC without data; OnInitializing not raised.");
                    return;
                }
                var appearance = data.Appearance;
                var args = new NpcInitializingArgs(data.BasicInfo.ID, appearance?.Mugshot);
                var before = args.Mugshot;
                NPC.RaiseInitializing(args);
                if (appearance != null && args.Mugshot != before)
                {
                    appearance.Mugshot = args.Mugshot;
                    NpcAttackEvents.Info($"{args.Id} mugshot replaced.");
                }
            }
            catch (Exception ex)
            {
                NpcAttackEvents.Error($"Raising OnInitializing failed; the NPC initializes as vanilla: {ex}");
            }
        }
    }
}
