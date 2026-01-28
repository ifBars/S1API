using System;
using System.Collections.Generic;
using S1API.Logging;

namespace S1API.Growing
{
    /// <summary>
    /// Global registry for additive IDs that should be allowed on all grow containers.
    /// These IDs are applied to <c>GrowContainer.AllowedAdditives</c> during <c>GrowContainer.InitializeGridItem</c>.
    /// </summary>
    public static class GrowContainerAdditives
    {
        private static readonly Log Logger = new Log("GrowContainerAdditives");
        private static readonly object Gate = new object();
        private static readonly HashSet<string> AllowedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> WarnedMissingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers an additive item ID as allowed on all grow containers (idempotent).
        /// </summary>
        /// <remarks>
        /// Recommended timing: call during <see cref="Lifecycle.GameLifecycle.OnPreLoad"/> (or earlier).
        /// </remarks>
        public static void AllowAdditive(string additiveItemId)
        {
            var id = (additiveItemId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Additive item ID is required.", nameof(additiveItemId));

            lock (Gate)
            {
                // Final decision: duplicates are a silent no-op.
                AllowedIds.Add(id);
            }
        }

        /// <summary>
        /// Returns a snapshot of allowed additive IDs registered via S1API.
        /// </summary>
        public static IReadOnlyList<string> GetAllowedAdditiveIds()
        {
            lock (Gate)
            {
                var arr = new string[AllowedIds.Count];
                AllowedIds.CopyTo(arr);
                return arr;
            }
        }

        internal static IReadOnlyList<string> GetAllowedAdditiveIdsInternal() => GetAllowedAdditiveIds();

        internal static void WarnMissing(string additiveItemId, string message)
        {
            if (string.IsNullOrWhiteSpace(additiveItemId))
                return;

            lock (Gate)
            {
                // Keep missing-ID warnings one-per-ID per session to avoid log spam.
                if (WarnedMissingIds.Add(additiveItemId))
                    Logger.Warning(message);
            }
        }
    }
}
