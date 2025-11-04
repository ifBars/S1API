namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Overpass Building".
	/// Modders can use <see cref="Building.Get{OverpassBuilding}()"/> to resolve it.
	/// </summary>
	[BuildingName("Overpass Building")]
	public sealed class OverpassBuilding : IBuildingIdentifier { }
}
