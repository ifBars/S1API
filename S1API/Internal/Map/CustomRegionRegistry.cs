#if IL2CPPMELON
using S1Map = Il2CppScheduleOne.Map;
using S1Cartel = Il2CppScheduleOne.Cartel;
using S1Audio = Il2CppScheduleOne.Audio;
#elif MONOMELON
using S1Map = ScheduleOne.Map;
using S1Cartel = ScheduleOne.Cartel;
using S1Audio = ScheduleOne.Audio;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Map;
using UnityEngine;

namespace S1API.Internal.Map
{
    /// <summary>
    /// INTERNAL: One registered custom region and the game objects built for it in the running world.
    /// </summary>
    internal sealed class CustomRegionEntry
    {
        internal CustomRegionEntry(RegionDefinition definition, RegionDefinition source)
        {
            Id = definition.Id;
            Definition = definition;
            Source = source;
            Handle = new RegionHandle(this);
        }

        internal string Id { get; }
        internal RegionDefinition Definition { get; set; }
        internal RegionDefinition Source { get; set; }
        internal RegionHandle Handle { get; }

        /// <summary>The number the game sees, valid once the registry is frozen.</summary>
        internal int Value = -1;

        internal S1Map.MapRegionData? MapData;
        internal S1Cartel.CartelInfluence.RegionInfluenceData? InfluenceData;
        internal S1Audio.PolygonalZone? Zone;
    }

    /// <summary>
    /// INTERNAL: Holds the registered custom regions and gives them numbers above the highest vanilla region.
    /// </summary>
    /// <remarks>
    /// Numbers follow the sorted ids, not the registration order, so every machine of a co-op session that has the
    /// same regions registered agrees on them whatever the mod load order. They are fixed when the first world loads.
    /// </remarks>
    internal static class CustomRegionRegistry
    {
        private static readonly Log Logger = new Log("CustomRegions");
        private static readonly object Gate = new object();
        private static readonly List<CustomRegionEntry> Entries = new List<CustomRegionEntry>();
        private static bool _frozen;
        private static bool _lifecycleHooked;
        private static int? _firstValue;

        internal static Action<string, string>? LogSinkForTesting;

        /// <summary>Keeps tests from subscribing to the game lifecycle.</summary>
        internal static bool SkipLifecycleHookForTesting;

        /// <summary>The first number used for custom regions: one above the highest vanilla region.</summary>
        internal static int FirstValue
        {
            get
            {
                if (_firstValue.HasValue) return _firstValue.Value;
                int max = -1;
                foreach (object value in Enum.GetValues(typeof(S1Map.EMapRegion)))
                    max = Math.Max(max, Convert.ToInt32(value));
                _firstValue = max + 1;
                return _firstValue.Value;
            }
        }

        internal static void Info(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("info", message);
            else Logger.Msg(message);
        }

        internal static void Warn(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("warn", message);
            else Logger.Warning(message);
        }

        internal static void Error(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("error", message);
            else Logger.Error(message);
        }

        internal static RegionHandle Register(RegionDefinition def)
        {
            Validate(def);
            RegionDefinition snapshot = Snapshot(def);
            lock (Gate)
            {
                CustomRegionEntry? existing = FindLocked(snapshot.Id);
                if (existing != null)
                {
                    if (ReferenceEquals(existing.Source, def))
                        return existing.Handle;
                    existing.Definition = snapshot;
                    existing.Source = def;
                    if (existing.MapData != null)
                        Warn($"Region '{existing.Id}' was registered again while its world is running; the new definition applies after the next world load.");
                    else
                        Info($"Region '{existing.Id}' registered again; the new definition replaces the old one.");
                    return existing.Handle;
                }

                var entry = new CustomRegionEntry(snapshot, def);
                Entries.Add(entry);
                if (_frozen)
                {
                    entry.Value = Entries.Max(e => e.Value) + 1;
                    Warn($"Region '{entry.Id}' was registered after the first world loaded and got number {entry.Value}; register regions before the world loads so the numbers do not depend on the registration order.");
                }
                else
                {
                    AssignSortedLocked();
                    Info($"Region '{entry.Id}' registered ('{snapshot.Name}', {snapshot.Polygon.Length} points).");
                }
                HookLifecycle();
                return entry.Handle;
            }
        }

        private static void HookLifecycle()
        {
            if (_lifecycleHooked || SkipLifecycleHookForTesting) return;
            _lifecycleHooked = true;
            GameLifecycle.OnPreSceneChange += CustomRegionWorld.ResetWorld;
        }

        internal static void Validate(RegionDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (string.IsNullOrWhiteSpace(def.Id))
                throw new ArgumentException("The region id must not be empty.", nameof(def));
            if (string.IsNullOrWhiteSpace(def.Name))
                throw new ArgumentException($"Region '{def.Id}' needs a name.", nameof(def));
            if (float.IsNaN(def.StartInfluence) || def.StartInfluence < 0f || def.StartInfluence > 1f)
                throw new ArgumentException($"Region '{def.Id}' start influence must be from 0 to 1.", nameof(def));
            if (def.Polygon == null || def.Polygon.Length < 3)
                throw new ArgumentException($"Region '{def.Id}' needs a polygon of at least three points.", nameof(def));
            foreach (Vector2 p in def.Polygon)
            {
                if (float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y))
                    throw new ArgumentException($"Region '{def.Id}' polygon holds a point that is not a finite number.", nameof(def));
            }
            if (!Enum.IsDefined(typeof(Region), def.StandIn))
                throw new ArgumentException($"Region '{def.Id}' stand-in must be a vanilla region.", nameof(def));
        }

        private static RegionDefinition Snapshot(RegionDefinition def) => new RegionDefinition
        {
            Id = def.Id.Trim(),
            Name = def.Name,
            UnlockRank = def.UnlockRank,
            StartInfluence = def.StartInfluence,
            ContactsMapImage = def.ContactsMapImage,
            Polygon = (Vector2[])def.Polygon.Clone(),
            StandIn = def.StandIn,
        };

        private static CustomRegionEntry? FindLocked(string id) =>
            Entries.FirstOrDefault(e => string.Equals(e.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));

        private static void AssignSortedLocked()
        {
            int next = FirstValue;
            foreach (CustomRegionEntry entry in Entries.OrderBy(e => e.Id, StringComparer.Ordinal))
                entry.Value = next++;
        }

        internal static CustomRegionEntry? Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            lock (Gate) return FindLocked(id);
        }

        /// <summary>The entry that owns a game region number, or null for vanilla and unknown numbers.</summary>
        internal static CustomRegionEntry? Find(int value)
        {
            if (value < FirstValue) return null;
            lock (Gate)
            {
                if (!_frozen) return null;
                return Entries.FirstOrDefault(e => e.Value == value);
            }
        }

        internal static bool IsCustom(S1Map.EMapRegion region) => Find((int)region) != null;

        internal static IReadOnlyList<RegionHandle> Handles()
        {
            lock (Gate) return Entries.Select(e => e.Handle).ToList();
        }

        /// <summary>The entries in number order, after fixing the numbers if that has not happened yet.</summary>
        internal static IReadOnlyList<CustomRegionEntry> Freeze()
        {
            lock (Gate)
            {
                if (!_frozen)
                {
                    AssignSortedLocked();
                    _frozen = true;
                    Info($"{Entries.Count} custom regions numbered from {FirstValue}: " +
                         string.Join(", ", Entries.OrderBy(e => e.Value).Select(e => $"{e.Id}={e.Value}")));
                }
                return Entries.OrderBy(e => e.Value).ToList();
            }
        }

        internal static IReadOnlyList<CustomRegionEntry> Current()
        {
            lock (Gate) return Entries.OrderBy(e => e.Value).ToList();
        }

        internal static bool IsAssigned(CustomRegionEntry entry)
        {
            lock (Gate) return _frozen && entry.Value >= 0;
        }

        internal static bool HasEntries
        {
            get { lock (Gate) return Entries.Count > 0; }
        }

        /// <summary>Allows new numbers to be given when the next world loads.</summary>
        internal static void Unfreeze()
        {
            lock (Gate) _frozen = false;
        }

        internal static void ResetForTesting()
        {
            lock (Gate)
            {
                Entries.Clear();
                _frozen = false;
            }
        }
    }
}
