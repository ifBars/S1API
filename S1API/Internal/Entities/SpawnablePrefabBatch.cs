using System;
using System.Collections.Generic;

namespace S1API.Internal.Entities
{
    internal sealed class SpawnablePrefabBatch<TPrefab>
    {
        private readonly HashSet<string> _names;
        private readonly List<TPrefab> _pending = new List<TPrefab>();

        internal SpawnablePrefabBatch(IEnumerable<string> registeredNames)
        {
            _names = new HashSet<string>(registeredNames, StringComparer.Ordinal);
        }

        internal int Count =>
            _pending.Count;

        internal void Add(string name, TPrefab prefab)
        {
            if (_names.Add(name))
                _pending.Add(prefab);
        }

        internal void Register(
            Action<IReadOnlyList<TPrefab>> registerBatch,
            Action<TPrefab> registerOne,
            Action<Exception> onBatchFailure,
            Action<TPrefab, Exception> onPrefabFailure)
        {
            if (_pending.Count == 0)
                return;

            try
            {
                registerBatch(_pending);
            }
            catch (Exception exception)
            {
                onBatchFailure(exception);
                // FishNet can append before initialization fails. The fallback callback
                // must check native object identity so partially appended entries are not duplicated.
                foreach (TPrefab prefab in _pending)
                {
                    try
                    {
                        registerOne(prefab);
                    }
                    catch (Exception inner)
                    {
                        onPrefabFailure(prefab, inner);
                    }
                }
            }
        }
    }
}
