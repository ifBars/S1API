#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
#endif

using System;
using System.Linq;
using UnityEngine;

namespace S1API.DeadDrops
{
    /// <summary>
    /// Provides access to dead drops present in the scene.
    /// </summary>
    public static class DeadDropManager
    {
        /// <summary>
        /// All dead drops currently registered in the scene.
        /// </summary>
        public static DeadDropInstance[] All =>
            S1Economy.DeadDrop.DeadDrops.ToArray()
                .Select(deadDrop => new DeadDropInstance(deadDrop))
                .ToArray();

        /// <summary>
        /// All dead drops that contain no items.
        /// </summary>
        public static DeadDropInstance[] Empty =>
            All.Where(d => d.IsEmpty).ToArray();

        /// <summary>
        /// Gets a dead drop by its GUID.
        /// </summary>
        /// <param name="guid">The GUID string to look up.</param>
        /// <returns>The dead drop instance if found; otherwise null.</returns>
        public static DeadDropInstance? GetByGUID(string guid) =>
            All.FirstOrDefault(d => string.Equals(d.GUID, guid, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the closest dead drop to a world position.
        /// </summary>
        /// <param name="origin">World position to measure from.</param>
        /// <param name="mustBeEmpty">If true, only considers empty dead drops.</param>
        /// <returns>The closest matching dead drop, or null if none exist.</returns>
        public static DeadDropInstance? GetClosest(Vector3 origin, bool mustBeEmpty = false)
        {
            var source = mustBeEmpty ? Empty : All;
            return source
                .OrderBy(d => Vector3.Distance(d.Position, origin))
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets a random empty dead drop near a world position.
        /// Applies a light bias: avoids the absolute nearest, then chooses randomly among closer half.
        /// </summary>
        /// <param name="origin">World position to bias selection around.</param>
        /// <returns>A random nearby empty dead drop, or null if none exist.</returns>
        public static DeadDropInstance? GetRandomEmptyNear(Vector3 origin)
        {
            var candidates = Empty
                .OrderBy(d => Vector3.Distance(d.Position, origin))
                .ToList();

            if (candidates.Count == 0)
                return null;

            // Avoid the absolute closest to add variety if possible.
            if (candidates.Count > 1)
                candidates.RemoveAt(0);

            // Keep the closer half to bias selection.
            if (candidates.Count > 1)
                candidates.RemoveRange(candidates.Count / 2, candidates.Count / 2);

            if (candidates.Count == 0)
                return null;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }
}

