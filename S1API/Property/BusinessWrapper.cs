using System.Collections.Generic;
#if (MONOMELON || IL2CPPBEPINEX || MONOBEPINEX)
using ScheduleOne.Money;
using S1Property = ScheduleOne.Property;

#elif IL2CPPMELON
using Il2CppScheduleOne.Money;
using S1Property = Il2CppScheduleOne.Property;
#endif

namespace S1API.Property
{
    /// <summary>
    /// Represents a wrapper class for handling business properties derived from the
    /// <see cref="S1Property.Business"/> class. Provides an abstraction for
    /// interacting with business property details and operations in Unity.
    /// </summary>
    public class BusinessWrapper : PropertyWrapper
    {
        /// <summary>
        /// Readonly backing field encapsulating the core business property instance
        /// used within the BusinessWrapper class. This field provides access
        /// to the underlying implementation of the business property functionalities
        /// and is leveraged across multiple members to delegate
        /// operations to the actual business property instance.
        /// </summary>
        internal readonly S1Property.Business InnerBusiness;

        /// <summary>
        /// A wrapper class that extends the functionality of <see cref="PropertyWrapper"/>
        /// and acts as a bridge to interact with an inner business property implementation
        /// from the Il2CppScheduleOne.Property namespace.
        /// </summary>
        public BusinessWrapper(S1Property.Business business) : base(business)
        {
            InnerBusiness = business;
        }

        /// <summary>
        /// Gets or sets the laundering capacity of the business.
        /// </summary>
        public float LaunderCapacity
        {
            get => InnerBusiness.LaunderCapacity;
            set
            {
                InnerBusiness.LaunderCapacity = value;
                foreach (var path in LaunderingCapacityPaths)
                {
                    if (TrySetTMPText(path, MoneyManager.FormatAmount(value), "laundering capacity"))
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the laundering operations associated with the business.
        /// </summary>
        public List<LaunderingOperation> LaunderingOperations
        {
            get
            {
                var list = new List<LaunderingOperation>();
                foreach (var op in InnerBusiness.LaunderingOperations)
                {
                    list.Add(new LaunderingOperation(op));
                }

                return list;
            }
        }

        /// <summary>
        /// Adds a new laundering operation to the business with the specified amount and minutes since started.
        /// Added LaunderingOperation will be reflected in the <see cref="LaunderingOperations"/> list after
        /// the client receives it.
        /// </summary>
        /// <param name="amount">The amount of money to be laundered.</param>
        /// <param name="minutesSinceStarted">The number of minutes since the laundering operation started.</param>
        public void AddLaunderingOperation(float amount, int minutesSinceStarted)
        {
            InnerBusiness.StartLaunderingOperation(amount, minutesSinceStarted);
        }
        
        /// <summary>
        /// Gets the current total amount of money being laundered by the business.
        /// </summary>
        public float CurrentLaunderTotal => InnerBusiness.currentLaunderTotal;

        /// <summary>
        /// Gets the applied launder limit for the business.
        /// </summary>
        public float AppliedLaunderLimit => InnerBusiness.appliedLaunderLimit;

        /// <summary>
        /// INTERNAL: Prefix used for locating business property signs in the scene hierarchy.
        /// </summary>
        internal override string SignPrefix => "@Businesses/";

        /// <summary>
        /// INTERNAL: Paths used for locating and updating the price display on business property signs in the scene hierarchy.
        /// </summary>
        internal override IEnumerable<string> SignPaths
        {
            get
            {
                var baseName = PropertyName.Replace(" ", "");
                yield return $"{SignPrefix}{baseName}/ForSaleSign_Blue/Price";
                yield return $"{SignPrefix}{baseName}/ForSaleSign_Blue (1)/Price";
                yield return $"{SignPrefix}{PropertyName}/ForSaleSign_Blue/Price";
                yield return $"{SignPrefix}{PropertyName}/ForSaleSign_Blue (1)/Price";
            }
        }

        /// <summary>
        /// INTERNAL: Path to the whiteboard text object in the scene hierarchy for updating the business property price display.
        /// </summary>
        internal override string WhiteboardPath =>
            $"Map/Container/RE Office/Interior/Whiteboard (1)/PropertyListing {PropertyName}/Price";

        /// <summary>
        /// INTERNAL: Collection of potential paths to the laundering capacity text objects in the scene hierarchy for updating the laundering capacity display.
        /// </summary>
        internal IEnumerable<string> LaunderingCapacityPaths
        {
            get
            {
                var baseName = PropertyName.Replace(" ", "");
                yield return $"@Businesses/{baseName}/Grid/ItemContainer/LaunderingStation_Built(Clone)/LaunderingInterface/CurrentOperations/Total/Max";
                yield return $"@Businesses/{PropertyName}/Grid/ItemContainer/LaunderingStation_Built(Clone)/LaunderingInterface/CurrentOperations/Total/Max";
                yield return $"@Businesses/{baseName}/Grid (1)/ItemContainer/LaunderingStation_Built(Clone)/LaunderingInterface/CurrentOperations/Total/Max";
                yield return $"@Businesses/{PropertyName}/Grid (1)/ItemContainer/LaunderingStation_Built(Clone)/LaunderingInterface/CurrentOperations/Total/Max";
            }
        }
    }
}