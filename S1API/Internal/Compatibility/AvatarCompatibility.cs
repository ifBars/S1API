#if IL2CPPMELON
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

namespace S1API.Internal.Compatibility
{
    internal static class AvatarCompatibility
    {
        internal static void ApplyLegacySettings(
            S1AvatarFramework.Avatar avatar,
            S1AvatarFramework.AvatarSettings settings)
        {
            if (avatar?.Appearance == null || settings == null)
                return;

            var nakedAppearance = settings.EquivalentNakedAppearance;
            if (nakedAppearance != null)
                avatar.Appearance.ApplyNakedAppearance(nakedAppearance.Appearance);

            var outfit = settings.EquivalentOutfit;
            if (outfit != null)
                avatar.Appearance.ApplyOutfit(outfit);
        }
    }
}
