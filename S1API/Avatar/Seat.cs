#if (IL2CPPMELON)
using S1AvatarAnimation = Il2CppScheduleOne.AvatarFramework.Animation;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarAnimation = ScheduleOne.AvatarFramework.Animation;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace S1API.Avatar
{
    /// <summary>
    /// Provides metadata about a world <c>AvatarSeat</c> for modders.
    /// Seats are registered automatically when <c>AvatarSeat.Awake</c> runs and cleared on scene unload via <see cref="Internal.Lifecycle.SceneStateCleaner"/>.
    /// </summary>
    public sealed class Seat
    {
        internal static readonly List<Seat> All = new List<Seat>();

        private readonly WeakReference<S1AvatarAnimation.AvatarSeat> seatReference;
        private readonly WeakReference<S1AvatarAnimation.AvatarSeatSet> seatSetReference;
        private readonly Transform seatTransform;
        private readonly Transform sittingPoint;
        private readonly Transform accessPoint;
        private readonly string hierarchyPath;
        private readonly string seatSetName;
        private readonly int? seatIndex;

        private Seat(S1AvatarAnimation.AvatarSeat seat)
        {
            seatReference = new WeakReference<S1AvatarAnimation.AvatarSeat>(seat);
            seatTransform = seat != null ? seat.transform : null;
            sittingPoint = seat?.SittingPoint;
            accessPoint = seat?.AccessPoint;

            // Track hierarchy and naming for mod author reference
            hierarchyPath = BuildTransformPath(seatTransform);

            // Resolve seat set (if any)
            var parentSet = seat != null ? seat.GetComponentInParent<S1AvatarAnimation.AvatarSeatSet>(includeInactive: true) : null;
            if (parentSet != null)
            {
                seatSetReference = new WeakReference<S1AvatarAnimation.AvatarSeatSet>(parentSet);
                var setGO = parentSet.gameObject;
                seatSetName = setGO != null ? setGO.name : string.Empty;

                // Determine index within the set for labeling purposes
                try
                {
                    if (parentSet.Seats != null)
                    {
                        for (int i = 0; i < parentSet.Seats.Length; i++)
                        {
                            if (ReferenceEquals(parentSet.Seats[i], seat))
                            {
                                seatIndex = i;
                                break;
                            }
                        }
                    }
                }
                catch { /* ignore reflection failures */ }
            }
            else
            {
                seatSetReference = new WeakReference<S1AvatarAnimation.AvatarSeatSet>(null);
                seatSetName = string.Empty;
            }
        }

        /// <summary>
        /// Human-readable label combining seat set name (if available) and hierarchy path.
        /// </summary>
        public string Label
        {
            get
            {
                if (!string.IsNullOrEmpty(seatSetName))
                {
                    return seatIndex.HasValue
                        ? $"{seatSetName}[{seatIndex.Value}] ({HierarchyPath})"
                        : $"{seatSetName} ({HierarchyPath})";
                }
                return HierarchyPath;
            }
        }

        /// <summary>
        /// Returns the seat's hierarchy path (scene root to GameObject).
        /// </summary>
        public string HierarchyPath => hierarchyPath;

        /// <summary>
        /// Name of the parent <c>AvatarSeatSet</c> GameObject, if any.
        /// </summary>
        public string SeatSetName => seatSetName;

        /// <summary>
        /// Index of the seat inside the parent <c>AvatarSeatSet.Seats</c> array, or <c>null</c> if not part of a set.
        /// </summary>
        public int? IndexInSet => seatIndex;

        /// <summary>
        /// World position of the seat's sitting point, or the seat transform position if no sitting point exists.
        /// </summary>
        public Vector3 SittingPosition
        {
            get
            {
                if (sittingPoint != null)
                    return sittingPoint.position;
                if (seatTransform != null)
                    return seatTransform.position;
                return Vector3.zero;
            }
        }

        /// <summary>
        /// World rotation of the seat's sitting point, or the seat transform rotation if no sitting point exists.
        /// </summary>
        public Quaternion SittingRotation
        {
            get
            {
                if (sittingPoint != null)
                    return sittingPoint.rotation;
                if (seatTransform != null)
                    return seatTransform.rotation;
                return Quaternion.identity;
            }
        }

        /// <summary>
        /// Access point location used by NPCs to approach the seat, if defined.
        /// </summary>
        public Vector3 AccessPosition
        {
            get
            {
                if (accessPoint != null)
                    return accessPoint.position;
                return SittingPosition;
            }
        }

        /// <summary>
        /// Access point rotation, or sitting rotation if not defined.
        /// </summary>
        public Quaternion AccessRotation
        {
            get
            {
                if (accessPoint != null)
                    return accessPoint.rotation;
                return SittingRotation;
            }
        }

        /// <summary>
        /// Attempts to retrieve the live game <c>AvatarSeat</c> component, if it still exists.
        /// </summary>
        public S1AvatarAnimation.AvatarSeat ResolveGameSeat()
        {
            seatReference.TryGetTarget(out var seat);
            return seat;
        }

        /// <summary>
        /// Attempts to retrieve the live parent <c>AvatarSeatSet</c>, if any.
        /// </summary>
        public S1AvatarAnimation.AvatarSeatSet ResolveSeatSet()
        {
            seatSetReference.TryGetTarget(out var seatSet);
            return seatSet;
        }

        /// <summary>
        /// Returns the seat GameObject, if the component still exists.
        /// </summary>
        public GameObject ResolveSeatGameObject()
        {
            var seat = ResolveGameSeat();
            return seat != null ? seat.gameObject : null;
        }

        /// <summary>
        /// Returns all registered seats (snapshot).
        /// </summary>
        public static Seat[] GetAll() => All.ToArray();

        /// <summary>
        /// Returns all seats belonging to a named <c>AvatarSeatSet</c> (case-insensitive).
        /// </summary>
        public static Seat[] GetBySeatSet(string setName)
        {
            if (string.IsNullOrEmpty(setName))
                return Array.Empty<Seat>();
            return All.Where(s => !string.IsNullOrEmpty(s.seatSetName) && string.Equals(s.seatSetName, setName, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        /// <summary>
        /// Finds the first seat whose hierarchy path ends with the provided suffix (case-insensitive).
        /// Useful when only a partial path is known (e.g. "Cafe/Booth01/SeatA").
        /// </summary>
        public static Seat FindByPathSuffix(string pathSuffix)
        {
            if (string.IsNullOrEmpty(pathSuffix))
                return null;
            for (int i = 0; i < All.Count; i++)
            {
                var seat = All[i];
                if (seat == null)
                    continue;
                if (seat.HierarchyPath.EndsWith(pathSuffix, StringComparison.OrdinalIgnoreCase))
                    return seat;
            }
            return null;
        }

        /// <summary>
        /// Registers a seat. Called from the Harmony patch on <c>AvatarSeat.Awake</c>.
        /// </summary>
        internal static void Register(S1AvatarAnimation.AvatarSeat seat)
        {
            if (seat == null)
                return;

            // Prevent duplicates when seats are re-awakened (e.g., during additive loading)
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i]?.ResolveGameSeat() == seat)
                {
                    All[i] = new Seat(seat);
                    return;
                }
            }

            All.Add(new Seat(seat));
        }

        /// <summary>
        /// Unregisters a seat. Called from the Harmony patch on <c>AvatarSeat.OnDestroy</c>.
        /// </summary>
        internal static void Unregister(S1AvatarAnimation.AvatarSeat seat)
        {
            if (seat == null)
                return;

            for (int i = All.Count - 1; i >= 0; i--)
            {
                var existing = All[i];
                if (existing == null || existing.ResolveGameSeat() == seat)
                    All.RemoveAt(i);
            }
        }

        /// <summary>
        /// Clears the registry (used during scene unload).
        /// </summary>
        internal static void Clear() => All.Clear();

        /// <summary>
        /// Returns the number of registered seats.
        /// </summary>
        public static int Count => All.Count;

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var builder = new StringBuilder(96);
            AppendRecursive(transform, builder);
            return builder.ToString();
        }

        private static void AppendRecursive(Transform current, StringBuilder builder)
        {
            if (current == null)
                return;

            if (current.parent != null)
            {
                AppendRecursive(current.parent, builder);
                if (builder.Length > 0)
                    builder.Append('/');
            }

            builder.Append(current.name);
        }
    }
}
