namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Chemical Plant A".
	/// Modders can use <see cref="Building.Get{ChemicalPlantA}()"/> to resolve it.
	/// </summary>
	[BuildingName("Chemical Plant A")]
	public sealed class ChemicalPlantA : IBuildingIdentifier { }
}
