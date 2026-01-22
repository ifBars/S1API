#if (IL2CPPMELON)
using S1Graffiti = Il2CppScheduleOne.Graffiti;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Graffiti = ScheduleOne.Graffiti;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Graffiti
{
    /// <summary>
    /// Provides access to graffiti-related game systems and spray surfaces.
    /// </summary>
    public static class GraffitiManager
    {
        /// <summary>
        /// Gets the in-game GraffitiManager singleton instance.
        /// </summary>
        private static S1Graffiti.GraffitiManager Instance
        {
            get
            {
#if (IL2CPPMELON)
                return S1DevUtilities.NetworkSingleton<S1Graffiti.GraffitiManager>.Instance;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
                return S1DevUtilities.NetworkSingleton<S1Graffiti.GraffitiManager>.Instance;
#endif
            }
        }

        /// <summary>
        /// Gets all world spray surfaces in the game.
        /// </summary>
        /// <returns>A list of all world spray surfaces, wrapped in S1API SpraySurface objects.</returns>
        public static List<SpraySurface> GetAllSpraySurfaces()
        {
            var instance = Instance;
            if (instance == null)
                return new List<SpraySurface>();

            var result = new List<SpraySurface>();

#if (IL2CPPMELON)
            // IL2CPP: Access WorldSpraySurfaces directly as Il2CppSystem.Collections.Generic.List
            if (instance.WorldSpraySurfaces == null)
                return result;

            for (int i = 0; i < instance.WorldSpraySurfaces.Count; i++)
            {
                var surface = instance.WorldSpraySurfaces[i];
                if (surface != null)
                {
                    result.Add(new SpraySurface(surface));
                }
            }
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
            // Mono: Access WorldSpraySurfaces and iterate directly
            if (instance.WorldSpraySurfaces == null)
                return result;

            foreach (var surface in instance.WorldSpraySurfaces)
            {
                if (surface != null)
                {
                    result.Add(new SpraySurface(surface));
                }
            }
#endif

            return result;
        }

        /// <summary>
        /// Gets all spray surfaces that have not been drawn on yet (no strokes).
        /// </summary>
        public static List<SpraySurface> UntaggedSpraySurfaces =>
            GetAllSpraySurfaces()
                .Where(surface => surface.StrokeCount == 0 && !surface.HasDrawingBeenFinalized)
                .ToList();

        /// <summary>
        /// Gets all spray surfaces that have not been drawn on yet (no strokes).
        /// </summary>
        /// <returns>A list of untagged spray surfaces.</returns>
        public static List<SpraySurface> GetUntaggedSpraySurfaces()
        {
            return UntaggedSpraySurfaces;
        }

        /// <summary>
        /// Finds the nearest untagged spray surface to a given position.
        /// </summary>
        /// <param name="position">The position to search from.</param>
        /// <returns>The nearest untagged spray surface, or null if none found.</returns>
        public static SpraySurface? FindNearestUntaggedSurface(Vector3 position)
        {
            var untaggedSurfaces = GetUntaggedSpraySurfaces();
            if (untaggedSurfaces.Count == 0)
                return null;

            SpraySurface? nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var surface in untaggedSurfaces)
            {
                float distance = Vector3.Distance(position, surface.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = surface;
                }
            }

            return nearest;
        }
    }
}

