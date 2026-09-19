using System;
using System.Collections.Generic;

namespace S1API.Entities
{
    internal sealed class NPCDialogueCallbackRegistry
    {
        private readonly Dictionary<string, List<Action>> _keyedCallbacks =
            new Dictionary<string, List<Action>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action> _callbacks = new List<Action>();

        internal bool HasCallbacks =>
            _keyedCallbacks.Count > 0 || _callbacks.Count > 0;

        internal void Add(string key, Action callback)
        {
            if (!_keyedCallbacks.TryGetValue(key, out var callbacks))
            {
                callbacks = new List<Action>();
                _keyedCallbacks[key] = callbacks;
            }

            callbacks.Add(callback);
        }

        internal void Add(Action callback) =>
            _callbacks.Add(callback);

        internal void Clear()
        {
            _keyedCallbacks.Clear();
            _callbacks.Clear();
        }

        internal void Invoke(string key)
        {
            if (string.IsNullOrEmpty(key) || !_keyedCallbacks.TryGetValue(key, out var callbacks))
                return;

            for (int i = 0; i < callbacks.Count; i++)
                InvokeSafely(callbacks[i]);
        }

        internal void InvokeAll()
        {
            for (int i = 0; i < _callbacks.Count; i++)
                InvokeSafely(_callbacks[i]);
        }

        private static void InvokeSafely(Action callback)
        {
            try { callback?.Invoke(); } catch { }
        }
    }
}
