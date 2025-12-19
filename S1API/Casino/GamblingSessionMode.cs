namespace S1API.Casino
{
    /// <summary>
    /// Defines how long an NPC should continue using a slot machine.
    /// </summary>
    public enum GamblingSessionMode
    {
        /// <summary>
        /// Play the slot machine exactly once.
        /// </summary>
        SingleSpin = 0,
        
        /// <summary>
        /// Play until a specific number of spins is reached.
        /// </summary>
        SpinCount = 1,
        
        /// <summary>
        /// Play until a specific time is reached.
        /// </summary>
        UntilTime = 2,
        
        /// <summary>
        /// Play until the NPC runs out of cash for the bet amount.
        /// </summary>
        UntilBroke = 3,
        
        /// <summary>
        /// Play until time is reached OR the NPC runs out of cash, whichever comes first.
        /// </summary>
        UntilTimeOrBroke = 4
    }
}

