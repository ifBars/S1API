using System;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Diagnostics
{
    /// <summary>
    /// INTERNAL: Hooks Unity's threaded log callback to emit detailed stack traces for NullReferenceException logs.
    /// </summary>
    internal static class UnityExceptionTraceHook
    {
        private static readonly Log Logger = new Log("UnityExceptionTrace");
        private static readonly object Sync = new object();

        private static string _lastExceptionSignature;
        private static DateTime _lastExceptionAtUtc;
        private static bool _installed;

        internal static void Install()
        {
            if (_installed)
            {
                return;
            }

            Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
            _installed = true;
        }

        internal static void Remove()
        {
            if (!_installed)
            {
                return;
            }

            Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
            _installed = false;
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
