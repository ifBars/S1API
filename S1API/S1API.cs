using MelonLoader;
using S1API.Internal;
using S1API.Internal.Lifecycle;
using S1API.Map;

[assembly: MelonInfo(typeof(S1API.S1API), "S1API (Forked by Bars)", "2.6.0", "KaBooMa")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace S1API
{
    /// <summary>
    /// S1API root MelonMod. Provides lifecycle hooks for internal systems.
    /// </summary>
    public class S1API : MelonMod
    {
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            SceneStateCleaner.ResetForSceneChange(sceneName, afterUnload: true);
        }
        
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            NPCNetworkBootstrap.EnsurePrefabsWarmup();
            SceneStateCleaner.ResetForSceneChange(sceneName, afterUnload: false);
        }
    }
}
