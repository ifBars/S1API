using UnityEngine;

namespace S1API.Law
{
    /// <summary>
    /// Contains comprehensive state information about a checkpoint.
    /// </summary>
    public sealed class CheckpointInfo
    {
        /// <summary>
        /// The checkpoint's location identifier.
        /// </summary>
        public CheckpointLocation Location { get; internal set; }

        /// <summary>
        /// Whether the checkpoint is currently enabled and operational.
        /// </summary>
        public bool IsEnabled { get; internal set; }

        /// <summary>
        /// The checkpoint's position in world space.
        /// </summary>
        public Vector3 Position { get; internal set; }

        /// <summary>
        /// The number of police officers currently assigned to this checkpoint.
        /// </summary>
        public int AssignedOfficerCount { get; internal set; }

        /// <summary>
        /// Whether the checkpoint's first gate is currently open.
        /// </summary>
        public bool IsGate1Open { get; internal set; }

        /// <summary>
        /// Whether the checkpoint's second gate is currently open.
        /// </summary>
        public bool IsGate2Open { get; internal set; }

        /// <summary>
        /// Whether both gates are currently closed.
        /// </summary>
        public bool AreBothGatesClosed =>
            !IsGate1Open && !IsGate2Open;

        /// <summary>
        /// Whether at least one gate is currently open.
        /// </summary>
        public bool IsAnyGateOpen =>
            IsGate1Open || IsGate2Open;

        /// <summary>
        /// Whether the checkpoint is enabled and has at least one officer assigned.
        /// </summary>
        public bool IsOperational =>
            IsEnabled && AssignedOfficerCount > 0;
    }
}

