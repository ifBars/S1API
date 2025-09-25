namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Town hall".
	/// Modders can use Building.Get<TownHall>() to resolve it.
	/// </summary>
	[BuildingName("Town hall")]
	public sealed class TownHall : IBuildingIdentifier { }
}
