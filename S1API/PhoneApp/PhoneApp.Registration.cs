using System;
using S1API.Internal.Abstraction;
using S1API.Internal.PhoneApps;
using S1API.Internal.Patches;

namespace S1API.PhoneApp
{
    public abstract partial class PhoneApp
    {
        /// <summary>
        /// Adds a phone app instance to the home screen, next to the apps S1API finds by their class.
        /// </summary>
        /// <remarks>
        /// Use it for apps built at runtime, for example several apps of one class or an app without a public
        /// parameterless constructor. If the home screen already exists, the app is built at once; otherwise it is
        /// built when the home screen starts. The app stays registered across scene loads and is built again on every
        /// home screen start, until <see cref="Unregister"/> is called. Registering the same instance twice, or a
        /// second app with an AppName already in use, logs a warning and does nothing. A registered instance replaces
        /// the instance S1API would create for its class.
        /// </remarks>
        /// <param name="app">The app instance to show.</param>
        /// <exception cref="ArgumentNullException"><paramref name="app"/> is null.</exception>
        public static void Register(PhoneApp app)
        {
            if (ReferenceEquals(app, null))
                throw new ArgumentNullException(nameof(app));
            if (!PhoneAppRuntime.Add(app))
                return;
            if (PhoneAppRuntime.TryGetHome(out var home))
                PhoneAppRuntime.Spawn(app, home!, "at once");
            else
                PhoneAppRuntime.Info($"Phone app {app.AppName} waits for the home screen.");
        }

        /// <summary>
        /// Removes a phone app instance added with <see cref="Register"/>, together with its icon and panel.
        /// </summary>
        /// <param name="app">The app instance to remove.</param>
        /// <returns>True when the app was registered and is now removed.</returns>
        public static bool Unregister(PhoneApp app)
        {
            if (ReferenceEquals(app, null))
                throw new ArgumentNullException(nameof(app));
            if (!PhoneAppRuntime.Remove(app))
                return false;
            try
            {
                if (!app.IsDestroying)
                    app.DestroyInternal();
                PhoneAppRegistry.RegisteredApps.Remove(app);
                PhoneAppRuntime.Info($"Phone app {app.AppName} unregistered and removed from the home screen.");
            }
            catch (Exception ex)
            {
                PhoneAppRuntime.Error($"Removing phone app {app.AppName} failed: {ex}");
            }
            return true;
        }

        internal string RuntimeName => AppName;

        /// <summary>True after a runtime build of this app; stays set across respawns.</summary>
        internal bool RuntimeBuilt;

        /// <summary>
        /// Clears the state of an earlier build, so a runtime app can be built again on a new home screen.
        /// </summary>
        internal void PrepareRespawn()
        {
            _isDestroying = false;
            // Delegates bound to the phone of an earlier scene; the new build subscribes fresh ones.
            _closeAppAction = null;
            _onPhoneClosedAction = null;
            _iconModified = false;
            _iconImage = null;
            _iconObject = null;
            _appPanel = null;
            _appContainer = null;
        }
    }
}
