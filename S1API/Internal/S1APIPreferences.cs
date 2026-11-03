using MelonLoader;

namespace S1API.Internal
{
    /// <summary>
    /// INTERNAL: MelonPreferences configuration for S1API mod behavior.
    /// </summary>
    internal static class S1APIPreferences
    {
        internal static MelonPreferences_Entry<bool>? EnableMugshotLoadingScreen { get; private set; }
        internal static MelonPreferences_Entry<bool>? EnableMugshotCache { get; private set; }
        internal static MelonPreferences_Entry<bool>? EnableUnityNullReferenceTraceLogging { get; private set; }
        internal static MelonPreferences_Entry<bool>? EnableVerboseLogging { get; private set; }

        /// <summary>
        /// Initializes the S1API preferences category and entries. Call from OnInitializeMelon.
        /// </summary>
        internal static void Initialize()
        {
            var category = MelonPreferences.CreateCategory("S1API");
            EnableMugshotLoadingScreen = category.CreateEntry<bool>(
                "EnableMugshotLoadingScreen",
                true,
                "When true, the loading screen stays open until custom NPC mugshots finish generating. Set to false to let the base game close the loading screen immediately.");

            EnableMugshotCache = category.CreateEntry<bool>(
                "EnableMugshotCache",
                true,
                "When true, generated custom NPC mugshots are kept in UserData/S1API/MugshotCache and reused while the NPC's appearance, the game version and the installed mods stay the same. Set to false to render every mugshot on every load.");

            EnableUnityNullReferenceTraceLogging = category.CreateEntry<bool>(
                "EnableUnityNullReferenceTraceLogging",
                false,
                "When true, S1API subscribes to Unity's threaded log callback and emits stack traces for NullReferenceException logs to help diagnose runtime issues.");

            EnableVerboseLogging = category.CreateEntry<bool>(
                "EnableVerboseLogging",
                false,
                "When true, S1API emits internal implementation diagnostics for rendering, resource registration, and runtime fallback behavior.");
        }
    }
}
