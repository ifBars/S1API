#if (IL2CPPMELON)
using S1Customization = Il2CppScheduleOne.AvatarFramework.Customization;
using S1Clothing = Il2CppScheduleOne.Clothing;
using Il2CppCollectionsGeneric = Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Customization = ScheduleOne.AvatarFramework.Customization;
using S1Clothing = ScheduleOne.Clothing;
#endif

using System;
using System.Collections.Generic;
using S1API.Avatar;
using UnityEngine;

namespace S1API.UI
{
    /// <summary>
    /// Provides programmatic control over the in-game character creator UI system.
    /// Allows mods to open the character creator, listen for completion events, and retrieve customized avatar settings.
    /// </summary>
    public static class CharacterCreatorManager
    {
        private static readonly Logging.Log Logger = new Logging.Log("CharacterCreatorManager");
        private static S1Customization.CharacterCreator _s1Creator;
        private static bool _isInitialized;
        private static bool _eventsRegistered;

        #region Public Events

        /// <summary>
        /// Fired when the character creator is opened.
        /// </summary>
        public static event Action OnOpened;

        /// <summary>
        /// Fired when the character creator is closed without completion.
        /// </summary>
        public static event Action OnClosed;

        /// <summary>
        /// Fired when character customization is completed successfully.
        /// </summary>
        /// <remarks>
        /// The BasicAvatarSettings parameter contains the finalized character configuration.
        /// </remarks>
        public static event Action<BasicAvatarSettings> OnCompleted;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the character creator is currently open and active.
        /// </summary>
        public static bool IsOpen
        {
            get
            {
                EnsureInitialized();
                return _s1Creator?.IsOpen ?? false;
            }
        }

        /// <summary>
        /// The current avatar settings being edited in the character creator.
        /// Returns null if the creator is not open.
        /// </summary>
        public static BasicAvatarSettings ActiveSettings
        {
            get
            {
                EnsureInitialized();
                if (_s1Creator?.ActiveSettings == null)
                    return null;

                return new BasicAvatarSettings(_s1Creator.ActiveSettings);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the character creator with the specified initial settings.
        /// </summary>
        /// <param name="initialSettings">Optional initial avatar settings. If null, default settings are used.</param>
        /// <param name="showUI">Whether to display the UI. Set to false to customize programmatically without showing UI.</param>
        public static void Open(BasicAvatarSettings initialSettings = null, bool showUI = true)
        {
            EnsureInitialized();

            if (_s1Creator == null)
            {
                Logger.Error("CharacterCreator singleton is not available");
                return;
            }

            if (_s1Creator.IsOpen)
            {
                Logger.Warning("CharacterCreator is already open");
                return;
            }

            RegisterEvents();

            var s1Settings = initialSettings?.S1BasicAvatarSettings;
            _s1Creator.Open(s1Settings, showUI);

            try
            {
                OnOpened?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnOpened event handler: {ex}");
            }
        }

        /// <summary>
        /// Closes the character creator without saving changes.
        /// </summary>
        public static void Close()
        {
            EnsureInitialized();

            if (_s1Creator == null)
            {
                Logger.Error("CharacterCreator singleton is not available");
                return;
            }

            if (!_s1Creator.IsOpen)
            {
                Logger.Warning("CharacterCreator is not open");
                return;
            }

            _s1Creator.Close();

            try
            {
                OnClosed?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnClosed event handler: {ex}");
            }
        }

        /// <summary>
        /// Completes the character customization and closes the creator.
        /// Fires the OnCompleted event with the final settings.
        /// </summary>
        public static void Complete()
        {
            EnsureInitialized();

            if (_s1Creator == null)
            {
                Logger.Error("CharacterCreator singleton is not available");
                return;
            }

            if (!_s1Creator.IsOpen)
            {
                Logger.Warning("CharacterCreator is not open");
                return;
            }

            _s1Creator.Done();
        }

        /// <summary>
        /// Selects a preset by name from the available presets.
        /// </summary>
        /// <param name="presetName">The name of the preset to select.</param>
        public static void SelectPreset(string presetName)
        {
            EnsureInitialized();

            if (_s1Creator == null)
            {
                Logger.Error("CharacterCreator singleton is not available");
                return;
            }

            if (!_s1Creator.IsOpen)
            {
                Logger.Warning("CharacterCreator must be open to select a preset");
                return;
            }

            if (string.IsNullOrWhiteSpace(presetName))
            {
                Logger.Warning("Preset name cannot be null or empty");
                return;
            }

            _s1Creator.SelectPreset(presetName);
        }

        /// <summary>
        /// Gets a list of available preset names.
        /// </summary>
        /// <returns>An array of preset names available in the character creator.</returns>
        public static string[] GetAvailablePresets()
        {
            EnsureInitialized();

            if (_s1Creator == null || _s1Creator.Presets == null)
                return Array.Empty<string>();

            var presets = new List<string>();
            for (int i = 0; i < _s1Creator.Presets.Count; i++)
            {
                var preset = _s1Creator.Presets[i];
                if (preset != null && !string.IsNullOrWhiteSpace(preset.name))
                {
                    presets.Add(preset.name);
                }
            }

            return presets.ToArray();
        }

        /// <summary>
        /// Rotates the character rig in the character creator.
        /// </summary>
        /// <param name="normalizedValue">Rotation value (0.0 to 1.0), where 0.0 is 0 degrees and 1.0 is 359 degrees.</param>
        public static void SetRigRotation(float normalizedValue)
        {
            EnsureInitialized();

            if (_s1Creator == null)
            {
                Logger.Error("CharacterCreator singleton is not available");
                return;
            }

            if (!_s1Creator.IsOpen)
            {
                Logger.Warning("CharacterCreator must be open to set rig rotation");
                return;
            }

            _s1Creator.SliderChanged(Mathf.Clamp01(normalizedValue));
        }

        #endregion

        #region Private Members

        private static void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            try
            {
                _s1Creator = S1Customization.CharacterCreator.Instance;

                if (_s1Creator == null)
                {
                    Logger.Warning("CharacterCreator singleton not found. Make sure you're in the correct scene.");
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize CharacterCreatorManager: {ex}");
                _isInitialized = true; // Set to true to avoid repeated initialization attempts
            }
        }

        private static void RegisterEvents()
        {
            if (_eventsRegistered || _s1Creator == null)
                return;

            try
            {
                // Register completion event
                if (_s1Creator.onComplete != null)
                {
#if (IL2CPPMELON)
                    _s1Creator.onComplete.AddListener((Action<S1Customization.BasicAvatarSettings>)OnCreatorCompleted);
#else
                    _s1Creator.onComplete.AddListener(OnCreatorCompleted);
#endif
                }

                _eventsRegistered = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register CharacterCreator events: {ex}");
            }
        }

        private static void OnCreatorCompleted(S1Customization.BasicAvatarSettings s1Settings)
        {
            try
            {
                if (s1Settings == null)
                {
                    Logger.Warning("CharacterCreator completed with null settings");
                    return;
                }

                var wrappedSettings = new BasicAvatarSettings(s1Settings);
                OnCompleted?.Invoke(wrappedSettings);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnCompleted event handler: {ex}");
            }
        }

        #endregion
    }
}
