namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Modern Mansion".
	/// Modders can use <see cref="Building.Get{ModernMansion}()"/> to resolve it.
	/// </summary>
	[BuildingName("Modern Mansion")]
	public sealed class ModernMansion : IBuildingIdentifier { }
}
