#if (IL2CPPMELON)
using S1NPCBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCBehaviour = ScheduleOne.NPCs.Behaviour;
#endif

using UnityEngine;

namespace S1API.Law
{
    /// <summary>
    /// Represents a foot patrol route with waypoints for police officers to follow.
    /// </summary>
    public sealed class FootPatrolRoute
    {
        internal readonly S1NPCBehaviour.FootPatrolRoute S1Route;

        internal FootPatrolRoute(S1NPCBehaviour.FootPatrolRoute s1Route)
        {
            S1Route = s1Route;
        }

        /// <summary>
        /// Gets the name of this patrol route.
        /// </summary>
        public string RouteName =>
            S1Route?.RouteName ?? string.Empty;

        /// <summary>
        /// Gets the number of waypoints in this route.
        /// </summary>
        public int WaypointCount =>
            S1Route?.Waypoints?.Length ?? 0;

        /// <summary>
        /// Gets the starting waypoint index for this route.
        /// </summary>
        public int StartWaypointIndex =>
            S1Route?.StartWaypointIndex ?? 0;

        /// <summary>
        /// Gets the position of a waypoint at the specified index.
        /// </summary>
        /// <param name="index">The waypoint index.</param>
        /// <returns>The waypoint position, or Vector3.zero if invalid index.</returns>
        public Vector3 GetWaypointPosition(int index)
        {
            if (S1Route?.Waypoints == null || index < 0 || index >= S1Route.Waypoints.Length)
                return Vector3.zero;
            return S1Route.Waypoints[index].position;
        }

        /// <summary>
        /// Gets the position of the route object itself.
        /// </summary>
        public Vector3 Position =>
            S1Route?.transform.position ?? Vector3.zero;
    }
}

