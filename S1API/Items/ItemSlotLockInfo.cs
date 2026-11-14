namespace S1API.Items
{
    /// <summary>
    /// Contains an immutable managed snapshot of an item-slot lock reason.
    /// </summary>
    public readonly struct ItemSlotLockInfo
    {
        internal ItemSlotLockInfo(string? lockReason)
        {
            LockReason = lockReason;
        }

        /// <summary>
        /// Gets the native lock reason, preserving empty strings and <c>null</c>.
        /// </summary>
        public string? LockReason { get; }
    }
}
