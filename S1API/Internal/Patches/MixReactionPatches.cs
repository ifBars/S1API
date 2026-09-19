#if (IL2CPPMELON)
using S1Effects = Il2CppScheduleOne.Effects;
using S1Product = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Packaging = Il2CppScheduleOne.Product.Packaging;
using EffectList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Effects.Effect>;
#elif MONOMELON
using S1Effects = ScheduleOne.Effects;
using S1Product = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
using S1Packaging = ScheduleOne.Product.Packaging;
using EffectList = System.Collections.Generic.List<ScheduleOne.Effects.Effect>;
#endif
using System;
using System.Linq;
using HarmonyLib;
using S1API.Logging;
using S1API.Products;
using S1API.Internal.Products;
using S1API.Internal.Utils;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Applies mod-registered mixing reactions after the game's mixing calculation.
    /// </summary>
    [HarmonyPatch(typeof(S1Effects.EffectMixCalculator), "MixProperties")]
    internal static class MixReactionPatches
    {
        private static readonly Log Logger = new Log("MixReactionPatches");

        private const int MaxProperties = 8;

        /// <summary>
        /// Applies registered mixing reactions to the effect list the game produced for a mix.
        /// </summary>
        /// <param name="__result">The mix result (Harmony-injected); reassigned to a cloned, transformed list so the game's own list is never mutated.</param>
        /// <param name="newProperty">The effect the mixed-in ingredient contributed.</param>
        /// <param name="drugType">The drug being mixed.</param>
        [HarmonyPostfix]
        private static void MixProperties_Postfix(
            ref EffectList __result,
            S1Effects.Effect newProperty,
            S1Product.EDrugType drugType)
        {
            try
            {
                if (__result == null || newProperty == null)
                    return;

                var rules = MixReactions.Snapshot();
                if (rules.Length == 0)
                    return;

                var mixerId = newProperty.ID;

                // The game can return a shared list (e.g. a product definition's Properties for a named recipe),
                // so never mutate __result directly. Clone once, only if a rule actually fires.
                EffectList? working = null;

                foreach (var rule in rules)
                {
                    if (rule.Drug.HasValue && (int)rule.Drug.Value != (int)drugType)
                        continue;

                    if (!string.Equals(rule.MixerEffectId, mixerId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var current = working ?? __result;
                    if (IndexOfId(current, rule.WhenContainsId) < 0)
                        continue;

                    var addEffect = MixReactions.ResolveAddResult(rule);
                    if (addEffect == null)
                        continue;

                    if (working == null)
                        working = Clone(__result);

                    var matchIndex = IndexOfId(working, rule.WhenContainsId);
                    if (matchIndex < 0)
                        continue;

                    if (rule.Replace)
                    {
                        working[matchIndex] = addEffect;
                        RemoveDuplicateIds(working, addEffect.ID);
                    }
                    else if (working.Count < MaxProperties && IndexOfId(working, addEffect.ID) < 0)
                    {
                        working.Add(addEffect);
                    }
                }

                if (working != null)
                    __result = working;
            }
            catch (Exception ex)
            {
                Logger.Error($"Mixing reaction postfix failed: {ex}");
            }
        }

        private static EffectList Clone(EffectList source)
        {
            var copy = new EffectList();
            if (source == null)
                return copy;
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i]);
            return copy;
        }

        private static int IndexOfId(EffectList list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var effect = list[i];
                if (effect != null && string.Equals(effect.ID, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static void RemoveDuplicateIds(EffectList list, string id)
        {
            var first = IndexOfId(list, id);
            if (first < 0)
                return;

            for (int i = list.Count - 1; i > first; i--)
            {
                var effect = list[i];
                if (effect != null && string.Equals(effect.ID, id, StringComparison.OrdinalIgnoreCase))
                    list.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// INTERNAL: Replaces only registered generic-product mix outputs before the native family switch.
    /// </summary>
    [HarmonyPatch(typeof(S1Product.ProductManager), "RpcLogic___FinishAndNameMix_4237212381")]
    internal static class CustomProductMixingPatches
    {
        private static readonly Log Logger = new Log("CustomProductMixingPatches");

        [HarmonyPrefix]
        private static bool FinishAndNameMix_Prefix(
            string productID,
            string ingredientID,
            string mixName,
            ref string mixID)
        {
            try
            {
                S1Product.ProductDefinition? source =
                    S1Registry.GetItem(productID) as S1Product.ProductDefinition;
                if (source == null || !CustomProductDefinitionRegistry.TryGetMetadata(source, out CustomProductDefinitionMetadata? metadata))
                    return true;

                if (!ProductMixingProfiles.TryGet(metadata!.ProductKind.Id, out ProductMixingProfile? profile))
                {
                    Logger.Error("Custom product '" + productID + "' has logical kind '" + metadata.ProductKind.Id + "' but no ProductMixingProfile is registered. The mix was rejected before the unsupported native output switch.");
                    return false;
                }

                if (!CustomProductMixingIdentity.IsGeneratedIdForSource(productID, mixID))
                    mixID = CustomProductMixingIdentity.CreateGeneratedProductId(productID, mixID);

                // FishNet can execute the same observer RPC after a local host path. Preserve
                // the native first-wins behavior and do not allocate a second definition.
                if (CustomProductDefinitionRegistry.IsRegistered(mixID))
                    return false;

                object? ingredientItem = S1Registry.GetItem(ingredientID);
                if (ingredientItem == null ||
                    !CrossType.Is(
                        ingredientItem,
                        out S1Product.PropertyItemDefinition ingredient))
                {
                    Logger.Error("Custom product mix '" + productID + "' could not resolve ingredient '" + ingredientID + "' as a property item.");
                    return false;
                }

                if (ingredient.Properties == null ||
                    !CustomProductMixingIngredientContract.HasUsableProperty(
                        ingredient.Properties.Count))
                {
                    Logger.Error("Custom product mix '" + productID + "' resolved ingredient '" + ingredientID + "' without any properties.");
                    return false;
                }

                // Match the native ProductManager contract: mixing ingredients only need
                // one usable property, and the first property drives the calculation.
                EffectList properties = S1Effects.EffectMixCalculator.MixProperties(
                    source.Properties,
                    ingredient.Properties[0],
                    source.DrugType);
                var resolvedProperties =
                    new System.Collections.Generic.List<S1Effects.Effect>(properties.Count);
                for (int i = 0; i < properties.Count; i++)
                    resolvedProperties.Add(properties[i]);
                var outputInput = new ProductMixingOutput(
                    mixID,
                    mixName,
                    productID,
                    metadata.ProductKind,
                    source.BasePrice);
                ProductMixingOutputDefinition output = profile!.OutputFactory(outputInput)
                    ?? throw new InvalidOperationException("The mixing output factory returned null.");
                DrugType nativeDrugType = ProductMixingMapContract.GetNativeDrugType(profile.MixerMap);
                if ((int)source.DrugType != (int)nativeDrugType)
                {
                    throw new InvalidOperationException(
                        "Custom product '" + productID + "' uses native drug type '" +
                        source.DrugType + "' but its ProductMixingProfile selects mixer map '" +
                        profile.MixerMap + "'. Configure the product's explicit native mixer map to match the profile.");
                }

                var packaging = new System.Collections.Generic.List<S1Packaging.PackagingDefinition>();
                for (int i = 0; i < metadata.ValidPackaging.Count; i++)
                    packaging.Add(metadata.ValidPackaging[i].S1PackagingDefinition);

                S1Product.ProductDefinition template = metadata.RepresentationTemplate ?? source;
                UnityEngine.Color32? generatedMixColor = null;
                if (profile.UsePropertyColorMixing)
                {
                    var colorSamples =
                        new System.Collections.Generic.List<
                            ProductMixingColorSample>(resolvedProperties.Count);
                    for (int i = 0; i < resolvedProperties.Count; i++)
                    {
                        UnityEngine.Color32 propertyColor =
                            resolvedProperties[i].ProductColor;
                        colorSamples.Add(
                            new ProductMixingColorSample(
                                (int)resolvedProperties[i].Tier,
                                new ProductMixingColorValue(
                                    propertyColor.r,
                                    propertyColor.g,
                                    propertyColor.b,
                                    propertyColor.a)));
                    }

                    generatedMixColor =
                        ProductMixingColorContract.CalculatePrimaryColor(
                            profile.MixerMap,
                            colorSamples).ToColor32();
                }
                S1Product.ProductDefinition generated = CustomProductDefinitionFactory.Create(
                    mixID,
                    output.Name,
                    source.Description,
                    output.Price,
                    (global::S1API.Items.LegalStatus)(int)source.legalStatus,
                    source.BaseAddictiveness,
                    source.PlayerEffectDuration,
                    source.NPCEffectDuration,
                    nativeDrugType,
                    resolvedProperties,
                    packaging,
                    template);
                var generatedMetadata = new CustomProductDefinitionMetadata(
                    output.ProductKind,
                    metadata.DefaultQuality,
                    metadata.ValidPackaging,
                    template,
                    generatedMixColor);
                var saveDescriptor = new CustomProductSaveDescriptorData
                {
                    ProductId = mixID,
                    OwnerId = CustomProductDefinitionBuilderContract.GetOwnerId(mixID),
                    ProductName = output.Name,
                    Description = source.Description,
                    InitialPrice = output.Price,
                    LegalStatus = (int)source.legalStatus,
                    BaseAddictiveness = source.BaseAddictiveness,
                    DefaultQuality = (int)metadata.DefaultQuality,
                    ProductKindId = output.ProductKind.Id,
                    CompatibilityDrugType = (int)nativeDrugType,
                    RepresentationTemplateId = template.ID,
                    PlayerEffectDurationSeconds = source.PlayerEffectDuration,
                    NpcEffectDurationSeconds = source.NPCEffectDuration,
                    PropertyIds = resolvedProperties.ConvertAll(property => property.ID).ToArray(),
                    PackagingIds = metadata.ValidPackaging.Select(packagingDefinition => packagingDefinition.ID).ToArray(),
                    IsGeneratedMix = true,
                    HasGeneratedMixColor = generatedMixColor.HasValue,
                    GeneratedMixColorR = generatedMixColor?.r ?? 0,
                    GeneratedMixColorG = generatedMixColor?.g ?? 0,
                    GeneratedMixColorB = generatedMixColor?.b ?? 0,
                    GeneratedMixColorA = generatedMixColor?.a ?? 0
                };
                try
                {
                    CustomProductDefinitionRegistry.Register(
                        CustomProductDefinitionBuilderContract.GetOwnerId(mixID),
                        mixID,
                        output.Name,
                        output.Price,
                        generated,
                        generatedMetadata,
                        saveDescriptor);

                    CompleteGeneratedMixCreation(generated);
                }
                catch
                {
                    CustomProductDefinitionFactory.Destroy(generated);
                    throw;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logger.Error("Custom product mixing output failed for '" + productID + "': " + exception);
                return false;
            }
        }

        private static void CompleteGeneratedMixCreation(
            S1Product.ProductDefinition generated)
        {
            S1Product.ProductManager productManager =
                S1Product.ProductManager.Instance
                ?? throw new InvalidOperationException(
                    "Cannot complete custom product mixing before ProductManager is available.");

            // Match the native Create* lifecycle after the definition has entered the
            // registry. Discovery makes the output listable, while this event creates
            // its Product Manager entry and lets S1API route it to the logical kind.
            productManager.SetProductDiscovered(
                null,
                generated.ID,
                autoList: false);
            productManager.onNewProductCreated?.Invoke(generated);
        }
    }

    internal static class CustomProductMixingIngredientContract
    {
        internal static bool HasUsableProperty(int propertyCount)
        {
            return propertyCount > 0;
        }
    }

    /// <summary>
    /// INTERNAL: Replaces the fully sanitized native ID at the RPC seams.
    /// </summary>
    [HarmonyPatch(typeof(S1Product.ProductManager), "FinishAndNameMix", new[] { typeof(string), typeof(string), typeof(string), typeof(string) })]
    internal static class CustomProductMixingIdPatches
    {
        [HarmonyPrefix]
        private static void FinishAndNameMix_Prefix(string productID, ref string mixID)
        {
            S1Product.ProductDefinition? source = S1Registry.GetItem(productID) as S1Product.ProductDefinition;
            if (source == null || !CustomProductDefinitionRegistry.TryGetMetadata(source, out CustomProductDefinitionMetadata? metadata))
                return;
            if (ProductMixingProfiles.TryGet(metadata!.ProductKind.Id, out _) &&
                !CustomProductMixingIdentity.IsGeneratedIdForSource(productID, mixID))
            {
                mixID = CustomProductMixingIdentity.CreateGeneratedProductId(productID, mixID);
            }
        }
    }

    /// <summary>INTERNAL: Uses the same generated ID on client-to-server mix requests.</summary>
    [HarmonyPatch(typeof(S1Product.ProductManager), "SendFinishAndNameMix", new[] { typeof(string), typeof(string), typeof(string), typeof(string) })]
    internal static class CustomProductMixingClientRpcPatches
    {
        [HarmonyPrefix]
        private static void SendFinishAndNameMix_Prefix(string productID, ref string mixID)
        {
            S1Product.ProductDefinition? source = S1Registry.GetItem(productID) as S1Product.ProductDefinition;
            if (source == null || !CustomProductDefinitionRegistry.TryGetMetadata(source, out CustomProductDefinitionMetadata? metadata))
                return;
            if (ProductMixingProfiles.TryGet(metadata!.ProductKind.Id, out _) &&
                !CustomProductMixingIdentity.IsGeneratedIdForSource(productID, mixID))
            {
                mixID = CustomProductMixingIdentity.CreateGeneratedProductId(productID, mixID);
            }
        }
    }
}
