#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#else
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif


using System.Linq;
using HarmonyLib;
using S1API.Entities;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches to apply to the Players for tracking.
    /// </summary>
    [HarmonyPatch]
    internal class PlayerPatches
    {
        /// <summary>
        /// INTERNAL: Adds players to the player list upon wake.
        /// </summary>
        /// <param name="__instance">The player to add.</param>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), "Awake")]
        [HarmonyPostfix]
        private static void PlayerAwake(S1PlayerScripts.Player __instance) =>
            new Player(__instance);


        /// <summary>
        /// INTERNAL: Removes players from the player list upon destruction.
        /// </summary>
        /// <param name="__instance">The player to remove.</param>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), "OnDestroy")]
        [HarmonyPostfix]
        private static void PlayerOnDestroy(S1PlayerScripts.Player __instance) =>
            Player.All.Remove(Player.All.First(player => player.S1Player == __instance));

        /// <summary>
        /// Prevent damage application for invincible players.
        /// </summary>
        /// <param name="__instance">Player instance taking damage.</param>
        /// <param name="damage">Damage amount.</param>
        /// <param name="flinch">Whether player should flinch.</param>
        /// <param name="playBloodMist">Whether blood mist VFX should play.</param>
        /// <returns>False to skip original when invincible, true to apply damage.</returns>
        [HarmonyPatch(typeof(S1PlayerScripts.Health.PlayerHealth), "TakeDamage")]
        [HarmonyPrefix]
        private static bool PlayerHealth_TakeDamage(S1PlayerScripts.Health.PlayerHealth __instance, float damage, bool flinch = true, bool playBloodMist = true)
        {
            var player = __instance.Player;
            if (player != null && Player.IsPlayerInvincible(player))
                return false;
            return true;
        }
    }
}
