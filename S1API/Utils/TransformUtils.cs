using UnityEngine;

namespace S1API.Utils
{
    /// <summary>
    /// Utilities for working with Unity <see cref="Transform"/> hierarchies.
    /// This class is intended for public use by mod developers.
    /// </summary>
    public static class TransformUtils
    {
        /// <summary>
        /// Finds a descendant by name using depth-first search. Use this when the target
        /// may not be a direct child (e.g. nested under a viewport or scroll container).
        /// </summary>
        /// <param name="root">Root transform to search under.</param>
        /// <param name="name">Name of the transform to find.</param>
        /// <returns>The first matching transform, or null if not found.</returns>
        public static Transform FindDescendant(Transform root, string name) =>
            Internal.Utils.TransformUtils.FindDescendant(root, name);
    }
}
