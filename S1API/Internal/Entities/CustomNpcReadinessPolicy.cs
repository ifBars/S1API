using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// Determines whether every registered custom NPC type completed runtime hydration.
    /// </summary>
    internal static class CustomNpcReadinessPolicy
    {
        internal static void MarkFinalized(Type customNpcType, ISet<Type> finalizedTypes) =>
            finalizedTypes.Add(customNpcType);

        internal static bool AreAllTypesFinalized(
            IEnumerable<Type> customNpcTypes,
            ISet<Type> finalizedTypes)
        {
            var expectedTypes = customNpcTypes as IReadOnlyCollection<Type>
                ?? customNpcTypes.ToList();

            return expectedTypes.Count > 0
                && expectedTypes.All(finalizedTypes.Contains);
        }
    }
}
