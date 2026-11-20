#if IL2CPPMELON
using S1Map = Il2CppScheduleOne.Map;
using S1Cartel = Il2CppScheduleOne.Cartel;
using S1Audio = Il2CppScheduleOne.Audio;
using S1Levelling = Il2CppScheduleOne.Levelling;
using S1DevUtils = Il2CppScheduleOne.DevUtilities;
using S1ContactsApp = Il2CppScheduleOne.UI.Phone.ContactsApp;
using S1Relations = Il2CppScheduleOne.UI.Relations;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1UI = Il2CppScheduleOne.UI;
using S1UI_MapPanel = Il2CppScheduleOne.UIMapPanel;
using S1TMP = Il2CppTMPro;
using S1InstanceFinder = Il2CppFishNet.InstanceFinder;
using RegionList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Map.EMapRegion>;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#elif MONOMELON
using S1Map = ScheduleOne.Map;
using S1Cartel = ScheduleOne.Cartel;
using S1Audio = ScheduleOne.Audio;
using S1Levelling = ScheduleOne.Levelling;
using S1DevUtils = ScheduleOne.DevUtilities;
using S1ContactsApp = ScheduleOne.UI.Phone.ContactsApp;
using S1Relations = ScheduleOne.UI.Relations;
using S1NPCs = ScheduleOne.NPCs;
using S1Economy = ScheduleOne.Economy;
using S1Persistence = ScheduleOne.Persistence;
using S1UI = ScheduleOne.UI;
using S1UI_MapPanel = ScheduleOne.UIMapPanel;
using S1TMP = TMPro;
using S1InstanceFinder = FishNet.InstanceFinder;
using RegionList = System.Collections.Generic.List<ScheduleOne.Map.EMapRegion>;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API.Internal.Map
{
    /// <summary>
    /// INTERNAL: Builds the game objects of the custom regions in the running world (map data, cartel influence,
    /// contacts tabs) and keeps them out of the game's save strings.
    /// </summary>
    internal static class CustomRegionWorld
    {
        private static Dictionary<string, CustomRegionSavedState>? _saved;

        private static void Info(string message) => CustomRegionRegistry.Info(message);
        private static void Warn(string message) => CustomRegionRegistry.Warn(message);
        private static void Error(string message) => CustomRegionRegistry.Error(message);

        private static bool InMainWorld(Component? c) => c != null && c.gameObject.scene.name == "Main";

#if IL2CPPMELON
        private static Il2CppReferenceArray<T> MakeArray<T>(List<T> items) where T : Il2CppObjectBase =>
            new Il2CppReferenceArray<T>(items.ToArray());
#else
        private static T[] MakeArray<T>(List<T> items) where T : class => items.ToArray();
#endif

        private static S1UI.RegionUnlockedCanvas? UnlockedCanvas => S1DevUtils.Singleton<S1UI.RegionUnlockedCanvas>.Instance;

        /// <summary>
        /// Saved state of the loaded save, read once per world. The cache is keyed by the save folder as well, so a
        /// world whose reset did not run still reads its own save. A co-op client has no save folder and starts from
        /// the defaults; the host then sends the influence of every region when the client joins.
        /// </summary>
        private static Dictionary<string, CustomRegionSavedState> Saved()
        {
            string? folder = S1Persistence.LoadManager.Instance?.LoadedGameFolderPath;
            if (_saved != null && string.Equals(_savedFolder, folder, StringComparison.Ordinal)) return _saved;
            _saved = CustomRegionState.Read(folder, Warn);
            _savedFolder = folder;
            Info($"{_saved.Count} saved custom region states read from '{folder}'.");
            return _saved;
        }

        private static string? _savedFolder;

        /// <summary>
        /// Resets the registry state of a finished world if the scene change reset did not run (an earlier subscriber
        /// of the scene change event threw). The zone of the old world is destroyed with its scene, so a zone that is
        /// set but gone marks a world that was not reset.
        /// </summary>
        private static void EnsureFreshWorld()
        {
            foreach (CustomRegionEntry entry in CustomRegionRegistry.Current())
            {
                if ((object?)entry.Zone != null && entry.Zone == null)
                {
                    Warn("The custom regions still hold the last world; the scene change reset did not run. Resetting now.");
                    ResetWorld();
                    return;
                }
            }
        }

        /// <summary>True on the host or in single player. A failed check counts as a client.</summary>
        internal static bool IsServer()
        {
            try
            {
                return S1InstanceFinder.IsServer;
            }
            catch (Exception ex)
            {
                Warn($"The network role could not be read; treated as a client: {ex.Message}");
                return false;
            }
        }

        // ---- handle operations ----

        internal static bool IsUnlocked(CustomRegionEntry entry) => entry.MapData != null && entry.MapData.IsUnlocked;

        internal static float GetInfluence(CustomRegionEntry entry) =>
            entry.InfluenceData != null ? entry.InfluenceData.Influence : entry.Definition.StartInfluence;

        internal static void Unlock(CustomRegionEntry entry)
        {
            if (entry.MapData == null)
            {
                Warn($"Region '{entry.Id}' cannot be unlocked while no world is loaded.");
                return;
            }
            if (entry.MapData.IsUnlocked)
            {
                Info($"Region '{entry.Id}' is already unlocked.");
                return;
            }
            if (IsServer() && S1DevUtils.NetworkSingleton<S1Levelling.LevelManager>.InstanceExists)
            {
                // The game's unlocked-region RPC runs locally on the host and reaches every client that knows the region.
                var list = new RegionList();
                list.Add(entry.MapData.Region);
                S1DevUtils.NetworkSingleton<S1Levelling.LevelManager>.Instance.SetUnlockedRegions(null, list);
                Info($"Region '{entry.Id}' unlocked and sent to the clients (unlocked here: {entry.MapData.IsUnlocked}).");
                return;
            }
            entry.MapData.SetUnlocked();
            Warn($"Region '{entry.Id}' unlocked on this machine only; only the host can unlock a region for the whole session.");
        }

        /// <summary>
        /// Removes the regions this machine does not have from an unlocked-region list the host sent. The game looks up
        /// each value without a null check, and a throw inside the RPC makes FishNet drop the rest of the packet.
        /// </summary>
        internal static void DropUnknownRegions(RegionList? unlocked)
        {
            if (unlocked == null)
            {
                Warn("Unlocked-region list is null; the game handles it.");
                return;
            }
            S1Map.Map? map = S1DevUtils.Singleton<S1Map.Map>.Instance;
            if (map == null)
            {
                Warn("Unlocked regions arrived without a map; the game handles them.");
                return;
            }
            var dropped = new List<int>();
            for (int i = unlocked.Count - 1; i >= 0; i--)
            {
                if (map.GetRegionData(unlocked[i]) != null) continue;
                dropped.Add((int)unlocked[i]);
                unlocked.RemoveAt(i);
            }
            if (dropped.Count == 0) return;
            dropped.Reverse();
            Warn($"Unlocked regions {string.Join(", ", dropped)} from the host are not on this map and were skipped. " +
                 "The host has custom regions this machine has not registered, or numbers them differently; every player needs the same region mods.");
        }

        internal static void ChangeInfluence(CustomRegionEntry entry, float amount)
        {
            if (entry.InfluenceData == null || !S1DevUtils.NetworkSingleton<S1Cartel.Cartel>.InstanceExists)
            {
                Warn($"Region '{entry.Id}' influence cannot change while no world is loaded.");
                return;
            }
            S1DevUtils.NetworkSingleton<S1Cartel.Cartel>.Instance.Influence.ChangeInfluence((S1Map.EMapRegion)entry.Value, amount);
            Info($"Region '{entry.Id}' influence change of {amount:0.###} requested.");
        }

        /// <summary>Drops the zones and the references into the finished world and allows new numbers.</summary>
        internal static void ResetWorld()
        {
            int zones = 0;
            foreach (CustomRegionEntry entry in CustomRegionRegistry.Current())
            {
                // The reference check keeps Unity's native calls out of a registry without a world.
                if ((object?)entry.Zone != null && DestroyZone(entry.Zone)) zones++;
                entry.Zone = null;
                entry.MapData = null;
                entry.InfluenceData = null;
            }
            _saved = null;
            _savedFolder = null;
            _missingActivitiesReported = false;
            CustomRegionRegistry.RemovePendingEntries();
            CustomRegionRegistry.Unfreeze();
            if (zones > 0) Info($"{zones} custom region zones removed.");
        }

        /// <summary>Destroys a zone that still exists. Kept out of line so the native Unity calls are compiled only when a zone is set.</summary>
        /// <returns>False if the zone was already destroyed with its scene.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool DestroyZone(S1Audio.PolygonalZone zone)
        {
            if (zone == null) return false;
            Object.Destroy(zone.gameObject);
            return true;
        }

        // ---- map ----

        private static float Area(Vector2[] poly)
        {
            float twice = 0f;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                twice += poly[j].x * poly[i].y - poly[i].x * poly[j].y;
            return Mathf.Abs(twice) * 0.5f;
        }

        /// <summary>
        /// Adds a map region for every custom region. The zone needs its points before it wakes, so it is built
        /// inactive. The game takes the first region whose outline holds a point (region lookup and the player's
        /// current region), so the custom regions go before the vanilla ones, smallest first: a custom region drawn
        /// inside a vanilla or another custom region is found. Positions outside every custom outline keep their
        /// vanilla region, since the vanilla regions keep their order.
        /// </summary>
        internal static void AddToMap(S1Map.Map map)
        {
            if (!InMainWorld(map))
            {
                Info($"Map woke in scene '{map?.gameObject.scene.name}', not the main world; no custom regions added.");
                return;
            }
            EnsureFreshWorld();
            IReadOnlyList<CustomRegionEntry> entries = CustomRegionRegistry.Freeze();
            if (entries.Count == 0)
            {
                Info("No custom regions to add to the map.");
                return;
            }
            Dictionary<string, CustomRegionSavedState> saved = Saved();
            var vanilla = new List<S1Map.MapRegionData>();
            foreach (S1Map.MapRegionData r in map.Regions) vanilla.Add(r);
            var regions = new List<S1Map.MapRegionData>();

            foreach (CustomRegionEntry e in entries.OrderBy(x => Area(x.Definition.Polygon)))
            {
                if (vanilla.Any(r => r != null && (int)r.Region == e.Value) || regions.Any(r => (int)r.Region == e.Value))
                {
                    Warn($"Region '{e.Id}' ({e.Value}) is already on the map; not added twice.");
                    continue;
                }
                RegionSnapshot def = new RegionSnapshot(e);
                var go = new GameObject($"S1API.Region.{e.Id}");
                go.SetActive(false);
                go.transform.SetParent(map.transform, false);
                Transform points = new GameObject("Points").transform;
                points.SetParent(go.transform, false);
                for (int i = 0; i < def.Polygon.Length; i++)
                {
                    Transform t = new GameObject("P" + i).transform;
                    t.SetParent(points, false);
                    t.position = new Vector3(def.Polygon[i].x, 0f, def.Polygon[i].y);
                }
                var zone = go.AddComponent<S1Audio.PolygonalZone>();
                zone.PointContainer = points;
                go.SetActive(true);
                e.Zone = zone;

                var rank = new S1Levelling.FullRank();
                rank.Rank = (S1Levelling.ERank)Math.Max(0, def.UnlockRank);
                rank.Tier = 1;

                var data = new S1Map.MapRegionData
                {
                    Region = (S1Map.EMapRegion)e.Value,
                    Name = def.Name,
                    UnlockedByDefault = false,
                    RankRequirement = rank,
                    StartingNPCs = MakeArray(new List<S1NPCs.NPC>()),
                    RegionDeliveryLocations = MakeArray(new List<S1Economy.DeliveryLocation>()),
                    AdjacentRegions = MakeArray(new List<S1Map.MapRegionData.RegionContainer>()),
                    RegionBounds = zone,
                    RegionSprite = def.Image,
                };
                // Map.Awake has already run its unlock loop over the vanilla regions.
                bool unlock = def.UnlockRank == 0 || (saved.TryGetValue(e.Id, out CustomRegionSavedState? s) && s.Unlocked);
                if (unlock) data.SetUnlocked();
                regions.Add(data);
                e.MapData = data;
                Info($"Region '{e.Id}' is {e.Value} on the map, {def.Polygon.Length} points, unlocked {data.IsUnlocked}.");
            }
            regions.AddRange(vanilla);
            map.Regions = MakeArray(regions);
            Info($"Map has {map.Regions.Length} regions including {entries.Count} custom ones.");
        }

        /// <summary>The values of a region taken from its definition once, so a replaced definition cannot change a built region.</summary>
        private sealed class RegionSnapshot
        {
            internal RegionSnapshot(CustomRegionEntry e)
            {
                Polygon = e.Definition.Polygon;
                Name = e.Definition.Name;
                UnlockRank = e.Definition.UnlockRank;
                Image = e.Definition.ContactsMapImage;
            }

            internal Vector2[] Polygon { get; }
            internal string Name { get; }
            internal int UnlockRank { get; }
            internal Sprite? Image { get; }
        }

        // ---- rank unlocks ----

        /// <summary>Unlocks the regions whose rank the player has reached.</summary>
        internal static void EvaluateRankUnlocks(int rank, bool announce)
        {
            foreach (CustomRegionEntry e in CustomRegionRegistry.Current())
            {
                if (e.MapData == null || e.MapData.IsUnlocked || e.Definition.UnlockRank <= 0 || rank < e.Definition.UnlockRank)
                    continue;
                e.MapData.SetUnlocked();
                Info($"Region '{e.Id}' unlocked by rank {rank}.");
                if (!announce) continue;
                S1UI.RegionUnlockedCanvas? canvas = UnlockedCanvas;
                if (canvas == null)
                    Warn($"Region '{e.Id}' unlocked without the unlock notice: the notice canvas is not available.");
                else
                    canvas.QueueUnlocked(e.MapData.Region);
            }
        }

        // ---- cartel influence ----

        /// <summary>
        /// Adds influence data for every custom region before the cartel reads its defaults. Without it the
        /// server-side influence change fails for the region.
        /// </summary>
        internal static void AddInfluenceData(S1Cartel.CartelInfluence influence)
        {
            if (!InMainWorld(influence))
            {
                Info("CartelInfluence woke outside the main world; no influence data added.");
                return;
            }
            EnsureFreshWorld();
            IReadOnlyList<CustomRegionEntry> entries = CustomRegionRegistry.Freeze();
            if (entries.Count == 0)
            {
                Info("No custom regions; no influence data added.");
                return;
            }
            Dictionary<string, CustomRegionSavedState> saved = Saved();
            var all = new List<S1Cartel.CartelInfluence.RegionInfluenceData>();
            foreach (S1Cartel.CartelInfluence.RegionInfluenceData d in influence.DefaultRegionInfluence) all.Add(d);
            foreach (CustomRegionEntry e in entries)
            {
                if (all.Any(d => d != null && (int)d.Region == e.Value))
                {
                    Warn($"Region '{e.Id}' ({e.Value}) already has influence data; not added twice.");
                    continue;
                }
                float start = saved.TryGetValue(e.Id, out CustomRegionSavedState? s) ? s.Influence : e.Definition.StartInfluence;
                var data = new S1Cartel.CartelInfluence.RegionInfluenceData((S1Map.EMapRegion)e.Value, start);
                e.InfluenceData = data;
                all.Add(data);
            }
            influence.DefaultRegionInfluence = MakeArray(all);
            Info($"Influence data added for {entries.Count} custom regions.");
        }

        /// <summary>
        /// Applies an influence change to a custom region. Vanilla unlocks region + 1 when an influence drops through
        /// 0.3, and that number is no region for a custom one, so the change is applied here without the unlock.
        /// </summary>
        /// <returns>False if the region is not custom and vanilla applies the change.</returns>
        internal static bool ApplyInfluenceChange(S1Cartel.CartelInfluence influence, S1Map.EMapRegion region, float oldInfluence, float newInfluence)
        {
            CustomRegionEntry? entry = CustomRegionRegistry.Find((int)region);
            if (entry == null) return false;
            try
            {
                S1Cartel.CartelInfluence.RegionInfluenceData? data = entry.InfluenceData;
                if (data == null)
                {
                    Warn($"Influence change for region '{entry.Id}' without influence data; ignored.");
                    return true;
                }
                // Same checks as the game's change: skip a value equal to the current one, clamp, and report the
                // clamped value only when it differs from the old one.
                if (newInfluence == data.Influence)
                {
                    Info($"Region '{entry.Id}' cartel influence stays at {data.Influence:0.###}.");
                    return true;
                }
                data.Influence = Mathf.Clamp01(newInfluence);
                if (oldInfluence != data.Influence)
                    influence.OnInfluenceChanged?.Invoke(region, oldInfluence, data.Influence);
                Info($"Region '{entry.Id}' cartel influence {oldInfluence:0.###} -> {data.Influence:0.###}.");
            }
            catch (Exception ex)
            {
                Error($"Influence change for region '{entry.Id}' failed: {ex}");
            }
            return true;
        }

        /// <summary>The global cartel activity rate averages the unlocked vanilla regions; custom regions stay out of it.</summary>
        /// <returns>False if vanilla computes the value.</returns>
        internal static bool TryInfluenceFraction(out float result)
        {
            result = 0f;
            if (!CustomRegionRegistry.HasEntries) return false;
            S1Map.Map? map = S1DevUtils.Singleton<S1Map.Map>.Instance;
            if (map == null || !S1DevUtils.NetworkSingleton<S1Cartel.Cartel>.InstanceExists
                || S1DevUtils.NetworkSingleton<S1Cartel.Cartel>.Instance.Influence == null)
            {
                Warn("Influence fraction without map or cartel; vanilla computes it.");
                return false;
            }
            S1Cartel.CartelInfluence cartel = S1DevUtils.NetworkSingleton<S1Cartel.Cartel>.Instance.Influence;
            float sum = 0f;
            int count = 0;
            foreach (S1Map.MapRegionData r in map.Regions)
            {
                if (r == null || !r.IsUnlocked || CustomRegionRegistry.IsCustom(r.Region)) continue;
                sum += cartel.GetInfluence(r.Region);
                count++;
            }
            result = count == 0 ? 0f : sum / count;
            return true;
        }

        private static bool _missingActivitiesReported;

        /// <summary>
        /// Cartel activities exist for the vanilla regions only; a custom region uses its stand-in's. Answered before
        /// the game looks, which logs an error for every region it does not know.
        /// </summary>
        /// <returns>False if vanilla answers.</returns>
        internal static bool TryRegionalActivities(S1Cartel.CartelActivities activities, S1Map.EMapRegion region, out S1Cartel.CartelRegionActivities? result)
        {
            result = null;
            CustomRegionEntry? entry = CustomRegionRegistry.Find((int)region);
            if (entry == null) return false;
            var standIn = (S1Map.EMapRegion)(int)entry.Definition.StandIn;
            var all = activities.RegionalActivities;
            if (all != null)
            {
                foreach (S1Cartel.CartelRegionActivities a in all)
                {
                    if (a != null && a.Region == standIn)
                    {
                        result = a;
                        return true;
                    }
                }
            }
            if (!_missingActivitiesReported)
                Warn($"Stand-in {standIn} of region '{entry.Id}' has no cartel activities; the game answers.");
            _missingActivitiesReported = true;
            return false;
        }

        // ---- saving ----

        /// <summary>Writes the region state of the running world into the save folder.</summary>
        internal static void Save(string saveFolder)
        {
            var states = new List<CustomRegionSavedState>();
            foreach (CustomRegionEntry e in CustomRegionRegistry.Current())
            {
                if (e.MapData == null || e.InfluenceData == null) continue;
                states.Add(new CustomRegionSavedState { Id = e.Id, Unlocked = e.MapData.IsUnlocked, Influence = e.InfluenceData.Influence });
            }
            if (states.Count == 0)
            {
                Info("No custom region state to save; the region file is left as it is.");
                return;
            }
            int written = CustomRegionState.Write(saveFolder, states, Warn);
            Info($"Saved {states.Count} custom region states ({written} in the file) to '{CustomRegionState.PathIn(saveFolder)}'.");
        }

        /// <summary>Removes the custom regions from the influence list the cartel saves.</summary>
#if IL2CPPMELON
        internal static Il2CppReferenceArray<S1Cartel.CartelInfluence.RegionInfluenceData> StripInfluence(Il2CppReferenceArray<S1Cartel.CartelInfluence.RegionInfluenceData> all)
#else
        internal static S1Cartel.CartelInfluence.RegionInfluenceData[] StripInfluence(S1Cartel.CartelInfluence.RegionInfluenceData[] all)
#endif
        {
            if (all == null || !CustomRegionRegistry.HasEntries) return all!;
            var kept = new List<S1Cartel.CartelInfluence.RegionInfluenceData>();
            foreach (S1Cartel.CartelInfluence.RegionInfluenceData d in all)
                if (d == null || !CustomRegionRegistry.IsCustom(d.Region)) kept.Add(d);
            return kept.Count == all.Length ? all : MakeArray(kept);
        }

        /// <summary>
        /// Rewrites the rank save without the custom regions in its unlocked list, so the save loads without the mod.
        /// </summary>
        /// <returns>The vanilla save string, or null if nothing had to change.</returns>
        internal static string? StripRankSave(S1Levelling.LevelManager level)
        {
            if (!CustomRegionRegistry.HasEntries) return null;
            S1Map.Map? map = S1DevUtils.Singleton<S1Map.Map>.Instance;
            if (map == null)
            {
                Warn("Rank save without a map; saved as vanilla wrote it.");
                return null;
            }
            try
            {
                var unlocked = map.GetUnlockedRegions();
                var kept = new RegionList();
                int dropped = 0;
                for (int i = 0; i < unlocked.Count; i++)
                {
                    if (CustomRegionRegistry.IsCustom(unlocked[i])) dropped++;
                    else kept.Add(unlocked[i]);
                }
                if (dropped == 0) return null;
                string json = new S1Levelling.RankData((int)level.Rank, level.Tier, level.XP, level.TotalXP, kept).GetJson();
                Info($"Rank save written without {dropped} custom regions.");
                return json;
            }
            catch (Exception ex)
            {
                Error($"Filtering the rank save failed; it holds the custom regions: {ex}");
                return null;
            }
        }

        // ---- contacts app ----

        internal static void AddContactsTabs(S1ContactsApp.ContactsApp app)
        {
            if (!InMainWorld(app)) return;
            IReadOnlyList<CustomRegionEntry> entries = CustomRegionRegistry.Freeze();
            if (entries.Count == 0)
            {
                Info("No custom regions; the contacts app keeps its tabs.");
                return;
            }
            var old = app.RegionUIs;
            if (old == null || old.Length < 2)
            {
                Error($"The contacts app has {old?.Length ?? 0} region tabs; no custom tab added.");
                return;
            }
            var list = new List<S1ContactsApp.ContactsApp.RegionUI>();
            foreach (S1ContactsApp.ContactsApp.RegionUI r in old) list.Add(r);

            foreach (CustomRegionEntry e in entries)
            {
                if (list.Any(r => (int)r.Region == e.Value))
                {
                    Warn($"Region '{e.Id}' ({e.Value}) already has a tab.");
                    continue;
                }
                string name = e.Definition.Name;
                S1ContactsApp.ContactsApp.RegionUI last = list[list.Count - 1];
                S1ContactsApp.ContactsApp.RegionUI before = list[list.Count - 2];
                Vector2 step = last.Button.GetComponent<RectTransform>().anchoredPosition - before.Button.GetComponent<RectTransform>().anchoredPosition;

                Button button = Object.Instantiate(last.Button.gameObject, last.Button.transform.parent).GetComponent<Button>();
                button.name = $"{name} Button";
                button.onClick.RemoveAllListeners();
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchoredPosition = last.Button.GetComponent<RectTransform>().anchoredPosition + step;
                int labels = 0;
                foreach (Text t in button.GetComponentsInChildren<Text>(true)) { t.text = name; labels++; }
                foreach (S1TMP.TMP_Text t in button.GetComponentsInChildren<S1TMP.TMP_Text>(true)) { t.text = name; labels++; }
                if (labels == 0) Warn($"The tab of region '{e.Id}' has no label to name it '{name}'.");

                list.Add(new S1ContactsApp.ContactsApp.RegionUI
                {
                    Region = (S1Map.EMapRegion)e.Value,
                    Button = button,
                    Container = EmptyLike(last.Container, $"{name} Circles"),
                    ConnectionsContainer = EmptyLike(last.ConnectionsContainer, $"{name} Connections"),
                });
                Info($"Region '{e.Id}' has a contacts tab at {rect.anchoredPosition} ({labels} labels).");
            }
            app.RegionUIs = MakeArray(list);
        }

        /// <summary>An empty page laid out like the given one, beside it.</summary>
        private static RectTransform EmptyLike(RectTransform like, string name)
        {
#if IL2CPPMELON
            var go = new GameObject(name, Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>());
#else
            var go = new GameObject(name, typeof(RectTransform));
#endif
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(like.parent, false);
            rect.anchorMin = like.anchorMin;
            rect.anchorMax = like.anchorMax;
            rect.pivot = like.pivot;
            rect.anchoredPosition = like.anchoredPosition;
            rect.sizeDelta = like.sizeDelta;
            rect.localScale = like.localScale;
            rect.SetSiblingIndex(like.GetSiblingIndex() + 1);
            go.SetActive(false);
            return rect;
        }

#if IL2CPPMELON
        private static S1UI_MapPanel MapPanelOf(S1ContactsApp.ContactsApp app) => app.uiPanel;
#else
        private static S1UI_MapPanel MapPanelOf(S1ContactsApp.ContactsApp app) =>
            (S1UI_MapPanel)HarmonyLib.AccessTools.Field(typeof(S1ContactsApp.ContactsApp), "uiPanel").GetValue(app);
#endif

        /// <summary>The scroll area of the contacts map, found through its content so the game's own scroll type is not referenced.</summary>
        private static ScrollRect? ScrollOf(S1ContactsApp.ContactsApp app) =>
            app.ContentRect != null ? app.ContentRect.GetComponentInParent<ScrollRect>() : null;

        /// <summary>
        /// Selects a custom region's tab. Vanilla shows the cartel influence of a region and the unlock rule of the
        /// next one by number; a custom region has neither, so its tab shows only its own people.
        /// </summary>
        /// <returns>False if vanilla selects the region.</returns>
        internal static bool TrySelectRegion(S1ContactsApp.ContactsApp app, S1Map.EMapRegion region)
        {
            CustomRegionEntry? entry = CustomRegionRegistry.Find((int)region);
            if (entry == null) return false;
            try
            {
                S1Map.Map map = S1DevUtils.Singleton<S1Map.Map>.Instance;
                S1Map.MapRegionData data = map.GetRegionData(region);
                if (data == null)
                {
                    // Vanilla would look up the region after this one by number and throw, so it does not get the call.
                    Error($"Region '{entry.Id}' has no map data; its tab is not selected.");
                    return true;
                }
                S1UI_MapPanel panel = MapPanelOf(app);
                app.SelectedRegion = region;
                panel.LockMapInput = !data.IsUnlocked;
                foreach (S1ContactsApp.ContactsApp.RegionUI ui in app.RegionUIs)
                {
                    S1Map.MapRegionData other = map.GetRegionData(ui.Region);
                    ui.Container.gameObject.SetActive(ui.Region == region);
                    ui.ConnectionsContainer.gameObject.SetActive(ui.Region == region);
                    ui.Button.interactable = ui.Region != region;
                    Transform locked = ui.Button.transform.Find("Locked");
                    if (locked != null && other != null) locked.gameObject.SetActive(!other.IsUnlocked);
                }
                ScrollRect? scroll = ScrollOf(app);
                if (scroll == null)
                    Warn($"The contacts app has no scroll area above its content; region '{entry.Id}' keeps the scroll state of the last tab.");
                else
                {
                    scroll.vertical = data.IsUnlocked;
                    scroll.horizontal = data.IsUnlocked;
                    scroll.enabled = data.IsUnlocked;
                }
                app.LowerContainer.gameObject.SetActive(false);
                app.HorizontalScrollbarRectTransform.anchoredPosition = new Vector2(0f, 7f);
                app.RegionLockedContainer.gameObject.SetActive(!data.IsUnlocked);
                S1ContactsApp.ContactsApp.RegionUI? page = app.RegionUIs.FirstOrDefault(r => r.Region == region);
                int circles = page != null ? CentreAndSelect(app, panel, page) : 0;
                Info($"Contacts tab of region '{entry.Id}' selected ({circles} people).");
                return true;
            }
            catch (Exception ex)
            {
                // Vanilla would look up the region after this one by number and throw, so it does not get the call.
                Error($"Selecting region '{entry.Id}' in the contacts app failed; its tab stays as it is: {ex}");
                return true;
            }
        }

        /// <summary>
        /// Moves the page of a region so its people sit in the middle of what the map shows, then selects the first
        /// of them like the first contact of a vanilla region. Returns the number of circles on the page.
        /// </summary>
        private static int CentreAndSelect(S1ContactsApp.ContactsApp app, S1UI_MapPanel panel, S1ContactsApp.ContactsApp.RegionUI ui)
        {
            S1Relations.RelationCircle[] circles = ui.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true);
            if (circles.Length == 0)
            {
                Info($"Region {(int)ui.Region} has no people on its page yet.");
                return 0;
            }

            Vector3 centroid = Vector3.zero;
            foreach (S1Relations.RelationCircle c in circles) centroid += c.Rect.position;
            centroid /= circles.Length;
            ScrollRect? scroll = ScrollOf(app);
            RectTransform view = scroll != null && scroll.viewport != null ? scroll.viewport : (scroll != null ? scroll.GetComponent<RectTransform>() : app.ContentRect);
            Vector3 middle = view.TransformPoint(view.rect.center);
            Vector3 shift = ui.Container.parent.InverseTransformVector(middle - centroid);
            shift.z = 0f;
            if (shift.sqrMagnitude > 1f)
            {
                ui.Container.localPosition += shift;
                Vector3 lines = ui.ConnectionsContainer.parent.InverseTransformVector(middle - centroid);
                lines.z = 0f;
                ui.ConnectionsContainer.localPosition += lines;
            }

            // Vanilla's Select also zooms to the circle, and that zoom stops at the edge of the scroll area, which
            // pushes the rest of the page out of view. The page is already centred, so only the panel and the ring move.
            panel.ResetSnappedItem();
            app.DetailPanel.Open(circles[0].AssignedNPC);
            app.SelectionIndicator.position = circles[0].Rect.position;
            return circles.Length;
        }
    }
}
