using UnityEngine;

using System;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Utilities for the <see cref="Color"/> class.
    /// This class is intended for internal API use only. Mod developers should use <see cref="S1API.Utils.ColorUtils"/> instead.
    /// </summary>
    [Obsolete("This class is for internal API use only. Mod developers should use S1API.Utils.ColorUtils instead. This class will be made internal in a future version.")]
    public static class ColorUtils
    {
        /// <summary>
        /// Convert's a <see cref="int"/> value to <see cref="Color"/>
        /// </summary>
        /// <param name="hexColor"></param>
        /// <returns></returns>
        public static Color ToColor(this uint hexColor)
            => ToColorInternal(hexColor);

        private static Color ToColorInternal(uint hexColor)
        {
            float a = (hexColor >> 24 & 0xFF) / 255f;
            float r = (hexColor >> 16 & 0xFF) / 255f;
            float g = (hexColor >> 8 & 0xFF) / 255f;
            float b = (hexColor & 0xFF) / 255f;
            return new Color(r, g, b, a);
        }
    }
}
