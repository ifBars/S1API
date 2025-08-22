using HarmonyLib;
using MelonLoader;

#if IL2CPPMELON
using Il2CppScheduleOne.UI.Phone.ProductManagerApp;
using Il2CppScheduleOne.Product;
using ProductManagerAppType = Il2CppScheduleOne.UI.Phone.ProductManagerApp.ProductManagerApp;
#elif MONOMELON || MONOBEPINEX || IL2CPPBEPINEX
using ScheduleOne.UI.Phone.ProductManagerApp;
using ScheduleOne.Product;
using ProductManagerAppType = ScheduleOne.UI.Phone.ProductManagerApp.ProductManagerApp;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Patches ProductManagerApp to prevent cloned instances from subscribing to product discovery events,
    /// which causes null reference exceptions on dedicated servers when custom phone apps are created.
    /// </summary>
    [HarmonyPatch(typeof(ProductManagerAppType), "Start")]
    internal static class ProductManagerAppStartPatch
    {
        /// <summary>
        /// Prevents cloned ProductManagerApp instances (used by custom phone apps) from subscribing
        /// to onProductDiscovered events that cause server crashes.
        /// </summary>
        /// <param name="__instance">The ProductManagerApp instance</param>
        /// <returns>True to continue with original method, false to skip it</returns>
        static bool Prefix(ProductManagerAppType __instance)
        {
            // Only allow the original ProductManagerApp to run Start()
            // Cloned instances (custom phone apps) get renamed and should not subscribe to discovery events
            if (__instance.gameObject.name != "ProductManagerApp")
            {
                // This is a cloned instance used by a custom phone app
                // Skip the Start method to prevent duplicate event subscriptions
                return false;
            }
            
            // Allow the original ProductManagerApp to run normally
            return true;
        }
    }
}
