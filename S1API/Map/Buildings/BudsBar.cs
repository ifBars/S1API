namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Bud's Bar".
	/// Modders can use <see cref="Building.Get{BudsBar}()"/> to resolve it.
	/// </summary>
	[BuildingName("Bud's Bar")]
	public sealed class BudsBar : IBuildingIdentifier { }
}
