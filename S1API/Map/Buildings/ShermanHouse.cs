namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Sherman House".
	/// Modders can use <see cref="Building.Get{ShermanHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Sherman House")]
	public sealed class ShermanHouse : IBuildingIdentifier { }
}
