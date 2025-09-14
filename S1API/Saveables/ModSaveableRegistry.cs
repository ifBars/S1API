using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Internal.Abstraction;

namespace S1API.Saveables
{
	/// <summary>
	/// [OBSOLETE] This manual registration system is deprecated. 
	/// Classes that directly inherit from Saveable are now automatically discovered and registered.
	/// Simply inherit from Saveable and your class will be automatically saved/loaded.
	/// </summary>
	[Obsolete("Manual registration is no longer required. Classes that directly inherit from Saveable are automatically discovered. Remove calls to ModSaveableRegistry.Register() and simply inherit from Saveable.")]
	public static class ModSaveableRegistry
	{
		internal class Entry
		{
			public Saveable Saveable;
			public string? FolderName;
		}

		internal static readonly List<Entry> Registered = new List<Entry>();

		/// <summary>
		/// DEPRECATED: Classes that directly inherit from Saveable are automatically discovered.
		/// </summary>
		[Obsolete("Manual registration is no longer required. Classes that directly inherit from Saveable are automatically discovered.")]
		public static void Register(Saveable saveable, string? folderName = null)
		{
			if (saveable == null)
				return;
			
			// Log deprecation warning for mod developers
			MelonLogger.Warning($"ModSaveableRegistry.Register() is deprecated! " +
			                   $"The saveable '{saveable.GetType().Name}' will be automatically discovered. " +
			                   $"Mod users can safely ignore this warning - their saves will continue to work normally. " +
			                   $"Mod developers should remove ModSaveableRegistry.Register() calls and simply inherit from Saveable.");
			
			Registered.Add(new Entry { Saveable = saveable, FolderName = folderName });
		}
	}
}


