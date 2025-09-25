namespace S1API.Map.Buildings
{
	/// <summary>
	/// Marker interface for building identifier types used with Building.Get<T>().
	/// Implement empty classes like 'public sealed class NorthApartments : IBuildingIdentifier {}'
	/// and optionally annotate with [BuildingName("North apartments")].
	/// </summary>
	public interface IBuildingIdentifier { }
}


