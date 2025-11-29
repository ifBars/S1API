namespace S1API.Law
{
    /// <summary>
    /// Represents the intensity level of a police pursuit.
    /// Higher levels indicate more aggressive police response.
    /// </summary>
    public enum PursuitLevel
    {
        /// <summary>
        /// No active pursuit. Player is not wanted.
        /// </summary>
        None = 0,

        /// <summary>
        /// Police are investigating the player's location.
        /// Officers will search for the player but won't use force.
        /// Search time: 60 seconds after losing sight.
        /// </summary>
        Investigating = 1,

        /// <summary>
        /// Police are attempting to arrest the player.
        /// Officers will attempt non-lethal takedown.
        /// Search time: 25 seconds after losing sight.
        /// Escalates to NonLethal after 25 seconds if player remains visible.
        /// </summary>
        Arresting = 2,

        /// <summary>
        /// Police are authorized to use non-lethal force (tasers, batons).
        /// Officers will be more aggressive in pursuit.
        /// Search time: 30 seconds after losing sight.
        /// Escalates to Lethal after 120 seconds if player remains visible.
        /// </summary>
        NonLethal = 3,

        /// <summary>
        /// Police are authorized to use lethal force.
        /// Maximum pursuit intensity - officers will shoot on sight.
        /// Search time: 40 seconds after losing sight.
        /// Does not escalate further.
        /// </summary>
        Lethal = 4
    }
}
