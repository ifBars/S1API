#if (IL2CPPMELON)
using S1Customization = Il2CppScheduleOne.CharacterCreator;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1UI = Il2CppScheduleOne.UI;
#elif MONOMELON
using S1Customization = ScheduleOne.CharacterCreator;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1UI = ScheduleOne.UI;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using S1API.Avatar;
using S1API.Entities;
using S1API.Internal.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.UI
{
    /// <summary>
    /// Provides programmatic control over the in-game character creator UI system.
    /// Allows mods to open the character creator, listen for completion events, and retrieve customized avatar settings.
    /// </summary>
    public static class CharacterCreatorManager
    {
        private static readonly Logging.Log Logger = new Logging.Log("CharacterCreatorManager");
        private static S1Customization.CharacterCreator? _s1Creator;
        private static bool _isInitialized;
        private static bool _eventsRegistered;

        #region Public Events

        /// <summary>
        /// Fired when the character creator is opened.
        /// </summary>
        public static event Action? OnOpened;

        /// <summary>
        /// Fired when the character creator is closed without completion.
        /// </summary>
        public static event Action? OnClosed;

        /// <summary>
        /// Fired when character customization is completed successfully.
        /// </summary>
        /// <remarks>
        /// The BasicAvatarSettings parameter contains the finalized character configuration.
        /// </remarks>
        public static event Action<BasicAvatarSettings>? OnCompleted;

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
        public static BasicAvatarSettings? ActiveSettings
        {
            get
            {
                EnsureInitialized();
                return null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the character creator with the specified initial settings.
        /// </summary>
        /// <param name="initialSettings">Optional initial avatar settings. If null, player's current avatar settings are loaded, or default settings if player has none.</param>
        /// <param name="showUI">Whether to display the UI. Set to false to customize programmatically without showing UI.</param>
        public static void Open(BasicAvatarSettings? initialSettings = null, bool showUI = true)
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

            if (initialSettings != null || !showUI)
            {
                Logger.Warning(
                    "Schedule I 0.4.7 no longer accepts legacy initial settings or a hidden " +
                    "character-creator canvas; opening the native creator with its defaults.");
            }

            _s1Creator.Open();

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

            var closeMethod = typeof(S1Customization.CharacterCreator).GetMethod(
                "Close",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
            closeMethod?.Invoke(_s1Creator, null);

            // Restore camera transform and FOV if no other UI elements are active
            // Use coroutine to wait for base game's Close() coroutine to finish
            MelonCoroutines.Start(RestoreAfterClose());

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
        /// Camera restoration is handled in the OnCreatorCompleted callback.
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

            Logger.Warning(
                "Character-creator presets are not available in Schedule I 0.4.7.");
        }

        /// <summary>
        /// Gets a list of available preset names.
        /// </summary>
        /// <returns>An array of preset names available in the character creator.</returns>
        public static string[] GetAvailablePresets()
        {
            EnsureInitialized();

            return Array.Empty<string>();
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

            _s1Creator.SetAvatarRotation(Mathf.Clamp01(normalizedValue) * 359f);
        }

        /// <summary>
        /// Pre-registers the character creator as an active UI element to prevent other systems (like dialogue) from restoring the camera.
        /// Call this before ending dialogue or other UI systems to ensure smooth camera transitions.
        /// </summary>
        public static void PreRegisterAsActiveUI()
        {
            EnsureInitialized();

            if (_s1Creator == null)
            {
                Logger.Warning("CharacterCreator singleton is not available for pre-registration");
                return;
            }

            if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.InstanceExists)
            {
                S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.Instance.AddActiveUIElement(_s1Creator.name);
            }
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
#if (IL2CPPMELON)
                _s1Creator.add_OnComplete(
                    (Action<S1Customization.CharacterCreatorState>)OnCreatorCompleted);
#else
                _s1Creator.OnComplete += OnCreatorCompleted;
#endif

                _eventsRegistered = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register CharacterCreator events: {ex}");
            }
        }

        private static void OnCreatorCompleted(S1Customization.CharacterCreatorState state)
        {
            try
            {
                if (state?.Appearance == null)
                {
                    Logger.Warning("CharacterCreator completed with null settings");
                    return;
                }

                BasicAvatarSettings wrappedSettings = BasicAvatarSettings.Create();
                wrappedSettings.Gender = (int)state.Appearance.Gender;
                wrappedSettings.Weight = state.Appearance.Weight;
                wrappedSettings.SkinColor = state.Appearance.SkinColor;
                wrappedSettings.HairStyle = state.Appearance.HairStyleId ?? string.Empty;
                wrappedSettings.HairColor = state.Appearance.HairColor;
                wrappedSettings.Mouth = state.Appearance.FaceId ?? string.Empty;
                wrappedSettings.FacialHair = state.Appearance.FacialHairId ?? string.Empty;
                wrappedSettings.FacialDetails = state.Appearance.FacialDetailId ?? string.Empty;
                wrappedSettings.FacialDetailsIntensity = state.Appearance.FacialDetailIntensity;
                wrappedSettings.EyeballColor = state.Appearance.EyeballColor;
                wrappedSettings.PupilDilation = state.Appearance.PupilDilation;
                wrappedSettings.UpperEyeLidRestingPosition = state.Appearance.UpperEyelidPosition;
                wrappedSettings.LowerEyeLidRestingPosition = state.Appearance.LowerEyelidPosition;
                wrappedSettings.EyebrowScale = state.Appearance.EyebrowScale;
                wrappedSettings.EyebrowThickness = state.Appearance.EyebrowThickness;
                wrappedSettings.EyebrowRestingHeight = state.Appearance.EyebrowHeight;
                wrappedSettings.EyebrowRestingAngle = state.Appearance.EyebrowAngle;
                
                // Restore camera after a delay to let the base game's Close() coroutine finish
                // The base game's Done() calls Close() which starts a coroutine that removes UI element
                MelonCoroutines.Start(RestoreGameState());
                
                OnCompleted?.Invoke(wrappedSettings);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnCompleted event handler: {ex}");
            }
        }

        /// <summary>
        /// Coroutine to restore camera and game state after character creator closes.
        /// Waits for the base game's Close() coroutine to finish before restoring.
        /// </summary>
        private static IEnumerator RestoreAfterClose()
        {
            yield return RestoreGameState();
        }

        /// <summary>
        /// Shared coroutine logic to restore camera, HUD, player movement, and inventory.
        /// </summary>
        private static IEnumerator RestoreGameState()
        {
            // Wait a frame for the base game's Close() coroutine to finish
            yield return null;
            
            // Additional small delay to ensure everything is cleaned up
            yield return new WaitForSeconds(0.1f);

            if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.InstanceExists)
            {
                var camera = S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerCamera>.Instance;
                
                // Remove UI element if it's still registered (should already be removed by base game, but be safe)
                camera.RemoveActiveUIElement(_s1Creator?.name ?? "CharacterCreator");
                
                // Only restore camera if no other UI elements are active
                if (GetActiveUIElementCount(camera) == 0)
                {
                    camera.StopTransformOverride(0f, reenableCameraLook: true, returnToOriginalRotation: false);
                    camera.StopFOVOverride(0f);
                    camera.SetCanLook(true);
                    camera.LockMouse();
                }
            }

            // Restore HUD, player movement, and inventory (base game's Close() doesn't restore these)
            if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.InstanceExists)
            {
                S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerMovement>.Instance.CanMove = true;
            }

            if (S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerInventory>.InstanceExists)
            {
                S1DevUtilities.PlayerSingleton<S1PlayerScripts.PlayerInventory>.Instance.SetInventoryEnabled(enabled: true);
            }

            if (S1DevUtilities.Singleton<S1UI.HUD>.InstanceExists)
            {
                S1DevUtilities.Singleton<S1UI.HUD>.Instance.canvas.enabled = true;
            }
        }

        /// <summary>
        /// Gets the player's current avatar settings, wrapped in BasicAvatarSettings.
        /// Returns null if player has no avatar settings yet.
        /// </summary>
        /// <returns>Player's current avatar settings, or null if not available.</returns>
        private static BasicAvatarSettings? GetPlayerAvatarSettings()
        {
            try
            {
                var localPlayer = Player.Local;
                if (localPlayer == null || localPlayer.S1Player == null)
                {
                    Logger.Warning("Local player not available");
                    return null;
                }

                var currentSettings = localPlayer.GetCurrentBasicAvatarSettings();
                if (currentSettings == null)
                {
                    Logger.Debug("Player has no current avatar settings, using default");
                    return null;
                }

                // Create a copy to avoid modifying the original
                var copy = Object.Instantiate(currentSettings.S1BasicAvatarSettings);
                return new BasicAvatarSettings(copy);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get player avatar settings: {ex}");
                return null;
            }
        }

        private static int GetActiveUIElementCount(S1PlayerScripts.PlayerCamera camera)
        {
            if (camera == null)
                return 0;

#if IL2CPPMELON
            if (ReflectionUtils.TryGetFieldOrProperty(camera, "ActiveUIElementCount") is int count)
                return count;
#else
            if (ReflectionUtils.TryGetFieldOrProperty(camera, "activeUIElementCount") is int count)
                return count;
#endif

            return 0;
        }

        #endregion
    }
}
