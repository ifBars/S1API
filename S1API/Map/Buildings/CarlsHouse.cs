namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Carl's House".
	/// Modders can use <see cref="Building.Get{CarlsHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Carl's House")]
	public sealed class CarlsHouse : IBuildingIdentifier { }
}
