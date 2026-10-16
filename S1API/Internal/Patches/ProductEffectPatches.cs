#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Product = Il2CppScheduleOne.Product;
using S1Properties = Il2CppScheduleOne.Effects;
#elif MONOMELON
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1NPCs = ScheduleOne.NPCs;
using S1Product = ScheduleOne.Product;
using S1Properties = ScheduleOne.Effects;
#endif
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Products;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Properties;
using S1API.Products;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Intercepts product instance effect application and routes through registered callbacks.
    /// </summary>
    [HarmonyPatch]
    internal static class ProductEffectPatches
    {
        private static readonly Log Logger = new Log("ProductEffectPatches");
        private static Dictionary<string, Type>? _npcWrapperTypeById;

        /// <summary>
        /// Targets only the base ProductItemInstance apply and clear implementations.
        /// Subclass overrides (e.g., WeedInstance) will run their custom logic and then call base,
        /// which is intercepted here to route through callbacks.
        /// </summary>
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var playerType = typeof(S1PlayerScripts.Player);
            var npcType = typeof(S1NPCs.NPC);
            var baseType = typeof(S1Product.ProductItemInstance);

            return new[]
            {
                baseType.GetMethod(
                    "ApplyEffectsToPlayer",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { playerType },
                    null),
                baseType.GetMethod(
                    "ApplyEffectsToNPC",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { npcType },
                    null),
                baseType.GetMethod(
                    "ClearEffectsFromPlayer",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { playerType },
                    null),
                baseType.GetMethod(
                    "ClearEffectsFromNPC",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { npcType },
                    null)
            }
            .Where(method => method != null)
            .Cast<MethodBase>();
        }

        /// <summary>
        /// Intercepts product effect application and clearing for players and NPCs and executes callbacks per effect ID.
        /// Unhandled effects fall back to their native apply or clear behavior.
        /// </summary>
        /// <param name="__instance">The product instance applying effects.</param>
        /// <param name="__0">The first argument (player or NPC receiving effects).</param>
        /// <param name="__originalMethod">The native product lifecycle method being intercepted.</param>
        /// <returns><c>false</c> when handled here; <c>true</c> to run original method.</returns>
        [HarmonyPrefix]
        private static bool ProductEffects_Prefix(
            S1Product.ProductItemInstance __instance,
            object __0,
            MethodBase __originalMethod)
        {
            var target = __0;
            var isClear = __originalMethod?.Name == "ClearEffectsFromPlayer" ||
                          __originalMethod?.Name == "ClearEffectsFromNPC";

            if (__instance == null || target == null)
                return true;

            var effects = ResolveEffects(__instance);
            var localPlayer = Player.All.FirstOrDefault(p => p.IsLocal);
            var targetPlayer = target as S1PlayerScripts.Player;
            var targetNpc = target as S1NPCs.NPC;

            if (targetPlayer == null && targetNpc == null)
                return true;

            if (targetPlayer != null && (localPlayer == null || targetPlayer != localPlayer.S1Player))
                return true;

            NPC? apiNpc = targetNpc != null
                ? ResolveApiNpc(targetNpc)
                : null;
            ProductConsumptionContext? consumptionContext = null;
            ProductConsumptionProfileRegistration? consumptionRegistration = null;
            if (effects == null && !ProductConsumptionProfileDispatcher.TryResolveRegisteredContext(
                    __instance,
                    targetPlayer != null ? localPlayer : null,
                    apiNpc,
                    targetNpc?.ID ?? string.Empty,
                    out consumptionContext,
                    out consumptionRegistration))
            {
                return true;
            }

            var invokedEffectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; effects != null && i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                var effectId = effect.ID;
                if (string.IsNullOrWhiteSpace(effectId) || !invokedEffectIds.Add(effectId))
                    continue;

                try
                {
                    if (targetPlayer != null)
                    {
                        var handled = isClear
                            ? ProductManager.TryInvokeEffectClearCallback(effectId, localPlayer!, out var allowDefaultEffect)
                            : ProductManager.TryInvokeEffectCallback(effectId, localPlayer!, out allowDefaultEffect);
                        if (!handled || allowDefaultEffect)
                        {
                            if (isClear)
                                effect.ClearFromPlayer(targetPlayer);
                            else
                                effect.ApplyToPlayer(targetPlayer);
                        }
                    }
                    else
                    {
                        var allowDefaultEffect = false;
                        var handled = apiNpc != null &&
                                      (isClear
                                          ? ProductManager.TryInvokeNpcEffectClearCallback(effectId, apiNpc, out allowDefaultEffect)
                                          : ProductManager.TryInvokeNpcEffectCallback(effectId, apiNpc, out allowDefaultEffect));
                        if (!handled || allowDefaultEffect)
                        {
                            if (isClear)
                                effect.ClearFromNPC(targetNpc);
                            else
                                effect.ApplyToNPC(targetNpc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var lifecycle = isClear ? "clear" : "apply";
                    Logger.Error($"Exception while invoking effect {lifecycle} callback for '{effectId}': {ex.Message}");
                    Logger.Error(ex.StackTrace ?? string.Empty);
                }
            }

            if (targetPlayer != null)
            {
                ProductConsumptionProfileDispatcher.DispatchPlayer(
                    __instance,
                    targetPlayer,
                    localPlayer!,
                    isClear,
                    consumptionContext,
                    consumptionRegistration);
            }
            else if (targetNpc != null)
            {
                ProductConsumptionProfileDispatcher.DispatchNpc(
                    __instance,
                    targetNpc,
                    apiNpc,
                    targetNpc.ID ?? string.Empty,
                    isClear,
                    consumptionContext,
                    consumptionRegistration);
            }

            return false;
        }

        private static List<S1Properties.Effect>? ResolveEffects(S1Product.ProductItemInstance productInstance)
        {
            try
            {
                var wrappedInstance = new ProductInstance(productInstance);
                var fromWrapper = wrappedInstance.Properties
                    .OfType<ProductPropertyWrapper>()
                    .Select(property => property.InnerProperty)
                    .Where(effect => effect != null)
                    .ToList();

                if (fromWrapper.Count > 0)
                    return fromWrapper;
            }
            catch (Exception ex)
            {
                Logger.Error($"ResolveEffects wrapper path failed: {ex.Message}");
            }

            var fromInstance = ExtractEffectsFromObject(TryGetPropertyValue(productInstance, "Properties"));
            if (fromInstance != null && fromInstance.Count > 0)
                return fromInstance;

            var definition = productInstance.Definition;
            var fromDefinition = ExtractEffectsFromObject(TryGetPropertyValue(definition, "Properties"));
            if (fromDefinition != null && fromDefinition.Count > 0)
                return fromDefinition;

            return null;
        }

        private static object? TryGetPropertyValue(object? instance, string propertyName)
        {
            if (instance == null)
                return null;

            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(instance);
        }

        private static List<S1Properties.Effect>? ExtractEffectsFromObject(object? propertiesObject)
        {
            if (propertiesObject == null)
                return null;

            var results = new List<S1Properties.Effect>();

            if (propertiesObject is IEnumerable enumerable)
            {
                foreach (var entry in enumerable)
                {
                    if (entry is S1Properties.Effect effect)
                        results.Add(effect);
                }
            }

            return results;
        }

        private static NPC? ResolveApiNpc(S1NPCs.NPC? targetNpc)
        {
            if (targetNpc == null)
                return null;

            var byRefOrId = NPC.All.FirstOrDefault(npc =>
                npc?.S1NPC == targetNpc ||
                (!string.IsNullOrWhiteSpace(npc?.S1NPC?.ID) &&
                 !string.IsNullOrWhiteSpace(targetNpc.ID) &&
                 string.Equals(npc.S1NPC.ID, targetNpc.ID, StringComparison.OrdinalIgnoreCase)));

            if (byRefOrId != null)
                return byRefOrId;

            if (string.IsNullOrWhiteSpace(targetNpc.ID))
                return null;

            try
            {
                var wrapperTypeById = GetNpcWrapperTypeById();
                if (!wrapperTypeById.TryGetValue(targetNpc.ID, out var wrapperType))
                    return null;

                var created = Activator.CreateInstance(wrapperType, nonPublic: true) as NPC;
                if (created?.S1NPC == targetNpc)
                    return created;

                return NPC.All.FirstOrDefault(npc => npc?.S1NPC == targetNpc);
            }
            catch (Exception ex)
            {
                Logger.Error($"NPC intercept: wrapper creation failed for '{targetNpc.ID}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Maps each NPC ID to the concrete S1API NPC wrapper type that declares it through a public static
        /// <c>NPCId</c> string property. The first wrapper found for an ID wins; the map is built once and cached.
        /// </summary>
        private static Dictionary<string, Type> GetNpcWrapperTypeById()
        {
            if (_npcWrapperTypeById != null)
                return _npcWrapperTypeById;

            var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            // Only assemblies that reference S1API can hold an NPC wrapper. Enumerating every assembly reaches the
            // IL2CPP interop assemblies, where GetTypes() can end the process without an exception to catch.
            foreach (var type in ReflectionUtils.GetDerivedClasses<NPC>())
            {
                if (type == null || type.IsAbstract)
                    continue;

                var npcIdProperty = type.GetProperty("NPCId", BindingFlags.Public | BindingFlags.Static);
                if (npcIdProperty == null || npcIdProperty.PropertyType != typeof(string))
                    continue;

                var npcId = npcIdProperty.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(npcId))
                    continue;

                if (!map.ContainsKey(npcId))
                    map[npcId] = type;
            }

            _npcWrapperTypeById = map;
            return map;
        }
    }
}
