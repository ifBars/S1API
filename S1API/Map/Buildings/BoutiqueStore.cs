namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Boutique Store".
	/// Modders can use <see cref="Building.Get{BoutiqueStore}()"/> to resolve it.
	/// </summary>
	[BuildingName("Boutique Store")]
	public sealed class BoutiqueStore : IBuildingIdentifier { }
}
