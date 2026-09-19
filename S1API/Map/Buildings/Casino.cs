namespace S1API.Map.Buildings
{
	/// <summary>
	/// Legacy identifier for the base-game location named "Casino".
	/// This identifier cannot resolve because the Casino is not an enterable building.
	/// </summary>
	[System.Obsolete("Casino is not an enterable building and cannot be resolved. This compatibility identifier may be removed in a future S1API version.", false)]
	[BuildingName("Casino")]
	public sealed class Casino : IBuildingIdentifier { }
}
