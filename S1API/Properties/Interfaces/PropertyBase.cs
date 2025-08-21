namespace S1API.Properties.Interfaces
{
    /// <summary>
    /// Runtime-agnostic base class for product properties.
    /// Provides a consistent API regardless of whether running on Mono or IL2CPP.
    /// Uses concrete class instead of interface to avoid IL2CPP TypeLoadException issues.
    /// </summary>
    public abstract class PropertyBase
    {
        /// <summary>
        /// The unique identifier for this property.
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// The display name of this property.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The Unity name of this property (lowercase for compatibility).
        /// This corresponds to the Unity ScriptableObject.name property.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// The description of what this property does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The tier/level of this property (1-5).
        /// </summary>
        public abstract int Tier { get; }

        /// <summary>
        /// How addictive this property makes the product (0-1).
        /// </summary>
        public abstract float Addictiveness { get; }
    }
}
