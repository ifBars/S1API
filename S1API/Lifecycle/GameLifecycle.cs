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
        /// Fired when the game has finished loading all data and the player can interact with the world.
        /// This is the ideal time to initialize mod systems that depend on game state being fully loaded.
        /// </summary>
        /// <remarks>
        /// Equivalent to LoadManager.onLoadComplete but abstracted for cross-runtime compatibility.
        /// Fires after all save data has been loaded and the loading screen is about to close.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Subscribe to OnLoadComplete to initialize systems after game loads
        /// GameLifecycle.OnLoadComplete += () => {
        ///     MelonLogger.Msg("Game loaded! Initializing mod systems...");
        ///     InitializeMyModSystems();
        /// };
        /// </code>
        /// </example>
        public static event Action OnLoadComplete;

        /// <summary>
        /// Fired before the game transitions to a different scene (e.g., Menu to Main, or exiting to Menu).
        /// Use this to clean up resources or save mod state before the scene changes.
        /// </summary>
        /// <remarks>
        /// Equivalent to LoadManager.onPreSceneChange but abstracted for cross-runtime compatibility.
        /// Fires before any cleanup occurs, giving mods a chance to perform their own cleanup.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Subscribe to OnPreSceneChange to clean up before scene transition
        /// GameLifecycle.OnPreSceneChange += () => {
        ///     MelonLogger.Msg("Scene changing, saving mod data...");
        ///     SaveMyModData();
        /// };
        /// </code>
        /// </example>
        public static event Action OnPreSceneChange;

        /// <summary>
        /// Fired when save game information has been loaded and refreshed.
        /// This occurs when the save menu is opened or when save data is re-scanned.
        /// </summary>
        /// <remarks>
        /// Equivalent to LoadManager.onSaveInfoLoaded but abstracted for cross-runtime compatibility.
        /// Useful for mods that need to know about available saves or display save information.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Subscribe to OnSaveInfoLoaded to react to save data updates
        /// GameLifecycle.OnSaveInfoLoaded += () => {
        ///     MelonLogger.Msg("Save info refreshed, updating mod UI...");
        ///     UpdateSaveDisplay();
        /// };
        /// </code>
        /// </example>
        public static event Action OnSaveInfoLoaded;

        /// <summary>
        /// Fired when the game begins saving data.
        /// Use this to prepare mod data for saving or trigger custom save logic.
        /// </summary>
        /// <remarks>
        /// Equivalent to SaveManager.onSaveStart but abstracted for cross-runtime compatibility.
        /// Only fires on the host/server. Clients do not trigger saves.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Subscribe to OnSaveStart to prepare data before save
        /// GameLifecycle.OnSaveStart += () => {
        ///     MelonLogger.Msg("Game saving started, preparing mod data...");
        ///     PrepareModDataForSave();
        /// };
        /// </code>
        /// </example>
        public static event Action OnSaveStart;

        /// <summary>
        /// Fired when the game has finished saving all data.
        /// Use this to confirm mod data was saved or perform post-save cleanup.
        /// </summary>
        /// <remarks>
        /// Equivalent to SaveManager.onSaveComplete but abstracted for cross-runtime compatibility.
        /// Only fires on the host/server after all save operations complete.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Subscribe to OnSaveComplete to confirm save completion
        /// GameLifecycle.OnSaveComplete += () => {
        ///     MelonLogger.Msg("Game save complete!");
        ///     ConfirmModDataSaved();
        /// };
        /// </code>
        /// </example>
        public static event Action OnSaveComplete;

        /// <summary>
        /// INTERNAL: Initializes the lifecycle system and hooks into game events.
        /// Called automatically by S1API.
        /// </summary>
        internal static void Initialize()
        {
            if (_initialized)
                return;

            var loadManager = S1Persistence.LoadManager.Instance;
            var saveManager = S1Persistence.SaveManager.Instance;
            
            if (loadManager == null || saveManager == null)
                return;

#if IL2CPPMELON || IL2CPPBEPINEX
            loadManager.onPreLoad.AddListener((UnityAction)InvokeOnPreLoad);
            loadManager.onLoadComplete.AddListener((UnityAction)InvokeOnLoadComplete);
            loadManager.onPreSceneChange.AddListener((UnityAction)InvokeOnPreSceneChange);
            loadManager.onSaveInfoLoaded.AddListener((UnityAction)InvokeOnSaveInfoLoaded);
            saveManager.onSaveStart.AddListener((UnityAction)InvokeOnSaveStart);
            saveManager.onSaveComplete.AddListener((UnityAction)InvokeOnSaveComplete);
#elif MONOMELON || MONOBEPINEX
            loadManager.onPreLoad.AddListener(new UnityAction(InvokeOnPreLoad));
            loadManager.onLoadComplete.AddListener(new UnityAction(InvokeOnLoadComplete));
            loadManager.onPreSceneChange.AddListener(new UnityAction(InvokeOnPreSceneChange));
            loadManager.onSaveInfoLoaded.AddListener(new UnityAction(InvokeOnSaveInfoLoaded));
            saveManager.onSaveStart.AddListener(new UnityAction(InvokeOnSaveStart));
            saveManager.onSaveComplete.AddListener(new UnityAction(InvokeOnSaveComplete));
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

        private static void InvokeOnLoadComplete()
        {
            try
            {
                OnLoadComplete?.Invoke();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error in GameLifecycle.OnLoadComplete: {ex}");
            }
        }

        private static void InvokeOnPreSceneChange()
        {
            try
            {
                OnPreSceneChange?.Invoke();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error in GameLifecycle.OnPreSceneChange: {ex}");
            }
        }

        private static void InvokeOnSaveInfoLoaded()
        {
            try
            {
                OnSaveInfoLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error in GameLifecycle.OnSaveInfoLoaded: {ex}");
            }
        }

        private static void InvokeOnSaveStart()
        {
            try
            {
                OnSaveStart?.Invoke();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error in GameLifecycle.OnSaveStart: {ex}");
            }
        }

        private static void InvokeOnSaveComplete()
        {
            try
            {
                OnSaveComplete?.Invoke();
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"Error in GameLifecycle.OnSaveComplete: {ex}");
            }
        }
    }
}
