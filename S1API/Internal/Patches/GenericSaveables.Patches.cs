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
		[HarmonyPatch(typeof(S1Persistence.SaveManager), "Save")]
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
				
				// Use automatic discovery instead of manual registry
				foreach (var saveable in SaveableAutoRegistry.GetRegisteredSaveables())
				{
					ListString extra = new ListString();
					string folder = saveable.GetType().Name;
					string path = Path.Combine(basePath, folder);
					Directory.CreateDirectory(path);
					saveable.SaveInternal(path, ref extra);
				}
			}
			catch { }
		}

		[HarmonyPatch(typeof(S1Loaders.NPCsLoader), "Load")]
		[HarmonyPostfix]
		private static void AfterBaseLoaders(string mainPath)
		{
			try
			{
				string basePath = Path.Combine(S1Persistence.LoadManager.Instance.LoadedGameFolderPath, "Modded", "Saveables");
				
				// Use automatic discovery instead of manual registry
				foreach (var saveable in SaveableAutoRegistry.GetRegisteredSaveables())
				{
					string folder = saveable.GetType().Name;
					string path = Path.Combine(basePath, folder);

					if (Directory.Exists(path))
					{
						// Existing save data found -> load
						saveable.LoadInternal(path);
					}
					else
					{
						// No save data yet for this save -> initialize once after full game load
						var lm = S1Persistence.LoadManager.Instance;
						void InitializeOnLoadComplete()
						{
							try
							{
								EventHelper.RemoveListener(InitializeOnLoadComplete, lm.onLoadComplete);
								((IRegisterable)saveable).CreateInternal();
							}
							catch { }
						}
						EventHelper.AddListener(InitializeOnLoadComplete, lm.onLoadComplete);
					}
				}
			}
			catch { }
		}
	}
}


