namespace S1API.Doors
{
    /// <summary>
    /// Defines the access level for players interacting with a door.
    /// </summary>
    public enum DoorAccess
    {
        /// <summary>
        /// Door is fully open - players can enter and exit freely.
        /// </summary>
        Open,

        /// <summary>
        /// Players can exit through the door but cannot enter from this side.
        /// </summary>
        ExitOnly,

        /// <summary>
        /// Players can enter through the door but cannot exit from this side.
        /// </summary>
        EnterOnly,

        /// <summary>
        /// Door is locked - no player access allowed.
        /// </summary>
        Closed
    }
}
