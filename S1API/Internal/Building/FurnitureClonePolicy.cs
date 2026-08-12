using System;

namespace S1API.Internal.Building
{
    internal static class FurnitureClonePolicy
    {
        internal static void ValidateNewId(string itemId, string? donorId)
        {
            if (donorId != null &&
                string.Equals(itemId, donorId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "A furniture variant must use a new stable item ID.");
            }
        }
    }
}
