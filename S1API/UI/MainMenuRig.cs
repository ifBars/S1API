#if (IL2CPPMELON)
using S1UIMainMenu = Il2CppScheduleOne.UI.MainMenu;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1UIMainMenu = ScheduleOne.UI.MainMenu;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

using System.Linq;
using S1API.Avatar;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.UI
{
    /// <summary>
    /// Modder-facing wrapper for the MainMenuRig component in the main menu scene.
    /// Provides access to the menu avatar and related UI components without exposing game types.
    /// </summary>
    public sealed class MainMenuRig
    {
        /// <summary>
        /// INTERNAL: Reference to the game MainMenuRig instance.
        /// </summary>
        internal readonly S1UIMainMenu.MainMenuRig S1MainMenuRig;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game MainMenuRig instance.
        /// </summary>
        /// <param name="mainMenuRig">The game MainMenuRig instance to wrap.</param>
        internal MainMenuRig(S1UIMainMenu.MainMenuRig mainMenuRig)
        {
            S1MainMenuRig = mainMenuRig;
        }

        /// <summary>
        /// The avatar displayed in the main menu.
        /// </summary>
        public Avatar.Avatar? Avatar
        {
            get
            {
                if (S1MainMenuRig == null || S1MainMenuRig.Avatar == null)
                    return null;

                return new Avatar.Avatar(S1MainMenuRig.Avatar);
            }
        }

        /// <summary>
        /// Finds all MainMenuRig instances in the current scene.
        /// </summary>
        /// <param name="includeInactive">Whether to include inactive GameObjects in the search.</param>
        /// <returns>An array of MainMenuRig wrappers found in the scene.</returns>
        public static MainMenuRig[] FindInScene(bool includeInactive = false)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var rigs = Object.FindObjectsOfType<S1UIMainMenu.MainMenuRig>(includeInactive);
#elif (MONOMELON || MONOBEPINEX)
            var rigs = Object.FindObjectsOfType<S1UIMainMenu.MainMenuRig>(includeInactive);
#else
            var rigs = System.Array.Empty<S1UIMainMenu.MainMenuRig>();
#endif
            if (rigs == null || rigs.Length == 0)
                return System.Array.Empty<MainMenuRig>();

            return rigs
                .Where(r => r != null)
                .Select(r => new MainMenuRig(r))
                .ToArray();
        }
    }
}

