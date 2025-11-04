namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Mick's House".
	/// Modders can use <see cref="Building.Get{MicksHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Mick's House")]
	public sealed class MicksHouse : IBuildingIdentifier { }
}
