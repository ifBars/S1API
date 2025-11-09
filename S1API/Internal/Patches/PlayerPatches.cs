#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#else
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif


using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Utils;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches to apply to the Players for tracking.
    /// </summary>
    [HarmonyPatch]
    internal class PlayerPatches
    {
        private static readonly Log Logger = new Log("PlayerPatches");
        /// <summary>
        /// INTERNAL: Static constructor to subscribe to game's player events.
        /// </summary>
        static PlayerPatches()
        {
            try
            {
                var playerType = typeof(S1PlayerScripts.Player);
                
                // Subscribe to onLocalPlayerSpawned (Action with no parameters)
                var localEvent = ReflectionUtils.TryGetStaticFieldOrProperty(playerType, "onLocalPlayerSpawned") as Action;
                if (localEvent != null)
                {
                    localEvent += HandleLocalPlayerSpawned;
                    ReflectionUtils.TrySetStaticFieldOrProperty(playerType, "onLocalPlayerSpawned", localEvent);
                }
                else
                {
                    // Event is null, create new delegate
                    ReflectionUtils.TrySetStaticFieldOrProperty(playerType, "onLocalPlayerSpawned", new Action(HandleLocalPlayerSpawned));
                }
                
                // Subscribe to onPlayerSpawned (Action<Player>)
                var playerSpawnedEvent = ReflectionUtils.TryGetStaticFieldOrProperty(playerType, "onPlayerSpawned");
                if (playerSpawnedEvent != null)
                {
                    // Use Delegate.Combine to add our handler
                    var existingDelegate = playerSpawnedEvent as Delegate;
                    var newDelegate = Delegate.CreateDelegate(playerSpawnedEvent.GetType(), typeof(PlayerPatches).GetMethod(nameof(HandlePlayerSpawned), BindingFlags.NonPublic | BindingFlags.Static));
                    var combined = Delegate.Combine(existingDelegate, newDelegate);
                    ReflectionUtils.TrySetStaticFieldOrProperty(playerType, "onPlayerSpawned", combined);
                }
                else
                {
                    // Event is null, create new delegate
                    var actionType = typeof(Action<>).MakeGenericType(playerType);
                    var newDelegate = Delegate.CreateDelegate(actionType, typeof(PlayerPatches).GetMethod(nameof(HandlePlayerSpawned), BindingFlags.NonPublic | BindingFlags.Static));
                    ReflectionUtils.TrySetStaticFieldOrProperty(playerType, "onPlayerSpawned", newDelegate);
                }
                
                // Subscribe to onPlayerDespawned (Action<Player>)
                var playerDespawnedEvent = ReflectionUtils.TryGetStaticFieldOrProperty(playerType, "onPlayerDespawned");
                if (playerDespawnedEvent != null)
                {
                    // Use Delegate.Combine to add our handler
                    var existingDelegate = playerDespawnedEvent as Delegate;
                    var newDelegate = Delegate.CreateDelegate(playerDespawnedEvent.GetType(), typeof(PlayerPatches).GetMethod(nameof(HandlePlayerDespawned), BindingFlags.NonPublic | BindingFlags.Static));
                    var combined = Delegate.Combine(existingDelegate, newDelegate);
                    ReflectionUtils.TrySetStaticFieldOrProperty(playerType, "onPlayerDespawned", combined);
                }
                else
                {
                    // Event is null, create new delegate
                    var actionType = typeof(Action<>).MakeGenericType(playerType);
                    var newDelegate = Delegate.CreateDelegate(actionType, typeof(PlayerPatches).GetMethod(nameof(HandlePlayerDespawned), BindingFlags.NonPublic | BindingFlags.Static));
                    ReflectionUtils.TrySetStaticFieldOrProperty(playerType, "onPlayerDespawned", newDelegate);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - events may not be available yet
                Logger.Error($"Failed to subscribe to game player events: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Handler for when the local player spawns.
        /// </summary>
        private static void HandleLocalPlayerSpawned()
        {
            try
            {
                var apiPlayer = Player.All.FirstOrDefault(p => p.IsLocal);
                if (apiPlayer != null)
                {
                    Player.RaiseLocalPlayerSpawned(apiPlayer);
                }
                else
                {
                    Logger.Warning("Game fired onLocalPlayerSpawned but no API Player instance found in Player.All list");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling local player spawned event: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Handler for when any player spawns.
        /// </summary>
        /// <param name="gamePlayer">The game's Player instance that spawned.</param>
        private static void HandlePlayerSpawned(S1PlayerScripts.Player gamePlayer)
        {
            try
            {
                if (gamePlayer == null)
                {
                    Logger.Warning("Game fired onPlayerSpawned with null player");
                    return;
                }

                var apiPlayer = Player.All.FirstOrDefault(p => p.S1Player == gamePlayer);
                if (apiPlayer != null)
                {
                    Player.RaisePlayerSpawned(apiPlayer);
                }
                else
                {
                    Logger.Warning($"Game fired onPlayerSpawned for {gamePlayer.PlayerName} but no API Player instance found in Player.All list (count: {Player.All.Count})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling player spawned event: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Handler for when any player despawns.
        /// </summary>
        /// <param name="gamePlayer">The game's Player instance that despawned.</param>
        private static void HandlePlayerDespawned(S1PlayerScripts.Player gamePlayer)
        {
            try
            {
                if (gamePlayer == null)
                {
                    Logger.Warning("Game fired onPlayerDespawned with null player");
                    return;
                }

                var apiPlayer = Player.All.FirstOrDefault(p => p.S1Player == gamePlayer);
                if (apiPlayer != null)
                {
                    Player.RaisePlayerDespawned(apiPlayer);
                }
                else
                {
                    Logger.Warning($"Game fired onPlayerDespawned for {gamePlayer.PlayerName} but no API Player instance found in Player.All list");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling player despawned event: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Adds players to the player list upon wake.
        /// </summary>
        /// <param name="__instance">The player to add.</param>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), "Awake")]
        [HarmonyPostfix]
        private static void PlayerAwake(S1PlayerScripts.Player __instance)
        {
            try
            {
                new Player(__instance);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in PlayerAwake: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }


        /// <summary>
        /// INTERNAL: Removes players from the player list upon destruction.
        /// </summary>
        /// <param name="__instance">The player to remove.</param>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), "OnDestroy")]
        [HarmonyPostfix]
        private static void PlayerOnDestroy(S1PlayerScripts.Player __instance)
        {
            try
            {
                var apiPlayer = Player.All.FirstOrDefault(player => player.S1Player == __instance);
                if (apiPlayer != null)
                {
                    Player.All.Remove(apiPlayer);
                }
                else
                {
                    Logger.Warning($"OnDestroy called but API Player instance not found in Player.All list");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in PlayerOnDestroy: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

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
