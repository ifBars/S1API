using System;
using S1API.Internal.Vehicles;

namespace S1API.Vehicles
{
    public static partial class VehicleRegistry
    {
        /// <summary>
        /// Raised after the game's vehicle manager has taken every registered type, once per world load and once
        /// after a type registered while a world is already loaded.
        /// </summary>
        public static event Action? OnTypesRegistered;

        /// <summary>
        /// Registers a new vehicle type cloned from a vanilla vehicle. Call it once, for example in
        /// <c>OnInitializeMelon</c>. The type is built when the game's vehicle manager wakes up, or at once when a
        /// world is already loaded.
        /// </summary>
        /// <param name="definition">The type to add.</param>
        /// <returns>False when the definition is invalid or its code is taken; the reason is logged as an error.</returns>
        public static bool RegisterType(VehicleTypeDefinition definition) => VehicleTypeRuntime.Register(definition);

        /// <summary>
        /// True when a mod registered a type with this code. Compared without case.
        /// </summary>
        public static bool IsTypeRegistered(string code) => VehicleTypeRuntime.IsRegistered(code);

        internal static void RaiseTypesRegistered()
        {
            Action? handlers = OnTypesRegistered;
            if (handlers == null)
                return;

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action)handler)();
                }
                catch (Exception ex)
                {
                    VehicleTypeRuntime.Fail($"OnTypesRegistered handler of {VehicleTypeRuntime.OwnerOf(handler)} threw. {ex}");
                }
            }
        }
    }
}
