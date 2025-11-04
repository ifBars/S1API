namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Cafe".
	/// Modders can use <see cref="Building.Get{Cafe}()"/> to resolve it.
	/// </summary>
	[BuildingName("Cafe")]
	public sealed class Cafe : IBuildingIdentifier { }
}
