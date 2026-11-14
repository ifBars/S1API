using System;
using S1API.Entities;
using S1API.Logging;

#if IL2CPPMELON
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1Player = Il2CppScheduleOne.PlayerScripts.Player;
#elif MONOMELON
using S1Vehicles = ScheduleOne.Vehicles;
using S1Player = ScheduleOne.PlayerScripts.Player;
#endif

namespace S1API.Vehicles
{
    /// <summary>
    /// Read-only metadata and live player occupancy for one native vehicle seat.
    /// </summary>
    /// <remarks>
    /// Instances remain stable for the loaded vehicle. Destruction or scene cleanup clears
    /// subscriptions and invalidates occupancy without synthesizing exit events.
    /// </remarks>
    public sealed class VehicleSeatInfo
    {
        private S1Vehicles.VehicleSeat? _seat;
        private S1Player? _observedOccupant;
        private Player? _observedPlayer;

        /// <summary>Captures seat metadata and initial occupancy without emitting a change event.</summary>
        /// <param name="index">The seat's index in the native vehicle seat array.</param>
        /// <param name="seat">The native seat, or null for an unavailable array slot.</param>
        internal VehicleSeatInfo(int index, S1Vehicles.VehicleSeat? seat)
        {
            Index = index;
            _seat = seat;
            IsDriverSeat = !ReferenceEquals(seat, null) && seat != null && seat.isDriverSeat;
            _observedOccupant = ReadOccupant();
            _observedPlayer = ResolvePlayer(_observedOccupant);
        }

        /// <summary>Gets the seat's stable index in the native vehicle seat array.</summary>
        public int Index { get; }

        /// <summary>Gets whether the native seat is configured as a driver seat.</summary>
        public bool IsDriverSeat { get; }

        /// <summary>Gets whether a live native player occupies this seat.</summary>
        public bool IsOccupied => !ReferenceEquals(ReadOccupant(), null);

        /// <summary>
        /// Gets the occupant from <see cref="Player.All"/>, or null if empty or no managed player is available.
        /// </summary>
        public Player? Occupant => ResolvePlayer(ReadOccupant());

        /// <summary>Raised with the previous and current managed occupants after native occupancy changes.</summary>
        /// <remarks>
        /// Native assignments notify synchronously after mutation on the observing peer.
        /// Player destruction without an assignment is observed on the next API update.
        /// Either argument may be null when its managed wrapper is unavailable; native identity
        /// changes still notify when both arguments are null. Repeated assignments do not notify.
        /// Subscriber exceptions are isolated. No network messages are added.
        /// </remarks>
        public event Action<Player?, Player?>? OccupantChanged;

        /// <summary>Updates observed occupancy and notifies subscribers when native player identity changes.</summary>
        /// <remarks>
        /// Updates the observed state before notifying. A managed-wrapper change alone does not notify;
        /// a native identity change still notifies when both managed occupants are unavailable.
        /// </remarks>
        internal void Refresh()
        {
            var current = ReadOccupant();
            var player = ResolvePlayer(current);
            if (SameIdentity(_observedOccupant, current))
            {
                _observedPlayer = player;
                return;
            }

            var previous = _observedPlayer ?? ResolvePlayer(_observedOccupant);
            _observedOccupant = current;
            _observedPlayer = player;
            NotifyOccupantChanged(previous, player);
        }

        /// <summary>Clears occupancy and subscriptions without notifying, retaining the seat's index and driver metadata.</summary>
        internal void Invalidate()
        {
            _seat = null;
            _observedOccupant = null;
            _observedPlayer = null;
            OccupantChanged = null;
        }

        /// <summary>Invokes the current occupant-change subscribers, isolating each subscriber's exceptions.</summary>
        /// <param name="previous">The previous managed occupant, or null if empty or unavailable.</param>
        /// <param name="current">The current managed occupant, or null if empty or unavailable.</param>
        /// <remarks>Both arguments may be null; this method does not suppress duplicate notifications.</remarks>
        internal void NotifyOccupantChanged(Player? previous, Player? current)
        {
            var handlers = OccupantChanged;
            if (handlers == null)
                return;

            foreach (Action<Player?, Player?> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(previous, current);
                }
                catch (Exception ex)
                {
                    try
                    {
                        new Log("VehicleSeatInfo").Warning($"An OccupantChanged subscriber failed: {ex.Message}");
                    }
                    catch
                    {
                        // A logging failure must not prevent the remaining callbacks.
                    }
                }
            }
        }

        /// <summary>Reads the live native occupant.</summary>
        /// <returns>Null if the seat or occupant is missing or destroyed; otherwise the native player.</returns>
        private S1Player? ReadOccupant()
        {
            if (ReferenceEquals(_seat, null) || _seat == null)
                return null;
            var occupant = _seat.Occupant;
            return ReferenceEquals(occupant, null) || occupant == null ? null : occupant;
        }

        /// <summary>Compares player identity without treating a destroyed Unity object as null.</summary>
        /// <param name="left">The first native player, or null.</param>
        /// <param name="right">The second native player, or null.</param>
        /// <returns>Whether native pointers match on IL2CPP or managed references match on Mono.</returns>
        private static bool SameIdentity(S1Player? left, S1Player? right)
        {
            // Unity equality treats a destroyed player as null, which would hide disconnects.
#if IL2CPPMELON
            return (left?.Pointer ?? IntPtr.Zero) == (right?.Pointer ?? IntPtr.Zero);
#else
            return ReferenceEquals(left, right);
#endif
        }

        /// <summary>Finds the first managed player with the supplied native identity in <see cref="Player.All"/>.</summary>
        /// <param name="native">The native player to resolve, or null.</param>
        /// <returns>The matching managed player, or null if the input is null or no match exists.</returns>
        private static Player? ResolvePlayer(S1Player? native)
        {
            if (ReferenceEquals(native, null))
                return null;
            foreach (var player in Player.All)
            {
                if (SameIdentity(player.S1Player, native))
                    return player;
            }
            return null;
        }
    }
}
