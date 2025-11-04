namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Courthouse".
	/// Modders can use <see cref="Building.Get{Courthouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Courthouse")]
	public sealed class Courthouse : IBuildingIdentifier { }
}
