#if (IL2CPPMELON)
using S1Properties = Il2CppScheduleOne.Properties;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Properties = ScheduleOne.Properties;
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using S1API.Properties.Interfaces;

namespace S1API.Properties.Internal
{
    /// <summary>
    /// INTERNAL: Resolves API property wrappers/tokens to runtime game properties.
    /// </summary>
    internal static class PropertyResolver
    {
        private static readonly string[] SearchPaths =
        {
            "Properties/Tier1",
            "Properties/Tier2",
            "Properties/Tier3",
            "Properties/Tier4",
            "Properties/Tier5"
        };

        internal static List<S1Properties.Property> ResolveToGameProperties(IEnumerable<PropertyBase> items)
        {
            var results = new List<S1Properties.Property>();
            if (items == null)
                return results;

            foreach (var item in items)
            {
                if (item == null)
                    continue;

                // Direct wrapper around a game property
                if (item is ProductPropertyWrapper wrapper)
                {
                    var inner = wrapper.InnerProperty;
                    if (inner != null && !results.Contains(inner))
                        results.Add(inner);
                    continue;
                }

                // Token: resolve by ID first, then by Unity name
                var found = FindByIdOrName(item.ID, item.name);
                if (found != null && !results.Contains(found))
                    results.Add(found);
            }

            return results;
        }

        private static S1Properties.Property FindByIdOrName(string id, string unityName)
        {
            var idNorm = (id ?? string.Empty).Trim();
            var nameNorm = (unityName ?? string.Empty).Trim();

            foreach (var path in SearchPaths)
            {
                var props = Resources.LoadAll<S1Properties.Property>(path);
                if (props == null || props.Length == 0)
                    continue;

                if (!string.IsNullOrEmpty(idNorm))
                {
                    var byId = props.FirstOrDefault(p => p != null && string.Equals(p.ID, idNorm, System.StringComparison.OrdinalIgnoreCase));
                    if (byId != null)
                        return byId;
                }

                if (!string.IsNullOrEmpty(nameNorm))
                {
                    var byName = props.FirstOrDefault(p => p != null && string.Equals(p.name, nameNorm, System.StringComparison.OrdinalIgnoreCase));
                    if (byName != null)
                        return byName;
                }
            }

            return null;
        }
    }
}


