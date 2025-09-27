using System;

namespace S1API.Map.DeliveryLocations
{
	/// <summary>
	/// Annotate delivery location identifier types with the display name of the delivery location.
	/// Usage: [DeliveryLocationName("Behind Bank")]
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class DeliveryLocationNameAttribute : Attribute
	{
		public string Name { get; }
		public DeliveryLocationNameAttribute(string name)
		{
			Name = name;
		}
	}
}
