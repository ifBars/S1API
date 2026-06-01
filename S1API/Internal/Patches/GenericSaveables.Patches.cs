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
	/// <para>
	/// Supports configurable load order via <see cref="S1API.Internal.Abstraction.Saveable.LoadOrder"/>:
	/// </para>
	/// <list type="bullet">
	/// <item><description>BeforeBaseGame: Loads before base game loaders run (prefix patch on LoadRequest constructor)</description></item>
	/// <item><description>AfterBaseGame (default): Loads after NPCsLoader.Load (postfix patch)</description></item>
	/// </list>
	/// </summary>
	[HarmonyPatch]
	internal static class GenericSaveablesPatches
	{
		[HarmonyPatch(typeof(S1Persistence.SaveManager), "Save", new Type[] { typeof(string) })]
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
			catch (Exception e)
			{
				try { MelonLoader.MelonLogger.Warning($"[Saveables] SaveManager_Save_Postfix failed: {e.Message}\n{e.StackTrace}"); } catch { }
			}
		}

		private static bool sameSession = false;

		/// <summary>
		/// Loads saveables marked with BeforeBaseGame load order BEFORE base game loaders run.
		/// This runs as a prefix to LoadManager.QueueLoadRequest on the first LoadRequest creation,
		/// which happens right before base game loaders start processing.
		/// Uses a session flag cleared by onLoadComplete to detect new load cycles.
		/// </summary>
		[HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.QueueLoadRequest))]
		[HarmonyPrefix]
		private static void BeforeBaseLoaders(S1Persistence.LoadRequest request)
		{
			try
			{
				var lm = S1Persistence.LoadManager.Instance;
				if (lm == null || string.IsNullOrEmpty(lm.LoadedGameFolderPath))
					return;

				// QueueLoadRequest may be called multiple times before loading into a save
				// this flag makes sure we only load once - it's cleared by onLoadComplete
				if (sameSession) return;
				string basePath = Path.Combine(lm.LoadedGameFolderPath, "Modded", "Saveables");
				
				foreach (var saveable in SaveableAutoRegistry.GetRegisteredSaveables())
				{
					// Only load saveables that want to load before base game
					if (saveable.LoadOrder != SaveableLoadOrder.BeforeBaseGame)
						continue;

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
						void InitializeOnLoadComplete()
						{
						    try
						    {
							    EventHelper.RemoveListener(InitializeOnLoadComplete, lm.onLoadComplete);
							    ((IRegisterable)saveable).CreateInternal();
						    }
						    catch (Exception e)
						    {
							    try { MelonLoader.MelonLogger.Warning($"[Saveables] InitializeOnLoadComplete (Before) failed: {e.Message}\n{e.StackTrace}"); } catch { }
						    }
						}
						EventHelper.AddListener(InitializeOnLoadComplete, lm.onLoadComplete);
					}
				}
				
				// Lock subsequent calls to this prefix until load completes to avoid loading multiple times in the same session
				sameSession = true;
				// Clear the lock once the game is loaded to allow loading again in future sessions without restarting the game
				void ClearLockOnLoadComplete()
				{
				    try
				    {
					    EventHelper.RemoveListener(ClearLockOnLoadComplete, lm.onLoadComplete);
					    sameSession = false;
				    }
				    catch (Exception e)
				    {
					    try { MelonLoader.MelonLogger.Warning($"[Saveables] ClearLockOnLoadComplete failed: {e.Message}\n{e.StackTrace}"); } catch { }
				    }
				}
				EventHelper.AddListener(ClearLockOnLoadComplete, lm.onLoadComplete);
			}
			catch (Exception e)
			{
				try { MelonLoader.MelonLogger.Warning($"[Saveables] BeforeBaseLoaders failed: {e.Message}\n{e.StackTrace}"); } catch { }
			}
		}

		/// <summary>
		/// Loads saveables marked with AfterBaseGame load order AFTER base game loaders run.
		/// This runs as a postfix to NPCsLoader.Load, which is one of the last loaders.
		/// </summary>
		[HarmonyPatch(typeof(S1Loaders.NPCsLoader), "Load")]
		[HarmonyPostfix]
		private static void AfterBaseLoaders(string mainPath)
		{
			try
			{
				string basePath = Path.Combine(S1Persistence.LoadManager.Instance.LoadedGameFolderPath, "Modded", "Saveables");
				
				foreach (var saveable in SaveableAutoRegistry.GetRegisteredSaveables())
				{
					// Only load saveables that want to load after base game (default behavior)
					if (saveable.LoadOrder != SaveableLoadOrder.AfterBaseGame)
						continue;

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
						    catch (Exception e)
						    {
							    try { MelonLoader.MelonLogger.Warning($"[Saveables] InitializeOnLoadComplete (After) failed: {e.Message}\n{e.StackTrace}"); } catch { }
						    }
						}
						EventHelper.AddListener(InitializeOnLoadComplete, lm.onLoadComplete);
					}
				}
			}
			catch (Exception e)
			{
				try { MelonLoader.MelonLogger.Warning($"[Saveables] AfterBaseLoaders failed: {e.Message}\n{e.StackTrace}"); } catch { }
			}
		}
	}
}


