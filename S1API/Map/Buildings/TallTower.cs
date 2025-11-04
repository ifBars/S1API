namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Tall Tower".
	/// Modders can use <see cref="Building.Get{TallTower}()"/> to resolve it.
	/// </summary>
	[BuildingName("Tall Tower")]
	public sealed class TallTower : IBuildingIdentifier { }
}
