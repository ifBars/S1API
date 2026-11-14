using System;
using System.Collections.Generic;

namespace S1API.Internal.Entities
{
    internal sealed class PrefabRegistrationFailures<TPrefab>
    {
        private readonly Dictionary<string, TPrefab> _failed =
            new Dictionary<string, TPrefab>(StringComparer.Ordinal);

        internal void Record(string name, TPrefab prefab) =>
            _failed[name] = prefab;

        internal void Retry(string name, Action<TPrefab> register)
        {
            if (!_failed.TryGetValue(name, out var prefab))
                return;

            // Retain the entry when registration throws, including failures after
            // FishNet appended the object but before it finished initialization.
            register(prefab);
            _failed.Remove(name);
        }
    }
}
