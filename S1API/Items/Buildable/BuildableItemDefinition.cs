#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif
using System;

namespace S1API.Items.Buildable
{
    /// <summary>
    /// Represents a buildable item definition that can be placed in the game world.
    /// Extends <see cref="StorableItemDefinition"/> with building-specific properties.
    /// </summary>
    /// <remarks>
    /// Use <see cref="BuildableItemCreator.CreateBuilder"/> to create new buildable items,
    /// or <see cref="BuildableItemCreator.CloneFrom"/> to create variants of existing items.
    /// </remarks>
    public sealed class BuildableItemDefinition : Storable.StorableItemDefinition
    {
        /// <summary>
        /// INTERNAL: Wraps an existing native buildable item definition.
        /// </summary>
        internal BuildableItemDefinition(S1ItemFramework.BuildableItemDefinition definition)
            : base(definition)
        {
            S1BuildableItemDefinition = definition;
        }

        /// <summary>
        /// INTERNAL: A reference to the native game buildable item definition.
        /// </summary>
        internal S1ItemFramework.BuildableItemDefinition S1BuildableItemDefinition { get; }

        /// <summary>
        /// The sound type played when this item is built.
        /// </summary>
        public BuildSoundType BuildSoundType
        {
            get => (BuildSoundType)S1BuildableItemDefinition.BuildSoundType;
            set => S1BuildableItemDefinition.BuildSoundType = (S1ItemFramework.BuildableItemDefinition.EBuildSoundType)value;
        }

    }

    /// <summary>
    /// Specifies the sound type played when a buildable item is placed.
    /// </summary>
    public enum BuildSoundType
    {
        /// <summary>Wood building sound.</summary>
        Wood,
        /// <summary>Metal building sound.</summary>
        Metal,
        /// <summary>Plastic building sound.</summary>
        Plastic,
        /// <summary>Cardboard building sound.</summary>
        Cardboard
    }
}
