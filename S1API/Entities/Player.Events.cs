using System;
using S1API.Internal.Entities;
using S1API.Products;
using S1API.Vehicles;

namespace S1API.Entities
{
    public partial class Player
    {
        private Action<ProductInstance>? _productConsumedHandlers;
        private Action<LandVehicle>? _exitingVehicleHandlers;
        private Action<LandVehicle?>? _vehicleChangedHandlers;

        /// <summary>
        /// Called when this player consumes a product.
        /// </summary>
        /// <remarks>
        /// Raised on the peer where the player is the local player, when the game's consume call runs. It is not
        /// raised for a remote player. A handler that throws is logged and the other handlers still run.
        /// </remarks>
        public event Action<ProductInstance> OnProductConsumed
        {
            add => _productConsumedHandlers += value;
            remove => _productConsumedHandlers -= value;
        }

        /// <summary>
        /// Called when this player is about to leave a vehicle, before the game moves the body to the exit point.
        /// </summary>
        /// <remarks>
        /// Raised for the local player only; a remote player's exit is reported by <see cref="OnVehicleChanged"/>.
        /// The game's own exit runs after all handlers. A handler that throws is logged and the other handlers still run.
        /// </remarks>
        public event Action<LandVehicle> OnExitingVehicle
        {
            add => _exitingVehicleHandlers += value;
            remove => _exitingVehicleHandlers -= value;
        }

        /// <summary>
        /// Called after this player's vehicle changed, with the new vehicle or null when the player left one.
        /// </summary>
        /// <remarks>
        /// Raised for the local player and for remote players. A remote player's change arrives when the game
        /// applies the synced vehicle on this peer. A handler that throws is logged and the other handlers still run.
        /// </remarks>
        public event Action<LandVehicle?> OnVehicleChanged
        {
            add => _vehicleChangedHandlers += value;
            remove => _vehicleChangedHandlers -= value;
        }

        internal void RaiseProductConsumed(ProductInstance product)
        {
            PlayerEvents.Info($"{Name} consumed a product.");
            PlayerEvents.Invoke(_productConsumedHandlers, $"OnProductConsumed of {Name}", h => ((Action<ProductInstance>)h)(product));
        }

        internal void RaiseExitingVehicle(LandVehicle vehicle)
        {
            PlayerEvents.Info($"{Name} exiting a vehicle.");
            PlayerEvents.Invoke(_exitingVehicleHandlers, $"OnExitingVehicle of {Name}", h => ((Action<LandVehicle>)h)(vehicle));
        }

        internal void RaiseVehicleChanged(LandVehicle? vehicle)
        {
            PlayerEvents.Info($"{Name} vehicle changed to {(vehicle != null ? "a vehicle" : "none")}.");
            PlayerEvents.Invoke(_vehicleChangedHandlers, $"OnVehicleChanged of {Name}", h => ((Action<LandVehicle?>)h)(vehicle));
        }
    }
}
