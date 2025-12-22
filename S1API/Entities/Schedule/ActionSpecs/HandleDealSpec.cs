using System;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that handles a customer deal handover at the active contract location.
    /// </summary>
    /// <remarks>
    /// As of v0.4.2f4, deal handling is now automatic through the DealerAttendDealBehaviour system.
    /// This spec is kept for backwards compatibility but is a no-op. Dealer NPCs set up with
    /// EnsureDealer() will automatically handle deals when contracts are assigned.
    /// </remarks>
    [Obsolete("HandleDealSpec is no longer needed as of game version 0.4.2f4. Deal handling is now automatic through DealerAttendDealBehaviour.")]
    public sealed class HandleDealSpec : IScheduleActionSpec
    {
        /// <summary>
        /// The time when this action should start, in minutes from midnight.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Optional custom name for the action.
        /// </summary>
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            // No-op: Deal handling is now automatic through DealerAttendDealBehaviour.
            // Dealers set up with EnsureDealer() will automatically handle deals when contracts are assigned.
            // This method intentionally does nothing to maintain backwards compatibility.
        }
    }
}


