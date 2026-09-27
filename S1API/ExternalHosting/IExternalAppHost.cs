using System;
using UnityEngine;

namespace S1API.ExternalHosting
{
    /// <summary>
    /// Opts a phone or TV app into creating an independent UI for another display.
    /// The original device instance and its canvas remain owned by S1API.
    /// </summary>
    public interface IExternalAppHost
    {
        /// <summary>
        /// Whether this app currently permits an independent display session.
        /// Return false when an equivalent app is already supplied by the display mod.
        /// After changing this value, call <see cref="ExternalAppCatalog.Refresh"/> on the game thread
        /// to update catalog membership and notify displays.
        /// </summary>
        bool AllowExternalHosting { get; }

        /// <summary>
        /// Creates a new session under a display-owned container. The session must not
        /// call the phone or TV navigation methods or move the original device UI.
        /// </summary>
        /// <param name="container">The UI root owned by the external display.</param>
        /// <param name="requestClose">Closes only the external display session.</param>
        /// <returns>An independently owned session.</returns>
        IExternalAppSession CreateExternalSession(GameObject container, Action requestClose);
    }

    /// <summary>
    /// One external display session. Dispose releases all listeners and owned UI resources.
    /// </summary>
    public interface IExternalAppSession : IDisposable
    {
        /// <summary>Starts the independent session.</summary>
        void Open();

        /// <summary>Updates a visible independent session on the game thread.</summary>
        void Tick();

        /// <summary>Stops the independent session without navigating the original device.</summary>
        void Close();
    }
}
