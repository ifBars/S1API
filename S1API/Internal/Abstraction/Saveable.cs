using System;
using System.ComponentModel;
using S1API.Saveables;

namespace S1API.Internal.Abstraction
{
    /// <summary>
    /// Compatibility base class for existing mods. Use <see cref="global::S1API.Saveables.Saveable"/>
    /// for new saveable classes.
    /// </summary>
    /// <remarks>
    /// Retained for existing compiled mods and scheduled for removal in a future breaking release.
    /// No removal version has been set.
    /// </remarks>
    [Obsolete("Use S1API.Saveables.Saveable instead. This compatibility type will be removed in a future breaking release.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Saveable : global::S1API.Saveables.Saveable
    {
        /// <inheritdoc/>
        public new virtual SaveableLoadOrder LoadOrder => SaveableLoadOrder.AfterBaseGame;

        /// <summary>
        /// Requests a game save. The immediate parameter is retained for compatibility and ignored.
        /// </summary>
        /// <param name="immediate">Ignored, as in the original implementation.</param>
        /// <returns>True if a save was requested; false if the game is not in a savable state.</returns>
        public new static bool RequestGameSave(bool immediate) =>
            global::S1API.Saveables.Saveable.RequestGameSave(immediate);

        /// <summary>
        /// Requests a game save when a game is loaded.
        /// </summary>
        /// <returns>True if a save was requested; false if the game is not in a savable state.</returns>
        public new static bool RequestGameSave() => global::S1API.Saveables.Saveable.RequestGameSave();

        /// <inheritdoc/>
        protected new virtual void OnLoaded() => base.OnLoaded();

        /// <inheritdoc/>
        protected new virtual void OnSaved() => base.OnSaved();

        // Keep the original virtual slots for already compiled mod overrides.
        internal override SaveableLoadOrder GetLoadOrder() => LoadOrder;
        internal override void InvokeOnLoaded() => OnLoaded();
        internal override void InvokeOnSaved() => OnSaved();
    }
}
