namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Pillville".
	/// Modders can use <see cref="Building.Get{Pillville}()"/> to resolve it.
	/// </summary>
	[BuildingName("Pillville")]
	public sealed class Pillville : IBuildingIdentifier { }
}
