using System;
using HarmonyLib;
using S1API.Internal.Products;
using UnityEngine;
#if IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Product = ScheduleOne.Product;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Routes registered custom product/package pairs around native drug-type visuals.
    /// </summary>
    [HarmonyPatch]
    internal static class ProductPackagingContentPatches
    {
        [HarmonyPatch(
            typeof(S1Product.MultiTypeVisualsSetter),
            nameof(S1Product.MultiTypeVisualsSetter.ApplyVisuals),
            new Type[] { typeof(S1Product.ProductItemInstance) })]
        [HarmonyPrefix]
        private static bool ApplyVisualsPrefix(
            S1Product.MultiTypeVisualsSetter __instance,
            S1Product.ProductItemInstance itemInstance)
        {
            if (ProductPackagingContentRuntime.IsNativeIconFallbackActive)
                return true;

            return !ProductPackagingContentRuntime.TryApply(
                __instance,
                itemInstance);
        }

        [HarmonyPatch(
            typeof(S1Product.ProductIconManager),
            nameof(S1Product.ProductIconManager.GenerateIcons))]
        [HarmonyPrefix]
        private static void GenerateIconsPrefix()
        {
            ProductPackagingContentRuntime.BeginNativeIconBatch();
        }

        [HarmonyPatch(
            typeof(S1Product.ProductIconManager),
            nameof(S1Product.ProductIconManager.GenerateIcons))]
        [HarmonyFinalizer]
        private static Exception? GenerateIconsFinalizer(Exception? __exception)
        {
            ProductPackagingContentRuntime.EndNativeIconBatch();
            return __exception;
        }

        [HarmonyPatch(
            typeof(S1DevUtilities.IconGenerator),
            nameof(S1DevUtilities.IconGenerator.GeneratePackagingIcon),
            new Type[] { typeof(string), typeof(string), typeof(int) })]
        [HarmonyPrefix]
        private static bool GeneratePackagingIconPrefix(
            S1DevUtilities.IconGenerator __instance,
            string packagingID,
            string productID,
            ref Texture2D __result,
            ref bool __state)
        {
            if (ProductPackagingContentRuntime
                .TryDeferNativeBatchPackagingIcon(packagingID, productID))
            {
                __state = true;
                return true;
            }

            if (!ProductPackagingContentRuntime.TryGeneratePackagingIcon(
                    __instance,
                    packagingID,
                    productID,
                    out Texture2D? texture))
            {
                return true;
            }

            __result = texture!;
            return false;
        }

        [HarmonyPatch(
            typeof(S1DevUtilities.IconGenerator),
            nameof(S1DevUtilities.IconGenerator.GeneratePackagingIcon),
            new Type[] { typeof(string), typeof(string), typeof(int) })]
        [HarmonyFinalizer]
        private static Exception? GeneratePackagingIconFinalizer(
            bool __state,
            Exception? __exception)
        {
            if (__state)
                ProductPackagingContentRuntime.EndNativeIconFallback();
            return __exception;
        }

        [HarmonyPatch(
            typeof(S1Product.ProductIconManager),
            nameof(S1Product.ProductIconManager.GetIcon),
            new Type[]
            {
                typeof(string),
                typeof(string),
                typeof(bool)
            })]
        [HarmonyPrefix]
        private static bool GetIconPrefix(
            string productID,
            string packagingID,
            ref Sprite __result)
        {
            if (!ProductPackagingContentRuntime.TryGetPackagingIcon(
                    productID,
                    packagingID,
                    out Sprite? icon))
            {
                return true;
            }

            __result = icon!;
            return false;
        }
    }
}
