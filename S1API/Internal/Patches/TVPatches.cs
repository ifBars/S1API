using System;
using System.Collections.Generic;
using HarmonyLib;
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
    }

    /// <summary>
    /// Patches TVHomeScreen.Close to prevent interface closing when custom app is opening.
    /// </summary>
    [HarmonyPatch(typeof(TVHomeScreen), "Close")]
    internal static class TVHomeScreen_Close_Patch
    {
        /// <summary>
        /// Flag indicating that a custom TV app is about to open.
        /// </summary>
        internal static bool SkipInterfaceClose { get; set; }

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
