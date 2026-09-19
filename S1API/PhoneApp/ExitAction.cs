using System;

namespace S1API.PhoneApp
{
    /// <summary>
    /// Represents a request to exit the active phone application without exposing
    /// runtime-specific Schedule One types.
    /// </summary>
    public sealed class ExitAction
    {
        private readonly Func<bool> _getUsed;
        private readonly Action<bool> _setUsed;

        internal ExitAction(Func<bool> getUsed, Action<bool> setUsed)
        {
            _getUsed = getUsed ?? throw new ArgumentNullException(nameof(getUsed));
            _setUsed = setUsed ?? throw new ArgumentNullException(nameof(setUsed));
        }

        /// <summary>
        /// Gets or sets whether another listener has handled the exit request.
        /// Set this to <see langword="true"/> after handling the request to prevent
        /// lower-priority listeners from processing it again.
        /// </summary>
        public bool Used
        {
            get => _getUsed();
            set => _setUsed(value);
        }
    }
}
