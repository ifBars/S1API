#if IL2CPPMELON
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1VehicleSeat = Il2CppScheduleOne.Vehicles.VehicleSeat;
#elif MONOMELON
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1VehicleSeat = ScheduleOne.Vehicles.VehicleSeat;
#endif

using System;
using S1API.Entities;
using S1API.Logging;

namespace S1API.Vehicles
{
    /// <summary>
    /// Provides read-only metadata and occupancy state for a land-vehicle seat.
    /// </summary>
    public sealed class VehicleSeatInfo
    {
        private static readonly Log Logger = new Log("VehicleSeatInfo");
        private readonly int _index;
        private readonly S1VehicleSeat _seat;

        internal VehicleSeatInfo(int index, S1VehicleSeat seat)
        {
            _index = index;
            _seat = seat;
        }

        /// <summary>
        /// Gets this seat's stable index in <see cref="LandVehicle.Seats"/>.
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// Gets whether this seat is configured as the driver's seat.
        /// </summary>
        public bool IsDriverSeat => _seat.isDriverSeat;

        /// <summary>
        /// Gets whether this seat has a native player occupant.
        /// </summary>
        public bool IsOccupied => _seat.isOccupied;

        /// <summary>
        /// Gets the managed occupant, or <see langword="null"/> when the seat is empty or no
        /// managed player wrapper is available.
        /// </summary>
        public Player? Occupant => ResolvePlayer(_seat.Occupant);

        /// <summary>
        /// Raised after the native occupant of this seat changes.
        /// </summary>
        /// <remarks>
        /// A previous or current value can be <see langword="null"/> when the corresponding
        /// native player has no managed wrapper.
        /// </remarks>
        public event Action<Player?, Player?>? OccupantChanged;

        internal void NotifyOccupantChanged(
            S1PlayerScripts.Player? previous,
            S1PlayerScripts.Player? current)
        {
            if (HasSameNativeIdentity(previous, current))
                return;

            Invoke(OccupantChanged, ResolvePlayer(previous), ResolvePlayer(current));
        }

        private static Player? ResolvePlayer(S1PlayerScripts.Player? native)
        {
            if (native == null)
                return null;

            foreach (Player player in Player.All)
            {
                if (player.S1Player == native)
                    return player;
            }

            return null;
        }

        internal static bool HasSameNativeIdentity(
            S1PlayerScripts.Player? previous,
            S1PlayerScripts.Player? current)
        {
            if (ReferenceEquals(previous, current))
                return true;

            if (ReferenceEquals(previous, null) || ReferenceEquals(current, null))
                return false;

            return previous == current;
        }

        private static void Invoke(
            Action<Player?, Player?>? handlers,
            Player? previous,
            Player? current)
        {
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
                        Logger.Warning($"An {nameof(OccupantChanged)} subscriber failed: {ex.Message}");
                    }
                    catch
                    {
                        // Logging must not prevent the remaining subscribers from running.
                    }
                }
            }
        }
    }
}
