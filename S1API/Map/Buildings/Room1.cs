namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Room 1".
	/// Modders can use <see cref="Building.Get{Room1}()"/> to resolve it.
	/// </summary>
	[BuildingName("Room 1")]
	public sealed class Room1 : IBuildingIdentifier { }
}
