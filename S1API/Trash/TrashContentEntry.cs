namespace S1API.Trash
{
    /// <summary>
    /// Describes one immutable entry in a trash-container content snapshot.
    /// </summary>
    public readonly struct TrashContentEntry
    {
        internal TrashContentEntry(string? trashId, int quantity, int unitSize, int unitValue)
        {
            TrashId = trashId;
            Quantity = quantity;
            UnitSize = unitSize;
            UnitValue = unitValue;
        }

        /// <summary>
        /// Gets the native trash-prefab identifier.
        /// </summary>
        public string? TrashId { get; }

        /// <summary>
        /// Gets the number of trash items represented by this entry.
        /// </summary>
        public int Quantity { get; }

        /// <summary>
        /// Gets the container-capacity units consumed by each item.
        /// </summary>
        public int UnitSize { get; }

        /// <summary>
        /// Gets the sell value of each item.
        /// </summary>
        public int UnitValue { get; }
    }
}
