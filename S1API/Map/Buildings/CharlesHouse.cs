namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Charles' House".
	/// Modders can use <see cref="Building.Get{CharlesHouse}()"/> to resolve it.
	/// </summary>
	[BuildingName("Charles' House")]
	public sealed class CharlesHouse : IBuildingIdentifier { }
}
