using System;
using MelonLoader;
using S1API.Internal;
using S1API.Internal.Lifecycle;
using S1API.Lifecycle;
using S1API.Map;

[assembly: MelonInfo(typeof(S1API.S1API), "S1API (Forked by Bars)", "2.9.5", "KaBooMa")]
[assembly: MelonPriority(Int32.MinValue)]
// Marked as incompatible as it breaks base game apps (causes them to show the mod manager instead of the base game app)
// See https://www.nexusmods.com/schedule1/mods/1484?tab=bugs
[assembly: MelonIncompatibleAssemblies("ModManager&PhoneApp")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace S1API
{
    /// <summary>
    /// S1API root MelonMod. Provides lifecycle hooks for internal systems.
    /// </summary>
    public class S1API : MelonMod
    {
        public override void OnPreSupportModule()
        {
            VersionChecker.CheckMelonLoaderVersion();
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
