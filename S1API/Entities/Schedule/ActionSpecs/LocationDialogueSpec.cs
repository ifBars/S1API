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
    /// Specifies an action that makes an NPC walk to a destination and start a location-based dialogue or interaction.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCEvent_LocationDialogue"/> that will
    /// make the NPC walk to the specified destination and then trigger a dialogue or interaction
    /// at that location. This is useful for creating NPCs that wait at specific locations
    /// for player interaction.
    /// </remarks>
    public sealed class LocationDialogueSpec : IScheduleActionSpec
    {
        /// <summary>
        /// Gets or sets the world position where the NPC should walk to.
        /// </summary>
        /// <value>The destination coordinates in world space.</value>
        public Vector3 Destination { get; set; }
        
        /// <summary>
        /// Gets or sets the optional forward direction for the destination marker.
        /// </summary>
        /// <value>The forward direction vector, or <c>null</c> to auto-calculate from NPC position.</value>
        /// <remarks>
        /// If specified, this vector will be used to orient the destination marker.
        /// If not specified, the direction will be calculated from the NPC's current position
        /// to the destination.
        /// </remarks>
        public Vector3? Forward { get; set; }
        
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
        /// Gets or sets the greeting override index to enable when the NPC reaches the destination.
        /// </summary>
        /// <value>The greeting override index, or -1 to disable. Default is -1.</value>
        /// <remarks>
        /// This allows the NPC to use a specific greeting override when the player interacts
        /// with them at this location. Set to -1 to use the default greeting behavior.
        /// </remarks>
        public int GreetingOverrideToEnable { get; set; } = -1;
        
        /// <summary>
        /// Gets or sets the choice index to enable when the NPC reaches the destination.
        /// </summary>
        /// <value>The choice index, or -1 to disable. Default is -1.</value>
        /// <remarks>
        /// This allows the NPC to present a specific dialogue choice when the player interacts
        /// with them at this location. Set to -1 to use the default choice behavior.
        /// </remarks>
        public int ChoiceToEnable { get; set; } = -1;
        
        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        /// <value>The action name, or <c>null</c> to use the default name "LocationDialogue".</value>
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_LocationDialogue>(StartTime, string.IsNullOrEmpty(Name) ? "LocationDialogue" : Name);
            if (action == null)
                return;

            action.Destination = CreateMarker(schedule, action.transform, Destination, Forward);
            action.FaceDestinationDir = FaceDestinationDirection;
            action.DestinationThreshold = Mathf.Max(0.01f, Within);
            action.WarpIfSkipped = WarpIfSkipped;
            action.GreetingOverrideToEnable = GreetingOverrideToEnable;
            action.ChoiceToEnable = ChoiceToEnable;
        }

        /// <summary>
        /// Creates a destination marker transform for the location dialogue action.
        /// </summary>
        /// <param name="schedule">The NPC schedule that owns this action.</param>
        /// <param name="parent">The parent transform to attach the marker to.</param>
        /// <param name="position">The world position for the marker.</param>
        /// <param name="forward">The optional forward direction for the marker.</param>
        /// <returns>A transform representing the destination marker.</returns>
        /// <remarks>
        /// This method creates a GameObject with a Transform that serves as the destination
        /// marker for the location dialogue action. If no forward direction is specified,
        /// it will be calculated from the NPC's current position to the destination.
        /// </remarks>
        internal static Transform CreateMarker(NPCSchedule schedule, Transform parent, Vector3 position, Vector3? forward)
        {
            var go = new GameObject("Marker");
            go.transform.position = position;
            if (forward.HasValue && forward.Value.sqrMagnitude > 0.001f)
                go.transform.forward = forward.Value.normalized;
            else
            {
                var look = schedule.NPC.gameObject.transform.position;
                var dir = (position - look);
                if (dir.sqrMagnitude > 0.001f)
                    go.transform.forward = dir.normalized;
            }
            return go.transform;
        }
    }
}
