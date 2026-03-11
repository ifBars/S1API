#if (IL2CPPMELON)
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1NPCsOther = Il2CppScheduleOne.NPCs.Other;
using S1InstanceFinder = Il2CppFishNet.InstanceFinder;
using S1Graffiti = Il2CppScheduleOne.Graffiti;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1NPCsBehaviour = ScheduleOne.NPCs.Behaviour;
using S1NPCsOther = ScheduleOne.NPCs.Other;
using S1InstanceFinder = FishNet.InstanceFinder;
using S1Graffiti = ScheduleOne.Graffiti;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif
using S1API.Graffiti;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using S1API.Internal.Abstraction;
using S1API.Map;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Defines the behavior to trigger when a location-based action starts.
    /// </summary>
    public enum LocationArriveBehaviour
    {
        None = 0,
        SmokeBreak = 1,
        Graffiti = 2,
        Drinking = 3,
        HoldItem = 4
    }

    /// <summary>
    /// Specifies an action that makes an NPC walk to a destination and perform a configured action while there.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCEvent_LocationBasedAction"/>. When the NPC arrives,
    /// the selected <see cref="ArriveBehaviour"/> is started, then stopped when the schedule event ends.
    /// </remarks>
    public sealed class LocationBasedActionSpec : IScheduleActionSpec
    {
        private static readonly Log Logger = new Log("LocationBasedActionSpec");

        /// <summary>
        /// Gets or sets the world position where the NPC should walk to.
        /// </summary>
        public Vector3 Destination { get; set; }

        /// <summary>
        /// Gets or sets the time when this action should start, in minutes from midnight.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Gets or sets the duration in minutes for this action.
        /// </summary>
        public int DurationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets whether the NPC should face the destination direction when walking.
        /// </summary>
        public bool FaceDestinationDirection { get; set; } = true;

        /// <summary>
        /// Gets or sets the distance threshold within which the NPC is considered to have arrived.
        /// </summary>
        public float Within { get; set; } = 1f;

        /// <summary>
        /// Gets or sets whether the NPC should be warped to the destination if the action is skipped.
        /// </summary>
        public bool WarpIfSkipped { get; set; } = false;

        /// <summary>
        /// Gets or sets the arrive behavior to start when the NPC reaches the destination.
        /// </summary>
        public LocationArriveBehaviour ArriveBehaviour { get; set; } = LocationArriveBehaviour.None;

        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// For Graffiti: optional region to pick a spray surface from. If null, nearest to destination is used.
        /// </summary>
        public Region? GraffitiRegion { get; set; }

        /// <summary>
        /// For Graffiti: optional GUID of a specific spray surface. Takes precedence over GraffitiRegion.
        /// </summary>
        public System.Guid? GraffitiSurfaceGuid { get; set; }

        /// <summary>
        /// For HoldItem: Resources path to the AvatarEquippable prefab (e.g. "Avatar/Equippables/Phone_Lowered").
        /// If null, uses the prefab's configured HoldItem equippable.
        /// </summary>
        public string EquippableAssetPath { get; set; }

        /// <summary>
        /// For Drinking: Resources path to the drink AvatarEquippable prefab (e.g. "Avatar/Equippables/Beer").
        /// If null, uses the prefab's configured DrinkItem.
        /// </summary>
        public string DrinkEquippablePath { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_LocationBasedAction>(
                StartTime,
                string.IsNullOrEmpty(Name) ? "LocationBasedAction" : Name);
            if (action == null)
                return;

            Vector3 markerPosition = Destination;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(Destination, out navHit, 5f, NavMesh.AllAreas))
            {
                markerPosition = new Vector3(Destination.x, navHit.position.y, Destination.z);
            }

            var destinationTransform = NPCDestinationContainer.CreateDestinationMarker(
                schedule.NPC.gameObject.name,
                "Marker",
                markerPosition);

            if (destinationTransform == null)
                return;

            action.Destination = destinationTransform;
            action.FaceDestinationDir = FaceDestinationDirection;
            action.DestinationThreshold = Mathf.Max(0.01f, Within);
            action.WarpIfSkipped = WarpIfSkipped;
            action.Duration = Mathf.Max(1, DurationMinutes);

            if (action.onStartAction == null)
                action.onStartAction = new UnityEvent();
            if (action.onEndAction == null)
                action.onEndAction = new UnityEvent();

            switch (ArriveBehaviour)
            {
                case LocationArriveBehaviour.SmokeBreak:
                    EventHelper.AddListener(() => ToggleSmoking(schedule, true), action.onStartAction);
                    EventHelper.AddListener(() => ToggleSmoking(schedule, false), action.onEndAction);
                    break;
                case LocationArriveBehaviour.Graffiti:
                    var destPos = destinationTransform != null ? destinationTransform.position : Destination;
                    EventHelper.AddListener(() => ToggleGraffiti(this, schedule, destPos, true), action.onStartAction);
                    EventHelper.AddListener(() => ToggleGraffiti(this, schedule, default, false), action.onEndAction);
                    break;
                case LocationArriveBehaviour.Drinking:
                    EventHelper.AddListener(() => ToggleDrinking(this, schedule, true), action.onStartAction);
                    EventHelper.AddListener(() => ToggleDrinking(this, schedule, false), action.onEndAction);
                    break;
                case LocationArriveBehaviour.HoldItem:
                    EventHelper.AddListener(() => ToggleItemHolding(this, schedule, true), action.onStartAction);
                    EventHelper.AddListener(() => ToggleItemHolding(this, schedule, false), action.onEndAction);
                    break;
                case LocationArriveBehaviour.None:
                default:
                    break;
            }
        }

        private static bool IsServer()
        {
            try
            {
                return S1InstanceFinder.IsServer;
            }
            catch
            {
                return false;
            }
        }

        private static bool ToggleBehaviourByName(NPCSchedule schedule, string behaviourName, bool enabled)
        {
            if (!IsServer() || schedule?.NPC?.S1NPC == null || string.IsNullOrEmpty(behaviourName))
                return false;

            var baseNpc = schedule.NPC.S1NPC;
            var npcBehaviour = baseNpc.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
            if (npcBehaviour == null)
                return false;

            var behaviour = npcBehaviour.GetBehaviour(behaviourName);
            if (behaviour == null)
                return false;

            ReflectionUtils.TrySetFieldOrProperty(npcBehaviour, "Npc", baseNpc);
            if (ReflectionUtils.TryGetFieldOrProperty(behaviour, "beh") == null)
                ReflectionUtils.TrySetFieldOrProperty(behaviour, "beh", npcBehaviour);

            var indexObj = ReflectionUtils.TryGetFieldOrProperty(behaviour, "BehaviourIndex");
            int index = indexObj is int i ? i : (indexObj != null && int.TryParse(indexObj.ToString(), out var parsed) ? parsed : -1);

            if (enabled)
            {
                if (behaviour.gameObject != null && !behaviour.gameObject.activeSelf)
                    behaviour.gameObject.SetActive(true);
                behaviour.Enable_Networked();
                if (index >= 0)
                    npcBehaviour.ActivateBehaviour_Server(index);
            }
            else
            {
                behaviour.Disable_Networked(null);
            }

            return true;
        }

        private static void ToggleSmoking(NPCSchedule schedule, bool enabled)
        {
            if (!IsServer())
                return;

            if (schedule?.NPC?.S1NPC == null)
                return;

            // Prefer the full SmokeBreakBehaviour path so modders can use its debug logs.
            if (ToggleBehaviourByName(schedule, "SmokeBreakBehaviour", enabled))
                return;

            // Fallback: use API action wrapper (which resolves SmokeCigarette internally).
            var smoking = schedule.NPC.Smoking;
            if (smoking == null)
                return;

            if (enabled)
                smoking.Begin();
            else
                smoking.End();
        }

        private static void ToggleGraffiti(LocationBasedActionSpec spec, NPCSchedule schedule, Vector3 destinationPosition, bool enabled)
        {
            if (!IsServer() || schedule?.NPC?.S1NPC == null)
                return;

            if (!enabled)
            {
                ToggleBehaviourByName(schedule, "GraffitiBehaviour", false);
                return;
            }

            S1Graffiti.WorldSpraySurface surface = null;
            if (spec.GraffitiSurfaceGuid.HasValue)
                surface = GraffitiManager.FindSurfaceByGuid(spec.GraffitiSurfaceGuid.Value);
            if (surface == null && spec.GraffitiRegion.HasValue)
            {
                var list = GraffitiManager.FindAvailableForNPCInRegion(spec.GraffitiRegion.Value);
                if (list.Count > 0)
                    surface = list[UnityEngine.Random.Range(0, list.Count)];
            }
            if (surface == null)
                surface = GraffitiManager.FindNearestAvailableForNPC(destinationPosition);

            if (surface == null)
            {
                Logger.Warning("[LocationBasedActionSpec] Graffiti: No spray surface found (guid/region/nearest).");
                return;
            }

            if (surface.NetworkObject == null)
            {
                Logger.Warning("[LocationBasedActionSpec] Graffiti: Spray surface has no NetworkObject, cannot assign behaviour.");
                return;
            }

            var baseNpc = schedule.NPC.S1NPC;
            var npcBehaviour = baseNpc.GetComponentInChildren<S1NPCsBehaviour.NPCBehaviour>(true);
            var behaviour = npcBehaviour?.GetBehaviour("GraffitiBehaviour");
            if (behaviour == null)
            {
                Logger.Warning("[LocationBasedActionSpec] Graffiti: GraffitiBehaviour not found.");
                return;
            }

            var graffitiBehaviour = behaviour as S1NPCsBehaviour.GraffitiBehaviour;
            if (graffitiBehaviour == null)
            {
                Logger.Warning("[LocationBasedActionSpec] Graffiti: GraffitiBehaviour could not be cast to the concrete type.");
                return;
            }

            graffitiBehaviour.SetSpraySurface_Client(null, surface.NetworkObject);

            ToggleBehaviourByName(schedule, "GraffitiBehaviour", true);
        }

        private static void ToggleDrinking(LocationBasedActionSpec spec, NPCSchedule schedule, bool enabled)
        {
            if (!IsServer() || schedule?.NPC?.S1NPC == null)
                return;

            var drink = schedule.NPC.S1NPC.GetComponentInChildren<S1NPCsOther.DrinkItem>(true);
            if (drink == null)
                return;

            if (enabled && !string.IsNullOrEmpty(spec.DrinkEquippablePath))
            {
                var prefab = Resources.Load<GameObject>(spec.DrinkEquippablePath);
                var equippable = prefab?.GetComponent<S1AvatarFramework.Equipping.AvatarEquippable>()
                    ?? prefab?.GetComponentInChildren<S1AvatarFramework.Equipping.AvatarEquippable>(true);
                if (equippable != null)
                    ReflectionUtils.TrySetFieldOrProperty(drink, "DrinkPrefab", equippable);
            }

            if (enabled)
                drink.Begin();
            else
                drink.End();
        }

        private static void ToggleItemHolding(LocationBasedActionSpec spec, NPCSchedule schedule, bool enabled)
        {
            if (!IsServer() || schedule?.NPC?.S1NPC == null)
                return;

            var holdItem = schedule.NPC.S1NPC.GetComponentInChildren<S1NPCsOther.HoldItem>(true);
            if (holdItem == null)
                return;

            if (enabled && !string.IsNullOrEmpty(spec.EquippableAssetPath))
            {
                var prefab = Resources.Load<GameObject>(spec.EquippableAssetPath);
                var equippable = prefab?.GetComponent<S1AvatarFramework.Equipping.AvatarEquippable>()
                    ?? prefab?.GetComponentInChildren<S1AvatarFramework.Equipping.AvatarEquippable>(true);
                if (equippable != null)
                    ReflectionUtils.TrySetFieldOrProperty(holdItem, "Equippable", equippable);
            }

            if (enabled)
                holdItem.Begin();
            else
                holdItem.End();
        }
    }
}
