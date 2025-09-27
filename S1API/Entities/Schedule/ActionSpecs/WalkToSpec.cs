#if (IL2CPPMELON)
using Il2Cpp;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Map = Il2CppScheduleOne.Map;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1VehiclesAI = Il2CppScheduleOne.Vehicles.AI;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Map = ScheduleOne.Map;
using S1Vehicles = ScheduleOne.Vehicles;
using S1VehiclesAI = ScheduleOne.Vehicles.AI;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
#endif
using UnityEngine;
using S1API.Map;
using S1API.Vehicles;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies a walk-to action that moves an NPC to a specific world position at a scheduled time.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCSignal_WalkToLocation"/> that will
    /// make the NPC walk to the specified destination. The NPC will face the destination
    /// direction by default and will be considered to have arrived when within the specified
    /// threshold distance.
    /// </remarks>
    public sealed class WalkToSpec : IScheduleActionSpec
    {
        /// <summary>
        /// Gets or sets the world position where the NPC should walk to.
        /// </summary>
        /// <value>The destination coordinates in world space.</value>
        public Vector3 Destination { get; set; }
        
        /// <summary>
        /// Gets or sets the time when this action should start, in minutes from midnight.
        /// </summary>
        /// <value>The start time in minutes (0-1439 for a 24-hour day).</value>
        public int StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets whether the NPC should face the destination direction when walking.
        /// </summary>
        /// <value><c>true</c> to make the NPC face the destination; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool FaceDestinationDirection { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the distance threshold within which the NPC is considered to have arrived.
        /// </summary>
        /// <value>The arrival threshold in world units. Default is 1.0f.</value>
        /// <remarks>
        /// The NPC will stop walking and consider the action complete when they are within
        /// this distance of the destination. Must be greater than 0.01f.
        /// </remarks>
        public float Within { get; set; } = 1f;
        
        /// <summary>
        /// Gets or sets whether the NPC should be warped to the destination if the action is skipped.
        /// </summary>
        /// <value><c>true</c> to warp if skipped; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        /// <remarks>
        /// If the action is skipped (e.g., due to time jumps or schedule changes), setting this
        /// to <c>true</c> will teleport the NPC directly to the destination.
        /// </remarks>
        public bool WarpIfSkipped { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        /// <value>The action name, or <c>null</c> to use the default name "WalkTo".</value>
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_WalkToLocation>(StartTime, string.IsNullOrEmpty(Name) ? "WalkTo" : Name);
            if (action == null)
                return;

            // Calculate forward direction towards current NPC position
            var look = schedule.NPC.gameObject.transform.position;
            var forward = (Destination - look);
            Vector3? forwardDirection = forward.sqrMagnitude > 0.001f ? forward.normalized : null;

            // Create destination marker in NPC's dedicated container
            var destinationTransform = NPCDestinationContainer.CreateDestinationMarker(
                schedule.NPC.gameObject.name, 
                "Destination", 
                Destination, 
                forwardDirection);

            if (destinationTransform != null)
            {
                action.Destination = destinationTransform;
                action.FaceDestinationDir = FaceDestinationDirection;
                action.DestinationThreshold = Mathf.Max(0.01f, Within);
                action.WarpIfSkipped = WarpIfSkipped;
            }
        }
    }
}
