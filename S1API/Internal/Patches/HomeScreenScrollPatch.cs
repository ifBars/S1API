using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using S1API.Internal.Phone;
using S1API.Logging;

#if (IL2CPPMELON)
using S1Phone = Il2CppScheduleOne.UI.Phone;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Phone = ScheduleOne.UI.Phone;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Adds scroll functionality to the phone home screen app icons
    /// to prevent overflow when many mods add phone apps.
    /// Wraps the AppIcons container in a ScrollRect with a visual scrollbar.
    /// </summary>
    [HarmonyPatch(typeof(S1Phone.HomeScreen), "Start")]
    internal static class HomeScreenScrollPatch
    {
        private static readonly Log Logger = new Log("HomeScreenScroll");
        private static bool _isInitialized = false;

        /// <summary>
        /// Sets up scrolling for the AppIcons container after HomeScreen initializes.
        /// </summary>
        static void Postfix(S1Phone.HomeScreen __instance)
        {
            if (_isInitialized || __instance == null)
                return;

            try
            {
                SetupScrollableGrid(__instance);
                _isInitialized = true;
            }
            catch (System.Exception ex)
            {
                // Setup failed; leave scroll disabled
            }
        }

        internal static void ResetInitializationState()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Wraps the AppIcons GameObject in a ScrollRect structure with a visual scrollbar.
        /// </summary>
        private static void SetupScrollableGrid(S1Phone.HomeScreen homeScreen)
        {
            // Find AppIcons container
            var appIconsTransform = homeScreen.transform.Find("AppIcons");
            if (appIconsTransform == null)
                return;

            var appIcons = appIconsTransform.gameObject;

            // Skip if already set up (parent should be Viewport after setup)
            var currentParent = appIconsTransform.parent;
            if (currentParent != null && currentParent.name == "Viewport")
                return;

            var originalParent = appIconsTransform.parent;
            var appIconsRect = appIcons.GetComponent<RectTransform>();
            var gridLayout = appIcons.GetComponent<GridLayoutGroup>();

            if (gridLayout == null)
                return;

            // Create ScrollView container
            var scrollView = new GameObject("AppIconsScrollView");
            scrollView.transform.SetParent(originalParent, false);
            scrollView.transform.SetSiblingIndex(appIconsTransform.GetSiblingIndex());
            scrollView.AddComponent<RectTransform>();

            var scrollViewRect = scrollView.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollViewRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
            // Size to show exactly 4 rows (4 * 190 + 3 * 60 = 940 height, plus margins = ~1000)
            // Width: 3 columns * 190 + 2 * 15 = 600
            scrollViewRect.sizeDelta = new Vector2(620f, 1000f);

            // Create Viewport with Mask
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            viewport.AddComponent<RectTransform>();

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-20f, 0f); // Leave room for scrollbar

            // Add mask components
            var maskImage = viewport.AddComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.AddComponent<Mask>();

            // Move AppIcons into Viewport
            appIconsTransform.SetParent(viewport.transform, false);

            // Configure AppIcons for vertical scroll (anchor at top)
            appIconsRect.anchorMin = new Vector2(0f, 1f);
            appIconsRect.anchorMax = new Vector2(1f, 1f);
            appIconsRect.pivot = new Vector2(0.5f, 1f);
            appIconsRect.anchoredPosition = Vector2.zero;
            appIconsRect.sizeDelta = Vector2.zero;

            // Add or configure ContentSizeFitter
            var contentFitter = appIcons.GetComponent<ContentSizeFitter>();
            if (contentFitter == null)
                contentFitter = appIcons.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Ensure GridLayoutGroup has proper settings
            gridLayout.cellSize = new Vector2(190f, 190f);
            gridLayout.spacing = new Vector2(15f, 60f);
            // Extra bottom padding avoids clipping of the final row in some layouts.
            gridLayout.padding = new RectOffset(15, 15, 20, 40);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            // Create visual scrollbar
            var scrollbar = CreateScrollbar(scrollView.transform);

            // Add ScrollRect
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.content = appIconsRect;
            scrollRect.viewport = viewportRect;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.08f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.06f;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.verticalNormalizedPosition = 1f;

            // Compatibility stub: path-based Find("HomeScreen/AppIcons") expects AppIcons as a direct child of HomeScreen.
            // The real AppIcons is under Viewport. The stub stays as a direct child so Find returns it.
            // AppIconsRedirect keeps the stub populated for mods that use GetChild() (e.g. CustomPhone).
            var stub = new GameObject("AppIcons");
            stub.transform.SetParent(originalParent, false);
            stub.transform.SetSiblingIndex(scrollView.transform.GetSiblingIndex() + 1);
            stub.AddComponent<RectTransform>();
            ConfigureHiddenStub(stub.GetComponent<RectTransform>());

            AppIconsRedirect redirect = stub.AddComponent<AppIconsRedirect>();
            redirect._realAppIcons = appIconsTransform;
        }

        /// <summary>
        /// Creates a visual scrollbar on the right side of the scroll view.
        /// </summary>
        private static Scrollbar CreateScrollbar(Transform parent)
        {
            // Create scrollbar container
            var scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(parent, false);
            scrollbarObj.AddComponent<RectTransform>();
            scrollbarObj.AddComponent<Image>();
            scrollbarObj.AddComponent<Scrollbar>(); // Unity built-in, unlikely to fail

            var scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.sizeDelta = new Vector2(12f, 0f);
            scrollbarRect.anchoredPosition = new Vector2(0f, 0f);

            // Scrollbar background
            var bgImage = scrollbarObj.GetComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
            bgImage.raycastTarget = true;

            var scrollbar = scrollbarObj.GetComponent<Scrollbar>();
            // Match standard ScrollRect semantics: dragging/scrolling down moves the handle down.
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Create handle
            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObj.transform, false);
            slidingArea.AddComponent<RectTransform>();

            var slidingAreaRect = slidingArea.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.sizeDelta = Vector2.zero;
            slidingAreaRect.anchoredPosition = Vector2.zero;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);
            handle.AddComponent<RectTransform>();
            handle.AddComponent<Image>();

            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;
            handleRect.pivot = new Vector2(0.5f, 0.5f);

            // Handle styling - semi-transparent white
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
            handleImage.raycastTarget = true;

            // Set handle in scrollbar
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.size = 0.3f; // Initial size, auto-adjusts based on content

            return scrollbar;
        }

        /// <summary>
        /// Keeps the compatibility stub available for Find/GetChild while making it non-visual.
        /// </summary>
        private static void ConfigureHiddenStub(RectTransform stubRect)
        {
            if (stubRect == null)
                return;

            // Keep it in hierarchy for compatibility, but effectively invisible/non-interactive.
            stubRect.anchorMin = new Vector2(0f, 0f);
            stubRect.anchorMax = new Vector2(0f, 0f);
            stubRect.pivot = new Vector2(0f, 0f);
            stubRect.anchoredPosition = new Vector2(-10000f, -10000f);
            stubRect.sizeDelta = new Vector2(1f, 1f);

            var canvasGroup = stubRect.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = stubRect.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = false;
        }
    }
}
