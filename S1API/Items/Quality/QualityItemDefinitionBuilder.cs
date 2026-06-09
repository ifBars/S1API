#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif
using S1API.Internal.Utils;
using S1API.Items.Storable;
using UnityEngine;

namespace S1API.Items.Quality
{
    /// <summary>
    /// Builder for composing quality item definitions at runtime.
    /// Use fluent methods to configure item properties before calling <see cref="Build"/>
    /// </summary>
    public sealed class QualityItemDefinitionBuilder
        : StorableItemDefinitionBuilderBase<QualityItemDefinitionBuilder>
    {
        private S1ItemFramework.QualityItemDefinition QualityDefinition =>
            CrossType.As<S1ItemFramework.QualityItemDefinition>(Definition);

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh QualityItemDefinition.
        /// Only <see cref="QualityItemCreator"/> can instantiate this.
        /// </summary>
        internal QualityItemDefinitionBuilder()
            : base(ScriptableObject.CreateInstance<S1ItemFramework.QualityItemDefinition>)
        {
            QualityDefinition.DefaultQuality =
                S1ItemFramework.EQuality.Standard;
        }

        /// <summary>
        /// INTERNAL: Creates a builder instance initialized by cloning an existing quality item definition.
        /// Only <see cref="QualityItemCreator"/> can instantiate this.
        /// </summary>
        /// <param name="source">The existing quality item definition to clone properties from.</param>
        internal QualityItemDefinitionBuilder(
            S1ItemFramework.QualityItemDefinition source)
            : base(
                source,
                ScriptableObject.CreateInstance<S1ItemFramework.QualityItemDefinition>)
        {
        }

        /// <inheritdoc/>
        protected override void CopyPropertiesFrom(
            S1ItemFramework.StorableItemDefinition source)
        {
            base.CopyPropertiesFrom(source);

            var qualitySource = CrossType.As<S1ItemFramework.QualityItemDefinition>(source);

            QualityDefinition.DefaultQuality = qualitySource.DefaultQuality;
        }

        /// <summary>
        /// Assigns a default quality for this definition.
        /// </summary>
        /// <param name="quality">The default quality to assign to items of this definition.</param>
        /// <returns>>The builder instance for fluent chaining.</returns>
        public QualityItemDefinitionBuilder WithDefaultQuality(Products.Quality quality)
        {
            QualityDefinition.DefaultQuality = (S1ItemFramework.EQuality)quality;
            return this;
        }

        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created quality item definition.</returns>
        public new QualityItemDefinition Build()
        {
            return (QualityItemDefinition)base.Build();
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal new S1ItemFramework.QualityItemDefinition BuildInternal()
        {
            return QualityDefinition;
        }

        /// <inheritdoc />
        protected override Storable.StorableItemDefinition CreateWrapper(
            S1ItemFramework.StorableItemDefinition definition)
        {
            return new QualityItemDefinition(CrossType.As<S1ItemFramework.QualityItemDefinition>(definition));
        }
    }
}
