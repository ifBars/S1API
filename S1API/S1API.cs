using System;
using MelonLoader;
using S1API.Cutscenes;
using S1API.Internal;
using S1API.Internal.Diagnostics;
using S1API.Internal.Entities;
using S1API.Internal.Lifecycle;
using S1API.Internal.NPCWorkbench;
using S1API.Internal.Products;
using S1API.Internal.Rendering;
using S1API.Internal.Weather;
using S1API.Lifecycle;
using S1API.Map;

[assembly: MelonInfo(typeof(S1API.S1API), "S1API (Forked by Bars)", "3.2.3", "KaBooMa")]
[assembly: MelonPriority(Int32.MinValue)]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace S1API
{
    /// <summary>
    /// S1API root MelonMod. Provides lifecycle hooks for internal systems.
    /// </summary>
    public class S1API : MelonMod
    {
        public override void OnInitializeMelon()
        {
            S1APIPreferences.Initialize();

            if (S1APIPreferences.EnableUnityNullReferenceTraceLogging?.Value == true)
            {
                MelonLogger.Warning(
                    "Exception trace logging is enabled. If you are " +
                    "not a mod developer, you probably do not need this enabled.");
                UnityExceptionTraceHook.Install();
            }
        }

        /// <summary>Releases tracked seats and resets internal runtime systems when the mod shuts down.</summary>
        public override void OnDeinitializeMelon()
        {
            Internal.Vehicles.VehicleSeats.Clear();
            Internal.Compatibility.AvatarCompatibility.ResetRendering();
            Rendering.IconFactory.ResetAccessoryIconState();
            WeatherRuntime.ResetBindings();
            NPCWorkbenchRuntime.Close();
            PresentationWorkbenchRuntime.Close();
            ProductPackagingContentRuntime.ResetForSceneChange();
            CutsceneManager.Deinitialize();
            MapPOIManager.RemoveAll();
            UnityExceptionTraceHook.Remove();
        }

        /// <summary>Refreshes tracked seat occupancy and advances the API's runtime systems each frame.</summary>
        public override void OnUpdate()
        {
            Internal.Vehicles.VehicleSeats.Tick();
            WeatherRuntime.Tick();
            PresentationWorkbenchRuntime.Tick();
            CutsceneManager.Tick(UnityEngine.Time.unscaledDeltaTime);
            NPCWorkbenchRuntime.Tick();
        }

        public override void OnGUI()
        {
            CutsceneManager.DrawPresentation();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                GameLifecycle.Initialize();
            }
        }

        /// <summary>Clears tracked seats and scene state, resetting the game lifecycle when Main unloads.</summary>
        /// <param name="buildIndex">The unloaded scene's build index; unused by this callback.</param>
        /// <param name="sceneName">The unloaded scene's name; Main and Tutorial receive additional cleanup.</param>
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            Internal.Vehicles.VehicleSeats.Clear();
            NPCWorkbenchRuntime.Close();
            PresentationWorkbenchRuntime.Close();
            CutsceneManager.CleanupForSceneChange();
            SceneStateCleaner.ResetForSceneChange(sceneName, afterUnload: true);

            if (sceneName == "Main" || sceneName == "Tutorial")
            {
                ProductPackagingContentRuntime.ResetForSceneChange();
                MapPOIManager.ResetForSceneChange();
            }

            if (sceneName == "Main")
            {
                GameLifecycle.Reset();
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            NPCNetworkBootstrap.EnsurePrefabsWarmup();
            SceneStateCleaner.ResetForSceneChange(sceneName, afterUnload: false);
        }
    }
}
