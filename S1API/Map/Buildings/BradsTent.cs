namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Brad's Tent".
	/// Modders can use <see cref="Building.Get{BradsTent}()"/> to resolve it.
	/// </summary>
	[BuildingName("Brad's Tent")]
	public sealed class BradsTent : IBuildingIdentifier { }
}
