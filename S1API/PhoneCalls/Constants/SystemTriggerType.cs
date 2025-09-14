namespace S1API.PhoneCalls.Constants
{
    /// <summary>
    /// Defines the timing when system triggers should execute during a phone call stage.
    /// </summary>
    public enum SystemTriggerType
    {
        /// <summary>
        /// Trigger executes when the call stage begins, before the text is displayed to the player.
        /// </summary>
        StartTrigger,

        /// <summary>
        /// Trigger executes when the call stage completes, after the player has finished reading the text.
        /// </summary>
        DoneTrigger
    }
}
