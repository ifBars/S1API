using System;

namespace S1API.Items.Buildable
{
    /// <summary>
    /// Surface types accepted by surface-placed furniture.
    /// </summary>
    [Flags]
    public enum FurnitureSurfaceType
    {
        /// <summary>No surface type.</summary>
        None = 0,

        /// <summary>Vertical wall surfaces.</summary>
        Wall = 1,

        /// <summary>Horizontal roof or ceiling surfaces.</summary>
        Roof = 2,

        /// <summary>All supported surface types.</summary>
        All = Wall | Roof,
    }
}
