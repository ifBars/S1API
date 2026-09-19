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
        private readonly Dictionary<Delegate, List<TNativeHandler>> _registrations = new Dictionary<Delegate, List<TNativeHandler>>();

        internal bool IsEmpty =>
            _registrations.Count == 0;

        internal void Add(Delegate managedHandler, TNativeHandler nativeHandler)
        {
            if (!_registrations.TryGetValue(managedHandler, out var nativeHandlers))
            {
                nativeHandlers = new List<TNativeHandler>();
                _registrations.Add(managedHandler, nativeHandlers);
            }

            nativeHandlers.Add(nativeHandler);
        }

        internal bool TryTakeLast(Delegate managedHandler, out TNativeHandler nativeHandler)
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

        internal IReadOnlyList<(Delegate ManagedHandler, TNativeHandler NativeHandler)> TakeAll()
        {
            var registrations = new List<(Delegate ManagedHandler, TNativeHandler NativeHandler)>();
            foreach (var registration in _registrations)
            {
                foreach (TNativeHandler nativeHandler in registration.Value)
                {
                    registrations.Add((registration.Key, nativeHandler));
                }
            }

            _registrations.Clear();
            return registrations;
        }
    }
}
