using System;
using System.Collections.Generic;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Clears every <see cref="SceneObjectIndex{TObject}"/> on scene change.
    /// </summary>
    internal static class SceneObjectIndexes
    {
        // Non-generic so one call clears indexes of every object type.
        private static readonly List<Action> Resets = new List<Action>();

        internal static void Register(Action reset)
        {
            lock (Resets)
                Resets.Add(reset);
        }

        /// <summary>Clears every scene object index. Called from SceneStateCleaner on scene change.</summary>
        internal static void ResetAll()
        {
            Action[] resets;
            lock (Resets)
                resets = Resets.ToArray();
            foreach (var reset in resets)
                reset();
        }
    }

    /// <summary>
    /// INTERNAL: Index of loaded Unity objects built from one scan, so repeated lookups do not each scan the scene.
    /// Only found objects are reused, and only while they are alive and still have the key they were indexed under;
    /// a lookup that finds nothing scans again, so an object created after the index was built is still found.
    /// </summary>
    internal sealed class SceneObjectIndex<TObject> where TObject : class
    {
        private readonly Func<IEnumerable<TObject>> _scan;
        private readonly Func<TObject, string?> _key;
        private readonly Func<TObject?, bool> _alive;
        private readonly Func<TObject, bool> _include;
        private readonly StringComparer _comparer;
        private Dictionary<string, List<TObject>>? _byKey;
        private List<TObject> _ordered = new List<TObject>();
        private List<string?> _orderedKeys = new List<string?>();

        internal SceneObjectIndex(
            Func<IEnumerable<TObject>> scan,
            Func<TObject, string?> key,
            Func<TObject?, bool> alive,
            StringComparer? comparer = null,
            Func<TObject, bool>? include = null)
        {
            _scan = scan;
            _key = key;
            _alive = alive;
            _include = include ?? (_ => true);
            _comparer = comparer ?? StringComparer.Ordinal;
            SceneObjectIndexes.Register(Invalidate);
        }

        /// <summary>Number of scans since creation, for diagnostics and tests.</summary>
        internal int Scans { get; private set; }

        internal void Invalidate()
        {
            _byKey = null;
            _ordered.Clear();
            _orderedKeys.Clear();
        }

        /// <summary>Invalidates a populated snapshot when the native lifecycle assigns a new object.</summary>
        internal void NotifyChanged(TObject item)
        {
            if (_byKey != null && !_ordered.Contains(item))
                Invalidate();
        }

        private bool IsSnapshotValid()
        {
            for (int i = 0; i < _ordered.Count; i++)
            {
                TObject item = _ordered[i];
                if (!_alive(item) || !_comparer.Equals(_key(item), _orderedKeys[i]))
                    return false;
            }
            return true;
        }

        /// <summary>The live objects whose key matches; empty only if a fresh scan finds none.</summary>
        internal List<TObject> Get(string? key)
        {
            var result = new List<TObject>();
            if (string.IsNullOrEmpty(key))
                return result;

            if (_byKey == null || !IsSnapshotValid() || !TryCollect(key!, result))
            {
                Build();
                result.Clear();
                TryCollect(key!, result);
            }
            return result;
        }

        /// <summary>All live objects, in scan order; empty only if a fresh scan finds none.</summary>
        internal List<TObject> All()
        {
            var result = new List<TObject>();
            if (_byKey == null || !TryCollectAll(result))
            {
                Build();
                result.Clear();
                TryCollectAll(result);
            }
            return result;
        }

        private bool TryCollect(string key, List<TObject> result)
        {
            if (!_byKey!.TryGetValue(key, out var found))
                return false;
            foreach (var item in found)
            {
                if (!_alive(item) || !_comparer.Equals(_key(item) ?? string.Empty, key))
                    return false;
                if (_include(item))
                    result.Add(item);
            }
            return true;
        }

        private bool TryCollectAll(List<TObject> result)
        {
            if (_ordered.Count == 0)
                return false;
            foreach (var item in _ordered)
            {
                if (!_alive(item))
                    return false;
                if (_include(item))
                    result.Add(item);
            }
            return true;
        }

        private void Build()
        {
            Scans++;
            var index = new Dictionary<string, List<TObject>>(_comparer);
            var ordered = new List<TObject>();
            var orderedKeys = new List<string?>();
            foreach (var item in _scan())
            {
                if (!_alive(item))
                    continue;
                ordered.Add(item);
                string? key = _key(item);
                orderedKeys.Add(key);
                if (string.IsNullOrEmpty(key))
                    continue;
                if (!index.TryGetValue(key!, out var list))
                    index[key!] = list = new List<TObject>();
                list.Add(item);
            }
            _ordered = ordered;
            _orderedKeys = orderedKeys;
            _byKey = index;
        }
    }
}
