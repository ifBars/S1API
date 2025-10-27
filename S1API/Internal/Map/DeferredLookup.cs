using System;

namespace S1API.Internal.Map
{
    /// <summary>
    /// INTERNAL: Represents a deferred lookup that will be resolved when the Main scene loads.
    /// Stores the identifier and callback to invoke once the target is found.
    /// </summary>
    internal sealed class DeferredLookup
    {
        /// <summary>
        /// The identifier type for typed lookups (e.g., typeof(ManorParking))
        /// </summary>
        public Type IdentifierType { get; private set; }

        /// <summary>
        /// The name-based identifier for string lookups
        /// </summary>
        public string IdentifierName { get; private set; }

        /// <summary>
        /// Callback to invoke with the resolved object
        /// </summary>
        public Action<object> Callback { get; }

        /// <summary>
        /// Whether this lookup has been resolved
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Creates a typed deferred lookup
        /// </summary>
        public DeferredLookup(Type identifierType, Action<object> callback)
        {
            IdentifierType = identifierType ?? throw new ArgumentNullException(nameof(identifierType));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            IdentifierName = null;
            IsResolved = false;
        }

        /// <summary>
        /// Creates a name-based deferred lookup
        /// </summary>
        public DeferredLookup(string identifierName, Action<object> callback)
        {
            IdentifierName = identifierName ?? throw new ArgumentNullException(nameof(identifierName));
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            IdentifierType = null;
            IsResolved = false;
        }

        /// <summary>
        /// Marks this lookup as resolved
        /// </summary>
        internal void MarkResolved() => IsResolved = true;
    }
}

