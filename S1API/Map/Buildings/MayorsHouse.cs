namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Mayor's House".
	/// Modders can use <see cref="Building.Get{MayorsHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Mayor's House")]
	public sealed class MayorsHouse : IBuildingIdentifier { }
}
