#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Storage = Il2CppScheduleOne.Storage;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
using S1Registry = ScheduleOne.Registry;
using S1Storage = ScheduleOne.Storage;
#endif

using System;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items.ItemBuilders
{
    /// <summary>
    /// Builder for composing additive definitions at runtime.
    /// Use fluent methods to configure additive properties before calling <see cref="Build"/>.
    /// </summary>
    public sealed class AdditiveDefinitionBuilder
        : StorableItemDefinitionBuilder<AdditiveDefinitionBuilder>
    {
        private static readonly Log Logger = new Log("AdditiveDefinitionBuilder");

        private S1ItemFramework.AdditiveDefinition AdditiveDefinition =>
            CrossType.As<S1ItemFramework.AdditiveDefinition>(Definition);

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh AdditiveDefinition.
        /// Only <see cref="AdditiveItemCreator"/> can instantiate this.
        /// </summary>
        internal AdditiveDefinitionBuilder()
            : base(ScriptableObject.CreateInstance<S1ItemFramework.AdditiveDefinition>)
        {
            Definition.Category = S1CoreItemFramework.EItemCategory.Agriculture;
        }

        /// <summary>
        /// INTERNAL: Creates a builder instance initialized by cloning an existing additive.
        /// </summary>
        internal AdditiveDefinitionBuilder(
            S1ItemFramework.AdditiveDefinition source)
            : base(source,
                ScriptableObject.CreateInstance<S1ItemFramework.AdditiveDefinition>)
        {
        }

        /// <inheritdoc/>
        protected override void CopyPropertiesFrom(
            S1ItemFramework.StorableItemDefinition source)
        {
            base.CopyPropertiesFrom(source);

            var additiveSource = CrossType.As<S1ItemFramework.AdditiveDefinition>(source);

            // AdditiveDefinition properties (auto-properties with private set in Mono)
            AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.DisplayMaterial),
                additiveSource.DisplayMaterial);
            AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.QualityChange),
                additiveSource.QualityChange);
            AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.YieldMultiplier),
                additiveSource.YieldMultiplier);
            AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.InstantGrowth),
                additiveSource.InstantGrowth);
        }

        /// <summary>
        /// Sets the display material for this additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithDisplayMaterial(Material material)
        {
            if (!AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.DisplayMaterial),
                    material))
            {
                Logger.Warning(
                    $"Failed to set DisplayMaterial on AdditiveDefinition '{AdditiveDefinition.ID ?? "<no id>"}'.");
            }

            return this;
        }

        /// <summary>
        /// Sets the effect values for this additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithEffects(float yieldMultiplier, float instantGrowth, float qualityChange)
        {
            if (!AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.YieldMultiplier),
                    yieldMultiplier))
            {
                Logger.Warning(
                    $"Failed to set YieldMultiplier on AdditiveDefinition '{AdditiveDefinition.ID ?? "<no id>"}'.");
            }

            if (!AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.InstantGrowth),
                    instantGrowth))
            {
                Logger.Warning(
                    $"Failed to set InstantGrowth on AdditiveDefinition '{AdditiveDefinition.ID ?? "<no id>"}'.");
            }

            if (!AutoPropertySetter.TrySet(AdditiveDefinition, nameof(S1ItemFramework.AdditiveDefinition.QualityChange),
                    qualityChange))
            {
                Logger.Warning(
                    $"Failed to set QualityChange on AdditiveDefinition '{AdditiveDefinition.ID ?? "<no id>"}'.");
            }

            return this;
        }
    
        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created additive definition.</returns>
        public new Additive.AdditiveDefinition Build()
        {
            return (Additive.AdditiveDefinition)base.Build();
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal new S1ItemFramework.AdditiveDefinition BuildInternal()
        {
            return AdditiveDefinition;
        }

        /// <inheritdoc />
        protected override Storable.StorableItemDefinition CreateWrapper(
            S1ItemFramework.StorableItemDefinition definition)
        {
            return new Additive.AdditiveDefinition(AdditiveDefinition);
        }
    }
}