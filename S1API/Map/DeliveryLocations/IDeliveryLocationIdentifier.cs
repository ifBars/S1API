namespace S1API.Map.DeliveryLocations
{
	/// <summary>
	/// Marker interface for delivery location identifier types used with DeliveryLocations.Get<T>().
	/// Implement empty classes like 'public sealed class BehindBank : IDeliveryLocationIdentifier {}'
	/// and optionally annotate with [DeliveryLocationName("Behind Bank")].
	/// </summary>
	public interface IDeliveryLocationIdentifier { }
}
