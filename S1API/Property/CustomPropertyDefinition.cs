using System;
using UnityEngine;

namespace S1API.Property
{
    /// <summary>
    /// Describes a property the vanilla game does not know, for <see cref="PropertyManager.Register"/>.
    /// The mod builds the property object itself; S1API keeps its save data and defers what must wait for it.
    /// </summary>
    public sealed class CustomPropertyDefinition
    {
        /// <summary>
        /// The property code the world property carries (<see cref="PropertyWrapper.PropertyCode"/>). Required and unique.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// The name the world property carries. The save file is named after it. When set and the world property has a
        /// different name, S1API logs a warning.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The price applied to the world property once it exists. Values of zero or less leave the price as built.
        /// </summary>
        public float Price { get; set; }

        /// <summary>
        /// Returns the transform the property's world content hangs under, or null while the mod has not built it.
        /// Loading the property's saved objects and employees, and the start of the heatmap, wait until this returns a
        /// transform and the property is in the world. Leave it null to wait for the property alone.
        /// </summary>
        public Func<Transform?>? Root { get; set; }

        /// <summary>
        /// The employee capacity applied to the world property once it exists. Values of zero or less leave it as built.
        /// </summary>
        public int EmployeeCapacity { get; set; }

        /// <summary>
        /// When true (the default), the save file of this property stays on disk while the property is not in the world,
        /// for example because the mod failed to build it. Vanilla deletes save files it did not list.
        /// </summary>
        public bool KeepSaveWhenMissing { get; set; } = true;
    }
}
