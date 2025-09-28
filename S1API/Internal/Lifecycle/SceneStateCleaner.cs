using System;
using S1API.Entities;
using S1API.Avatar;
using S1API.Logging;
using S1API.Map;
using S1API.Quests;
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

                    // Quests: clear S1API quest registry. The base game manages its own instances.
                    int questCount = QuestManager.Quests.Count;
                    QuestManager.Quests.Clear();

                    // Buildings: clear S1API building registry. Objects are destroyed by scene unload.
                    int buildingCount = Building.All.Count;
                    Building.All.Clear();

                    // Delivery Locations: clear S1API delivery location registry. Objects are destroyed by scene unload.
                    int deliveryLocationCount = DeliveryLocation.All.Count;
                    DeliveryLocation.All.Clear();

                    // Seats: clear the avatar seat registry to avoid stale references across loads.
                    int seatCount = Seat.Count;
                    Seat.Clear();

                    Logger.Msg($"[S1API] Cleaned scene state after unload of '{sceneName}' (NPCs: {npcCount} -> 0, Quests: {questCount} -> 0, Buildings: {buildingCount} -> 0, DeliveryLocations: {deliveryLocationCount} -> 0, Seats: {seatCount} -> 0)");
                }
                else
                {
                    // On scene initialization, only log that we're preparing for the scene
                    Logger.Msg($"[S1API] Preparing for scene '{sceneName}' initialization");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Scene cleanup encountered an error: {ex.Message}");
            }
        }
    }
}


