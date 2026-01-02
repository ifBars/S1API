#if (IL2CPPMELON)
using S1Cartel = Il2CppScheduleOne.Cartel;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Cartel = ScheduleOne.Cartel;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Cartel
{
    /// <summary>
    /// Manages the spawning and tracking of cartel goons.
    /// Provides access to the game's goon pool for spawning hostile NPCs.
    /// </summary>
    public class GoonManager
    {
        /// <summary>
        /// INTERNAL: Reference to the game's GoonPool instance.
        /// </summary>
        internal readonly S1Cartel.GoonPool S1GoonPool;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game GoonPool instance.
        /// </summary>
        internal GoonManager(S1Cartel.GoonPool goonPool)
        {
            S1GoonPool = goonPool;
        }

        /// <summary>
        /// The number of goons available to spawn from the pool.
        /// </summary>
        public int AvailableGoonCount => S1GoonPool.UnspawnedGoonCount;

        /// <summary>
        /// Spawns a single goon at the specified position.
        /// </summary>
        /// <param name="position">The world position to spawn the goon at.</param>
        /// <returns>The spawned goon, or null if no goons are available.</returns>
        public CartelGoon? SpawnGoon(Vector3 position)
        {
            if (AvailableGoonCount == 0)
                return null;

            var goons = S1GoonPool.SpawnMultipleGoons(position, 1);
            if (goons == null || goons.Count == 0)
                return null;

            return new CartelGoon(goons[0]);
        }

        /// <summary>
        /// Spawns multiple goons at the specified position.
        /// </summary>
        /// <param name="position">The world position to spawn the goons at.</param>
        /// <param name="count">The number of goons to spawn.</param>
        /// <returns>A list of spawned goons. May contain fewer than requested if pool is depleted.</returns>
        public List<CartelGoon> SpawnGoons(Vector3 position, int count)
        {
            var result = new List<CartelGoon>();

            if (count <= 0 || AvailableGoonCount == 0)
                return result;

            int toSpawn = System.Math.Min(count, AvailableGoonCount);
            var goons = S1GoonPool.SpawnMultipleGoons(position, toSpawn);

            if (goons == null)
                return result;

#if (IL2CPPMELON)
            foreach (var goon in goons)
            {
                if (goon != null)
                    result.Add(new CartelGoon(goon));
            }
#else
            foreach (var goon in goons)
            {
                if (goon != null)
                    result.Add(new CartelGoon(goon));
            }
#endif

            return result;
        }

        /// <summary>
        /// Spawns goons at multiple positions, one goon per position.
        /// </summary>
        /// <param name="positions">The positions to spawn goons at.</param>
        /// <returns>A list of spawned goons with their positions set.</returns>
        public List<CartelGoon> SpawnGoonsAtPositions(Vector3[] positions)
        {
            var result = new List<CartelGoon>();

            if (positions == null || positions.Length == 0)
                return result;

            // Spawn all at first position, then warp to individual positions
            var goons = SpawnGoons(positions[0], positions.Length);

            for (int i = 0; i < goons.Count && i < positions.Length; i++)
            {
                goons[i].WarpTo(positions[i]);
                result.Add(goons[i]);
            }

            return result;
        }
    }
}
