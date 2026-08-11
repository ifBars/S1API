using System;
using System.Collections.Generic;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// Tracks one native registration per managed event add operation.
    /// </summary>
    /// <typeparam name="TNativeHandler">The runtime-specific handler type.</typeparam>
    internal sealed class ManagedEventRegistrationTracker<TNativeHandler>
    {
        private readonly Dictionary<Action, List<TNativeHandler>> _registrations = new Dictionary<Action, List<TNativeHandler>>();

        internal void Add(Action managedHandler, TNativeHandler nativeHandler)
        {
            if (!_registrations.TryGetValue(managedHandler, out var nativeHandlers))
            {
                nativeHandlers = new List<TNativeHandler>();
                _registrations.Add(managedHandler, nativeHandlers);
            }

            nativeHandlers.Add(nativeHandler);
        }

        internal bool TryTakeLast(Action managedHandler, out TNativeHandler nativeHandler)
        {
            if (!_registrations.TryGetValue(managedHandler, out var nativeHandlers)
                || nativeHandlers.Count == 0)
            {
                nativeHandler = default!;
                return false;
            }

            int lastIndex = nativeHandlers.Count - 1;
            nativeHandler = nativeHandlers[lastIndex];
            nativeHandlers.RemoveAt(lastIndex);
            if (nativeHandlers.Count == 0)
            {
                _registrations.Remove(managedHandler);
            }

            return true;
        }
    }
}
