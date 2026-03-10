using UnityEngine;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// Internal transform hierarchy helpers.
    /// </summary>
    internal static class TransformUtils
    {
        /// <summary>
        /// Finds a descendant by name using depth-first search. Use this when the target
        /// may not be a direct child (e.g. AppIcons under Viewport after scroll patch).
        /// </summary>
        /// <param name="root">Root transform to search under.</param>
        /// <param name="name">Name of the transform to find.</param>
        /// <returns>The first matching transform, or null if not found.</returns>
        internal static Transform? FindDescendant(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            if (string.Equals(root.name, name, System.StringComparison.Ordinal))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
