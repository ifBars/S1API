namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Wilkinson House".
	/// Modders can use <see cref="Building.Get{WilkinsonHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Wilkinson House")]
	public sealed class WilkinsonHouse : IBuildingIdentifier { }
}
