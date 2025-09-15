using MelonLoader;
using S1API.Internal.Lifecycle;

[assembly: MelonInfo(typeof(S1API.S1API), "S1API (Forked by Bars)", "1.8.3", "KaBooMa")]

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
            SceneStateCleaner.ResetForSceneChange(sceneName, afterUnload: false);
        }
    }
}