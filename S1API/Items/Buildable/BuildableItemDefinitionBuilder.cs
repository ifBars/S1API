#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
using S1EntityFramework = ScheduleOne.EntityFramework;
#endif
using System;
using S1API.Internal.Building;
using S1API.Internal.Utils;
using S1API.Items.Storable;
using UnityEngine;

namespace S1API.Items.Buildable
{
    /// <summary>
    /// Builder for composing buildable item definitions at runtime.
    /// Use fluent methods to configure buildable item properties before calling <see cref="Build"/>.
    /// </summary>
    public class BuildableItemDefinitionBuilder
        : StorableItemDefinitionBuilderBase<BuildableItemDefinitionBuilder>
    {
        private Func<Transform, GameObject>? _ghostVisualFactory;
        private bool _replaceExistingGhostVisual;

        private S1ItemFramework.BuildableItemDefinition BuildableDefinition =>
            CrossType.As<S1ItemFramework.BuildableItemDefinition>(Definition);

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh BuildableItemDefinition.
        /// Only <see cref="BuildableItemCreator"/> can instantiate this.
        /// </summary>
        internal BuildableItemDefinitionBuilder()
            : base(ScriptableObject.CreateInstance<S1ItemFramework.BuildableItemDefinition>)
        {
            Definition.Category = S1CoreItemFramework.EItemCategory.Furniture;
            BuildableDefinition.BuildSoundType =
                S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Wood;
        }

        /// <summary>
        /// INTERNAL: Creates a builder instance initialized by cloning an existing item.
        /// </summary>
        internal BuildableItemDefinitionBuilder(S1ItemFramework.BuildableItemDefinition source)
            : base(source, ScriptableObject.CreateInstance<S1ItemFramework.BuildableItemDefinition>)
        {
        }

        /// <inheritdoc />
        protected override void CopyPropertiesFrom(S1ItemFramework.StorableItemDefinition source)
        {
            base.CopyPropertiesFrom(source);
            var buildableSource = CrossType.As<S1ItemFramework.BuildableItemDefinition>(source);

            BuildableDefinition.BuildSoundType = buildableSource.BuildSoundType;
            BuildableDefinition.BuiltItem = buildableSource.BuiltItem;
        }

        /// <summary>
        /// Sets the sound type played when this item is built.
        /// </summary>
        /// <param name="soundType">The build sound type.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithBuildSound(BuildSoundType soundType)
        {
            BuildableDefinition.BuildSoundType = (S1ItemFramework.BuildableItemDefinition.EBuildSoundType)soundType;
            return this;
        }

        /// <summary>
        /// INTERNAL: Assigns a native build-sound value after a caller-specific compatibility mapping.
        /// </summary>
        internal BuildableItemDefinitionBuilder WithNativeBuildSound(
            S1ItemFramework.BuildableItemDefinition.EBuildSoundType soundType)
        {
            BuildableDefinition.BuildSoundType = soundType;
            return this;
        }

        /// <summary>
        /// Configures an optional visual created whenever the native placement system creates this
        /// buildable's ghost. The factory runs on Unity's main thread and must return the created
        /// visual. S1API parents and activates it under the supplied ghost transform.
        /// </summary>
        /// <param name="visualFactory">Creates the visual under the supplied placement-ghost transform.</param>
        /// <param name="replaceExistingVisual">
        /// Whether S1API should hide renderers inherited from the native buildable before creating the visual.
        /// </param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithGhostVisual(
            Func<Transform, GameObject> visualFactory,
            bool replaceExistingVisual = false)
        {
            _ghostVisualFactory = visualFactory ?? throw new ArgumentNullException(nameof(visualFactory));
            _replaceExistingGhostVisual = replaceExistingVisual;
            return this;
        }

        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created buildable item definition.</returns>
        public new BuildableItemDefinition Build()
        {
            BuildableItemDefinition definition = (BuildableItemDefinition)base.Build();
            if (_ghostVisualFactory != null)
            {
                BuildableGhostRuntime.RegisterVisual(
                    BuildableDefinition.ID,
                    _ghostVisualFactory,
                    _replaceExistingGhostVisual);
            }

            return definition;
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal new S1ItemFramework.BuildableItemDefinition BuildInternal()
        {
            return BuildableDefinition;
        }

        /// <summary>
        /// INTERNAL: Assigns the native placed-item prefab composed by S1API.
        /// </summary>
        internal BuildableItemDefinitionBuilder WithBuiltItem(S1EntityFramework.BuildableItem builtItem)
        {
            BuildableDefinition.BuiltItem = builtItem;
            return this;
        }

        /// <inheritdoc />
        protected override Storable.StorableItemDefinition CreateWrapper(
            S1ItemFramework.StorableItemDefinition definition)
        {
            return new BuildableItemDefinition(CrossType.As<S1ItemFramework.BuildableItemDefinition>(definition));
        }
    }
}
