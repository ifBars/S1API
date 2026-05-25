using System;
using MelonLoader;
using S1API.Internal;
using S1API.Internal.Diagnostics;
using S1API.Internal.Entities;
using S1API.Internal.Lifecycle;
using S1API.Lifecycle;
using S1API.Map;

[assembly: MelonInfo(typeof(S1API.S1API), "S1API (Forked by Bars)", "3.0.4", "KaBooMa")]
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

            if (S1APIPreferences.EnableUnityNullReferenceTraceLogging.Value)
            {
                UnityExceptionTraceHook.Install();
            }
        }

        public override void OnDeinitializeMelon()
        {
            UnityExceptionTraceHook.Remove();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                GameLifecycle.Initialize();
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            SceneStateCleaner.ResetForSceneChange(sceneName, afterUnload: true);

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
