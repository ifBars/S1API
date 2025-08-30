using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using S1API.Internal.Abstraction;
using S1API.Internal.Patches;
using System;
using MelonLoader;
#if IL2CPPMELON
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using S1GameInput = Il2CppScheduleOne.GameInput;
#elif MONOBEPINEX || IL2CPPBEPINEX
using ScheduleOne.UI.Phone;
using ScheduleOne;
using S1GameInput = ScheduleOne.GameInput;
#elif MONOMELON
using ScheduleOne.UI;
using ScheduleOne.DevUtilities;
using ScheduleOne.UI.Phone;
using ScheduleOne;
using MelonLoader.Utils;
using S1GameInput = ScheduleOne.GameInput;
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

        /// <summary>
        /// Indicates whether the application UI has been successfully created and initialized.
        /// </summary>
        /// <remarks>
        /// This variable is used internally to track the state of the application's UI.
        /// When set to true, it denotes that the app UI panel has been created and configured.
        /// </remarks>
        private bool _appCreated;

        /// <summary>
        /// Indicates whether the phone application icon has been modified.
        /// This flag prevents redundant modification of the icon once it has already
        /// been updated or created.
        /// </summary>
        private bool _iconModified;
        
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
        /// Gets the orientation of the phone app (Horizontal or Vertical).
        /// Determines how the phone is rotated when the app is opened.
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
            if (_appPanel != null)
            {
                Object.Destroy(_appPanel);
                _appPanel = null;
            }

            _appCreated = false;
            _iconModified = false;
            
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
        /// Handles exit/home button functionality. Called when user presses escape or home.
        /// </summary>
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
        /// This method locates the parent container in the UI hierarchy, clones a template panel if needed,
        /// clears its content, and then invokes the implementation-specific OnCreatedUI method
        /// for further customization of the UI panel.
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
                Transform templateApp = appsCanvas.transform.Find("ProductManagerApp");
                if (templateApp == null)
                {
                    Logger.Error("Template ProductManagerApp not found.");
                    return;
                }

                _appPanel = Object.Instantiate(templateApp.gameObject, appsCanvas.transform);
                _appPanel.name = AppName;

                Transform containerTransform = _appPanel.transform.Find("Container");
                if (containerTransform != null)
                {
                    GameObject container = containerTransform.gameObject;
                    ClearContainer(container);
                    OnCreatedUI(container);
                }

                _appCreated = true;
            }

            _appPanel.SetActive(true);
            
            // Add button handler component to detect physical button clicks
            if (_appPanel.GetComponent<PhoneAppButtonHandler>() == null)
            {
                var buttonHandler = _appPanel.AddComponent<PhoneAppButtonHandler>();
                buttonHandler.phoneApp = this;
            }
            
            // Subscribe to phone close apps event and register exit handler like native apps
            if (Phone.InstanceExists)
            {
                _closeAppAction = new System.Action(CloseApp);
                Phone.Instance.closeApps += _closeAppAction;
                
                // Create IL2CPP-safe delegate instance
#if IL2CPPMELON
                _exitDelegate = DelegateSupport.ConvertDelegate<S1GameInput.ExitDelegate>(new System.Action<ExitAction>(Exit));
#else
                _exitDelegate = new S1GameInput.ExitDelegate(Exit);
#endif
                GameInput.RegisterExitListener(_exitDelegate, 1);

                // Subscribe to phone closed to notify apps
                _onPhoneClosedAction = OnPhoneClosed;
                Phone.Instance.onPhoneClosed += _onPhoneClosedAction;
            }
        }

        /// <summary>
        /// Creates or modifies the application icon displayed on the in-game phone's home screen.
        /// This method clones an existing icon, updates its label, and changes its image based on the provided file name.
        /// </summary>
        internal void SpawnIcon(HomeScreen homeScreenInstance)
        {
            if (_iconModified)
                return;

            GameObject? appIcons = homeScreenInstance.transform.Find("AppIcons")?.gameObject;
            if (appIcons == null)
            {
                Logger.Error("AppIcons not found under HomeScreen.");
                return;
            }

            // Find the LAST icon (the one most recently added)
            Transform? lastIcon = appIcons.transform.childCount > 0 ? appIcons.transform.GetChild(appIcons.transform.childCount - 1) : null;
            if (lastIcon == null)
            {
                Logger.Error("No icons found in AppIcons.");
                return;
            }

            GameObject iconObj = lastIcon.gameObject;
            iconObj.name = AppName; // Rename it now

            // Update label
            Transform labelTransform = iconObj.transform.Find("Label");
            Text? label = labelTransform?.GetComponent<Text>();
            if (label != null)
                label.text = IconLabel;

            // Update image
            _iconModified = ChangeAppIconImage(iconObj, IconFileName);
            
            // Set up click handler for the icon
            Button? iconButton = iconObj.GetComponent<Button>();
            if (iconButton != null)
            {
                iconButton.onClick.RemoveAllListeners();
                EventHelper.AddListener(OpenApp, iconButton.onClick);
            }
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

                Logger.Msg($"Opened phone app: {AppName}");
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

                Logger.Msg($"Closed phone app: {AppName}");
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
                if (_appPanel != null)
                {
                    var containerTransform = _appPanel.transform.Find("Container");
                    var container = containerTransform != null ? containerTransform.gameObject : null;
                    if (container != null)
                        container.SetActive(true);
                }
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
                if (_appPanel != null)
                {
                    var containerTransform = _appPanel.transform.Find("Container");
                    var container = containerTransform != null ? containerTransform.gameObject : null;
                    if (container != null)
                        container.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Configures an existing app panel by clearing and rebuilding its UI elements if necessary.
        /// </summary>
        /// <param name="panel">The app panel to configure, represented as a GameObject.</param>
        private void SetupExistingAppPanel(GameObject panel)
        {
            Transform containerTransform = panel.transform.Find("Container");
            if (containerTransform != null)
            {
                GameObject container = containerTransform.gameObject;
                if (container.transform.childCount < 2)
                {
                    ClearContainer(container);
                    OnCreatedUI(container);
                }
            }

            _appCreated = true;
        }

        /// <summary>
        /// Removes all child objects from the specified container to clear its contents.
        /// </summary>
        /// <param name="container">The parent GameObject whose child objects will be destroyed.</param>
        private void ClearContainer(GameObject container)
        {
            for (int i = container.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(container.transform.GetChild(i).gameObject);

            container.SetActive(false);
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

#if MONOMELON || IL2CPPMELON
            string path = Path.Combine(MelonEnvironment.ModsDirectory, filename);
#elif MONOBEPINEX || IL2CPPBEPINEX
            string path = Path.Combine(BepInEx.Paths.PluginPath, filename);
#endif
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
        internal PhoneApp phoneApp;

        private void Update()
        {
            // Replicate the native App<T> Update logic for physical button detection
            if (phoneApp != null && phoneApp.IsOpen() && Phone.InstanceExists && Phone.Instance.IsOpen && IsHoveringButton() && GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick))
            {
                phoneApp.CloseApp();
            }
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