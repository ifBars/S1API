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
    /// Go to a destination and start a location-based dialogue/action.
    /// </summary>
    public sealed class LocationDialogueSpec : IScheduleActionSpec
    {
        public Vector3 Destination { get; set; }
        public Vector3? Forward { get; set; }
        public int StartTime { get; set; }
        public bool FaceDestinationDirection { get; set; } = true;
        public float Within { get; set; } = 1f;
        public bool WarpIfSkipped { get; set; } = false;
        public int GreetingOverrideToEnable { get; set; } = -1;
        public int ChoiceToEnable { get; set; } = -1;
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
