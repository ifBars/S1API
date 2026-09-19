#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif
using System;
using S1API.Items.Buildable;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Maps the public furniture sound choices without changing the legacy buildable builder's ordinal behavior.
    /// </summary>
    internal static class FurnitureBuildSoundMapper
    {
        internal const BuildSoundType Default = BuildSoundType.Wood;

        internal static S1ItemFramework.BuildableItemDefinition.EBuildSoundType ToNative(
            BuildSoundType soundType)
        {
            return soundType switch
            {
                BuildSoundType.Wood => S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Wood,
                BuildSoundType.Metal => S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Metal,
                BuildSoundType.Plastic => S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Metal,
                BuildSoundType.Cardboard => S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Cardboard,
                _ => throw new ArgumentOutOfRangeException(nameof(soundType)),
            };
        }

        internal static BuildSoundType FromNative(
            S1ItemFramework.BuildableItemDefinition.EBuildSoundType soundType)
        {
            return soundType switch
            {
                S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Cardboard => BuildSoundType.Cardboard,
                S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Wood => BuildSoundType.Wood,
                S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Metal => BuildSoundType.Metal,
                _ => throw new ArgumentOutOfRangeException(nameof(soundType)),
            };
        }
    }
}
