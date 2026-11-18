#if IL2CPPMELON
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Property = ScheduleOne.Property;
#endif

namespace S1API.Internal.Properties
{
    /// <summary>
    /// Looks up property objects in the loaded world by code.
    /// </summary>
    internal static class CustomPropertyWorld
    {
        /// <summary>The property or business with this code, or null when the world has none.</summary>
        internal static S1Property.Property? Find(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;
            var all = S1Property.Property.Properties;
            for (int i = 0; all != null && i < all.Count; i++)
            {
                var p = all[i];
                if (p != null && p.PropertyCode == code)
                    return p;
            }
            var businesses = S1Property.Business.Businesses;
            for (int i = 0; businesses != null && i < businesses.Count; i++)
            {
                var b = businesses[i];
                if (b != null && b.PropertyCode == code)
                    return b;
            }
            return null;
        }
    }
}
