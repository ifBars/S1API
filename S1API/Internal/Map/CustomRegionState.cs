using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace S1API.Internal.Map
{
    /// <summary>INTERNAL: One region's saved state.</summary>
    internal sealed class CustomRegionSavedState
    {
        public string Id { get; set; } = string.Empty;
        public bool Unlocked { get; set; }
        public float Influence { get; set; }
    }

    /// <summary>INTERNAL: Content of the custom region save file.</summary>
    internal sealed class CustomRegionSaveFile
    {
        public int FormatVersion { get; set; } = CustomRegionState.CurrentFormatVersion;
        public List<CustomRegionSavedState> Regions { get; set; } = new List<CustomRegionSavedState>();
    }

    /// <summary>
    /// INTERNAL: Reads and writes the custom region state in <c>Modded/CustomRegions.json</c> of a save folder, apart
    /// from the game's own files.
    /// </summary>
    internal static class CustomRegionState
    {
        internal const int CurrentFormatVersion = 1;
        internal const int MaximumRegionCount = 256;
        internal static readonly string RelativePath = Path.Combine("Modded", "CustomRegions.json");

        internal static string PathIn(string saveFolder) => Path.Combine(saveFolder, RelativePath);

        /// <summary>
        /// Reads the saved state by region id. A missing file is a new save and returns an empty map without a
        /// message; an unreadable one is reported through <paramref name="warn"/> and returns an empty map.
        /// </summary>
        internal static Dictionary<string, CustomRegionSavedState> Read(string? saveFolder, Action<string> warn)
        {
            var result = new Dictionary<string, CustomRegionSavedState>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(saveFolder)) return result;
            string path = PathIn(saveFolder!);
            if (!File.Exists(path)) return result;
            try
            {
                CustomRegionSaveFile? file = JsonConvert.DeserializeObject<CustomRegionSaveFile>(File.ReadAllText(path));
                if (file == null || file.FormatVersion != CurrentFormatVersion || file.Regions == null || file.Regions.Count > MaximumRegionCount)
                {
                    warn($"The custom region file '{path}' is incompatible; the regions start from their defaults.");
                    return result;
                }
                foreach (CustomRegionSavedState state in file.Regions)
                {
                    if (state == null || string.IsNullOrWhiteSpace(state.Id) || float.IsNaN(state.Influence))
                    {
                        warn("Ignored an invalid entry in the custom region file.");
                        continue;
                    }
                    state.Influence = Math.Min(1f, Math.Max(0f, state.Influence));
                    result[state.Id] = state;
                }
            }
            catch (Exception ex)
            {
                warn($"Could not read the custom region file '{path}'; the regions start from their defaults: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Writes the given states and keeps the states of other ids already in the file, so removing a mod for a
        /// while does not lose its regions. Writes nothing for an empty list.
        /// </summary>
        /// <returns>The number of states written, including the kept ones.</returns>
        internal static int Write(string saveFolder, IEnumerable<CustomRegionSavedState> states, Action<string> warn)
        {
            List<CustomRegionSavedState> current = states.ToList();
            if (current.Count == 0) return 0;

            Dictionary<string, CustomRegionSavedState> merged = Read(saveFolder, warn);
            foreach (CustomRegionSavedState state in current)
                merged[state.Id] = state;

            string path = PathIn(saveFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporaryPath = path + ".tmp";
            var file = new CustomRegionSaveFile { Regions = merged.Values.OrderBy(s => s.Id, StringComparer.Ordinal).ToList() };
            File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(file, Formatting.None));
            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
            return file.Regions.Count;
        }
    }
}
