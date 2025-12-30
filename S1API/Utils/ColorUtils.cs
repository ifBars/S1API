using UnityEngine;

namespace S1API.Utils
{
    /// <summary>
    /// Utilities for the <see cref="Color"/> class.
    /// This class is intended for public use by mod developers.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Convert's a <see cref="int"/> value to <see cref="Color"/>
        /// </summary>
        /// <param name="hexColor"></param>
        /// <returns></returns>
        public static Color ToColor(this uint hexColor) =>
            Internal.Utils.ColorUtils.ToColor(hexColor);
    }
}

