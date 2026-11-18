using S1API.Internal.Properties;
using UnityEngine;

namespace S1API.Property
{
    public partial class PropertyWrapper
    {
        /// <summary>
        /// Appends one employee idle point at a position. An employee waits there when it has nothing to do.
        /// </summary>
        /// <param name="position">World position of the point.</param>
        /// <param name="rotation">World rotation; the employee turns to its forward once there.</param>
        /// <returns>The new point, a child of the property. Null when the property is gone; that is logged as an error.</returns>
        public Transform? AddEmployeeIdlePoint(Vector3 position, Quaternion rotation) =>
            EmployeeIdlePointsRuntime.Add(InnerProperty, position, rotation);

        /// <summary>
        /// Makes sure the property has at least <paramref name="count"/> employee idle points. Missing points copy the
        /// position and rotation of the last one, so extra employees share that spot. Use
        /// <see cref="AddEmployeeIdlePoint"/> first for spots of your own.
        /// </summary>
        /// <param name="count">The number of points the property should have.</param>
        public void EnsureEmployeeIdlePoints(int count) =>
            EmployeeIdlePointsRuntime.Ensure(InnerProperty, count, "EnsureEmployeeIdlePoints");
    }
}
