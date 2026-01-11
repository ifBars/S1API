using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using S1API.Internal.Abstraction;
using S1API.Internal.Patches;
using S1API.Logging;
using MelonLoader;

#if IL2CPPMELON
using Il2CppTMPro;
using Il2CppScheduleOne.DevUtilities;
using Il2CppInterop.Runtime;
using S1GameInput = Il2CppScheduleOne.GameInput;
using S1TVHomeScreen = Il2CppScheduleOne.TV.TVHomeScreen;
using S1TVApp = Il2CppScheduleOne.TV.TVApp;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using TMPro;
using ScheduleOne.DevUtilities;
using S1GameInput = ScheduleOne.GameInput;
using S1TVHomeScreen = ScheduleOne.TV.TVHomeScreen;
using S1TVApp = ScheduleOne.TV.TVApp;
#endif

namespace S1API.TVApp
{
    /// <summary>
    /// Abstract base class for creating custom TV applications.
    /// </summary>
    /// <remarks>
    /// This class provides an extensible framework for defining TV application behaviors, user interface elements,
    /// and registration mechanics for integration into the TV system. Modders should extend this class
    /// and implement the required abstract members.
    /// </remarks>
    public abstract class TVApp : Registerable
    {
        /// <summary>
        /// Logger instance used for logging messages, warnings, or errors
        /// related to the functionality of TV applications.
        /// </summary>
        protected static readonly Log Logger = new Log("TVApp");

        #region Private Members

        /// <summary>
        /// Represents the root GameObject containing the app's Canvas.
        /// </summary>
        private GameObject? _appRoot;

        /// <summary>
        /// The Canvas component for this TV app.
        /// </summary>
        private Canvas? _canvas;

        /// <summary>
        /// The CanvasGroup component for alpha/interactivity control.
        /// </summary>
        private CanvasGroup? _canvasGroup;

        /// <summary>
        /// The container GameObject where user UI elements are placed.
        /// </summary>
        private GameObject? _container;

        /// <summary>
        /// Reference to the TV home screen for navigation.
        /// </summary>
        private S1TVHomeScreen? _homeScreen;

        /// <summary>
        /// Cached exit delegate for GameInput registration (IL2CPP compatibility).
        /// </summary>
        private S1GameInput.ExitDelegate? _exitDelegate;

        /// <summary>
        /// Indicates whether the app is currently open.
        /// </summary>
        private bool _isOpen;

        /// <summary>
        /// Indicates whether the app is currently paused.
        /// </summary>
        private bool _isPaused;

        /// <summary>
        /// Indicates whether the UI has been created.
        /// </summary>
        private bool _uiCreated;

        #endregion

        #region Abstract Properties

        /// <summary>
        /// Gets the unique identifier for this TV app.
        /// </summary>
        /// <remarks>
        /// This property is used as a key to identify the application within the TV system.
        /// It must be unique among all registered TV apps.
        /// </remarks>
        protected abstract string AppName { get; }

        /// <summary>
        /// Gets the display title shown on the TV interface.
        /// </summary>
        /// <remarks>
        /// This is the human-readable name displayed to the user on the app button.
        /// </remarks>
        protected abstract string AppTitle { get; }

        /// <summary>
        /// Gets the icon sprite displayed on the TV app button.
        /// </summary>
        /// <remarks>
        /// This sprite is shown in the TV home screen's app list.
        /// </remarks>
        protected abstract Sprite Icon { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Determines if this TV app is currently open.
        /// </summary>
        public bool IsOpen =>
            _isOpen;

        /// <summary>
        /// Determines if this TV app is currently paused.
        /// </summary>
        public bool IsPaused =>
            _isPaused;

        #endregion

        #region Lifecycle Hooks

        /// <summary>
        /// Called when the app UI container is created. Override to build your UI.
        /// </summary>
        /// <param name="container">The GameObject container for UI elements.</param>
        protected abstract void OnCreatedUI(GameObject container);

        /// <summary>
        /// Called every frame while the app is open and not paused. Override for game logic updates.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Called when the app is opened.
        /// </summary>
        protected virtual void OnOpened() { }

        /// <summary>
        /// Called when the app is closed.
        /// </summary>
        protected virtual void OnClosed() { }

        /// <summary>
        /// Called when the app is paused.
        /// </summary>
        protected virtual void OnPaused() { }

        /// <summary>
        /// Called when the app is resumed from pause.
        /// </summary>
        protected virtual void OnResumed() { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens this TV application.
        /// </summary>
        public void Open()
        {
            if (_isOpen)
                return;

            try
            {
                _isOpen = true;
                _isPaused = false;

                // Show our canvas
                if (_appRoot != null)
                    _appRoot.SetActive(true);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = 1f;

                if (_container != null)
                    _container.SetActive(true);

                OnOpened();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to open TV app {AppName}: {e.Message}");
            }
        }

        /// <summary>
        /// Closes this TV application and returns to the TV home screen.
        /// </summary>
        public void Close()
        {
            if (!_isOpen)
                return;

            try
            {
                _isOpen = false;
                _isPaused = false;

                // Hide our canvas
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0f;

                if (_container != null)
                    _container.SetActive(false);

                if (_appRoot != null)
                    _appRoot.SetActive(false);

                OnClosed();

                // Return to home screen
                _homeScreen?.Open();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to close TV app {AppName}: {e.Message}");
            }
        }

        /// <summary>
        /// Forces the app to close without navigating to home screen.
        /// Used internally when home screen opens to clean up any orphaned open apps.
        /// </summary>
        internal void ForceClose()
        {
            if (!_isOpen)
                return;

            try
            {
                _isOpen = false;
                _isPaused = false;

                // Hide our canvas
                if (_canvasGroup != null)
                    _canvasGroup.alpha = 0f;

                if (_container != null)
                    _container.SetActive(false);

                if (_appRoot != null)
                    _appRoot.SetActive(false);

                OnClosed();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to force close TV app {AppName}: {e.Message}");
            }
        }

        /// <summary>
        /// Pauses the TV application.
        /// </summary>
        public void Pause()
        {
            if (!_isOpen || _isPaused)
                return;

            _isPaused = true;
            OnPaused();
        }

        /// <summary>
        /// Resumes the TV application from pause.
        /// </summary>
        public void Resume()
        {
            if (!_isOpen || !_isPaused)
                return;

            _isPaused = false;
            OnResumed();
        }

        #endregion

        #region Registration

        /// <summary>
        /// Called when the TVApp instance is created.
        /// Registers the app with the TVAppRegistry.
        /// </summary>
        protected override void OnCreated() =>
            TVAppRegistry.Register(this);

        /// <summary>
        /// Cleans up resources when the app is destroyed.
        /// </summary>
        protected override void OnDestroyed()
        {
            // Unregister exit listener
            if (_exitDelegate != null)
            {
                S1GameInput.DeregisterExitListener(_exitDelegate);
                _exitDelegate = null;
            }

            // Destroy UI
            if (_appRoot != null)
            {
                Object.Destroy(_appRoot);
                _appRoot = null;
            }

            _canvas = null;
            _canvasGroup = null;
            _container = null;
            _homeScreen = null;
            _uiCreated = false;
            _isOpen = false;
            _isPaused = false;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates the UI structure for this TV app.
        /// Called by the Harmony patch during S1TVHomeScreen.Awake.
        /// </summary>
        /// <param name="homeScreen">The TV home screen instance.</param>
        internal void SpawnUI(S1TVHomeScreen homeScreen)
        {
            if (_uiCreated)
                return;

            _homeScreen = homeScreen;

            // Get the parent transform (same level as other TV apps)
            Transform? parentTransform = homeScreen.transform.parent;
            if (parentTransform == null)
            {
                Logger.Error($"Cannot find parent transform for TV app: {AppName}");
                return;
            }

            // Create app root GameObject
            _appRoot = new GameObject($"{AppName}_TVApp");
            _appRoot.transform.SetParent(parentTransform, false);

            // Copy transform from an existing TVApp's canvas for proper positioning
            S1TVApp[]? existingApps = homeScreen.Apps;
            if (existingApps != null && existingApps.Length > 0 && existingApps[0].Canvas != null)
            {
                Transform sourceTransform = existingApps[0].Canvas.transform;
                _appRoot.transform.localPosition = sourceTransform.localPosition;
                _appRoot.transform.localRotation = sourceTransform.localRotation;
                _appRoot.transform.localScale = sourceTransform.localScale;
            }

            // Add Canvas component
            _canvas = _appRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            // Copy canvas settings from existing app
            if (existingApps != null && existingApps.Length > 0 && existingApps[0].Canvas != null)
            {
                Canvas sourceCanvas = existingApps[0].Canvas;
                _canvas.sortingOrder = sourceCanvas.sortingOrder;
                _canvas.worldCamera = sourceCanvas.worldCamera;

                // Copy layer from source canvas
                _appRoot.layer = existingApps[0].Canvas.gameObject.layer;

                // Copy RectTransform size
                RectTransform sourceRT = sourceCanvas.GetComponent<RectTransform>();
                RectTransform rt = _appRoot.GetComponent<RectTransform>();
                if (sourceRT != null && rt != null)
                {
                    rt.sizeDelta = sourceRT.sizeDelta;
                }
            }

            // Add CanvasGroup for alpha/interactivity
            _canvasGroup = _appRoot.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            // Add GraphicRaycaster for input
            _appRoot.AddComponent<GraphicRaycaster>();

            // Create container for user UI
            _container = new GameObject("Container");
            _container.transform.SetParent(_appRoot.transform, false);

            RectTransform containerRT = _container.AddComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;

            // Let the modder build their UI
            OnCreatedUI(_container);

            // Add update handler for frame updates
            // Note: Set field directly instead of using a method (IL2CPP limitation)
            var updateHandler = _appRoot.AddComponent<TVAppUpdateHandler>();
            updateHandler.tvApp = this;

            // Register exit listener at priority 3 (same as game's built-in TV apps)
#if IL2CPPMELON
            _exitDelegate = DelegateSupport.ConvertDelegate<S1GameInput.ExitDelegate>(new Action<ExitAction>(HandleExit));
#else
            _exitDelegate = new S1GameInput.ExitDelegate(HandleExit);
#endif
            S1GameInput.RegisterExitListener(_exitDelegate, 3);

            // Start hidden
            _appRoot.SetActive(false);
            _uiCreated = true;
        }

        /// <summary>
        /// Creates an app button in the TV home screen.
        /// Called by the Harmony patch during S1TVHomeScreen.Awake.
        /// </summary>
        /// <param name="homeScreen">The TV home screen instance.</param>
        internal void SpawnButton(S1TVHomeScreen homeScreen)
        {
            if (homeScreen.AppButtonPrefab == null || homeScreen.AppButtonContainer == null)
            {
                Logger.Error($"Cannot create button for TV app {AppName}: missing prefab or container");
                return;
            }

            // Instantiate button from prefab
            GameObject buttonObj = Object.Instantiate(homeScreen.AppButtonPrefab, homeScreen.AppButtonContainer);
            buttonObj.name = $"Button_{AppName}";

            // Set icon
            Transform? iconTransform = buttonObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image? iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && Icon != null)
                    iconImage.sprite = Icon;
            }

            // Set name
            Transform? nameTransform = buttonObj.transform.Find("Name");
            if (nameTransform != null)
            {
                TextMeshProUGUI? nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = AppTitle;
            }

            // Set up click handler
            Button? button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                EventHelper.AddListener(OnButtonClicked, button.onClick);
            }
        }

        /// <summary>
        /// Invokes the OnUpdate method. Called by TVAppUpdateHandler.
        /// </summary>
        internal void InvokeUpdate()
        {
            if (_isOpen && !_isPaused)
                OnUpdate();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the app button is clicked.
        /// </summary>
        private void OnButtonClicked()
        {
            if (_homeScreen == null)
                return;

            try
            {
                // Mimic the game's AppSelected() pattern:
                // 1. Signal the Harmony patch to set skipExit = true
                // 2. Call Close() on the home screen
                // 3. Open our app

                // Set the flag so the Harmony patch will set skipExit = true
                TVHomeScreen_Close_Patch.SkipInterfaceClose = true;

                // Close the home screen (the patch will prevent Interface.Close())
                _homeScreen.Close();

                // Open this app
                Open();
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to open TV app {AppName}: {e.Message}");
            }
        }

        /// <summary>
        /// Handles exit/escape key press.
        /// </summary>
        private void HandleExit(ExitAction exit)
        {
            if (exit.Used || !_isOpen)
                return;

            exit.Used = true;
            Close();
        }

        #endregion
    }

    /// <summary>
    /// MonoBehaviour component that handles Update() calls for TV apps.
    /// </summary>
    /// <remarks>
    /// Note: The tvApp field is set directly rather than through a method because
    /// IL2CPP-registered types cannot have methods with custom managed type parameters.
    /// </remarks>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal class TVAppUpdateHandler : MonoBehaviour
    {
        /// <summary>
        /// Reference to the TV app. Set directly after AddComponent due to IL2CPP limitations.
        /// </summary>
        internal TVApp? tvApp;

        private void Update()
        {
            if (tvApp != null)
                tvApp.InvokeUpdate();
        }
    }
}
