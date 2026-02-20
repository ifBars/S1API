#if (IL2CPPMELON)
using S1UI = Il2CppScheduleOne.UI;
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1Audio = Il2CppScheduleOne.Audio;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1UI = ScheduleOne.UI;
using S1Persistence = ScheduleOne.Persistence;
using S1Audio = ScheduleOne.Audio;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using HarmonyLib;
using MelonLoader;
using S1API.Entities;
using S1API.Internal.Utils;
using S1API.Logging;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches the LoadingScreen to delay closing until NPC mugshot generation is complete.
    /// This ensures players see the loading screen while S1API NPC portraits are being generated.
    /// </summary>
    [HarmonyPatch]
    internal static class LoadingScreenPatches
    {
        private static readonly Log Logger = new Log("LoadingScreenPatches");
        private static bool _isWaitingForMugshots = false;
        private static bool _hasCustomNpcTypes = false;

        /// <summary>
        /// Patch GetLoadStatusText to return our custom text when waiting for mugshots
        /// </summary>
        [HarmonyPatch(typeof(S1Persistence.LoadManager), "GetLoadStatusText")]
        [HarmonyPostfix]
        private static void GetLoadStatusText_Postfix(ref string __result)
        {
            if (_isWaitingForMugshots)
            {
                __result = "Generating NPC Mugshots...";
            }
        }

        /// <summary>
        /// Target the Close method on LoadingScreen
        /// </summary>
        [HarmonyPatch(typeof(S1UI.LoadingScreen), "Close")]
        [HarmonyPrefix]
        private static bool Close_Prefix(S1UI.LoadingScreen __instance)
        {
            if (!IsGameLoading())
                return true;

            if (!S1APIPreferences.EnableMugshotLoadingScreen.Value)
                return true;

            if (!_hasCustomNpcTypes)
                return true;

            if (NPCAppearance.MugshotsProcessingComplete)
                return true;

            if (_isWaitingForMugshots)
                return false;

            _isWaitingForMugshots = true;
            MelonCoroutines.Start(WaitForMugshotsThenClose(__instance));

            return false;
        }

        /// <summary>
        /// Check if we're currently in the final phase of game loading where NPC mugshots should complete.
        /// This is true when LoadStatus is None but IsLoading is still true (the moment before Close is called).
        /// Returns false when exiting to menu or in other contexts where mugshots aren't relevant.
        /// </summary>
        private static bool IsGameLoading()
        {
            try
            {
                var loadManager = S1DevUtilities.Singleton<S1Persistence.LoadManager>.Instance;
                if (loadManager == null)
                    return false;

                // We're in the game loading close phase when:
                // - IsLoading is true
                // - LoadStatus is None (set just before Close is called in LoadManager)
                // This happens at the very end of StartGame/LoadAsClient before LoadingScreen.Close()
                if (!loadManager.IsLoading)
                    return false;
                
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName != "Main" && sceneName != "Tutorial")
                    return false;
                
                return loadManager.LoadStatus == S1Persistence.LoadManager.ELoadStatus.None ||
                       loadManager.LoadStatus == S1Persistence.LoadManager.ELoadStatus.LoadingData;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Coroutine that waits for mugshot generation to complete, then closes the loading screen
        /// </summary>
        private static IEnumerator WaitForMugshotsThenClose(S1UI.LoadingScreen loadingScreen)
        {
            const float TIMEOUT = 90f;
            float timer = 0f;

            while (!NPCAppearance.MugshotsProcessingComplete && timer < TIMEOUT)
            {
                yield return new WaitForSeconds(0.1f);
                timer += 0.1f;
            }
            
            _isWaitingForMugshots = false;
            
            if (timer >= TIMEOUT)
            {
                int remaining = GetMugshotQueueCount();
                Logger.Warning($"Mugshot generation timeout reached after {TIMEOUT}s. {remaining} NPCs may have incomplete portraits.");
            }
            
            CloseLoadingScreenDirectly(loadingScreen);
        }

        /// <summary>
        /// Gets the current mugshot queue count via reflection (internal member)
        /// </summary>
        private static int GetMugshotQueueCount()
        {
            var queue = ReflectionUtils.TryGetStaticFieldOrProperty(typeof(NPCAppearance), "_mugshotQueue");
            if (queue is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable)
                    count++;
                return count;
            }
            
            return -1;
        }

        /// <summary>
        /// Closes the loading screen directly using reflection to bypass our harmony patch
        /// </summary>
        private static void CloseLoadingScreenDirectly(S1UI.LoadingScreen loadingScreen)
        {
            try
            {
                ReflectionUtils.TrySetFieldOrProperty(loadingScreen, "IsOpen", false);

                var musicPlayer = S1DevUtilities.Singleton<S1Audio.MusicPlayer>.Instance;
                if (musicPlayer != null)
                {
                    musicPlayer.SetTrackEnabled("Loading Screen", enabled: false);
                    musicPlayer.StopTrack("Loading Screen");
                }
                
                var fadeMethod = typeof(S1UI.LoadingScreen).GetMethod("Fade",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (fadeMethod != null)
                {
                    fadeMethod.Invoke(loadingScreen, new object[] { 0f });
                }
                else
                {
                    MelonCoroutines.Start(ManualFadeOut(loadingScreen.Group, loadingScreen.Canvas));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error closing loading screen: {ex.Message}");
                if (loadingScreen.Canvas != null)
                    loadingScreen.Canvas.enabled = false;
            }
        }

        /// <summary>
        /// Manual fade out coroutine as fallback
        /// </summary>
        private static IEnumerator ManualFadeOut(CanvasGroup group, Canvas canvas)
        {
            const float FADE_TIME = 0.25f;
            
            if (group == null || canvas == null)
                yield break;

            float startAlpha = group.alpha;
            
            for (float t = 0f; t < FADE_TIME; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(startAlpha, 0f, t / FADE_TIME);
                yield return new WaitForEndOfFrame();
            }
            
            group.alpha = 0f;
            canvas.enabled = false;
        }

        /// <summary>
        /// Called when the scene changes to reset our state
        /// </summary>
        internal static void ResetState()
        {
            _isWaitingForMugshots = false;
            _hasCustomNpcTypes = ReflectionUtils.GetDerivedClasses<NPC>()
                .Any(t => t != null && !t.IsAbstract && t.Assembly != typeof(NPC).Assembly);
        }
    }
}
