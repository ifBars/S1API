using MelonLoader;

namespace S1API.Internal
{
    /// <summary>
    /// INTERNAL: MelonPreferences configuration for S1API mod behavior.
    /// </summary>
    internal static class S1APIPreferences
    {
        private static MelonPreferences_Category _category;
        internal static MelonPreferences_Entry<bool> EnableMugshotLoadingScreen;
        internal static MelonPreferences_Entry<bool> EnableUnityNullReferenceTraceLogging;

        /// <summary>
        /// Initializes the S1API preferences category and entries. Call from OnInitializeMelon.
        /// </summary>
        internal static void Initialize()
        {
            _category = MelonPreferences.CreateCategory("S1API");
            EnableMugshotLoadingScreen = _category.CreateEntry<bool>(
                "EnableMugshotLoadingScreen",
                true,
                "When true, the loading screen stays open until custom NPC mugshots finish generating. Set to false to let the base game close the loading screen immediately.");

            EnableUnityNullReferenceTraceLogging = _category.CreateEntry<bool>(
                "EnableUnityNullReferenceTraceLogging",
                false,
                "When true, S1API subscribes to Unity's threaded log callback and emits stack traces for NullReferenceException logs to help diagnose runtime issues.");
        }
    }
}
