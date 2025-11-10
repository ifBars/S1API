namespace S1API.Cartel
{
    /// <summary>
    /// Represents the current status/relationship state of the Cartel.
    /// </summary>
    public enum CartelStatus
    {
        /// <summary>
        /// Unknown status - initial state before cartel relationship is established.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Truced status - cartel is friendly/neutral with the player.
        /// </summary>
        Truced = 1,

        /// <summary>
        /// Hostile status - cartel is actively hostile toward the player.
        /// </summary>
        Hostile = 2,

        /// <summary>
        /// Defeated status - cartel has been defeated.
        /// </summary>
        Defeated = 3
    }
}

