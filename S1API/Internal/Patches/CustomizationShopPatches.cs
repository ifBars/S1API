#if (IL2CPPMELON)
using S1UICustomization = Il2CppScheduleOne.UI.CharacterCustomization;
#elif MONOMELON
using S1UICustomization = ScheduleOne.UI.CharacterCustomization;
#endif

using HarmonyLib;
using S1API.Internal.Shops;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Lets <see cref="CustomizationShopBuilder"/> clone registered options into a shop category before the
    /// vanilla Awake gathers and wires its options.
    /// </summary>
    [HarmonyPatch]
    internal static class CustomizationShopPatches
    {
        [HarmonyPatch(typeof(S1UICustomization.CharacterCustomizationCategory), "Awake")]
        [HarmonyPrefix]
        private static void CategoryAwake_Prefix(S1UICustomization.CharacterCustomizationCategory __instance) =>
            CustomizationShopBuilder.OnCategoryAwake(__instance);
    }
}
