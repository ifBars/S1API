namespace S1API.PhoneCalls.Constants
{
    /// <summary>
    /// Defines how conditional triggers should evaluate their conditions.
    /// </summary>
    public enum EvaluationType
    {
        /// <summary>
        /// The trigger passes and executes when the condition evaluates to true.
        /// </summary>
        PassOnTrue,

        /// <summary>
        /// The trigger passes and executes when the condition evaluates to false.
        /// </summary>
        PassOnFalse
    }
}
