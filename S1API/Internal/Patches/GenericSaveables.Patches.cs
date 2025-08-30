using System;
using System.IO;
using HarmonyLib;
using Newtonsoft.Json;
using S1API.Internal.Abstraction;
using S1API.Saveables;

#if (IL2CPPMELON)
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1Loaders = Il2CppScheduleOne.Persistence.Loaders;
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Persistence = ScheduleOne.Persistence;
using S1Loaders = ScheduleOne.Persistence.Loaders;
using S1Datas = ScheduleOne.Persistence.Datas;
#endif

#if (MONOMELON || MONOBEPINEX)
using ListString = System.Collections.Generic.List<string>;
#elif (IL2CPPMELON || IL2CPPBEPINEX)
using ListString = Il2CppSystem.Collections.Generic.List<string>;
#endif

namespace S1API.Internal.Patches
{
	/// <summary>
	/// INTERNAL: Save/Load pipeline for mod-registered Saveables not tied to base entities.
	/// Writes to Modded/Saveables and restores on load. Cross-compatible for Mono/Il2Cpp.
	/// </summary>
	[HarmonyPatch]
	internal static class GenericSaveablesPatches
	{
		[HarmonyPatch(typeof(S1Persistence.SaveManager), nameof(S1Persistence.SaveManager.Save), typeof(string))]
		[HarmonyPostfix]
		private static void SaveManager_Save_Postfix(string saveFolderPath)
		{
			try
			{
				// Ensure top-level Modded paths are whitelisted so the game's cleanup doesn't delete our files
				var saveManager = S1Persistence.SaveManager.Instance;
				string approvedModded = "Modded";
				string approvedFolder = Path.Combine("Modded", "Saveables");
				if (!saveManager.ApprovedBaseLevelPaths.Contains(approvedModded))
					saveManager.ApprovedBaseLevelPaths.Add(approvedModded);
				if (!saveManager.ApprovedBaseLevelPaths.Contains(approvedFolder))
					saveManager.ApprovedBaseLevelPaths.Add(approvedFolder);

				string basePath = Path.Combine(saveFolderPath, "Modded", "Saveables");
				Directory.CreateDirectory(basePath);
				foreach (var entry in ModSaveableRegistry.Registered)
				{
					var saveable = entry.Saveable;
					ListString extra = new ListString();
					string folder = string.IsNullOrEmpty(entry.FolderName) ? saveable.GetType().Name : entry.FolderName;
					string path = Path.Combine(basePath, folder);
					Directory.CreateDirectory(path);
					saveable.SaveInternal(path, ref extra);
				}
			}
			catch { }
		}

		[HarmonyPatch(typeof(S1Loaders.QuestsLoader), "Load")]
		[HarmonyPostfix]
		private static void AfterBaseLoaders(string mainPath)
		{
			try
			{
				string basePath = Path.Combine(S1Persistence.LoadManager.Instance.LoadedGameFolderPath, "Modded", "Saveables");
				if (!Directory.Exists(basePath))
					return;
				foreach (var entry in ModSaveableRegistry.Registered)
				{
					var saveable = entry.Saveable;
					string folder = string.IsNullOrEmpty(entry.FolderName) ? saveable.GetType().Name : entry.FolderName;
					string path = Path.Combine(basePath, folder);
					if (!Directory.Exists(path))
						continue;
					saveable.LoadInternal(path);
				}
			}
			catch { }
		}
	}
}


