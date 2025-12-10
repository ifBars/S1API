#if (IL2CPPMELON)
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Persistence = ScheduleOne.Persistence;
#endif

using System;
using UnityEngine.Events;

namespace S1API.Lifecycle
{
    /// <summary>
    /// Provides lifecycle events for game initialization and loading.
    /// Subscribe to these events to execute code at specific points in the game's lifecycle.
    /// </summary>
    /// <remarks>
    /// This API provides a cross-runtime abstraction over ScheduleOne's LoadManager events,
    /// eliminating the need for runtime-specific conditional compilation in mods.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to PreLoad event to initialize items before game loads
    /// GameLifecycle.OnPreLoad += () => {
    ///     MelonLogger.Msg("Creating custom items...");
    ///     CreateMyCustomItems();
    /// };
    /// </code>
    /// </example>
    public static class GameLifecycle
    {
        private static bool _initialized;

        /// <summary>
        /// Fired before the game begins loading saved data.
        /// This is the ideal time to register custom items, as they need to exist before save data is loaded.
        /// </summary>
        /// <remarks>
        /// Equivalent to LoadManager.onPreLoad but abstracted for cross-runtime compatibility.
        /// </remarks>
        public static event Action OnPreLoad;

        /// <summary>
        /// INTERNAL: Initializes the lifecycle system and hooks into game events.
        /// Called automatically by S1API.
        /// </summary>
        internal static void Initialize()
        {
            if (_initialized)
                return;

            var loadManager = S1Persistence.LoadManager.Instance;
            if (loadManager == null)
                return;

#if IL2CPPMELON || IL2CPPBEPINEX
            loadManager.onPreLoad.AddListener((UnityAction)InvokeOnPreLoad);
#elif MONOMELON || MONOBEPINEX
            loadManager.onPreLoad.AddListener(new UnityAction(InvokeOnPreLoad));
#endif

            _initialized = true;
        }

        /// <summary>
        /// INTERNAL: Resets the initialization state.
        /// Called when scenes are unloaded.
        /// </summary>
        internal static void Reset()
        {
            _initialized = false;
        }

        private static void InvokeOnPreLoad()
        {
            try
            {
                OnPreLoad?.Invoke();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error in GameLifecycle.OnPreLoad: {ex}");
            }
        }
    }
}
