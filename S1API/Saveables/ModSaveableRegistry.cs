using System.Collections.Generic;
using S1API.Internal.Abstraction;

namespace S1API.Saveables
{
	/// <summary>
	/// Public registry for standalone mod saveables that are not tied to base-game entities.
	/// S1API will persist and restore all registered saveables under Modded/Saveables.
	/// </summary>
	public static class ModSaveableRegistry
	{
		internal class Entry
		{
			public Saveable Saveable;
			public string? FolderName;
		}

		internal static readonly List<Entry> Registered = new List<Entry>();

		/// <summary>
		/// Register a saveable for persistence. Optional custom folder name under Modded/Saveables.
		/// </summary>
		public static void Register(Saveable saveable, string? folderName = null)
		{
			if (saveable == null)
				return;
			Registered.Add(new Entry { Saveable = saveable, FolderName = folderName });
		}
	}
}


