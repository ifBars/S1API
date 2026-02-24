#if (IL2CPPMELON)
using S1DealerManagementApp = Il2CppScheduleOne.UI.Phone.Messages.DealerManagementApp;
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1DealerManagementApp = ScheduleOne.UI.Phone.Messages.DealerManagementApp;
using S1Economy = ScheduleOne.Economy;
#endif

using System;
using System.Reflection;
using HarmonyLib;
using S1API.Internal.Utils;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches DealerManagementApp to refresh the dropdown when opened.
    /// Fixes stale mugshot sprites and region text for custom S1API dealers whose
    /// properties are set in OnCreated (after the dropdown was initially populated).
    /// </summary>
    [HarmonyPatch]
    internal static class DealerManagementAppPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("DealerManagementAppPatches");

        #region Private Implementation

        /// <summary>
        /// Calls the private RefreshDropdown method and restores the selected dealer index.
        /// </summary>
        private static void RefreshAndSyncDropdown(S1DealerManagementApp instance)
        {
            var selectedDealer = instance.SelectedDealer;

#if (IL2CPPMELON)
            try
            {
                instance.RefreshDropdown();
            }
            catch
            {
                var method = typeof(S1DealerManagementApp).GetMethod("RefreshDropdown",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                method?.Invoke(instance, null);
            }
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
            var method = typeof(S1DealerManagementApp).GetMethod("RefreshDropdown",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                return;
            method.Invoke(instance, null);
#endif

            if (selectedDealer == null)
                return;

#if (IL2CPPMELON)
            var dealers = instance.dealers;
            if (dealers == null)
                return;

            int index = -1;
            for (int i = 0; i < dealers.Count; i++)
            {
                if (dealers[i] == selectedDealer)
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
                instance._dropdown?.SetValueWithoutNotify(index);
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
            var dealersObj = ReflectionUtils.TryGetFieldOrProperty(instance, "dealers");
            var dealers = dealersObj as System.Collections.Generic.List<S1Economy.Dealer>;
            if (dealers == null)
                return;

            int index = dealers.IndexOf(selectedDealer);
            if (index < 0)
                return;

            var dropdownObj = ReflectionUtils.TryGetFieldOrProperty(instance, "_dropdown");
            var dropdown = dropdownObj as UnityEngine.UI.Dropdown;
            if (dropdown != null)
                dropdown.SetValueWithoutNotify(index);
#endif
        }

        #endregion

        #region Harmony Patches

        /// <summary>
        /// After SetOpen(true), rebuild the dropdown so it reflects current MugshotSprite and Region values.
        /// </summary>
        [HarmonyPatch(typeof(S1DealerManagementApp), nameof(S1DealerManagementApp.SetOpen))]
        [HarmonyPostfix]
        private static void SetOpen_Postfix(S1DealerManagementApp __instance, bool open)
        {
            if (!open)
                return;

            try
            {
                RefreshAndSyncDropdown(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to refresh dealer dropdown on open: {ex.Message}");
            }
        }

        /// <summary>
        /// After Refresh(), rebuild the dropdown so it reflects current values when the phone is reopened.
        /// </summary>
        [HarmonyPatch(typeof(S1DealerManagementApp), nameof(S1DealerManagementApp.Refresh))]
        [HarmonyPostfix]
        private static void Refresh_Postfix(S1DealerManagementApp __instance)
        {
            try
            {
                RefreshAndSyncDropdown(__instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to refresh dealer dropdown on refresh: {ex.Message}");
            }
        }

        #endregion
    }
}
