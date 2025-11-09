#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Avatar
{
    /// <summary>
    /// Modder-facing wrapper for an Avatar component in the game.
    /// Provides access to avatar appearance and state without exposing game types.
    /// </summary>
    public sealed class Avatar
    {
        /// <summary>
        /// INTERNAL: Reference to the game Avatar instance.
        /// </summary>
        internal readonly S1AvatarFramework.Avatar S1Avatar;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game Avatar instance.
        /// </summary>
        /// <param name="avatar">The game Avatar instance to wrap.</param>
        internal Avatar(S1AvatarFramework.Avatar avatar)
        {
            S1Avatar = avatar;
        }

        /// <summary>
        /// The GameObject associated with this avatar.
        /// </summary>
        public GameObject GameObject =>
            S1Avatar?.gameObject;

        /// <summary>
        /// Whether the avatar GameObject is active.
        /// </summary>
        public bool IsActive =>
            S1Avatar != null && S1Avatar.gameObject != null && S1Avatar.gameObject.activeSelf;

        /// <summary>
        /// Loads avatar settings onto this avatar.
        /// </summary>
        /// <param name="settings">The avatar settings to load.</param>
        public void LoadAvatarSettings(AvatarSettings settings)
        {
            if (S1Avatar == null || settings?.S1AvatarSettings == null)
                return;

            S1Avatar.LoadAvatarSettings(settings.S1AvatarSettings);
        }

        /// <summary>
        /// Finds all Avatar instances in the current scene.
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive GameObjects in the search.</param>
        /// <returns>An array of Avatar wrappers found in the scene.</returns>
        public static Avatar[] FindInScene(bool includeInactive = false)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var avatars = Object.FindObjectsOfType<S1AvatarFramework.Avatar>(includeInactive);
#elif (MONOMELON || MONOBEPINEX)
            var avatars = Object.FindObjectsOfType<S1AvatarFramework.Avatar>(includeInactive);
#else
            var avatars = System.Array.Empty<S1AvatarFramework.Avatar>();
#endif
            if (avatars == null || avatars.Length == 0)
                return System.Array.Empty<Avatar>();

            return avatars
                .Where(a => a != null)
                .Select(a => new Avatar(a))
                .ToArray();
        }
    }
}

