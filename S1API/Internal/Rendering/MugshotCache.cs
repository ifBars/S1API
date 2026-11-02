#if IL2CPPMELON
using S1Avatar = Il2CppScheduleOne.Avatar;
using S1CoreAvatar = Il2CppScheduleOne.Core.Avatar;
#elif MONOMELON
using S1Avatar = ScheduleOne.Avatar;
using S1CoreAvatar = ScheduleOne.Core.Avatar;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Utils;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Rendering
{
    /// <summary>
    /// INTERNAL: Disk cache for generated custom NPC mugshots.
    /// </summary>
    /// <remarks>
    /// A portrait is stored under a hash of everything the capture reads: the render inputs built from the NPC's
    /// avatar settings, the content of runtime-created textures those inputs reference (custom tattoos loaded from
    /// PNG files), the generator kind, the game version, the S1API build and the bytes of every loaded mod assembly.
    /// A change to any of them is a different key, so a stale portrait is never found. Files that do not decode to a
    /// complete portrait are deleted and rendered again. Files unused for <see cref="MaxUnusedAge"/> are pruned once
    /// per game start.
    /// </remarks>
    internal static class MugshotCache
    {
        /// <summary>
        /// Bumped whenever the capture changes what it renders (camera, lights, size, pose), so old files miss.
        /// </summary>
        internal const string RenderRecipe = "native-portrait-512-v1";

        internal const int PortraitSize = 512;
        internal static readonly TimeSpan MaxUnusedAge = TimeSpan.FromDays(30);

        private static readonly Log Logger = new Log("MugshotCache");
        private static readonly Dictionary<int, string> RuntimeTextureHashes = new Dictionary<int, string>();
        private static string? _environment;
        private static bool _pruneStarted;

        private static int _hits;
        private static int _misses;
        private static int _stored;
        private static int _discarded;
        private static int _uncacheable;
        private static readonly Stopwatch LoadTime = new Stopwatch();
        private static readonly Stopwatch KeyTime = new Stopwatch();

        internal static string Directory =>
            Path.Combine(MelonEnvironment.UserDataDirectory, "S1API", "MugshotCache");

        internal static bool Enabled => S1APIPreferences.EnableMugshotCache?.Value ?? true;

        /// <summary>
        /// Builds the cache key for one capture, or returns null when the portrait must not be cached: the cache is
        /// off, or the inputs reference a runtime texture whose content cannot be read.
        /// </summary>
        internal static string? TryBuildKey(
            S1CoreAvatar.NakedAppearance appearance,
            S1CoreAvatar.Outfit outfit,
            string generatorKind,
            string npcName)
        {
            if (!Enabled)
                return null;

            KeyTime.Start();
            try
            {
                StartPruneOnce();
                var textures = new StringBuilder();
                if (!AppendTextureFingerprints(appearance.AvatarObjects, textures, npcName) ||
                    !AppendTextureFingerprints(outfit.AvatarObjects, textures, npcName))
                {
                    _uncacheable++;
                    return null;
                }

                return BuildKey(
                    GetEnvironment(),
                    generatorKind,
                    appearance.ToJson(),
                    JsonUtility.ToJson(outfit),
                    textures.ToString());
            }
            catch (Exception ex)
            {
                _uncacheable++;
                Logger.Warning($"{npcName}: no cache key, rendering without cache: {ex.Message}");
                return null;
            }
            finally
            {
                KeyTime.Stop();
            }
        }

        /// <summary>
        /// Hashes the key parts into a file-name-safe hex string. Each part is length-prefixed, so moving text from
        /// one part into the next changes the key.
        /// </summary>
        internal static string BuildKey(params string[] parts)
        {
            var builder = new StringBuilder();
            builder.Append(RenderRecipe).Append('\n');
            foreach (var part in parts)
            {
                var value = part ?? string.Empty;
                builder.Append(value.Length).Append(':').Append(value).Append('\n');
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return ToHex(hash);
            }
        }

        internal static string PathFor(string key) => Path.Combine(Directory, key + ".png");

        /// <summary>
        /// Loads a cached portrait. A file that is empty, does not decode, has the wrong size or holds no complete
        /// portrait is deleted with a warning and reported as a miss.
        /// </summary>
        internal static bool TryLoad(string key, string npcName, out Texture2D? portrait)
        {
            portrait = null;
            var path = PathFor(key);
            LoadTime.Start();
            try
            {
                if (!File.Exists(path))
                {
                    _misses++;
                    return false;
                }

                var bytes = File.ReadAllBytes(path);
                var problem = CheckFileBytes(bytes);
                if (problem == null)
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(bytes, false))
                        problem = "PNG does not decode";
                    else if (texture.width != PortraitSize || texture.height != PortraitSize)
                        problem = $"size {texture.width}x{texture.height}, expected {PortraitSize}x{PortraitSize}";
                    else if (!global::S1API.Entities.NPCAppearance.HasPortraitContent(texture, 0.01f))
                        problem = "no complete portrait in the image";

                    if (problem == null)
                    {
                        texture.name = "S1API Mugshot " + npcName;
                        portrait = texture;
                    }
                    else
                    {
                        Object.Destroy(texture);
                    }
                }

                if (problem != null)
                {
                    Discard(path, npcName, problem);
                    _misses++;
                    return false;
                }

                TouchAsync(path, npcName);
                _hits++;
                return true;
            }
            catch (Exception ex)
            {
                Discard(path, npcName, ex.Message);
                _misses++;
                return false;
            }
            finally
            {
                LoadTime.Stop();
            }
        }

        /// <summary>
        /// Rejects files that cannot be a PNG before Unity decodes them.
        /// </summary>
        internal static string? CheckFileBytes(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "file is empty";
            if (bytes.Length < PngSignature.Length + 12)
                return $"file is truncated ({bytes.Length} bytes)";
            for (var index = 0; index < PngSignature.Length; index++)
            {
                if (bytes[index] != PngSignature[index])
                    return "file is not a PNG";
            }

            return null;
        }

        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        /// Encodes the portrait on the main thread and writes it on a worker thread. The file appears under its final
        /// name only once it is complete.
        /// </summary>
        internal static void Store(string key, Texture2D portrait, string npcName)
        {
            byte[] bytes;
            try
            {
                bytes = portrait.EncodeToPNG();
            }
            catch (Exception ex)
            {
                Logger.Warning($"{npcName}: portrait not cached, PNG encoding failed: {ex.Message}");
                return;
            }

            var path = PathFor(key);
            Task.Run(() =>
            {
                var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    File.WriteAllBytes(temp, bytes);
                    if (File.Exists(path))
                        File.Delete(path);
                    File.Move(temp, path);
                    System.Threading.Interlocked.Increment(ref _stored);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"{npcName}: portrait not cached, writing {path} failed: {ex.Message}");
                    TryDelete(temp);
                }
            });
        }

        /// <summary>
        /// Logs what the cache did since the last report and resets the counters. Called when the mugshot queue drains.
        /// </summary>
        internal static void ReportAndReset(double queueSeconds)
        {
            if (!Enabled)
            {
                Logger.Msg($"Cache is off (EnableMugshotCache = false). Mugshot queue took {queueSeconds:F1} s.");
            }
            else
            {
                Logger.Msg(
                    $"{_hits} from cache, {_misses} rendered, {_uncacheable} not cacheable, " +
                    $"{_discarded} broken files discarded. Queue {queueSeconds:F1} s, cache reads {LoadTime.Elapsed.TotalSeconds:F2} s, " +
                    $"keys {KeyTime.Elapsed.TotalSeconds:F2} s. Files written so far: {System.Threading.Volatile.Read(ref _stored)}.");
            }

            _hits = 0;
            _misses = 0;
            _uncacheable = 0;
            _discarded = 0;
            LoadTime.Reset();
            KeyTime.Reset();
        }

        internal static bool IsExpired(DateTime lastWriteUtc, DateTime nowUtc) =>
            nowUtc - lastWriteUtc > MaxUnusedAge;

        private static bool AppendTextureFingerprints(
            IEnumerable<S1CoreAvatar.SerializedAvatarObject>? objects,
            StringBuilder target,
            string npcName)
        {
            if (objects == null)
                return true;

            foreach (var serialized in objects)
            {
#if IL2CPPMELON
                if (serialized == null)
                    continue;
#endif
                var id = serialized.Id;
                if (string.IsNullOrEmpty(id))
                    continue;

                target.Append(id).Append('=');
                S1CoreAvatar.AvatarObject? avatarObject = null;
                if (!S1Avatar.AvatarObjectLibrary.TryGetAvatarObjectById(id, out avatarObject) || avatarObject == null)
                {
                    target.Append("missing;");
                    continue;
                }

                var layers = avatarObject.Layers;
                if (layers == null)
                    continue;

                foreach (var layer in layers)
                {
                    if (layer == null)
                        continue;
                    if (!AppendTexture(layer.GetTextureToApply(), target, id, npcName) ||
                        !AppendTexture(layer.GetNormalTextureToApply(), target, id, npcName))
                        return false;
                }

                target.Append(';');
            }

            return true;
        }

        /// <summary>
        /// Game textures are identified by name and size; the game version in the key covers their content. Textures
        /// created at runtime (negative instance id), such as a mod's tattoo loaded from a PNG, can change under the
        /// same id, so their pixels are hashed. One that cannot be read makes the portrait uncacheable.
        /// </summary>
        private static bool AppendTexture(Texture2D? texture, StringBuilder target, string objectId, string npcName)
        {
            if (texture == null)
            {
                target.Append("-,");
                return true;
            }

            var instanceId = texture.GetInstanceID();
            if (instanceId >= 0)
            {
                target.Append(texture.name).Append('@').Append(texture.width).Append('x').Append(texture.height).Append(',');
                return true;
            }

            if (!RuntimeTextureHashes.TryGetValue(instanceId, out var hash))
            {
                if (!texture.isReadable)
                {
                    Logger.Msg(
                        $"{npcName}: '{objectId}' uses runtime texture '{texture.name}' that cannot be read; " +
                        "this portrait is rendered on every load.");
                    return false;
                }

                byte[] data = texture.GetRawTextureData();
                using (var sha = SHA256.Create())
                    hash = ToHex(sha.ComputeHash(data));
                RuntimeTextureHashes[instanceId] = hash;
            }

            target.Append("rt:").Append(hash).Append(',');
            return true;
        }

        private static string GetEnvironment()
        {
            if (_environment != null)
                return _environment;

            var watch = Stopwatch.StartNew();
            var builder = new StringBuilder();
            builder.Append("game=").Append(Application.version).Append('\n');
            builder.Append("unity=").Append(Application.unityVersion).Append('\n');
            builder.Append("s1api=").Append(typeof(MugshotCache).Assembly.ManifestModule.ModuleVersionId).Append('\n');

            var melons = new List<string>();
            foreach (var melon in MelonBase.RegisteredMelons)
            {
                string location = string.Empty;
                try
                {
                    location = melon.MelonAssembly?.Location ?? string.Empty;
                    var fileHash = location.Length > 0 && File.Exists(location) ? HashFile(location) : "no-file";
                    melons.Add($"{melon.Info?.Name}|{melon.Info?.Version}|{fileHash}");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Could not fingerprint mod {melon.Info?.Name} ({location}): {ex.Message}");
                    melons.Add($"{melon.Info?.Name}|{melon.Info?.Version}|unreadable");
                }
            }

            melons.Sort(StringComparer.Ordinal);
            foreach (var melon in melons)
                builder.Append(melon).Append('\n');

            _environment = builder.ToString();
            Logger.Msg($"Environment fingerprint from {melons.Count} mods in {watch.ElapsedMilliseconds} ms; cache folder {Directory}");
            return _environment;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return ToHex(sha.ComputeHash(stream));
        }

        private static void StartPruneOnce()
        {
            if (_pruneStarted)
                return;
            _pruneStarted = true;

            var directory = Directory;
            Task.Run(() =>
            {
                try
                {
                    if (!System.IO.Directory.Exists(directory))
                        return;

                    var now = DateTime.UtcNow;
                    int removed = 0;
                    foreach (var file in System.IO.Directory.GetFiles(directory))
                    {
                        var isTemp = file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
                        var lastWrite = File.GetLastWriteTimeUtc(file);
                        if ((isTemp && now - lastWrite > TimeSpan.FromHours(1)) || IsExpired(lastWrite, now))
                        {
                            if (TryDelete(file))
                                removed++;
                        }
                    }

                    if (removed > 0)
                        Logger.Msg($"Pruned {removed} portrait(s) unused for {MaxUnusedAge.TotalDays:F0} days");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Pruning {directory} failed: {ex.Message}");
                }
            });
        }

        private static void TouchAsync(string path, string npcName)
        {
            Task.Run(() =>
            {
                try
                {
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"{npcName}: could not mark {path} as used: {ex.Message}");
                }
            });
        }

        private static void Discard(string path, string npcName, string reason)
        {
            _discarded++;
            Logger.Warning($"{npcName}: discarding {Path.GetFileName(path)} ({reason}); rendering it again");
            TryDelete(path);
        }

        private static bool TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not delete {path}: {ex.Message}");
                return false;
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
                builder.Append(value.ToString("x2"));
            return builder.ToString();
        }
    }
}
