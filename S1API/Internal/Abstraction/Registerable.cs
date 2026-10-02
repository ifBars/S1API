namespace S1API.Internal.Abstraction
{
    /// <summary>
    /// Infrastructure base class for S1API registration and lifecycle hooks.
    /// </summary>
    /// <remarks>
    /// This class is public because public API classes inherit from it.
    /// Modders should inherit from the appropriate API base class, such as
    /// <c>NPC</c>, <c>Quest</c>, or <c>Saveable</c>, rather than this infrastructure class directly.
    /// </remarks>
    public abstract class Registerable : IRegisterable
    {
        /// <summary>
        /// INTERNAL: Explicit interface call into the protected
        /// creation pipeline. Do not call directly from API users.
        /// </summary>
        void IRegisterable.CreateInternal() =>
            CreateInternal();
        
        /// <summary>
        /// INTERNAL: Entry point for internal creation logic.
        /// Invokes <see cref="OnCreated"/> to allow subclasses to
        /// run initialization after being registered.
        /// </summary>
        internal virtual void CreateInternal() =>
            OnCreated();
        
        /// <summary>
        /// INTERNAL: Explicit interface call into the protected
        /// destruction pipeline. Do not call directly from API users.
        /// </summary>
        void IRegisterable.DestroyInternal() => 
            DestroyInternal();
        
        /// <summary>
        /// INTERNAL: Entry point for internal destruction logic.
        /// Invokes <see cref="OnDestroyed"/> to allow subclasses to
        /// clean up when unregistered.
        /// </summary>
        internal virtual void DestroyInternal() =>
            OnDestroyed();
        
        /// <summary>
        /// Called after the instance has been created/registered.
        /// Override to perform setup that depends on registration.
        /// </summary>
        void IRegisterable.OnCreated() => 
            OnCreated();

        /// <summary>
        /// Override hook for creation/registration completion.
        /// </summary>
        protected virtual void OnCreated() { }
        
        /// <summary>
        /// Called before the instance is destroyed/unregistered.
        /// Override to release resources acquired during creation.
        /// </summary>
        void IRegisterable.OnDestroyed() => 
            OnDestroyed();

        /// <summary>
        /// Override hook for destruction/unregistration completion.
        /// </summary>
        protected virtual void OnDestroyed() { }
    }
}
