#if (IL2CPPMELON)
using S1Law = Il2CppScheduleOne.Law;
using S1Police = Il2CppScheduleOne.Police;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Law = ScheduleOne.Law;
using S1Police = ScheduleOne.Police;
#endif

namespace S1API.Law
{
    /// <summary>
    /// Manages road checkpoints throughout the game world.
    /// Provides control over checkpoint activation and officer assignment.
    /// </summary>
    public static class CheckpointManager
    {
        /// <summary>
        /// INTERNAL: Provides access to the underlying checkpoint manager singleton.
        /// </summary>
        private static S1Law.CheckpointManager Internal =>
            S1Law.CheckpointManager.Instance;

        /// <summary>
        /// Enables or disables a checkpoint at the specified location.
        /// </summary>
        /// <param name="location">The checkpoint location to modify.</param>
        /// <param name="enabled">Whether to enable or disable the checkpoint.</param>
        /// <param name="requestedOfficers">The number of officers to assign to the checkpoint when enabling.</param>
        /// <remarks>
        /// Officers will be pulled from the nearest police station.
        /// If insufficient officers are available, fewer than requested may be assigned.
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
        public static void DisableAllCheckpoints()
        {
            SetCheckpointEnabled(CheckpointLocation.Western, false, 0);
            SetCheckpointEnabled(CheckpointLocation.Docks, false, 0);
            SetCheckpointEnabled(CheckpointLocation.NorthResidential, false, 0);
            SetCheckpointEnabled(CheckpointLocation.WestResidential, false, 0);
        }
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
