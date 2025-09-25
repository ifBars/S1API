namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Jane's Caravan".
	/// Modders can use Building.Get<JanesCaravan>() to resolve it.
	/// </summary>
	[BuildingName("Jane's Caravan")]
	public sealed class JanesCaravan : IBuildingIdentifier { }
}
