#if (IL2CPPMELON)
using S1Properties = Il2CppScheduleOne.Properties;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Properties = ScheduleOne.Properties;
#endif

using S1API.Properties.Interfaces;

namespace S1API.Properties
{
    /// <summary>
    /// Runtime-agnostic wrapper for Schedule One product properties.
    /// Provides consistent access whether running on Mono or IL2CPP.
    /// </summary>
    public class ProductPropertyWrapper : PropertyBase
    {
        private readonly S1Properties.Property _innerProperty;

        /// <summary>
        /// Creates a wrapper around a Schedule One property.
        /// </summary>
        internal ProductPropertyWrapper(S1Properties.Property property)
        {
            _innerProperty = property;
        }

        /// <summary>
        /// The unique identifier for this property.
        /// </summary>
        public override string ID => _innerProperty.ID;

        /// <summary>
        /// The display name of this property.
        /// </summary>
        public override string Name => _innerProperty.Name;

        /// <summary>
        /// The Unity name of this property.
        /// This corresponds to the Unity ScriptableObject.name property.
        /// </summary>
        public override string name => _innerProperty.name;

        /// <summary>
        /// The description of what this property does.
        /// </summary>
        public override string Description => _innerProperty.Description;

        /// <summary>
        /// The tier/level of this property (1-5).
        /// </summary>
        public override int Tier => _innerProperty.Tier;

        /// <summary>
        /// How addictive this property makes the product (0-1).
        /// </summary>
        public override float Addictiveness => _innerProperty.Addictiveness;

        /// <summary>
        /// INTERNAL: Gets the underlying Schedule One property.
        /// </summary>
        internal S1Properties.Property InnerProperty => _innerProperty;
    }
}
