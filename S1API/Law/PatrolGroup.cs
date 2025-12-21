#if (IL2CPPMELON)
using S1NPCBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCBehaviour = ScheduleOne.NPCs.Behaviour;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Law
{
    /// <summary>
    /// Represents a group of police officers on foot patrol.
    /// Manages patrol group movement, waypoints, and member coordination.
    /// </summary>
    public sealed class PatrolGroup
    {
        internal readonly S1NPCBehaviour.PatrolGroup S1PatrolGroup;

        internal PatrolGroup(S1NPCBehaviour.PatrolGroup s1PatrolGroup)
        {
            S1PatrolGroup = s1PatrolGroup;
        }

        /// <summary>
        /// Gets the number of officers in this patrol group.
        /// </summary>
        public int MemberCount =>
            S1PatrolGroup?.Members.Count ?? 0;

        /// <summary>
        /// Gets the current waypoint index the patrol group is moving toward.
        /// </summary>
        public int CurrentWaypoint =>
            S1PatrolGroup?.CurrentWaypoint ?? 0;

        /// <summary>
        /// Gets the patrol route this group is following.
        /// </summary>
        public FootPatrolRoute Route
        {
            get
            {
                if (S1PatrolGroup?.Route == null) return null;
                return new FootPatrolRoute(S1PatrolGroup.Route);
            }
        }

        /// <summary>
        /// Disbands the patrol group, releasing all officers back to their normal duties.
        /// </summary>
        public void DisbandGroup()
        {
            S1PatrolGroup?.DisbandGroup();
        }

        /// <summary>
        /// Advances the patrol group to the next waypoint.
        /// </summary>
        public void AdvanceGroup()
        {
            S1PatrolGroup?.AdvanceGroup();
        }

        /// <summary>
        /// Checks if all members of the patrol group are ready to advance to the next waypoint.
        /// </summary>
        /// <returns>True if all members are ready to advance.</returns>
        public bool IsGroupReadyToAdvance()
        {
            return S1PatrolGroup?.IsGroupReadyToAdvance() ?? false;
        }

        /// <summary>
        /// Checks if the patrol group is currently paused.
        /// </summary>
        /// <returns>True if the patrol is paused.</returns>
        public bool IsPaused()
        {
            return S1PatrolGroup?.IsPaused() ?? true;
        }
    }
}

