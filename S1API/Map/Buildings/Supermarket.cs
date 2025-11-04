namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Supermarket".
	/// Modders can use <see cref="Building.Get{Supermarket}()"/> to resolve it.
	/// </summary>
	[BuildingName("Supermarket")]
	public sealed class Supermarket : IBuildingIdentifier { }
}
