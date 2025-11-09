#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using Il2CppInterop.Runtime;
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

                // onLocalPlayerSpawned (no parameters)
#if IL2CPPMELON
                SubscribeToIL2CPPEvent(playerType, "onLocalPlayerSpawned",
                    nameof(HandleLocalPlayerSpawned), null);
#else
                SubscribeToStandardEvent(playerType, "onLocalPlayerSpawned", 
                    new Action(HandleLocalPlayerSpawned));
#endif

                // onPlayerSpawned (Action<Player>)
#if IL2CPPMELON
                SubscribeToIL2CPPEvent(playerType, "onPlayerSpawned",
                    nameof(HandlePlayerSpawned), playerType);
#else
                SubscribeToStandardEvent(playerType, "onPlayerSpawned", 
                    new Action<S1PlayerScripts.Player>(HandlePlayerSpawned));
#endif

                // onPlayerDespawned (Action<Player>)
#if IL2CPPMELON
                SubscribeToIL2CPPEvent(playerType, "onPlayerDespawned",
                    nameof(HandlePlayerDespawned), playerType);
#else
                SubscribeToStandardEvent(playerType, "onPlayerDespawned", 
                    new Action<S1PlayerScripts.Player>(HandlePlayerDespawned));
#endif
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to subscribe to game player events: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Subscribes to a static event field on Mono using standard .NET delegates.
        /// </summary>
        private static void SubscribeToStandardEvent(Type targetType, string eventName, Delegate handler)
        {
            var existing = ReflectionUtils.TryGetStaticFieldOrProperty(targetType, eventName) as Delegate;
            var combined = existing != null ? Delegate.Combine(existing, handler) : handler;
            ReflectionUtils.TrySetStaticFieldOrProperty(targetType, eventName, combined);
        }

#if IL2CPPMELON
        /// <summary>
        /// Subscribes to a static event field on IL2CPP using converted delegates.
        /// Handles both parameterless Action and generic Action&lt;T&gt; events.
        /// </summary>
        private static void SubscribeToIL2CPPEvent(Type targetType, string eventName, string methodName, Type? parameterType)
        {
            var methodInfo = typeof(PlayerPatches).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            var existing = ReflectionUtils.TryGetStaticFieldOrProperty(targetType, eventName);
    
            object il2cppDelegate;
    
            if (parameterType == null)
            {
                // Action with no parameters
                var managedAction = (Action)Delegate.CreateDelegate(typeof(Action), methodInfo);
                il2cppDelegate = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(managedAction);
            }
            else
            {
                // Action<Player>
                var managedActionType = typeof(Action<>).MakeGenericType(parameterType);
                var managedDelegate = Delegate.CreateDelegate(managedActionType, methodInfo);
        
                // Convert using DelegateSupport with the generic IL2CPP Action type
                var il2cppActionType = typeof(Il2CppSystem.Action<>).MakeGenericType(parameterType);
                var convertMethod = typeof(DelegateSupport).GetMethod("ConvertDelegate", BindingFlags.Public | BindingFlags.Static);
                var genericConvert = convertMethod.MakeGenericMethod(il2cppActionType);
                il2cppDelegate = genericConvert.Invoke(null, new object[] { managedDelegate });
            }
    
            if (existing != null)
            {
                var combined = Il2CppSystem.Delegate.Combine(
                    existing as Il2CppSystem.Delegate, 
                    il2cppDelegate as Il2CppSystem.Delegate
                );
                ReflectionUtils.TrySetStaticFieldOrProperty(targetType, eventName, combined);
            }
            else
            {
                ReflectionUtils.TrySetStaticFieldOrProperty(targetType, eventName, il2cppDelegate);
            }
        }
#endif
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