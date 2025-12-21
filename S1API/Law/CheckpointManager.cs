#if (IL2CPPMELON)
using S1Law = Il2CppScheduleOne.Law;
using S1Police = Il2CppScheduleOne.Police;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Law = ScheduleOne.Law;
using S1Police = ScheduleOne.Police;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Law
{
    /// <summary>
    /// Manages road checkpoints throughout the game world.
    /// Provides control over checkpoint activation and officer assignment.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: The game includes an automatic checkpoint evaluation system that runs every in-game minute.
    /// This system may automatically enable checkpoints based on conditions such as:
    /// <list type="bullet">
    /// <item><description>Law enforcement intensity level (see <see cref="LawController"/>)</description></item>
    /// <item><description>Time of day</description></item>
    /// <item><description>Curfew status</description></item>
    /// <item><description>Player distance from checkpoint</description></item>
    /// </list>
    /// Checkpoints require law enforcement intensity >= 5 (default) to activate automatically.
    /// Other conditions include: time of day, curfew status, player distance (50+ units), and officer availability.
    /// To prevent automatic re-enabling, set intensity to 1-4 using <see cref="LawController.SetIntensityLevel"/>.
    /// </remarks>
    public static class CheckpointManager
    {
        /// <summary>
        /// INTERNAL: Provides access to the underlying checkpoint manager singleton.
        /// </summary>
        private static S1Law.CheckpointManager Internal =>
            S1Law.CheckpointManager.Instance;

        #region Checkpoint Control

        /// <summary>
        /// Enables or disables a checkpoint at the specified location.
        /// </summary>
        /// <param name="location">The checkpoint location to modify.</param>
        /// <param name="enabled">Whether to enable or disable the checkpoint.</param>
        /// <param name="requestedOfficers">The number of officers to assign to the checkpoint when enabling.</param>
        /// <remarks>
        /// <para>Officers will be pulled from the nearest police station.</para>
        /// <para>If insufficient officers are available, fewer than requested may be assigned.</para>
        /// <para>WARNING: The automatic checkpoint evaluation system may re-enable disabled checkpoints
        /// based on game conditions. See <see cref="CheckpointManager"/> remarks for details.</para>
        /// </remarks>
        public static void SetCheckpointEnabled(CheckpointLocation location, bool enabled, int requestedOfficers = 2)
        {
            if (Internal == null) return;
            Internal.SetCheckpointEnabled((S1Law.CheckpointManager.ECheckpointLocation)location, enabled, requestedOfficers);
        }

        /// <summary>
        /// Checks if a checkpoint at the specified location is currently enabled.
        /// </summary>
        /// <param name="location">The checkpoint location to check.</param>
        /// <returns>True if the checkpoint is enabled, false otherwise.</returns>
        public static bool IsCheckpointEnabled(CheckpointLocation location)
        {
            if (Internal == null) return false;
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return false;
            return checkpoint.ActivationState == S1Police.RoadCheckpoint.ECheckpointState.Enabled;
        }

        /// <summary>
        /// Enables all checkpoints in the game world.
        /// </summary>
        /// <param name="officersPerCheckpoint">The number of officers to assign to each checkpoint.</param>
        public static void EnableAllCheckpoints(int officersPerCheckpoint = 2)
        {
            SetCheckpointEnabled(CheckpointLocation.Western, true, officersPerCheckpoint);
            SetCheckpointEnabled(CheckpointLocation.Docks, true, officersPerCheckpoint);
            SetCheckpointEnabled(CheckpointLocation.NorthResidential, true, officersPerCheckpoint);
            SetCheckpointEnabled(CheckpointLocation.WestResidential, true, officersPerCheckpoint);
        }

        /// <summary>
        /// Disables all checkpoints in the game world.
        /// </summary>
        /// <remarks>
        /// WARNING: The automatic checkpoint evaluation system may re-enable checkpoints
        /// based on game conditions. Consider adjusting law enforcement intensity instead.
        /// </remarks>
        public static void DisableAllCheckpoints()
        {
            SetCheckpointEnabled(CheckpointLocation.Western, false, 0);
            SetCheckpointEnabled(CheckpointLocation.Docks, false, 0);
            SetCheckpointEnabled(CheckpointLocation.NorthResidential, false, 0);
            SetCheckpointEnabled(CheckpointLocation.WestResidential, false, 0);
        }

        #endregion

        #region Checkpoint State Queries

        /// <summary>
        /// Gets the world position of a checkpoint.
        /// </summary>
        /// <param name="location">The checkpoint location to query.</param>
        /// <returns>The checkpoint's position in world space, or Vector3.zero if not found.</returns>
        public static Vector3 GetCheckpointPosition(CheckpointLocation location)
        {
            if (Internal == null) return Vector3.zero;
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return Vector3.zero;
            return checkpoint.transform.position;
        }

        /// <summary>
        /// Gets the number of officers currently assigned to a checkpoint.
        /// </summary>
        /// <param name="location">The checkpoint location to query.</param>
        /// <returns>The number of assigned officers, or 0 if the checkpoint is not found.</returns>
        public static int GetAssignedOfficerCount(CheckpointLocation location)
        {
            if (Internal == null) return 0;
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return 0;
            return checkpoint.AssignedNPCs.Count;
        }

        /// <summary>
        /// Gets the NPCs (police officers) currently assigned to a checkpoint.
        /// </summary>
        /// <param name="location">The checkpoint location to query.</param>
        /// <returns>A list of NPCs assigned to the checkpoint, or an empty list if none.</returns>
        public static List<S1NPCs.NPC> GetAssignedOfficers(CheckpointLocation location)
        {
            var result = new List<S1NPCs.NPC>();
            if (Internal == null) return result;
            
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return result;

#if (IL2CPPMELON)
            // IL2CPP uses Il2CppSystem.Collections.Generic.List
            for (int i = 0; i < checkpoint.AssignedNPCs.Count; i++)
            {
                result.Add(checkpoint.AssignedNPCs[i]);
            }
#else
            // Mono uses standard System.Collections.Generic.List
            result.AddRange(checkpoint.AssignedNPCs);
#endif
            return result;
        }

        /// <summary>
        /// Checks if a checkpoint's first gate is currently open.
        /// </summary>
        /// <param name="location">The checkpoint location to check.</param>
        /// <returns>True if gate 1 is open, false otherwise.</returns>
        public static bool IsGate1Open(CheckpointLocation location)
        {
            if (Internal == null) return false;
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return false;
            
#if (IL2CPPMELON)
            return checkpoint.Gate1Open;
#else
            return checkpoint.Gate1Open;
#endif
        }

        /// <summary>
        /// Checks if a checkpoint's second gate is currently open.
        /// </summary>
        /// <param name="location">The checkpoint location to check.</param>
        /// <returns>True if gate 2 is open, false otherwise.</returns>
        public static bool IsGate2Open(CheckpointLocation location)
        {
            if (Internal == null) return false;
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return false;
            
#if (IL2CPPMELON)
            return checkpoint.Gate2Open;
#else
            return checkpoint.Gate2Open;
#endif
        }

        /// <summary>
        /// Gets comprehensive information about a checkpoint's current state.
        /// </summary>
        /// <param name="location">The checkpoint location to query.</param>
        /// <returns>A CheckpointInfo object containing state information, or null if the checkpoint is not found.</returns>
        public static CheckpointInfo GetCheckpointInfo(CheckpointLocation location)
        {
            if (Internal == null) return null;
            var checkpoint = Internal.GetCheckpoint((S1Law.CheckpointManager.ECheckpointLocation)location);
            if (checkpoint == null) return null;

            return new CheckpointInfo
            {
                Location = location,
                IsEnabled = checkpoint.ActivationState == S1Police.RoadCheckpoint.ECheckpointState.Enabled,
                Position = checkpoint.transform.position,
                AssignedOfficerCount = checkpoint.AssignedNPCs.Count,
                IsGate1Open = checkpoint.Gate1Open,
                IsGate2Open = checkpoint.Gate2Open
            };
        }

        /// <summary>
        /// Gets information about all checkpoints in the game.
        /// </summary>
        /// <returns>A list of CheckpointInfo objects for all checkpoint locations.</returns>
        public static List<CheckpointInfo> GetAllCheckpointInfo()
        {
            var result = new List<CheckpointInfo>();
            
            foreach (CheckpointLocation location in System.Enum.GetValues(typeof(CheckpointLocation)))
            {
                var info = GetCheckpointInfo(location);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            
            return result;
        }

        #endregion
    }

    /// <summary>
    /// Represents the available checkpoint locations in the game world.
    /// </summary>
    public enum CheckpointLocation
    {
        /// <summary>
        /// Western checkpoint location.
        /// </summary>
        Western = 0,

        /// <summary>
        /// Docks checkpoint location.
        /// </summary>
        Docks = 1,

        /// <summary>
        /// North Residential checkpoint location.
        /// </summary>
        NorthResidential = 2,

        /// <summary>
        /// West Residential checkpoint location.
        /// </summary>
        WestResidential = 3
    }
}
