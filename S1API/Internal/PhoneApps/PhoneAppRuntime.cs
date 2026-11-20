using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using S1API.Internal.Abstraction;
using S1API.Logging;

#if IL2CPPMELON
using Il2CppScheduleOne.UI.Phone;
#elif MONOMELON
using ScheduleOne.UI.Phone;
#endif

namespace S1API.Internal.PhoneApps
{
    /// <summary>
    /// INTERNAL: Phone app instances registered at runtime through <see cref="PhoneApp.PhoneApp.Register"/>.
    /// </summary>
    internal static class PhoneAppRuntime
    {
        private static readonly Log Logger = new Log("PhoneApp");
        private static readonly List<PhoneApp.PhoneApp> Apps = new List<PhoneApp.PhoneApp>();

        /// <summary>The home screen of the Main scene, set when it starts.</summary>
        internal static HomeScreen? Home;

        internal static Action<string, string>? LogSinkForTesting;

        internal static int Count => Apps.Count;

        internal static bool Contains(PhoneApp.PhoneApp app) => Apps.Contains(app);

        internal static bool Add(PhoneApp.PhoneApp app)
        {
            if (Apps.Contains(app))
            {
                Warn($"Phone app {app.RuntimeName} is already registered; the second call does nothing.");
                return false;
            }
            // The app panel is found by AppName, so a second app with the same name would draw into the first one.
            foreach (var other in Apps)
            {
                if (string.Equals(other.RuntimeName, app.RuntimeName, StringComparison.Ordinal))
                {
                    Warn($"Phone app {app.RuntimeName} is not registered: another registered app uses the same AppName.");
                    return false;
                }
            }
            Apps.Add(app);
            Info($"Phone app {app.RuntimeName} registered at runtime.");
            return true;
        }

        internal static bool Remove(PhoneApp.PhoneApp app)
        {
            if (Apps.Remove(app))
                return true;
            Warn($"Phone app {app.RuntimeName} was not registered at runtime; nothing to remove.");
            return false;
        }

        /// <summary>The current home screen, when it still exists.</summary>
        internal static bool TryGetHome(out HomeScreen? home)
        {
            home = null;
            if (ReferenceEquals(Home, null))
                return false;
            if (!HomeAlive())
            {
                Home = null;
                return false;
            }
            home = Home;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool HomeAlive() => Home != null;

        /// <summary>Builds every runtime app on a home screen that just started.</summary>
        internal static void SpawnAll(HomeScreen home)
        {
            Home = home;
            if (Apps.Count == 0)
                return;
            Info($"Building {Apps.Count} runtime phone app(s) on the new home screen.");
            foreach (var app in Apps.ToArray())
                Spawn(app, home, "on the home screen start");
        }

        /// <summary>True when a registered instance has exactly this type, so the home screen does not create another.</summary>
        internal static bool HasInstanceOf(Type type)
        {
            foreach (var app in Apps)
            {
                if (app.GetType() == type)
                    return true;
            }
            return false;
        }

        /// <summary>A live app other than <paramref name="app"/> that already uses its AppName, if any.</summary>
        internal static PhoneApp.PhoneApp? FindNameClash(PhoneApp.PhoneApp app)
        {
            foreach (var other in Patches.PhoneAppRegistry.RegisteredApps)
            {
                if (!ReferenceEquals(other, app) && !other.IsDestroying &&
                    string.Equals(other.RuntimeName, app.RuntimeName, StringComparison.Ordinal))
                    return other;
            }
            return null;
        }

        internal static void Spawn(PhoneApp.PhoneApp app, HomeScreen home, string when)
        {
            try
            {
                var clash = FindNameClash(app);
                if (clash != null)
                {
                    Warn($"Phone app {app.RuntimeName} is not shown: {clash.GetType().FullName} already uses that AppName.");
                    return;
                }
                // A build whose panel was not destroyed with the old scene still holds its handlers; tear it down
                // first so OnDestroyed runs once for every OnCreated.
                if (app.RuntimeBuilt && !app.IsDestroying)
                {
                    Info($"Phone app {app.RuntimeName}: removing its earlier build before building it again.");
                    app.DestroyInternal();
                }
                app.PrepareRespawn();
                ((IRegisterable)app).CreateInternal();
                app.SpawnUI(home);
                app.SpawnIcon(home);
                app.RuntimeBuilt = true;
                Info($"Phone app {app.RuntimeName} shown {when}.");
            }
            catch (Exception ex)
            {
                Error($"Building runtime phone app {app.RuntimeName} failed: {ex}");
            }
        }

        internal static void ResetForTesting()
        {
            Apps.Clear();
            Home = null;
            LogSinkForTesting = null;
        }

        internal static void Info(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("info", message);
            else Logger.Msg(message);
        }

        internal static void Warn(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("warn", message);
            else Logger.Warning(message);
        }

        internal static void Error(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("error", message);
            else Logger.Error(message);
        }
    }
}
