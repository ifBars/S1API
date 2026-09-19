using HarmonyLib;
using S1API.Internal.Products;
#if IL2CPPMELON
using S1Product = Il2CppScheduleOne.Product;
using EffectList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Effects.Effect>;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using EffectList = System.Collections.Generic.List<ScheduleOne.Effects.Effect>;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Shows registered custom product visuals in the native new-mix display.
    /// </summary>
    [HarmonyPatch(
        typeof(S1Product.NewMixDiscoveryBox),
        nameof(S1Product.NewMixDiscoveryBox.ShowProduct))]
    internal static class ProductPresentationPatches
    {
        [HarmonyPostfix]
        private static void ShowProductPostfix(
            S1Product.NewMixDiscoveryBox __instance,
            S1Product.ProductDefinition baseDefinition,
            EffectList properties)
        {
            if (__instance?.Visuals == null)
                return;

            CustomProductPresentationRuntime.ApplyDiscoveryVisual(
                __instance.Visuals,
                baseDefinition,
                properties);
        }
    }
}
