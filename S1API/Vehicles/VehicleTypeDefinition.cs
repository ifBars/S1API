using System;
using UnityEngine;

namespace S1API.Vehicles
{
    /// <summary>
    /// A new vehicle type, cloned from a vanilla vehicle. Register it with <see cref="VehicleRegistry.RegisterType"/>.
    /// </summary>
    /// <remarks>
    /// The clone gets its own vehicle code, so it can be spawned by that code and a save that holds it loads it again.
    /// It is added to FishNet's spawnable prefabs in the same order on every peer, which gives it the same prefab id
    /// everywhere as long as host and clients run the same mods.
    /// </remarks>
    public sealed class VehicleTypeDefinition
    {
        /// <summary>
        /// The new vehicle code, unique across vanilla and every mod. Compared without case, as the game does.
        /// </summary>
        public string Code = string.Empty;

        /// <summary>
        /// The vanilla vehicle code to clone, for example "shitbox" or "hounddog".
        /// </summary>
        public string BaseCode = string.Empty;

        /// <summary>
        /// The display name. Empty keeps the name of the base vehicle.
        /// </summary>
        public string Name = string.Empty;

        /// <summary>
        /// The price. Negative keeps the price of the base vehicle.
        /// </summary>
        public float Price = -1f;

        /// <summary>
        /// The top speed. Negative keeps the top speed of the base vehicle.
        /// </summary>
        public float TopSpeed = -1f;

        /// <summary>
        /// Dresses the clone once, before it is registered: the root GameObject of the inactive template. Runs once
        /// per game process. An exception is logged and the type stays registered with the base look.
        /// </summary>
        public Action<GameObject>? OnBuild;
    }
}
