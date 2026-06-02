namespace S1API.Stations
{
    /// <summary>
    /// Calculation method to use when determining quality of
    /// the <see cref="ChemistryStationRecipe"/> result.
    /// </summary>
    public enum QualityCalculationMethod
    {
        /// <summary>
        /// Quality is calculated by adding the quality contributions of each ingredient together.
        /// </summary>
        Additive,
        /// <summary>
        /// Quality is determined by the product's default quality.
        /// </summary>
        /// <remarks>
        /// Requires the result to be of type QualityItemInstance or its inheritor.
        /// </remarks>
        Absolute
    }
}