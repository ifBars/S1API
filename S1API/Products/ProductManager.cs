#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Product = ScheduleOne.Product;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Entities;
using S1API.Properties.Interfaces;

namespace S1API.Products
{
    /// <summary>
    /// Provides management over all products in the game.
    /// </summary>
    public static class ProductManager
    {
        private sealed class EffectCallbackRegistration
        {
            internal EffectCallbackRegistration(Action<Player> callback, bool allowDefaultEffect)
            {
                Callback = callback;
                AllowDefaultEffect = allowDefaultEffect;
            }

            internal Action<Player> Callback { get; }

            internal bool AllowDefaultEffect { get; }
        }

        private sealed class NpcEffectCallbackRegistration
        {
            internal NpcEffectCallbackRegistration(Action<NPC> callback, bool allowDefaultEffect)
            {
                Callback = callback;
                AllowDefaultEffect = allowDefaultEffect;
            }

            internal Action<NPC> Callback { get; }

            internal bool AllowDefaultEffect { get; }
        }

        private static readonly Dictionary<string, EffectCallbackRegistration> EffectCallbacks = new Dictionary<string, EffectCallbackRegistration>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, NpcEffectCallbackRegistration> NpcEffectCallbacks = new Dictionary<string, NpcEffectCallbackRegistration>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Minimum price for any product (1).
        /// </summary>
        public const int MinPrice = 1;

        /// <summary>
        /// Maximum price for any product (999).
        /// </summary>
        public const int MaxPrice = 999;

        /// <summary>
        /// A list of product definitions discovered on this save.
        /// </summary>
        public static ProductDefinition[] DiscoveredProducts => S1Product.ProductManager.DiscoveredProducts.ToArray()
            .Select(productDefinition => ProductDefinitionWrapper.Wrap(new ProductDefinition(productDefinition)))
            .ToArray();

        /// <summary>
        /// A list of products currently listed for sale.
        /// </summary>
        public static ProductDefinition[] ListedProducts => S1Product.ProductManager.ListedProducts.ToArray()
            .Select(productDefinition => ProductDefinitionWrapper.Wrap(new ProductDefinition(productDefinition)))
            .ToArray();

        /// <summary>
        /// A list of favourited products.
        /// </summary>
        public static ProductDefinition[] FavouritedProducts => S1Product.ProductManager.FavouritedProducts.ToArray()
            .Select(productDefinition => ProductDefinitionWrapper.Wrap(new ProductDefinition(productDefinition)))
            .ToArray();

        /// <summary>
        /// Gets whether the player is currently accepting orders.
        /// </summary>
        public static bool IsAcceptingOrders => S1Product.ProductManager.IsAcceptingOrders;

        /// <summary>
        /// Gets whether meth has been discovered.
        /// </summary>
        public static bool MethDiscovered => S1Product.ProductManager.MethDiscovered;

        /// <summary>
        /// Gets whether cocaine has been discovered.
        /// </summary>
        public static bool CocaineDiscovered => S1Product.ProductManager.CocaineDiscovered;

        /// <summary>
        /// Gets whether shrooms have been discovered.
        /// </summary>
        public static bool ShroomsDiscovered => S1Product.ProductManager.ShroomsDiscovered;

        /// <summary>
        /// Gets the current price of a product.
        /// </summary>
        /// <param name="product">The product definition.</param>
        /// <returns>The price of the product.</returns>
        public static float GetPrice(ProductDefinition product) => S1Product.ProductManager.Instance.GetPrice(product.S1ProductDefinition);

        /// <summary>
        /// Calculates the value of a product based on its properties.
        /// </summary>
        /// <param name="product">The product definition.</param>
        /// <param name="baseValue">The base value to calculate from.</param>
        /// <returns>The calculated value.</returns>
        public static float CalculateProductValue(ProductDefinition product, float baseValue) =>
            S1Product.ProductManager.CalculateProductValue(product.S1ProductDefinition, baseValue);

        /// <summary>
        /// Registers or replaces a callback for a product effect.
        /// When this effect triggers on the local player, the callback is invoked and can optionally allow base behavior.
        /// </summary>
        /// <param name="property">The product effect/property to override.</param>
        /// <param name="callback">The callback to invoke with the local player.</param>
        /// <param name="allowDefaultEffect">
        /// If <c>true</c>, the base game effect is also applied.
        /// If <c>false</c>, only the callback runs for this effect.
        /// </param>
        public static void SetEffectCallback(PropertyBase property, Action<Player> callback, bool allowDefaultEffect = false)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            SetEffectCallback(property.ID, callback, allowDefaultEffect);
        }

        /// <summary>
        /// Registers or replaces a callback for a product effect ID.
        /// When this effect triggers on the local player, the callback is invoked and can optionally allow base behavior.
        /// </summary>
        /// <param name="effectId">The product effect ID.</param>
        /// <param name="callback">The callback to invoke with the local player.</param>
        /// <param name="allowDefaultEffect">
        /// If <c>true</c>, the base game effect is also applied.
        /// If <c>false</c>, only the callback runs for this effect.
        /// </param>
        public static void SetEffectCallback(string effectId, Action<Player> callback, bool allowDefaultEffect = false)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException("Effect ID cannot be null or whitespace.", nameof(effectId));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            EffectCallbacks[effectId] = new EffectCallbackRegistration(callback, allowDefaultEffect);
        }

        /// <summary>
        /// Removes an effect callback by property.
        /// </summary>
        /// <param name="property">The product effect/property to remove.</param>
        /// <returns><c>true</c> if a callback was removed; otherwise <c>false</c>.</returns>
        public static bool RemoveEffectCallback(PropertyBase property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            return RemoveEffectCallback(property.ID);
        }

        /// <summary>
        /// Removes an effect callback by effect ID.
        /// </summary>
        /// <param name="effectId">The product effect ID.</param>
        /// <returns><c>true</c> if a callback was removed; otherwise <c>false</c>.</returns>
        public static bool RemoveEffectCallback(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            return EffectCallbacks.Remove(effectId);
        }

        /// <summary>
        /// Removes all registered product effect callbacks.
        /// </summary>
        public static void ClearEffectCallbacks() =>
            EffectCallbacks.Clear();

        /// <summary>
        /// Registers or replaces an NPC callback for a product effect.
        /// When this effect triggers on an NPC, the callback is invoked and can optionally allow base behavior.
        /// </summary>
        /// <param name="property">The product effect/property to override.</param>
        /// <param name="callback">The callback to invoke with the target NPC.</param>
        /// <param name="allowDefaultEffect">
        /// If <c>true</c>, the base game effect is also applied.
        /// If <c>false</c>, only the callback runs for this effect.
        /// </param>
        public static void SetNpcEffectCallback(PropertyBase property, Action<NPC> callback, bool allowDefaultEffect = false)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            SetNpcEffectCallback(property.ID, callback, allowDefaultEffect);
        }

        /// <summary>
        /// Registers or replaces an NPC callback for a product effect ID.
        /// When this effect triggers on an NPC, the callback is invoked and can optionally allow base behavior.
        /// </summary>
        /// <param name="effectId">The product effect ID.</param>
        /// <param name="callback">The callback to invoke with the target NPC.</param>
        /// <param name="allowDefaultEffect">
        /// If <c>true</c>, the base game effect is also applied.
        /// If <c>false</c>, only the callback runs for this effect.
        /// </param>
        public static void SetNpcEffectCallback(string effectId, Action<NPC> callback, bool allowDefaultEffect = false)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                throw new ArgumentException("Effect ID cannot be null or whitespace.", nameof(effectId));

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            NpcEffectCallbacks[effectId] = new NpcEffectCallbackRegistration(callback, allowDefaultEffect);
        }

        /// <summary>
        /// Removes an NPC effect callback by property.
        /// </summary>
        /// <param name="property">The product effect/property to remove.</param>
        /// <returns><c>true</c> if a callback was removed; otherwise <c>false</c>.</returns>
        public static bool RemoveNpcEffectCallback(PropertyBase property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            return RemoveNpcEffectCallback(property.ID);
        }

        /// <summary>
        /// Removes an NPC effect callback by effect ID.
        /// </summary>
        /// <param name="effectId">The product effect ID.</param>
        /// <returns><c>true</c> if a callback was removed; otherwise <c>false</c>.</returns>
        public static bool RemoveNpcEffectCallback(string effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return false;

            return NpcEffectCallbacks.Remove(effectId);
        }

        /// <summary>
        /// Removes all registered NPC product effect callbacks.
        /// </summary>
        public static void ClearNpcEffectCallbacks() =>
            NpcEffectCallbacks.Clear();

        /// <summary>
        /// INTERNAL: Tries to invoke a registered product effect callback.
        /// </summary>
        /// <param name="effectId">The product effect ID.</param>
        /// <param name="player">The local player wrapper to pass into callback.</param>
        /// <param name="allowDefaultEffect">Outputs whether the base game effect should also be applied.</param>
        /// <returns><c>true</c> if a callback was found and invoked; otherwise <c>false</c>.</returns>
        internal static bool TryInvokeEffectCallback(string effectId, Player player, out bool allowDefaultEffect)
        {
            allowDefaultEffect = false;

            if (string.IsNullOrWhiteSpace(effectId) || player == null)
                return false;

            if (!EffectCallbacks.TryGetValue(effectId, out var registration) || registration?.Callback == null)
                return false;

            allowDefaultEffect = registration.AllowDefaultEffect;
            registration.Callback(player);
            return true;
        }

        /// <summary>
        /// INTERNAL: Tries to invoke a registered NPC product effect callback.
        /// </summary>
        /// <param name="effectId">The product effect ID.</param>
        /// <param name="npc">The NPC wrapper to pass into callback.</param>
        /// <param name="allowDefaultEffect">Outputs whether the base game effect should also be applied.</param>
        /// <returns><c>true</c> if a callback was found and invoked; otherwise <c>false</c>.</returns>
        internal static bool TryInvokeNpcEffectCallback(string effectId, NPC npc, out bool allowDefaultEffect)
        {
            allowDefaultEffect = false;

            if (string.IsNullOrWhiteSpace(effectId) || npc == null)
                return false;

            if (!NpcEffectCallbacks.TryGetValue(effectId, out var registration) || registration?.Callback == null)
                return false;

            allowDefaultEffect = registration.AllowDefaultEffect;
            registration.Callback(npc);
            return true;
        }
    }
}
