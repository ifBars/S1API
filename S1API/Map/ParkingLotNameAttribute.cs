using System;

namespace S1API.Map
{
    /// <summary>
    /// Attribute to mark a parking lot identifier class with its GameObject name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ParkingLotNameAttribute : Attribute
    {
        /// <summary>
        /// The GameObject name of the parking lot.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the ParkingLotNameAttribute.
        /// </summary>
        /// <param name="name">The GameObject name of the parking lot.</param>
        public ParkingLotNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
