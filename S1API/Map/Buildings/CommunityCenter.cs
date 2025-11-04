namespace S1API.Map.Buildings
{
	/// <summary>
	/// Identifier for the base-game building named "Community Center".
	/// Modders can use <see cref="Building.Get{CommunityCenter}()"/> to resolve it.
	/// </summary>
	[BuildingName("Community Center")]
	public sealed class CommunityCenter : IBuildingIdentifier { }
}
