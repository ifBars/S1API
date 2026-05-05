using System;
using S1API.Logging;
using UnityEngine;
#if IL2CPPMELON
using Il2CppInterop.Runtime;
#endif

namespace S1API.Internal.Diagnostics
{
    /// <summary>
    /// INTERNAL: Hooks Unity's threaded log callback to emit detailed stack traces for NullReferenceException logs.
    /// </summary>
    internal static class UnityExceptionTraceHook
    {
        private static readonly Log Logger = new Log("S1API.UnityExceptionTrace");
        private static readonly object Sync = new object();
#if IL2CPPMELON
        private static readonly System.Action<string, string, LogType> ManagedCallback = OnUnityLogMessageReceived;
        private static readonly Application.LogCallback Callback = DelegateSupport.ConvertDelegate<Application.LogCallback>(ManagedCallback);
#endif

        private static string _lastExceptionSignature;
        private static DateTime _lastExceptionAtUtc;
        private static bool _installed;

        internal static void Install()
        {
            if (_installed)
            {
                return;
            }

#if IL2CPPMELON
            Application.add_logMessageReceivedThreaded(Callback);
#else
            Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
#endif
            _installed = true;
        }

        internal static void Remove()
        {
            if (!_installed)
            {
                return;
            }

#if IL2CPPMELON
            _installed = false;
            return;
#else
            try
            {
                Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
            }
            catch (Exception ex)
            {
                Logger.Warning($"[Unity] Failed to remove threaded log callback during shutdown: {ex.Message}");
            }

            _installed = false;
#endif
        }

        private static void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            if (!LooksLikeNullReference(condition, stackTrace))
            {
                return;
            }

            string signature = string.Concat(condition ?? string.Empty, "\n", stackTrace ?? string.Empty);
            if (ShouldSuppressDuplicate(signature))
            {
                return;
            }

            Logger.Error($"[Unity] {condition}");

            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                Logger.Error($"[Unity] stack trace:\n{stackTrace}");
            }
        }

        private static bool LooksLikeNullReference(string condition, string stackTrace)
        {
            string haystack = string.Concat(condition ?? string.Empty, "\n", stackTrace ?? string.Empty);
            return haystack.Contains("NullReferenceException", StringComparison.Ordinal);
        }

        private static bool ShouldSuppressDuplicate(string signature)
        {
            lock (Sync)
            {
                DateTime now = DateTime.UtcNow;
                if (signature == _lastExceptionSignature && (now - _lastExceptionAtUtc).TotalSeconds < 1)
                {
                    return true;
                }

                _lastExceptionSignature = signature;
                _lastExceptionAtUtc = now;
                return false;
            }
        }
    }
}
