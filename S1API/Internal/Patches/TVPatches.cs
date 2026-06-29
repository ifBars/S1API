using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using S1API.Internal.Utils;
using S1API.Internal.Abstraction;
using S1API.Logging;

#if IL2CPPMELON
using Il2CppScheduleOne.TV;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using ScheduleOne.TV;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Provides functionality for managing the registration of custom TV applications.
    /// </summary>
    internal static class TVAppRegistry
    {
        /// <summary>
        /// A static readonly list that stores instances of TV applications.
        /// </summary>
        public static readonly List<TVApp.TVApp> RegisteredApps = new List<TVApp.TVApp>();

        /// <summary>
        /// Registers a specified TV app into the registry.
        /// </summary>
        public static void Register(TVApp.TVApp app) =>
            RegisteredApps.Add(app);

        /// <summary>
        /// Clears all registered TV apps from the registry.
        /// </summary>
        public static void Clear() =>
            RegisteredApps.Clear();

        /// <summary>
        /// Closes all open TV apps.
        /// </summary>
        public static void CloseAllApps()
        {
            foreach (var app in RegisteredApps)
            {
                if (app.IsOpen)
                    app.ForceClose();
            }
        }
    }

    /// <summary>
    /// Patches TVHomeScreen.Awake to register and initialize custom TV apps.
    /// </summary>
    [HarmonyPatch(typeof(TVHomeScreen), "Awake")]
    internal static class TVHomeScreen_Awake_Patch
    {
        private static readonly Log Logger = new Log("TVApp");

        static void Postfix(TVHomeScreen __instance)
        {
            if (__instance == null)
                return;

            // Only register custom apps in the Main scene
            if (!string.Equals(SceneManager.GetActiveScene().name, "Main", StringComparison.OrdinalIgnoreCase))
                return;

            // Set up horizontal scrolling for the app button container (if not already set up)
            SetupScrollRect(__instance);

            // Clear existing registrations (scene reload handling)
            TVAppRegistry.Clear();

            // Discover and register all TVApp subclasses
            var tvAppTypes = ReflectionUtils.GetDerivedClasses<TVApp.TVApp>();
            foreach (var type in tvAppTypes)
            {
                if (type.IsAbstract)
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Logger.Warning($"[TVApp] Skipping {type.FullName}: no parameterless constructor");
                    continue;
                }

                try
                {
                    var instance = (TVApp.TVApp)Activator.CreateInstance(type)!;
                    ((IRegisterable)instance).CreateInternal();
                    instance.SpawnUI(__instance);
                    instance.SpawnButton(__instance);
                }
                catch (Exception e)
                {
                    Logger.Warning($"[TVApp] Failed to register {type.FullName}: {e.Message}");
                    Logger.Warning($"[TVApp] Stack trace: {e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Sets up a horizontal ScrollRect for the app button container to allow scrolling
        /// when there are more apps than can fit on screen.
        /// </summary>
        private static void SetupScrollRect(TVHomeScreen homeScreen)
        {
            if (homeScreen.AppButtonContainer == null)
            {
                Logger.Warning("[TVApp] Cannot setup scroll: AppButtonContainer is null");
                return;
            }

            // Check if scroll is already set up
            if (homeScreen.AppButtonContainer.GetComponentInParent<ScrollRect>() != null)
                return;

            try
            {
                RectTransform content = homeScreen.AppButtonContainer;
                Transform originalParent = content.parent;
                int siblingIndex = content.GetSiblingIndex();

                // Check existing components
                var existingLayout = content.GetComponent<HorizontalLayoutGroup>();
                var existingGrid = content.GetComponent<GridLayoutGroup>();

                // Store original values
                Vector2 originalAnchorMin = content.anchorMin;
                Vector2 originalAnchorMax = content.anchorMax;
                Vector2 originalAnchoredPos = content.anchoredPosition;
                Vector2 originalPivot = content.pivot;

                // Get the ACTUAL rendered size of content
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Rect contentRect = content.rect;

                // Get parent's rect to determine viewport size
                RectTransform parentRT = originalParent as RectTransform;
                Rect parentRect = parentRT != null ? parentRT.rect : new Rect(0, 0, 800, 200);

                // Create scroll view - stretch to fill parent horizontally, use content height
                GameObject scrollViewObj = new GameObject("AppScrollView");
                scrollViewObj.transform.SetParent(originalParent, false);
                scrollViewObj.transform.SetSiblingIndex(siblingIndex);

                RectTransform scrollViewRT = scrollViewObj.AddComponent<RectTransform>();
                // Stretch horizontally to fill parent, keep at same vertical position
                scrollViewRT.anchorMin = new Vector2(0f, originalAnchorMin.y);
                scrollViewRT.anchorMax = new Vector2(1f, originalAnchorMax.y);
                scrollViewRT.anchoredPosition = new Vector2(0f, originalAnchoredPos.y);
                scrollViewRT.sizeDelta = new Vector2(0f, contentRect.height > 0 ? contentRect.height : 200f);
                scrollViewRT.pivot = originalPivot;

                // Create viewport
                GameObject viewportObj = new GameObject("Viewport");
                viewportObj.transform.SetParent(scrollViewObj.transform, false);

                RectTransform viewportRT = viewportObj.AddComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.sizeDelta = Vector2.zero;
                viewportRT.anchoredPosition = Vector2.zero;
                viewportRT.pivot = originalPivot;

                // Reparent content into viewport
                content.SetParent(viewportObj.transform, false);

                // Position content at left edge of viewport, vertically centered
                content.anchorMin = new Vector2(0f, 0.5f);
                content.anchorMax = new Vector2(0f, 0.5f);
                content.pivot = new Vector2(0f, 0.5f);
                content.anchoredPosition = Vector2.zero; // Start at left edge

                // Configure layout group with padding for tidy appearance
                if (existingLayout != null)
                {
                    // Update existing layout group
                    existingLayout.padding = new RectOffset(80, 80, 0, 0); // 80px padding left and right
                }
                else if (existingGrid == null)
                {
                    // Add new layout group with padding
                    var layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    layoutGroup.childControlWidth = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.spacing = 15f;
                    layoutGroup.padding = new RectOffset(80, 80, 0, 0); // 80px padding left and right
                }

                // Add ContentSizeFitter - let it control both dimensions based on children
                ContentSizeFitter sizeFitter = content.gameObject.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                {
                    sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                }
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Configure ScrollRect
                ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
                scrollRect.content = content;
                scrollRect.viewport = viewportRT;
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
                scrollRect.movementType = ScrollRect.MovementType.Elastic;
                scrollRect.elasticity = 0.1f;
                scrollRect.inertia = true;
                scrollRect.decelerationRate = 0.135f;

                // Force layout rebuild
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewRT);

                // Log each button
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i);
                    var childRT = child.GetComponent<RectTransform>();
                }
            }
            catch (Exception e)
            {
                Logger.Error($"[TVApp] Failed to setup scroll rect: {e.Message}");
                Logger.Error($"[TVApp] Stack trace: {e.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Patches TVHomeScreen.Close to prevent interface closing when custom app is opening.
    /// </summary>
    [HarmonyPatch]
    internal static class TVHomeScreen_Close_Patch
    {
        /// <summary>
        /// Flag indicating that a custom TV app is about to open.
        /// </summary>
        internal static bool SkipInterfaceClose { get; set; }

        static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(TVHomeScreen))
                .Find(method => method.Name == "Close" || method.Name == "OnClose");
        }

        static void Prefix(TVHomeScreen __instance)
        {
            if (!SkipInterfaceClose)
                return;

            // Set skipExit via reflection. If this fails on IL2CPP,
            // TVInterface_Close_Patch will catch and skip Interface.Close()
            var field = typeof(TVHomeScreen).GetField("skipExit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(__instance, true);
                SkipInterfaceClose = false;
            }
        }
    }

    /// <summary>
    /// Patches TVHomeScreen.Open to close any open custom TV apps.
    /// </summary>
    [HarmonyPatch(typeof(TVHomeScreen), "Open")]
    internal static class TVHomeScreen_Open_Patch
    {
        static void Postfix() =>
            TVAppRegistry.CloseAllApps();
    }

    /// <summary>
    /// Patches TVInterface.Close as fallback for IL2CPP where reflection may fail.
    /// </summary>
    [HarmonyPatch(typeof(TVInterface), "Close")]
    internal static class TVInterface_Close_Patch
    {
        static bool Prefix()
        {
            if (!TVHomeScreen_Close_Patch.SkipInterfaceClose)
                return true;

            TVHomeScreen_Close_Patch.SkipInterfaceClose = false;
            return false;
        }
    }
}
