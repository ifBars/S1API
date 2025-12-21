#if (IL2CPPMELON)
using S1Law = Il2CppScheduleOne.Law;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1NPCBehaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1Police = Il2CppScheduleOne.Police;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Law = ScheduleOne.Law;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1NPCBehaviour = ScheduleOne.NPCs.Behaviour;
using S1Police = ScheduleOne.Police;
#endif

using S1API.Entities;
using UnityEngine;

namespace S1API.Law
{
    /// <summary>
    /// Provides access to law enforcement and police dispatch functionality.
    /// Manages police responses, patrol operations, and wanted levels.
    /// </summary>
    public static class LawManager
    {
        /// <summary>
        /// INTERNAL: Provides access to the underlying law management singleton.
        /// </summary>
        private static S1Law.LawManager Internal => S1Law.LawManager.Instance;

        /// <summary>
        /// The default number of officers dispatched when police are called.
        /// </summary>
        public static int DispatchOfficerCount => 2;

        /// <summary>
        /// The distance threshold (in units) above which dispatched officers will use a vehicle.
        /// Below this threshold, officers will respond on foot.
        /// </summary>
        public static float DispatchVehicleUseThreshold => 25f;

        /// <summary>
        /// Search time in seconds for Investigating pursuit level.
        /// Police will search for this long after losing sight of the player.
        /// </summary>
        public static float SearchTimeInvestigating => 60f;

        /// <summary>
        /// Search time in seconds for Arresting pursuit level.
        /// Police will search for this long after losing sight of the player.
        /// </summary>
        public static float SearchTimeArresting => 25f;

        /// <summary>
        /// Search time in seconds for NonLethal pursuit level.
        /// Police will search for this long after losing sight of the player.
        /// </summary>
        public static float SearchTimeNonLethal => 30f;

        /// <summary>
        /// Search time in seconds for Lethal pursuit level.
        /// Police will search for this long after losing sight of the player.
        /// </summary>
        public static float SearchTimeLethal => 40f;

        /// <summary>
        /// Time in seconds before pursuit escalates from Arresting to NonLethal
        /// if the player remains visible to police.
        /// </summary>
        public static float EscalationTimeArresting => 25f;

        /// <summary>
        /// Time in seconds before pursuit escalates from NonLethal to Lethal
        /// if the player remains visible to police.
        /// </summary>
        public static float EscalationTimeNonLethal => 120f;

        /// <summary>
        /// Dispatches police officers to pursue the specified player for a crime.
        /// Officers will be sent from the nearest police station.
        /// </summary>
        /// <param name="target">The player who committed the crime and will be pursued.</param>
        /// <remarks>
        /// This method will not dispatch police during tutorial mode.
        /// The number of officers dispatched is determined by <see cref="DispatchOfficerCount"/>.
        /// Officers will use vehicles if the distance exceeds <see cref="DispatchVehicleUseThreshold"/>.
        /// </remarks>
        public static void CallPolice(Player target)
        {
            if (Internal == null || target == null) return;

            // Call with null crime parameter as the game manager handles crime internally
            Internal.PoliceCalled(target.S1Player, null);
        }

        /// <summary>
        /// Sets the wanted level (pursuit level) for the specified player.
        /// </summary>
        /// <param name="target">The player to set the wanted level for.</param>
        /// <param name="level">The pursuit level to set.</param>
        /// <remarks>
        /// Setting to <see cref="PursuitLevel.None"/> will clear all crimes and end the pursuit.
        /// This is a convenience method that calls <see cref="PlayerCrimeData.SetPursuitLevel"/>.
        /// </remarks>
        public static void SetWantedLevel(Player target, PursuitLevel level)
        {
            if (target == null) return;
            target.CrimeData.SetPursuitLevel(level);
        }

        /// <summary>
        /// Clears the wanted level for the specified player, ending any active pursuit.
        /// </summary>
        /// <param name="target">The player to clear the wanted level for.</param>
        /// <remarks>
        /// This sets the pursuit level to None and clears all crimes from the player's record.
        /// </remarks>
        public static void ClearWantedLevel(Player target)
        {
            if (target == null) return;
            target.CrimeData.SetPursuitLevel(PursuitLevel.None);
        }

        /// <summary>
        /// Gets the current wanted level (pursuit level) for the specified player.
        /// </summary>
        /// <param name="target">The player to get the wanted level for.</param>
        /// <returns>The player's current pursuit level.</returns>
        public static PursuitLevel GetWantedLevel(Player target)
        {
            if (target == null) return PursuitLevel.None;
            return target.CrimeData.CurrentPursuitLevel;
        }

        /// <summary>
        /// Increases the wanted level for the specified player by one level.
        /// </summary>
        /// <param name="target">The player to escalate the wanted level for.</param>
        /// <remarks>
        /// Progression: None → Investigating → Arresting → NonLethal → Lethal.
        /// This is a convenience method that calls <see cref="PlayerCrimeData.Escalate"/>.
        /// </remarks>
        public static void EscalateWantedLevel(Player target)
        {
            if (target == null) return;
            target.CrimeData.Escalate();
        }

        /// <summary>
        /// Decreases the wanted level for the specified player by one level.
        /// </summary>
        /// <param name="target">The player to de-escalate the wanted level for.</param>
        /// <remarks>
        /// Progression: Lethal → NonLethal → Arresting → Investigating → None.
        /// This is a convenience method that calls <see cref="PlayerCrimeData.Deescalate"/>.
        /// </remarks>
        public static void DeescalateWantedLevel(Player target)
        {
            if (target == null) return;
            target.CrimeData.Deescalate();
        }

        /// <summary>
        /// Gets the total number of active police officers currently in the game world.
        /// </summary>
        public static int ActiveOfficerCount =>
            S1Police.PoliceOfficer.Officers.Count;

        /// <summary>
        /// Checks if the player is currently in an active police pursuit.
        /// </summary>
        /// <param name="target">The player to check.</param>
        /// <returns>True if the player has any wanted level above None.</returns>
        public static bool IsPlayerWanted(Player target)
        {
            if (target == null) return false;
            return target.CrimeData.CurrentPursuitLevel != PursuitLevel.None;
        }

        /// <summary>
        /// Checks if the player is currently being investigated by police.
        /// This includes all pursuit levels except None.
        /// </summary>
        /// <param name="target">The player to check.</param>
        /// <returns>True if police are actively pursuing or investigating the player.</returns>
        public static bool IsUnderInvestigation(Player target)
        {
            if (target == null) return false;
            return target.CrimeData.CurrentPursuitLevel >= PursuitLevel.Investigating;
        }

        /// <summary>
        /// Checks if police are authorized to use lethal force against the player.
        /// </summary>
        /// <param name="target">The player to check.</param>
        /// <returns>True if the player's wanted level is at Lethal.</returns>
        public static bool IsLethalForceAuthorized(Player target)
        {
            if (target == null) return false;
            return target.CrimeData.CurrentPursuitLevel == PursuitLevel.Lethal;
        }

        #region Patrol Management

        /// <summary>
        /// Starts a foot patrol using the specified route and number of officers.
        /// Officers are pulled from the nearest police station to the route's starting point.
        /// </summary>
        /// <param name="route">The foot patrol route to use.</param>
        /// <param name="requestedMembers">The number of officers to assign to the patrol.</param>
        /// <returns>A PatrolGroup object representing the active patrol, or null if insufficient officers are available.</returns>
        /// <remarks>
        /// If insufficient officers are available at the nearest police station, the patrol will not be created.
        /// Use <see cref="FindFootPatrolRoute"/> to locate existing patrol routes in the scene.
        /// </remarks>
        public static PatrolGroup StartFootPatrol(FootPatrolRoute route, int requestedMembers = 2)
        {
            if (Internal == null || route?.S1Route == null) return null;
            var s1PatrolGroup = Internal.StartFootpatrol(route.S1Route, requestedMembers);
            return s1PatrolGroup != null ? new PatrolGroup(s1PatrolGroup) : null;
        }

        /// <summary>
        /// Starts a vehicle patrol using the specified route.
        /// One officer is pulled from the nearest police station and assigned a patrol vehicle.
        /// </summary>
        /// <param name="route">The vehicle patrol route to use.</param>
        /// <returns>True if the patrol was started successfully, false otherwise.</returns>
        /// <remarks>
        /// If no officers are available at the nearest police station, the patrol will not be created.
        /// Use <see cref="FindVehiclePatrolRoute"/> to locate existing patrol routes in the scene.
        /// The assigned officer will automatically patrol the route with a police vehicle.
        /// </remarks>
        public static bool StartVehiclePatrol(VehiclePatrolRoute route)
        {
            if (Internal == null || route?.S1Route == null) return false;
            var officer = Internal.StartVehiclePatrol(route.S1Route);
            return officer != null;
        }

        /// <summary>
        /// Finds a foot patrol route in the scene by name.
        /// </summary>
        /// <param name="routeName">The name of the patrol route to find.</param>
        /// <returns>The FootPatrolRoute wrapper, or null if not found.</returns>
        public static FootPatrolRoute FindFootPatrolRoute(string routeName)
        {
            if (string.IsNullOrEmpty(routeName)) return null;
            var routes = Object.FindObjectsOfType<S1NPCBehaviour.FootPatrolRoute>();
            for (int i = 0; i < routes.Length; i++)
            {
                if (routes[i].RouteName == routeName)
                    return new FootPatrolRoute(routes[i]);
            }
            return null;
        }

        /// <summary>
        /// Finds a vehicle patrol route in the scene by name.
        /// </summary>
        /// <param name="routeName">The name of the patrol route to find.</param>
        /// <returns>The VehiclePatrolRoute wrapper, or null if not found.</returns>
        public static VehiclePatrolRoute FindVehiclePatrolRoute(string routeName)
        {
            if (string.IsNullOrEmpty(routeName)) return null;
            var routes = Object.FindObjectsOfType<S1NPCBehaviour.VehiclePatrolRoute>();
            for (int i = 0; i < routes.Length; i++)
            {
                if (routes[i].RouteName == routeName)
                    return new VehiclePatrolRoute(routes[i]);
            }
            return null;
        }

        /// <summary>
        /// Gets all foot patrol routes currently in the scene.
        /// </summary>
        /// <returns>An array of FootPatrolRoute wrappers.</returns>
        public static FootPatrolRoute[] GetAllFootPatrolRoutes()
        {
            var s1Routes = Object.FindObjectsOfType<S1NPCBehaviour.FootPatrolRoute>();
            var routes = new FootPatrolRoute[s1Routes.Length];
            for (int i = 0; i < s1Routes.Length; i++)
            {
                routes[i] = new FootPatrolRoute(s1Routes[i]);
            }
            return routes;
        }

        /// <summary>
        /// Gets all vehicle patrol routes currently in the scene.
        /// </summary>
        /// <returns>An array of VehiclePatrolRoute wrappers.</returns>
        public static VehiclePatrolRoute[] GetAllVehiclePatrolRoutes()
        {
            var s1Routes = Object.FindObjectsOfType<S1NPCBehaviour.VehiclePatrolRoute>();
            var routes = new VehiclePatrolRoute[s1Routes.Length];
            for (int i = 0; i < s1Routes.Length; i++)
            {
                routes[i] = new VehiclePatrolRoute(s1Routes[i]);
            }
            return routes;
        }

        #endregion
    }
}
