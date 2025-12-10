using System;
using S1API.Entities;
using S1API.Avatar;
using S1API.Logging;
using S1API.Map;
using S1API.Quests;
using S1API.Shops;
using S1API.GameTime;
using S1API.Internal.Map;
using S1API.Internal.Patches;
using UnityEngine;

namespace S1API.Internal.Lifecycle
{
    /// <summary>
    /// INTERNAL: Resets S1API-managed runtime state on scene changes to avoid leakage across sessions.
    /// Currently clears NPC and Quest registries when transitioning in/out of gameplay scenes.
    /// </summary>
    internal static class SceneStateCleaner
    {
        private static readonly Log Logger = new Log("Lifecycle");

        /// <summary>
        /// Determines whether S1API should reset internal state for the given scene.
        /// </summary>
        private static bool ShouldResetForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            // Support known gameplay scene names; extend as needed.
            string s = sceneName.Trim();
            return string.Equals(s, "Main", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reset S1API runtime state around a scene change.
        /// </summary>
        /// <param name="sceneName">The scene name transitioning.</param>
        /// <param name="afterUnload">True if invoked after unload, false if invoked after initialize.</param>
        internal static void ResetForSceneChange(string sceneName, bool afterUnload)
        {
            if (!ShouldResetForScene(sceneName))
                return;

            try
            {
                if (afterUnload)
                {
                    // Only clear registries when actually unloading a scene
                    // NPCs: try to destroy custom NPC game objects to ensure full cleanup,
                    // then clear the wrapper registry.
                    int npcCount = NPC.All.Count;
                    for (int i = 0; i < NPC.All.Count; i++)
                    {
                        var npc = NPC.All[i];
                        if (npc != null && npc.gameObject != null)
                        {
                            // Best-effort cleanup for custom NPCs; base NPCs are destroyed by scene unload.
                            try { UnityEngine.Object.Destroy(npc.gameObject); } catch { /* ignore */ }
                        }
                    }
                    NPC.All.Clear();
                    NPCPatches.CustomNpcsReady = false; // Reset flag for next scene load

                    // Quests: clear S1API quest registry. The base game manages its own instances.
                    int questCount = QuestManager.Quests.Count;
                    QuestManager.Quests.Clear();

                    // Buildings: clear S1API building registry. Objects are destroyed by scene unload.
                    int buildingCount = global::S1API.Map.Building.All.Count;
                    global::S1API.Map.Building.All.Clear();

                    // Delivery Locations: clear S1API delivery location registry. Objects are destroyed by scene unload.
                    int deliveryLocationCount = DeliveryLocation.All.Count;
                    DeliveryLocation.All.Clear();

                    // Seats: clear the avatar seat registry to avoid stale references across loads.
                    int seatCount = Seat.Count;
                    Seat.Clear();

                    // Parking Lots: clear S1API parking lot registry.
                    int parkingLotCount = ParkingLotRegistry.All.Count;
                    ParkingLotRegistry.All.Clear();

                    // Shops: invalidate the shop cache
                    ShopManager.InvalidateCache();

                    // Clear deferred lookups
                    DeferredMapResolver.Clear();

                    // Drop bindings to the previous TimeManager so the next scene can rebind cleanly.
                    TimeManager.ResetBindings();
                }
                else
                {
                    // Notify NPC bootstrap for readiness and prefab pre-registration
                    try
                    {
                        NPCNetworkBootstrap.EnsurePrefabsWarmup();

                        if (string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
                        {
                            NPCNetworkBootstrap.OnMainSceneInitialized();
                            // Kick off delayed seating scan once Main initializes to avoid early Awake crashes
                            try { Internal.SeatBootstrap.OnMainSceneInitialized(); } catch { }
                            
                            // Resolve all deferred map entity lookups now that Main scene is loaded
                            try 
                            { 
                                DeferredMapResolver.ResolveAll(); 
                            } 
                            catch (Exception ex) 
                            { 
                                Logger.Warning($"[S1API] Failed to resolve deferred map lookups: {ex.Message}"); 
                            }

                            // Try to migrate any delegates from TimeManagerShim into the real TimeManager now that Main is initialized
                            try { TimeManagerShim.Instance.AddDelegatesToReal(); } catch { }
                        }
                        else
                        {
                            NPCNetworkBootstrap.ResetFlags();
                            TimeManagerShim.Instance.DeleteDelegatesFromReal();
                        }
                    }
                    catch { }

                    // Attempt to bind S1API events to the fresh TimeManager instance for the new scene.
                    try { TimeManager.TryBindToCurrentInstance(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Scene cleanup encountered an error: {ex.Message}");
            }
        }
    }
}


