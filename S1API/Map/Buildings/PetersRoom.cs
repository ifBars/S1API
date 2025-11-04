namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Peter's Room".
	/// Modders can use <see cref="Building.Get{PetersRoom}()"/> to resolve it.
	/// </summary>
	[BuildingName("Peter's Room")]
	public sealed class PetersRoom : IBuildingIdentifier { }
}
