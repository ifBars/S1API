namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Shack".
	/// Modders can use <see cref="Building.Get{Shack}()"/> to resolve it.
	/// </summary>
	[BuildingName("Shack")]
	public sealed class Shack : IBuildingIdentifier { }
}
