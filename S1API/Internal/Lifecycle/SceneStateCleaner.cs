using System;
using S1API.Dialogues;
using S1API.Entities;
using S1API.Avatar;
using S1API.Logging;
using S1API.Map;
using S1API.Quests;
using S1API.Shops;
using S1API.GameTime;
using S1API.Internal.Entities;
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

        private static void TryRun(Action action, string warningMessage = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(warningMessage))
                    Logger.Warning($"[S1API] {warningMessage}: {ex.Message}");
            }
        }

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
                    for (int i = 0; i < NPC.All.Count; i++)
                    {
                        var npc = NPC.All[i];
                        if (npc != null && npc.gameObject != null)
                        {
                            TryRun(() => UnityEngine.Object.Destroy(npc.gameObject));
                        }
                    }
                    NPC.All.Clear();
                    NPCPatches.CustomNpcsReady = false; // Reset flag for next scene load
                    
                    QuestManager.Quests.Clear();
                    global::S1API.Map.Building.All.Clear();
                    DeliveryLocation.All.Clear();
                    Seat.Clear();
                    ParkingLotRegistry.All.Clear();
                    ShopManager.InvalidateCache();
                    DeferredMapResolver.Clear();
                    TimeManager.ResetBindings();
                    HomeScreenScrollPatch.ResetInitializationState();
                    NPCAppearance.ResetMugshotState();
                    LoadingScreenPatches.ResetState();
                    DialogueInjector.ResetState();
                    DialogueChoiceListener.ResetState();
                    ContactsAppPatches.ResetState();
                    NPCPatches.ResetState();
                    NPCDealer.ClearStaticDelegates();
                    TimeManagerShim.Instance.ResetDelegates();
                }
                else
                {
                    LoadingScreenPatches.ResetState();
                    NPCNetworkBootstrap.EnsurePrefabsWarmup();

                    if (string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
                    {
                        NPCNetworkBootstrap.OnMainSceneInitialized();
                        TryRun(SeatBootstrap.OnMainSceneInitialized);
                        TryRun(DeferredMapResolver.ResolveAll, "Failed to resolve deferred map lookups");
                        TryRun(TimeManagerShim.Instance.AddDelegatesToReal);
                    }
                    else
                    {
                        NPCNetworkBootstrap.ResetFlags();
                        TryRun(TimeManagerShim.Instance.DeleteDelegatesFromReal);
                    }

                    TryRun(TimeManager.TryBindToCurrentInstance);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Scene cleanup encountered an error: {ex.Message}");
            }
        }
    }
}


