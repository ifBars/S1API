using System;

namespace S1API.Map.Buildings
{
	/// <summary>
	/// Annotate building identifier types with the display name of the building.
	/// Usage: [BuildingName("North apartments")]
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class BuildingNameAttribute : Attribute
	{
		public string Name { get; }
		public BuildingNameAttribute(string name)
		{
			Name = name;
		}
	}
}


