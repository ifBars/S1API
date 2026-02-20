#if (IL2CPPMELON)
using S1Graffiti = Il2CppScheduleOne.Graffiti;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Map = Il2CppScheduleOne.Map;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Graffiti = ScheduleOne.Graffiti;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Map = ScheduleOne.Map;
#endif

using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Utils;
using S1API.Map;
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

        /// <summary>
        /// Finds a spray surface by GUID. Returns the raw game surface for use with GraffitiBehaviour.SetSpraySurface_Client.
        /// </summary>
        /// <param name="guid">The surface GUID.</param>
        /// <returns>The raw WorldSpraySurface, or null if not found.</returns>
        internal static S1Graffiti.WorldSpraySurface FindSurfaceByGuid(System.Guid guid)
        {
            var instance = Instance;
            if (instance?.WorldSpraySurfaces == null)
                return null;

#if (IL2CPPMELON)
            for (int i = 0; i < instance.WorldSpraySurfaces.Count; i++)
            {
                var s = instance.WorldSpraySurfaces[i];
                if (s != null && s.GUID.ToString() == guid.ToString())
                    return s;
            }
#else
            foreach (var s in instance.WorldSpraySurfaces)
            {
                if (s != null && s.GUID.ToString() == guid.ToString())
                    return s;
            }
#endif
            return null;
        }

        /// <summary>
        /// Finds spray surfaces available for NPCs in the given region (CanBeSprayedByNPCs, CanBeEdited).
        /// </summary>
        /// <param name="region">The map region.</param>
        /// <returns>List of available raw WorldSpraySurface instances.</returns>
        internal static List<S1Graffiti.WorldSpraySurface> FindAvailableForNPCInRegion(Region region)
        {
            var instance = Instance;
            var result = new List<S1Graffiti.WorldSpraySurface>();
            if (instance?.WorldSpraySurfaces == null)
                return result;

            var gameRegion = (S1Map.EMapRegion)(int)region;

#if (IL2CPPMELON)
            for (int i = 0; i < instance.WorldSpraySurfaces.Count; i++)
            {
                var s = instance.WorldSpraySurfaces[i];
                if (s != null && s.Region == gameRegion && s.CanBeSprayedByNPCs && s.CanBeEdited(checkEditor: true))
                    result.Add(s);
            }
#else
            foreach (var s in instance.WorldSpraySurfaces)
            {
                if (s != null && s.Region == gameRegion && s.CanBeSprayedByNPCs && s.CanBeEdited(checkEditor: true))
                    result.Add(s);
            }
#endif
            return result;
        }

        /// <summary>
        /// Finds the nearest spray surface available for NPCs to the given position.
        /// </summary>
        /// <param name="position">Search from this position.</param>
        /// <returns>The nearest available raw WorldSpraySurface, or null.</returns>
        internal static S1Graffiti.WorldSpraySurface FindNearestAvailableForNPC(Vector3 position)
        {
            var instance = Instance;
            if (instance?.WorldSpraySurfaces == null)
                return null;

            S1Graffiti.WorldSpraySurface nearest = null;
            float nearestDist = float.MaxValue;

#if (IL2CPPMELON)
            for (int i = 0; i < instance.WorldSpraySurfaces.Count; i++)
            {
                var s = instance.WorldSpraySurfaces[i];
                if (s == null || !s.CanBeSprayedByNPCs || !s.CanBeEdited(checkEditor: true))
                    continue;
                var center = s.CenterPoint;
                float d = Vector3.Distance(position, center);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = s;
                }
            }
#else
            foreach (var s in instance.WorldSpraySurfaces)
            {
                if (s == null || !s.CanBeSprayedByNPCs || !s.CanBeEdited(checkEditor: true))
                    continue;
                var center = s.CenterPoint;
                float d = Vector3.Distance(position, center);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = s;
                }
            }
#endif
            return nearest;
        }
    }
}

