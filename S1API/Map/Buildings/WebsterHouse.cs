namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Webster House".
	/// Modders can use <see cref="Building.Get{WebsterHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Webster House")]
	public sealed class WebsterHouse : IBuildingIdentifier { }
}
