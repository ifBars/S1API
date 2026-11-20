#if IL2CPPMELON
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Property = ScheduleOne.Property;
#endif

using System;
using System.Collections.Generic;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Property;

namespace S1API.Internal.Properties
{
    /// <summary>
    /// State behind <see cref="PropertyWrapper.ContentCullingLocked"/> and <see cref="S1API.Property.PropertyManager.OnPropertyLoaded"/>.
    /// </summary>
    /// <remarks>
    /// Wrappers are created per call, so the lock is kept here by property code. Vanilla culls a property's content by
    /// the distance from the camera to the property root in <c>Property.UpdateCulling</c>, which a mod cannot
    /// influence when it moves the content away from that root.
    /// </remarks>
    internal static class PropertyHooks
    {
        private static readonly Log Logger = new Log("PropertyHooks");
        private static readonly HashSet<string> CullLocked = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> RefusedLogged = new HashSet<string>(StringComparer.Ordinal);
        private static bool _lifecycleHooked;

        internal static event Action<PropertyWrapper, int>? Loaded;

        /// <summary>Test hook that receives every log line in place of MelonLoader.</summary>
        internal static Action<string, string>? LogSinkForTesting;

        internal static bool IsCullingLocked(string code) => code != null && CullLocked.Contains(code);

        internal static void SetCullingLocked(string code, bool locked)
        {
            if (string.IsNullOrEmpty(code))
            {
                Warning("ContentCullingLocked was set on a property without a code; ignored.");
                return;
            }
            EnsureLifecycleHook();
            if (locked ? CullLocked.Add(code) : CullLocked.Remove(code))
                Info($"Content culling of '{code}' {(locked ? "locked, its content stays shown" : "unlocked")}.");
            if (!locked)
                RefusedLogged.Remove(code);
        }

        /// <summary>True when a cull request for this property must not run.</summary>
        internal static bool RefuseCull(string code, bool culled)
        {
            if (!culled || !IsCullingLocked(code))
                return false;
            if (RefusedLogged.Add(code))
                Info($"Content of '{code}' stays shown; culling is locked.");
            return true;
        }

        internal static void RaiseLoaded(S1Property.Property property, int savedEmployees)
        {
            var handlers = Loaded;
            if (handlers == null || ReferenceEquals(property, null))
                return;
            var wrapper = new PropertyWrapper(property);
            var list = handlers.GetInvocationList();
            Info($"OnPropertyLoaded for '{property.PropertyCode}' ({savedEmployees} saved employee(s), {list.Length} handler(s)).");
            foreach (var handler in list)
            {
                try { ((Action<PropertyWrapper, int>)handler)(wrapper, savedEmployees); }
                catch (Exception ex) { Error($"OnPropertyLoaded handler {handler.Method.DeclaringType?.FullName}.{handler.Method.Name} threw for '{property.PropertyCode}': {ex}"); }
            }
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;
            _lifecycleHooked = true;
            GameLifecycle.OnPreSceneChange += ClearSceneState;
        }

        // A lock belongs to the loaded world; the next save starts unlocked.
        private static void ClearSceneState()
        {
            if (CullLocked.Count > 0)
                Info($"Content culling locks cleared on scene change ({CullLocked.Count}).");
            CullLocked.Clear();
            RefusedLogged.Clear();
        }

        internal static void ResetForTesting()
        {
            CullLocked.Clear();
            RefusedLogged.Clear();
            Loaded = null;
            LogSinkForTesting = null;
            _lifecycleHooked = true;
        }

        private static void Info(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("info", message); return; }
            Logger.Msg(message);
        }

        private static void Warning(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("warn", message); return; }
            Logger.Warning(message);
        }

        private static void Error(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("error", message); return; }
            Logger.Error(message);
        }
    }
}
