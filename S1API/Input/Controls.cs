using System;
using System.Collections.Generic;
using UnityEngine;

#if (IL2CPPMELON)
using S1GameInput = Il2CppScheduleOne.GameInput;
using S1ButtonCode = Il2CppScheduleOne.GameInput.ButtonCode;
using S1InputDeviceType = Il2CppScheduleOne.GameInput.InputDeviceType;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1GameInput = ScheduleOne.GameInput;
using S1ButtonCode = ScheduleOne.GameInput.ButtonCode;
using S1InputDeviceType = ScheduleOne.GameInput.InputDeviceType;
#endif

namespace S1API.Input
{
    /// <summary>
    /// Button codes for all game actions that can be checked via GetButton methods.
    /// Mirrors ScheduleOne.GameInput.ButtonCode for cross-platform compatibility.
    /// </summary>
    public enum ButtonCode
    {
        PrimaryClick = 0,
        SecondaryClick = 1,
        TertiaryClick = 2,
        Forward = 3,
        Backward = 4,
        Left = 5,
        Right = 6,
        Jump = 7,
        Crouch = 8,
        Sprint = 9,
        Escape = 10,
        Back = 11,
        Interact = 12,
        Submit = 13,
        TogglePhone = 14,
        VehicleToggleLights = 15,
        VehicleHandbrake = 16,
        RotateLeft = 17,
        RotateRight = 18,
        ManagementMode = 19,
        OpenMap = 20,
        OpenJournal = 21,
        OpenTexts = 22,
        QuickMove = 23,
        ToggleFlashlight = 24,
        ViewAvatar = 25,
        Reload = 26,
        InventoryLeft = 27,
        InventoryRight = 28,
        Holster = 29,
        VehicleResetCamera = 30,
        SkateboardDismount = 31,
        SkateboardMount = 32,
        TogglePauseMenu = 33
    }

    /// <summary>
    /// Types of input devices supported by the game.
    /// Mirrors ScheduleOne.GameInput.InputDeviceType for cross-platform compatibility.
    /// </summary>
    public enum InputDeviceType
    {
        KeyboardMouse = 0,
        Gamepad = 1
    }

    /// <summary>
    /// INTERNAL: Stores the last known input device for change detection.
    /// </summary>
    internal static class ControlsState
    {
        internal static InputDeviceType LastKnownDevice;
        internal static readonly Dictionary<Action<InputDeviceType>, Action<S1InputDeviceType>> TrackedListeners 
            = new Dictionary<Action<InputDeviceType>, Action<S1InputDeviceType>>();
    }

    /// <summary>
    /// Modder-facing facade over the base game's input state to keep S1API consumers decoupled
    /// from the underlying <c>ScheduleOne.GameInput</c> type across Mono/IL2CPP.
    /// </summary>
    public static class Controls
    {
        /// <summary>
        /// Gets or sets whether the player is currently typing in a UI field.
        /// When true, gameplay input should generally be ignored by systems listening for controls.
        /// </summary>
        public static bool IsTyping
        {
            get => S1GameInput.IsTyping;
            set => S1GameInput.IsTyping = value;
        }

        /// <summary>
        /// Gets the current motion input axis (WASD / left analog stick).
        /// X component represents left/right movement, Y component represents forward/backward.
        /// </summary>
        public static Vector2 MotionAxis => S1GameInput.MotionAxis;

        /// <summary>
        /// Gets the current camera/mouse delta input.
        /// This represents how much the camera has moved this frame.
        /// </summary>
        public static Vector2 CameraAxis => S1GameInput.CameraAxis;

        /// <summary>
        /// Gets the current mouse delta input.
        /// This mirrors the game's mouse delta convenience property.
        /// </summary>
        public static Vector2 MouseDelta => S1GameInput.MouseDelta;

        /// <summary>
        /// Gets the current cursor position, accounting for controller-driven virtual cursor mode.
        /// </summary>
        public static Vector3 MousePosition => S1GameInput.MousePosition;

        /// <summary>
        /// Gets the current mouse scroll wheel delta.
        /// Positive values indicate scrolling up, negative values indicate scrolling down.
        /// </summary>
        public static float MouseScrollDelta => S1GameInput.MouseWheelAxis;

        /// <summary>
        /// Gets the currently active input device type (Keyboard/Mouse or Gamepad).
        /// </summary>
        public static InputDeviceType CurrentInputDevice =>
            (InputDeviceType)(int)S1GameInput.CurrentInputDevice;

        /// <summary>
        /// Gets whether the controller combo modifier is currently active.
        /// Used for controller-specific input combinations.
        /// </summary>
        public static bool ControllerComboActive => S1GameInput.ControllerComboActive;

        /// <summary>
        /// Gets the current vehicle drive axis value.
        /// </summary>
        public static float VehicleDriveAxis => S1GameInput.VehicleDriveAxis;

        /// <summary>
        /// Gets the current UI navigation direction.
        /// </summary>
        public static Vector2 UINavigationDirection => S1GameInput.UINavigationDirection;

        /// <summary>
        /// Gets the current UI panel cycling direction.
        /// </summary>
        public static Vector2 UICyclePanelDirection => S1GameInput.UICyclePanelDirection;

        /// <summary>
        /// Gets the primary UI tab navigation axis.
        /// </summary>
        public static float UITabNavigationPrimaryAxis => S1GameInput.UITabNavigationPrimaryAxis;

        /// <summary>
        /// Gets the secondary UI tab navigation axis.
        /// </summary>
        public static float UITabNavigationSecondaryAxis => S1GameInput.UITabNavigationSecondaryAxis;

        /// <summary>
        /// Gets the current UI scrollbar axis.
        /// </summary>
        public static float UIScrollbarAxis => S1GameInput.UIScrollbarAxis;

        /// <summary>
        /// Gets the current UI map navigation direction.
        /// </summary>
        public static Vector2 UIMapNavigationDirection => S1GameInput.UIMapNavigationDirection;

        /// <summary>
        /// Gets the current UI map zoom axis.
        /// </summary>
        public static float UIMapZoomAxis => S1GameInput.UIMapZoomAxis;

        /// <summary>
        /// Gets the small-step modify amount UI axis.
        /// </summary>
        public static float UIModifyAmountIncrementTierOneAxis => S1GameInput.UIModifyAmountIncrementTierOneAxis;

        /// <summary>
        /// Gets the medium-step modify amount UI axis.
        /// </summary>
        public static float UIModifyAmountIncrementTierTwoAxis => S1GameInput.UIModifyAmountIncrementTierTwoAxis;

        /// <summary>
        /// Gets the large-step modify amount UI axis.
        /// </summary>
        public static float UIModifyAmountIncrementTierThreeAxis => S1GameInput.UIModifyAmountIncrementTierThreeAxis;

        /// <summary>
        /// Registers a callback for when the active input device changes (e.g., from keyboard to gamepad).
        /// The callback receives the new input device type.
        /// </summary>
        /// <param name="callback">The callback to invoke when input device changes.</param>
        public static void RegisterDeviceChangedListener(Action<InputDeviceType> callback)
        {
            if (callback == null)
                return;

            if (ControlsState.TrackedListeners.ContainsKey(callback))
                return;

            Action<S1InputDeviceType> wrapper = (device) =>
                callback((InputDeviceType)(int)device);

            ControlsState.TrackedListeners[callback] = wrapper;
            S1GameInput.OnInputDeviceChanged += wrapper;
        }

        /// <summary>
        /// Unregisters a callback from input device change events.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        public static void UnregisterDeviceChangedListener(Action<InputDeviceType> callback)
        {
            if (callback == null)
                return;

            if (!ControlsState.TrackedListeners.TryGetValue(callback, out Action<S1InputDeviceType>? wrapper))
                return;

            S1GameInput.OnInputDeviceChanged -= wrapper;
            ControlsState.TrackedListeners.Remove(callback);
        }

        /// <summary>
        /// Checks if the specified button is currently held down.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button is currently held down.</returns>
        public static bool GetButton(ButtonCode button) =>
            S1GameInput.GetButton((S1ButtonCode)(int)button);

        /// <summary>
        /// Checks if the specified button was pressed down this frame.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button was pressed this frame.</returns>
        public static bool GetButtonDown(ButtonCode button) =>
            S1GameInput.GetButtonDown((S1ButtonCode)(int)button);

        /// <summary>
        /// Checks if the specified button was released this frame.
        /// </summary>
        /// <param name="button">The button to check.</param>
        /// <returns>True if the button was released this frame.</returns>
        public static bool GetButtonUp(ButtonCode button) =>
            S1GameInput.GetButtonUp((S1ButtonCode)(int)button);

        /// <summary>
        /// Gets whether the active input device is keyboard and mouse.
        /// </summary>
        public static bool GetCurrentInputDeviceIsKeyboardMouse() =>
            S1GameInput.GetCurrentInputDeviceIsKeyboardMouse();

        /// <summary>
        /// Gets whether the active input device is a gamepad.
        /// </summary>
        public static bool GetCurrentInputDeviceIsGamepad() =>
            S1GameInput.GetCurrentInputDeviceIsGamepad();
    }
}
