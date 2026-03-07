#if (IL2CPPMELON)
using Il2Cpp;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1AvatarAnimation = Il2CppScheduleOne.AvatarFramework.Animation;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1AvatarAnimation = ScheduleOne.AvatarFramework.Animation;
#endif

using System;
using System.Text;
using UnityEngine;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that seats an NPC at an <see cref="S1AvatarAnimation.AvatarSeatSet"/>.
    /// </summary>
    /// <remarks>
    /// The specification resolves an <see cref="S1AvatarAnimation.AvatarSeatSet"/> using one of the provided
    /// lookup mechanisms and configures a <see cref="S1NPCsSchedules.NPCEvent_Sit"/> instance on the target
    /// schedule. This allows mods to declaratively schedule seating behaviour through the S1API builder.
    /// </remarks>
    public sealed class SitSpec : IScheduleActionSpec
    {
        /// <summary>
        /// Gets or sets the start time for this action, in 24-hour time (e.g. 830 for 8:30 AM).
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Gets or sets the duration of the sit action in minutes.
        /// If not set (0 or negative), defaults to the time remaining until the next scheduled action.
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets the optional display name for this action. Defaults to "Sit".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the NPC should be warped to the seat if the action is skipped.
        /// </summary>
        public bool WarpIfSkipped { get; set; }

        /// <summary>
        /// Gets or sets a direct Unity object reference that contains the desired seat set.
        /// </summary>
        /// <remarks>
        /// The reference may be an <see cref="S1AvatarAnimation.AvatarSeatSet"/>, a <see cref="GameObject"/>,
        /// or any <see cref="Component"/> that is part of the seat set hierarchy.
        /// </remarks>
        public UnityEngine.Object SeatSetReference { get; set; }

        /// <summary>
        /// Gets or sets a transform path (e.g. "@Locations/Cafe/Seats/Booth01") used to locate the seat set.
        /// </summary>
        /// <remarks>
        /// Paths are matched case-insensitively against full transform hierarchies. Inactive objects are included
        /// when <see cref="IncludeInactiveSearch"/> is <c>true</c>.
        /// </remarks>
        public string SeatSetPath { get; set; }

        /// <summary>
        /// Gets or sets the GameObject name of the desired seat set.
        /// </summary>
        /// <remarks>
        /// Name lookups search all seat sets in the scene and are case-insensitive.
        /// </remarks>
        public string SeatSetName { get; set; }

        /// <summary>
        /// Gets or sets whether lookups should consider inactive seat sets. Defaults to <c>true</c>.
        /// </summary>
        public bool IncludeInactiveSearch { get; set; } = true;

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            if (schedule == null)
                return;

            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_Sit>(StartTime, string.IsNullOrEmpty(Name) ? "Sit" : Name);
            if (action == null)
                return;

            try
            {
                action.NetworkInitializeIfDisabled();
            }
            catch { /* ignore */ }

            var seatSet = ResolveSeatSet(schedule);
            if (seatSet != null)
            {
                action.SeatSet = seatSet;
            }
            else
            {
                var npcName = schedule.NPC?.gameObject != null ? schedule.NPC.gameObject.name : "Unknown";
                Debug.LogWarning($"[S1API] SitSpec could not resolve a seat set (Name={SeatSetName}, Path={SeatSetPath}) for NPC '{npcName}'.");
            }

            action.WarpIfSkipped = WarpIfSkipped;
            action.Duration = DurationMinutes > 0 ? DurationMinutes : 60;
        }

        private S1AvatarAnimation.AvatarSeatSet ResolveSeatSet(NPCSchedule schedule)
        {
            var direct = TryGetFromObject(SeatSetReference);
            if (direct != null)
                return direct;

            S1AvatarAnimation.AvatarSeatSet[] cache = null;

            if (!string.IsNullOrEmpty(SeatSetPath))
            {
                var fromPath = TryResolveFromPath(SeatSetPath, ref cache);
                if (fromPath != null)
                    return fromPath;
            }

            if (!string.IsNullOrEmpty(SeatSetName))
            {
                var fromName = TryResolveFromName(SeatSetName, ref cache);
                if (fromName != null)
                    return fromName;
            }

            // As a final fallback, try to locate a seat set parented under the NPC itself
            if (schedule?.NPC?.gameObject != null)
            {
                var npcTransform = schedule.NPC.gameObject.transform;
                if (npcTransform != null)
                {
                    var nested = npcTransform.GetComponentInChildren<S1AvatarAnimation.AvatarSeatSet>(IncludeInactiveSearch);
                    if (nested != null)
                        return nested;
                }
            }

            return null;
        }

        private S1AvatarAnimation.AvatarSeatSet TryResolveFromPath(string path, ref S1AvatarAnimation.AvatarSeatSet[] cache)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // Direct GameObject.Find lookup (only finds active objects)
            try
            {
                var go = GameObject.Find(path);
                var resolved = TryGetFromObject(go);
                if (resolved != null)
                    return resolved;
            }
            catch { }

            var all = cache ??= FindAllSeatSets();
            if (all == null || all.Length == 0)
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                    continue;

                var transform = candidate.transform;
                if (transform == null)
                    continue;

                if (MatchesPath(transform, path))
                    return candidate;
            }

            return null;
        }

        private S1AvatarAnimation.AvatarSeatSet TryResolveFromName(string name, ref S1AvatarAnimation.AvatarSeatSet[] cache)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var all = cache ??= FindAllSeatSets();
            if (all == null || all.Length == 0)
                return null;

            for (int i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                    continue;

                var go = candidate.gameObject;
                if (go == null)
                    continue;

                if (string.Equals(go.name, name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        private S1AvatarAnimation.AvatarSeatSet[] FindAllSeatSets()
        {
            try
            {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                return UnityEngine.Object.FindObjectsOfType<S1AvatarAnimation.AvatarSeatSet>(includeInactive: IncludeInactiveSearch);
#elif (MONOMELON || MONOBEPINEX)
                return UnityEngine.Object.FindObjectsOfType<S1AvatarAnimation.AvatarSeatSet>(IncludeInactiveSearch);
#else
                return Array.Empty<S1AvatarAnimation.AvatarSeatSet>();
#endif
            }
            catch
            {
                return Array.Empty<S1AvatarAnimation.AvatarSeatSet>();
            }
        }

        private static bool MatchesPath(Transform transform, string expectedPath)
        {
            if (transform == null || string.IsNullOrEmpty(expectedPath))
                return false;

            var normalized = expectedPath.Replace('\\', '/');
            if (normalized.Length == 0)
                return false;

            // Compare case-insensitively against the full hierarchy path
            string fullPath = BuildHierarchyPath(transform);
            if (string.Equals(fullPath, normalized, StringComparison.OrdinalIgnoreCase))
                return true;

            // Allow suffix matches so callers can omit scene root names
            return fullPath.EndsWith(normalized, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            StringBuilder builder = new StringBuilder(128);
            BuildPathRecursive(transform, builder);
            return builder.ToString();
        }

        private static void BuildPathRecursive(Transform node, StringBuilder builder)
        {
            if (node == null)
                return;

            if (node.parent != null)
            {
                BuildPathRecursive(node.parent, builder);
                if (builder.Length > 0)
                    builder.Append('/');
            }

            builder.Append(node.name);
        }

        private static S1AvatarAnimation.AvatarSeatSet TryGetFromObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return null;

            if (obj is S1AvatarAnimation.AvatarSeatSet seatSet)
                return seatSet;

            if (obj is GameObject go)
                return go.GetComponent<S1AvatarAnimation.AvatarSeatSet>();

            if (obj is Component component)
                return component.GetComponent<S1AvatarAnimation.AvatarSeatSet>();

            return null;
        }
    }
}
