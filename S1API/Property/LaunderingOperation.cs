#if (MONOMELON || IL2CPPBEPINEX || MONOBEPINEX)
using S1Property = ScheduleOne.Property;

#elif IL2CPPMELON
using S1Property = Il2CppScheduleOne.Property;
#endif

namespace S1API.Property
{
    /// <summary>
    /// Represents a laundering operation associated with a business property.
    /// </summary>
    public class LaunderingOperation
    {
        /// <summary>
        /// Readonly backing field encapsulating the core laundering operation instance
        /// used within the LaunderingOperation class. This field provides access
        /// to the underlying implementation of the laundering operation functionalities
        /// </summary>
        internal readonly S1Property.LaunderingOperation InnerLaunderingOperation;

        /// <summary>
        /// A wrapper class that acts as a bridge to interact with an inner laundering operation implementation
        /// from the Il2CppScheduleOne.Property namespace.
        /// </summary>
        public LaunderingOperation(S1Property.LaunderingOperation launderingOperation)
        {
            InnerLaunderingOperation = launderingOperation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LaunderingOperation"/> class with the specified business, amount, minutes since started, and optional completion time.
        /// </summary>
        /// <param name="business">The business associated with the laundering operation.</param>
        /// <param name="amount">The amount of money to be laundered.</param>
        /// <param name="minutesSinceStarted">The number of minutes since the laundering operation started.</param>
        /// <param name="completionTimeInMinutes">The total time in minutes required to complete the laundering operation. Default is 1140 minutes.</param>
        public LaunderingOperation(BusinessWrapper business, float amount, int minutesSinceStarted,
            int completionTimeInMinutes = 1140)
        {
            InnerLaunderingOperation = new S1Property.LaunderingOperation(business.InnerBusiness, amount,
                minutesSinceStarted);
            InnerLaunderingOperation.completionTime_Minutes = completionTimeInMinutes;
        }

        /// <summary>
        /// Gets or sets the business associated with the laundering operation.
        /// </summary>
        public BusinessWrapper Business
        {
            get => new BusinessWrapper(InnerLaunderingOperation.business);
            set => InnerLaunderingOperation.business = value.InnerBusiness;
        }

        /// <summary>
        /// Gets or sets the amount of money to be laundered in this operation.
        /// </summary>
        public float Amount
        {
            get => InnerLaunderingOperation.amount;
            set => InnerLaunderingOperation.amount = value;
        }

        /// <summary>
        /// Gets or sets the number of minutes that have passed since the laundering operation started.
        /// </summary>
        public int MinutesSinceStarted
        {
            get => InnerLaunderingOperation.minutesSinceStarted;
            set => InnerLaunderingOperation.minutesSinceStarted = value;
        }

        /// <summary>
        /// Gets or sets the total time in minutes required to complete the laundering operation.
        /// </summary>
        public int CompletionTimeInMinutes
        {
            get => InnerLaunderingOperation.completionTime_Minutes;
            set => InnerLaunderingOperation.completionTime_Minutes = value;
        }
    }
}