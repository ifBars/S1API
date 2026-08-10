using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MelonLoader;
using S1API.Internal.Abstraction;
using S1API.Internal.Patches;
using S1API.Internal.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if IL2CPPMELON
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using S1GameInput = Il2CppScheduleOne.GameInput;
using S1ExitAction = Il2CppScheduleOne.ExitAction;
#elif MONOMELON
using ScheduleOne.UI;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone;
using ScheduleOne;
using MelonLoader.Utils;
using S1GameInput = ScheduleOne.GameInput;
using S1ExitAction = ScheduleOne.ExitAction;
#endif
namespace S1API.PhoneApp
{
    /// <summary>
    /// Abstract base class for creating custom applications to be used within an in-game phone system.
    /// </summary>
    /// <remarks>
    /// This class provides an extensible framework for defining application behaviors, user interface elements,
    /// and registration mechanics for integration into the phone's ecosystem.
    /// </remarks>
    public abstract class PhoneApp : Registerable
    {
        /// <summary>
        /// Logger instance used for logging messages, warnings, or errors
        /// related to the functionality of in-game phone applications.
        /// </summary>
        protected static readonly Logging.Log Logger = new Logging.Log("PhoneApp");

        /// <summary>
        /// Represents the panel associated with the phone app's UI.
        /// This is dynamically instantiated or retrieved when the app is initiated and serves as the container
        /// for the app's user interface elements within the phone system. The panel exists within the
        /// app canvas structure in the game's Unity hierarchy.
        /// </summary>
        private GameObject? _appPanel;
        private GameObject? _appContainer;
        private bool _isDestroying;

        internal bool IsDestroying => _isDestroying;

        /// <summary>
        /// Indicates whether the phone application icon has been modified.
        /// This flag prevents redundant modification of the icon once it has already
        /// been updated or created.
        /// </summary>
        private bool _iconModified;
        
        /// <summary>
        /// Cached reference to the icon <see cref="Image"/> component for quick sprite updates.
        /// </summary>
        private Image? _iconImage;
        
        /// <summary>
        /// If set before the icon exists, this sprite will be applied once the icon spawns.
        /// </summary>
        private Sprite? _pendingIconSprite;
        
        /// <summary>
        /// Reference to the home screen instance for managing app state transitions.
        /// </summary>
        private HomeScreen? _homeScreenInstance;
        
        /// <summary>
        /// Cached action delegate for closeApps event subscription (IL2CPP compatibility).
        /// </summary>
        private System.Action? _closeAppAction;
        
        /// <summary>
        /// Cached exit delegate for GameInput registration (IL2CPP compatibility).
        /// </summary>
        private S1GameInput.ExitDelegate? _exitDelegate;

        /// <summary>
        /// Cached action delegate for phone closed subscription (IL2CPP compatibility).
        /// </summary>
        private System.Action? _onPhoneClosedAction;

        /// <summary>
        /// Gets the unique identifier for the application within the phone system.
        /// </summary>
        /// <remarks>
        /// This property is used as a key to identify the application when creating UI elements or interacting with other components
        /// of the in-game phone system. It must be implemented in derived classes to provide a consistent and unique name for
        /// the application.
        /// </remarks>
        protected abstract string AppName { get; }

        /// <summary>
        /// Gets the display title of the application as it appears in the in-game phone system.
        /// </summary>
        /// <remarks>
        /// This property specifies the human-readable name of the application, different from the internal
        /// <c>AppName</c> that uniquely identifies the app within the system. It is displayed to the user
        /// on the application icon or within the application UI.
        /// </remarks>
        protected abstract string AppTitle { get; }

        /// <summary>
        /// Gets the label text displayed on the application's icon.
        /// </summary>
        /// <remarks>
        /// The <c>IconLabel</c> property is an abstract member that must be overridden by each implementation
        /// of the <see cref="PhoneApp"/> class. It specifies the label text shown directly below the application's
        /// icon on the in-game phone's home screen.
        /// This property is utilized when creating or modifying the app's icon, as part of the <c>SpawnIcon</c> method,
        /// to ensure that the label represents the application's name or a relevant description. The value should
        /// be concise and contextually meaningful to the user.
        /// </remarks>
        /// <value>
        /// A string representing the label text displayed under the app icon, which explains or identifies
        /// the app to the user.
        /// </value>
        protected abstract string IconLabel { get; }

        /// <summary>
        /// Specifies the file name of the icon used to represent the phone application in the in-game phone system.
        /// </summary>
        /// <remarks>
        /// The value of this property is typically a string containing the file name of the icon asset,
        /// such as "icon-name.png". It is used to identify and load the appropriate icon for the application.
        /// </remarks>
        protected abstract string IconFileName { get; }

        /// <summary>
        /// Optional direct icon sprite. If provided, it takes precedence over <see cref="IconFileName"/>.
        /// </summary>
        protected virtual Sprite? IconSprite => null;

        /// <summary>
        /// Gets the orientation of the phone app (Horizontal or Vertical).
        /// Determines both the phone rotation and the initial layout of the app panel.
        /// </summary>
        protected virtual EOrientation Orientation => EOrientation.Horizontal;

        /// <summary>
        /// Represents the orientation settings for phone applications.
        /// </summary>
        public enum EOrientation
        {
            Horizontal = 0,
            Vertical = 1
        }

        /// <summary>
        /// Invoked to define the user interface layout when the application panel is created.
        /// The method is used to populate the provided container with custom UI elements specific to the application.
        /// </summary>
        /// <param name="container">The GameObject container where the application's UI elements will be added.</param>
        protected abstract void OnCreatedUI(GameObject container);

        /// <summary>
        /// Invoked when the PhoneApp instance is created.
        /// Responsible for registering the app with the PhoneAppRegistry,
        /// integrating it into the in-game phone system.
        /// </summary>
        protected override void OnCreated()
        {
            PhoneAppRegistry.Register(this);
        }

        /// <summary>
        /// Cleans up resources and resets state when the app is destroyed.
        /// This method ensures any associated UI elements and resources are properly disposed of and variables tracking the app state are reset.
        /// </summary>
        protected override void OnDestroyed()
        {
            if (_isDestroying)
                return;

            _isDestroying = true;

            if (_appPanel != null)
            {
                Object.Destroy(_appPanel);
                _appPanel = null;
            }

            _appContainer = null;

            _iconModified = false;
            _iconImage = null;
            _pendingIconSprite = null;
            
            // Unsubscribe from phone events if subscribed
            if (Phone.InstanceExists && _closeAppAction != null)
            {
                Phone.Instance.closeApps -= _closeAppAction;
                _closeAppAction = null;
            }
            
            // Unregister exit listener if registered
            if (_exitDelegate != null)
            {
                GameInput.DeregisterExitListener(_exitDelegate);
                _exitDelegate = null;
            }

            // Unsubscribe from phone closed event if subscribed
            if (Phone.InstanceExists && _onPhoneClosedAction != null)
            {
                Phone.Instance.onPhoneClosed -= _onPhoneClosedAction;
                _onPhoneClosedAction = null;
            }
        }

        /// <summary>
        /// Handles exit/home button functionality without exposing runtime-specific game types.
        /// Called when the user presses escape or home.
        /// </summary>
        /// <param name="exit">The cross-runtime exit request.</param>
        public virtual void Exit(ExitAction exit)
        {
            if (!exit.Used && IsOpen() && Phone.InstanceExists && Phone.Instance.IsOpen)
            {
                exit.Used = true;
                CloseApp();
            }
        }

        /// <summary>
        /// Called when the in-game phone is closed. Override in derived apps to reset state.
        /// </summary>
        protected virtual void OnPhoneClosed() { }

        /// <summary>
        /// Determines if this phone app is currently open.
        /// </summary>
        /// <returns>True if the app is open, false otherwise</returns>
        public bool IsOpen()
        {
            return _appPanel != null && _appPanel.activeInHierarchy && Phone.ActiveApp == _appPanel;
        }

        /// <summary>
        /// Generates and initializes the UI panel for the application within the in-game phone system.
        /// This method locates the parent container in the UI hierarchy, creates an independent app panel,
        /// and then invokes the implementation-specific OnCreatedUI method for further customization.
        /// </summary>
        internal void SpawnUI(HomeScreen homeScreenInstance)
        {
            _homeScreenInstance = homeScreenInstance;
            
            GameObject? appsCanvas = homeScreenInstance.transform.parent.Find("AppsCanvas")?.gameObject;
            if (appsCanvas == null)
            {
                Logger.Error("AppsCanvas not found.");
                return;
            }

            Transform existingApp = appsCanvas.transform.Find(AppName);
            if (existingApp != null)
            {
                _appPanel = existingApp.gameObject;
                SetupExistingAppPanel(_appPanel);
            }
            else
            {
                _appPanel = CreateAppPanel(AppName, appsCanvas.transform);
                _appContainer = CreateAppContainer(_appPanel.transform);
            }

            _appPanel.SetActive(true);
            
            // Add button handler component to detect physical button clicks
            var buttonHandler = _appPanel.GetComponent<PhoneAppButtonHandler>() ?? _appPanel.AddComponent<PhoneAppButtonHandler>();
            buttonHandler.phoneApp = this;
            
            // Subscribe to phone close apps event and register exit handler like native apps
            if (Phone.InstanceExists)
            {
                _closeAppAction = new System.Action(CloseApp);
                Phone.Instance.closeApps += _closeAppAction;
                
                // Create IL2CPP-safe delegate instance
#if IL2CPPMELON
                _exitDelegate = DelegateSupport.ConvertDelegate<S1GameInput.ExitDelegate>(new System.Action<S1ExitAction>(HandleNativeExit));
#else
                _exitDelegate = new S1GameInput.ExitDelegate(HandleNativeExit);
#endif
                GameInput.RegisterExitListener(_exitDelegate, 1);

                // Subscribe to phone closed to notify apps
                _onPhoneClosedAction = OnPhoneClosed;
                Phone.Instance.onPhoneClosed += _onPhoneClosedAction;
            }
        }

        private void HandleNativeExit(S1ExitAction exit)
        {
            Exit(new ExitAction(
                () => exit.Used,
                used => exit.Used = used));
        }

        /// <summary>
        /// Creates or modifies the application icon displayed on the in-game phone's home screen.
        /// This method clones an existing icon, updates its label, and changes its image based on the provided file name.
        /// </summary>
        internal void SpawnIcon(HomeScreen homeScreenInstance)
        {
            if (_iconModified)
                return;

            // Use FindDescendant so we get the real AppIcons (under Viewport when scroll patch is active), not the stub.
            GameObject? appIcons = TransformUtils.FindDescendant(homeScreenInstance.transform, "AppIcons")?.gameObject;
            if (appIcons == null)
            {
                Logger.Error("AppIcons not found under HomeScreen.");
                return;
            }

            GameObject? iconObj = CreateAppIcon(homeScreenInstance, appIcons.transform);
            if (iconObj == null)
            {
                Logger.Error($"Failed to create an icon for {AppName}.");
                return;
            }

            iconObj.name = AppName;
            
            // Cache icon image for future updates
            Transform imageTransform = iconObj.transform.Find("Mask/Image");
            _iconImage = imageTransform != null ? imageTransform.GetComponent<Image>() : null;

            // Update label
            Transform labelTransform = iconObj.transform.Find("Label");
            Text? label = labelTransform?.GetComponent<Text>();
            if (label != null)
                label.text = IconLabel;

            // Update image (prefer provided sprite or pending sprite over file path)
            if (_iconImage != null)
            {
                Sprite? chosen = _pendingIconSprite != null ? _pendingIconSprite : IconSprite;
                if (chosen != null)
                {
                    _iconImage.sprite = chosen;
                    _iconModified = true;
                    _pendingIconSprite = null; // consumed
                }
                else
                {
                    _iconModified = ChangeAppIconImage(iconObj, IconFileName);
                }
            }
            else
            {
                _iconModified = ChangeAppIconImage(iconObj, IconFileName);
            }
            
            // Set up click handler for the icon
            Button? iconButton = iconObj.GetComponent<Button>();
            if (iconButton != null)
            {
                iconButton.onClick.RemoveAllListeners();
                global::S1API.Utils.EventHelper.AddListener(OpenApp, iconButton.onClick);
            }
        }

        /// <summary>
        /// Creates and registers an independent home-screen icon using the native icon prefab.
        /// </summary>
        private static GameObject? CreateAppIcon(HomeScreen homeScreenInstance, Transform parent)
        {
#if IL2CPPMELON
            GameObject? iconPrefab = homeScreenInstance.appIconPrefab;
#else
            GameObject? iconPrefab = AccessTools.Field(
                typeof(HomeScreen),
                "appIconPrefab")?.GetValue(homeScreenInstance) as GameObject;
#endif
            if (iconPrefab == null)
            {
                Logger.Error("HomeScreen appIconPrefab was unavailable.");
                return null;
            }

            GameObject iconObject = Object.Instantiate(iconPrefab, parent);
            iconObject.transform.Find("Notifications")?.gameObject.SetActive(false);

            Button? button = iconObject.GetComponent<Button>();
            UISelectable? selectable = iconObject.GetComponent<UISelectable>();
            if (button == null || selectable == null)
            {
                Logger.Error("Native phone app icon prefab is missing Button or UISelectable.");
                Object.Destroy(iconObject);
                return null;
            }

#if IL2CPPMELON
            var nativeButtons = homeScreenInstance.appIcons;
            var uiPanel = homeScreenInstance.uiPanel;
#else
            var appIconsField = AccessTools.Field(typeof(HomeScreen), "appIcons");
            var uiPanelField = AccessTools.Field(typeof(HomeScreen), "uiPanel");
            var nativeButtons = appIconsField?.GetValue(homeScreenInstance) as List<Button>;
            var uiPanel = uiPanelField?.GetValue(homeScreenInstance) as UIPanel;
#endif
            if (nativeButtons == null || uiPanel == null)
            {
                Logger.Error("HomeScreen icon registration fields were unavailable.");
                Object.Destroy(iconObject);
                return null;
            }

            nativeButtons.Add(button);
            uiPanel.AddSelectable(selectable);
            return iconObject;
        }

        /// <summary>
        /// Opens this phone application, managing proper app state transitions.
        /// </summary>
        public void OpenApp()
        {
            try
            {
                // Close any currently active app first (following native app pattern)
                if (Phone.ActiveApp != null && Phone.ActiveApp != _appPanel)
                {
                    Phone.Instance.RequestCloseApp();
                }

                // Set app state to open using the same pattern as native apps
                SetAppOpen(true);

                Logger.Debug($"Opened phone app: {AppName}");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to open phone app {AppName}: {e.Message}");
            }
        }

        /// <summary>
        /// Closes this phone application, cleaning up its state.
        /// </summary>
        public void CloseApp()
        {
            try
            {
                // Set app state to closed using the same pattern as native apps
                if (IsOpen())
                {
                    SetAppOpen(false);
                }

                Logger.Debug($"Closed phone app: {AppName}");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to close phone app {AppName}: {e.Message}");
            }
        }

        /// <summary>
        /// Sets the open state of the application following the same pattern as native App class.
        /// This properly handles orientation, HomeScreen/AppsCanvas visibility, and ActiveApp tracking.
        /// </summary>
        /// <param name="open">Whether to open or close the app</param>
        private void SetAppOpen(bool open)
        {
            if (open && Phone.ActiveApp != null && Phone.ActiveApp != _appPanel)
            {
                Logger.Warning($"{Phone.ActiveApp.name} is already open");
                return;
            }

            // Use singleton instances like native apps do
            if (AppsCanvas.InstanceExists)
                AppsCanvas.Instance.SetIsOpen(open);
                
            if (HomeScreen.InstanceExists)
                HomeScreen.Instance.SetIsOpen(!open);

            if (open)
            {
                // Handle orientation and camera offset like native apps
                if (Orientation == EOrientation.Horizontal)
                {
                    if (Phone.InstanceExists)
                    {
                        Phone.Instance.SetIsHorizontal(true);
                        Phone.Instance.SetLookOffsetMultiplier(0.6f);
                    }
                }
                else
                {
                    if (Phone.InstanceExists)
                    {
                        Phone.Instance.SetLookOffsetMultiplier(1f);
                    }
                }

                // Set as active app and activate panel
                Phone.ActiveApp = _appPanel;
                _appContainer?.SetActive(true);
            }
            else
            {
                // Clear active app if it was this app
                if (Phone.ActiveApp == _appPanel)
                {
                    Phone.ActiveApp = null;
                }

                // Reset orientation and camera offset
                if (Phone.InstanceExists)
                {
                    Phone.Instance.SetIsHorizontal(false);
                    Phone.Instance.SetLookOffsetMultiplier(1f);
                }

                // Deactivate container
                _appContainer?.SetActive(false);
            }
        }

        /// <summary>
        /// Configures an existing app panel by replacing its content with one S1API-owned container.
        /// </summary>
        /// <param name="panel">The app panel to configure, represented as a GameObject.</param>
        private void SetupExistingAppPanel(GameObject panel)
        {
            panel.SetActive(false);

            for (int i = panel.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = panel.transform.GetChild(i);
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Object.Destroy(child.gameObject);
            }

            ConfigureAppPanel(panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>());
            _appContainer = CreateAppContainer(panel.transform);
        }

        /// <summary>
        /// Creates an app panel whose layout matches the configured phone orientation.
        /// </summary>
        private GameObject CreateAppPanel(string name, Transform parent)
        {
            GameObject panel = CreateFullStretchObject(name, parent);
            ConfigureAppPanel(panel.GetComponent<RectTransform>());
            return panel;
        }

        /// <summary>
        /// Applies the native phone app layout for the configured orientation.
        /// </summary>
        private void ConfigureAppPanel(RectTransform rectTransform)
        {
            if (Orientation == EOrientation.Horizontal)
            {
                ConfigureFullStretch(rectTransform);
                return;
            }

            RectTransform? parentRectTransform = rectTransform.parent?.GetComponent<RectTransform>();
            if (parentRectTransform == null)
            {
                Logger.Warning($"Cannot configure vertical layout for {AppName}: parent is not a RectTransform.");
                ConfigureFullStretch(rectTransform);
                return;
            }

            Vector2 center = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = center;
            rectTransform.anchorMax = center;
            rectTransform.pivot = center;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(parentRectTransform.rect.height, parentRectTransform.rect.width);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// Creates the full-screen container exposed to custom phone apps.
        /// </summary>
        private GameObject CreateAppContainer(Transform parent)
        {
            GameObject container = CreateFullStretchObject("Container", parent);
            OnCreatedUI(container);
            container.SetActive(false);
            return container;
        }

        /// <summary>
        /// Creates a UI object that fills its parent RectTransform.
        /// </summary>
        private static GameObject CreateFullStretchObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            ConfigureFullStretch(rectTransform);
            gameObject.layer = parent.gameObject.layer;
            return gameObject;
        }

        private static void ConfigureFullStretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// Changes the image of the app icon based on the specified filename, and applies the new icon to the given GameObject.
        /// </summary>
        /// <param name="iconObj">The GameObject representing the app icon that will have its image changed.</param>
        /// <param name="filename">The name of the file containing the new icon image to be loaded.</param>
        /// <returns>
        /// A boolean value indicating whether the operation was successful.
        /// Returns true if the image was successfully loaded and applied; otherwise, returns false.
        /// </returns>
        private bool ChangeAppIconImage(GameObject iconObj, string filename)
        {
            Transform imageTransform = iconObj.transform.Find("Mask/Image");
            Image? image = imageTransform?.GetComponent<Image>();
            if (image == null)
            {
                Logger.Error("Image component not found in icon.");
                return false;
            }

            string path = Path.Combine(MelonEnvironment.ModsDirectory, filename);
            if (!File.Exists(path))
            {
                Logger.Error("Icon file not found: " + path);
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    return true;
                }
                Object.Destroy(tex);
            }
            catch (System.Exception e)
            {
                Logger.Error("Failed to load image: " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// Sets the app icon directly from a sprite, bypassing file loading.
        /// If the icon is not yet spawned, stores the sprite to apply later.
        /// </summary>
        /// <param name="sprite">Sprite to use for the app icon.</param>
        /// <returns>True if applied immediately or stored for later application.</returns>
        public bool SetIconSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            if (_iconImage != null)
            {
                _iconImage.sprite = sprite;
                _iconModified = true;
                _pendingIconSprite = null;
                return true;
            }

            // Icon not spawned yet; remember desired sprite for when it appears
            _pendingIconSprite = sprite;
            return true;
        }

        /// <summary>
        /// Sets the app icon directly from a texture by creating a sprite.
        /// </summary>
        /// <param name="texture">Texture to convert into a sprite for the app icon.</param>
        /// <returns>True if applied or stored; false if texture is null.</returns>
        public bool SetIconTexture(Texture2D texture)
        {
            if (texture == null)
                return false;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return SetIconSprite(sprite);
        }
    }

    /// <summary>
    /// MonoBehaviour component that handles physical button clicks for S1API phone apps.
    /// This replicates the Update() logic from native App class to detect BoxCollider button clicks.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal class PhoneAppButtonHandler : MonoBehaviour
    {
        internal PhoneApp? phoneApp;

        private void Update()
        {
            // Replicate the native App<T> Update logic for physical button detection
            if (phoneApp != null && phoneApp.IsOpen() && Phone.InstanceExists && Phone.Instance.IsOpen && IsHoveringButton() && GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            {
                phoneApp.CloseApp();
            }
        }

        private void OnDestroy()
        {
            // Destroy phone app when button handler is destroyed
            if (phoneApp != null && !phoneApp.IsDestroying)
                phoneApp.DestroyInternal();
        }

        private bool IsHoveringButton()
        {
            // This is the same logic as native App<T>.IsHoveringButton()
            if (Physics.Raycast(Singleton<GameplayMenu>.Instance.OverlayCamera.ScreenPointToRay(UnityEngine.Input.mousePosition), out var hitInfo, 2f, 1 << LayerMask.NameToLayer("Overlay")) && hitInfo.collider.gameObject.name == "Button")
            {
                return true;
            }
            return false;
        }
    }
}
