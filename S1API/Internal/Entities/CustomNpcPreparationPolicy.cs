using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// Selects prepared custom NPC instances for reuse across native loader phases.
    /// </summary>
    internal static class CustomNpcPreparationPolicy
    {
        internal static T? FindExactType<T>(
            IEnumerable<T> instances,
            Type requestedType)
            where T : class =>
            instances.FirstOrDefault(
                instance => instance != null && instance.GetType() == requestedType);
    }
}
