using System;
using HarmonyLib;
using UnityEngine.SceneManagement;
using S1API.Internal.Utils;
using S1API.Internal.Abstraction;
using S1API.PhoneApp;
using S1API.Logging;
using S1API.ExternalHosting;
using S1API.Internal.PhoneApps;

#if (IL2CPPMELON)
using Il2CppScheduleOne.UI.Phone;
#else
using ScheduleOne.UI.Phone;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// A Harmony patch for the Start method of the HomeScreen class, facilitating the registration and initialization of PhoneApps.
    /// </summary>
    [HarmonyPatch(typeof(HomeScreen), "Start")]
    internal static class HomeScreen_Start_Patch
    {
        /// <summary>
        /// A logging instance used for handling log messages pertaining to PhoneApp registration
        /// and operations. Provides methods to log messages with different severity levels such
        /// as Info, Warning, Error, and Fatal.
        /// </summary>
        private static readonly Log Logger = new Log("PhoneApp");

        /// <summary>
        /// Executes after the HomeScreen's Start method to handle the registration
        /// and initialization of PhoneApps.
        /// </summary>
        /// <param name="__instance">The HomeScreen instance being targeted in the patch.</param>
        static void Postfix(HomeScreen __instance)
        {
            if (__instance == null)
                return;

            // Only register custom apps in the Main scene to avoid prologue issues
            if (!string.Equals(SceneManager.GetActiveScene().name, "Main", StringComparison.OrdinalIgnoreCase))
                return;

            // The phone registry retains old instances across scene loads; external hosts must not.
            ExternalAppCatalog.Clear(ExternalAppFamily.Phone);
            // Re-register all PhoneApps
            var phoneApps = ReflectionUtils.GetDerivedClasses<PhoneApp.PhoneApp>();
            Logger.Msg($"Discovered {phoneApps.Count} custom phone app type(s).");
            foreach (var type in phoneApps)
            {
                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Logger.Msg($"Phone app type {type.FullName} has no public parameterless constructor; it is shown only through PhoneApp.Register.");
                    continue;
                }

                Logger.Msg($"Initializing phone app: {type.FullName}");

                try
                {
                    var instance = (PhoneApp.PhoneApp)Activator.CreateInstance(type)!;
                    ((IRegisterable)instance).CreateInternal();
                    instance.SpawnUI(__instance);
                    instance.SpawnIcon(__instance);
                }
                catch (Exception e)
                {
                    Logger.Warning($"Failed to register phone app {type.FullName}: {e}");
                }
            }

            // Apps registered with PhoneApp.Register outlive the scene and are built again on every home screen.
            PhoneAppRuntime.SpawnAll(__instance);
        }
    }
}
