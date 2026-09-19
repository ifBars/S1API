using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Internal.Entities
{
    internal static class NPCRelationshipGraphPolicy
    {
        internal static IReadOnlyList<string> BuildUndirectedConnectionIds(
            string ownerId,
            IReadOnlyDictionary<string, IReadOnlyList<string>> declarations)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || declarations == null)
                return Array.Empty<string>();

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (declarations.TryGetValue(ownerId, out IReadOnlyList<string>? outgoing))
            {
                foreach (string id in outgoing)
                {
                    if (!string.IsNullOrWhiteSpace(id)
                        && !string.Equals(id, ownerId, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(id);
                    }
                }
            }

            foreach (KeyValuePair<string, IReadOnlyList<string>> declaration in declarations)
            {
                if (string.Equals(declaration.Key, ownerId, StringComparison.OrdinalIgnoreCase)
                    || declaration.Value == null
                    || !declaration.Value.Contains(ownerId, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(declaration.Key);
            }

            return result.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
