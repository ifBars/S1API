#if IL2CPPMELON
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Property = ScheduleOne.Property;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Property;

namespace S1API.Internal.Properties
{
    /// <summary>
    /// The registry behind <see cref="PropertyManager.Register"/>: which property codes are custom, what vanilla must keep
    /// of them, and which loads wait until the mod has built the property.
    /// </summary>
    /// <remarks>
    /// Vanilla knows the property list when the world loads. A mod that builds a property later would lose its save file
    /// (vanilla deletes files it did not list), see its objects fail to load (the property is not there yet) and get no
    /// heatmap (the heatmap reads the property list once). The patches in <c>CustomPropertyPatches</c> route those three
    /// cases through this class.
    /// </remarks>
    internal static class CustomProperties
    {
        /// <summary>Seconds a load or the heatmap waits for a registered property before it gives up.</summary>
        internal const float WaitSeconds = 180f;

        private static readonly Log Logger = new Log("CustomProperties");
        private static readonly Regex SavedCode = new Regex("\"PropertyCode\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.CultureInvariant);

        private sealed class Registration
        {
            public CustomPropertyDefinition Def = null!;
            public bool Bound;
            public bool RootErrorLogged;
        }

        private sealed class DeferredLoad
        {
            public string Code = string.Empty;
            public string What = string.Empty;
            public Action Replay = null!;
            public float Deadline;
        }

        private static readonly Dictionary<string, Registration> Registrations = new Dictionary<string, Registration>(StringComparer.Ordinal);
        private static readonly List<DeferredLoad> Loads = new List<DeferredLoad>();
        private static Action? _heatmapStart;
        private static float _heatmapDeadline;
        private static bool _startingHeatmap;
        private static bool _pumpRunning;
        private static bool _lifecycleHooked;

        /// <summary>Test hook that receives every log line in place of MelonLoader.</summary>
        internal static Action<string, string>? LogSinkForTesting;
        /// <summary>Test hook that answers "is this property in the world" without the game.</summary>
        internal static Func<string, bool>? PropertyExistsForTesting;
        /// <summary>Test hook for the clock, in seconds.</summary>
        internal static Func<float>? NowForTesting;
        /// <summary>Test hook that replaces starting the per-frame pump.</summary>
        internal static Action? StartPumpForTesting;

        internal static int RegisteredCount => Registrations.Count;
        internal static int DeferredLoadCount => Loads.Count;
        internal static bool HeatmapDeferred => _heatmapStart != null;

        internal static bool IsRegistered(string? code) => code != null && Registrations.ContainsKey(code);

        internal static PropertyWrapper Register(CustomPropertyDefinition def)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));
            if (string.IsNullOrEmpty(def.Code))
            {
                Error("Register needs a property code; the definition was refused.");
                throw new ArgumentException("A custom property needs a Code.", nameof(def));
            }
            if (Registrations.ContainsKey(def.Code))
            {
                Error($"Property '{def.Code}' is registered already; the second registration was refused.");
                throw new InvalidOperationException($"Property '{def.Code}' is already registered.");
            }

            // A copy, so later edits of the caller's object do not change what was registered.
            Registrations[def.Code] = new Registration
            {
                Def = new CustomPropertyDefinition
                {
                    Code = def.Code,
                    Name = def.Name,
                    Price = def.Price,
                    Root = def.Root,
                    EmployeeCapacity = def.EmployeeCapacity,
                    KeepSaveWhenMissing = def.KeepSaveWhenMissing,
                },
            };
            Info($"Registered custom property '{def.Code}' (save kept while missing: {def.KeepSaveWhenMissing}, waits for root: {def.Root != null}).");
            EnsureLifecycleHook();
            StartPump();
            return new PropertyWrapper(def.Code);
        }

        // ---- save keeping ----

        /// <summary>
        /// Lists the saved property files vanilla must not delete: those of registered properties that are not in the
        /// world right now. Vanilla writes the file of every property that is in the world itself.
        /// </summary>
        /// <param name="kind">"property" or "business", for the log.</param>
        /// <param name="saveFolderName">Folder name of the manager inside the save.</param>
        /// <param name="parentFolderPath">Save folder the manager writes into.</param>
        /// <param name="keep">Receives the file names to keep.</param>
        internal static void KeepSavedFiles(string kind, string saveFolderName, string parentFolderPath, Action<string> keep)
        {
            try
            {
                if (Registrations.Count == 0)
                    return;
                var folder = Path.Combine(parentFolderPath, saveFolderName);
                if (!Directory.Exists(folder))
                    return;
                foreach (var file in Directory.GetFiles(folder, "*.json"))
                {
                    var name = Path.GetFileName(file);
                    string text;
                    try
                    {
                        text = File.ReadAllText(file);
                    }
                    catch (Exception ex)
                    {
                        // Unreadable: keeping the file is safer than letting vanilla delete data that may belong to a registered property.
                        Warning($"Could not read '{file}' to check it before the save; it is kept: {ex.Message}");
                        keep(name);
                        continue;
                    }
                    var match = SavedCode.Match(text);
                    if (!match.Success)
                        continue;
                    var code = match.Groups[1].Value;
                    if (!Registrations.TryGetValue(code, out var reg) || !reg.Def.KeepSaveWhenMissing || Exists(code))
                        continue;
                    keep(name);
                    Info($"Custom {kind} '{code}' is not in the world; its saved file '{name}' is kept unchanged.");
                }
            }
            catch (Exception ex)
            {
                Error($"Checking the saved {kind} files of custom properties failed; the save may delete them: {ex}");
            }
        }

        // ---- deferred loads and heatmap ----

        /// <summary>The property code in a legacy property folder, read from its Property or Business file; null when there is none.</summary>
        internal static string? ReadSavedCode(string mainPath)
        {
            foreach (var file in new[] { "Property.json", "Business.json" })
            {
                var path = Path.Combine(mainPath, file);
                if (!File.Exists(path))
                    continue;
                var match = SavedCode.Match(File.ReadAllText(path));
                if (match.Success)
                    return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        /// Holds a load back when its property is registered and not ready. Returns true when the load was deferred and the
        /// caller must skip the original; <paramref name="replay"/> runs it once the property is ready.
        /// </summary>
        internal static bool TryDeferLoad(string? code, string what, Action replay)
        {
            if (code == null || !Registrations.TryGetValue(code, out var reg) || IsReady(reg))
                return false;
            Loads.Add(new DeferredLoad { Code = code, What = what, Replay = replay, Deadline = Now() + WaitSeconds });
            Info($"Loading '{code}' waits until the property and its root exist ({what}).");
            StartPump();
            return true;
        }

        /// <summary>
        /// Holds the heatmap start back while a registered property is not ready, because the heatmap reads the property
        /// list once. Returns true when the start was deferred; <paramref name="start"/> runs it later.
        /// </summary>
        internal static bool TryDeferHeatmap(Action start)
        {
            if (_startingHeatmap || Registrations.Count == 0)
                return false;
            var waiting = new List<string>();
            foreach (var reg in Registrations.Values)
                if (!IsReady(reg))
                    waiting.Add(reg.Def.Code);
            if (waiting.Count == 0)
                return false;
            _heatmapStart = start;
            _heatmapDeadline = Now() + WaitSeconds;
            Info($"Heatmap start waits for {waiting.Count} custom propert(ies): {string.Join(", ", waiting)}.");
            StartPump();
            return true;
        }

        /// <summary>
        /// One step of the wait: binds properties that appeared, runs deferred loads and the heatmap start whose properties
        /// are ready, and gives up on those past their deadline. Returns true while something is still waiting.
        /// </summary>
        internal static bool Pump()
        {
            bool waiting = false;
            foreach (var reg in Registrations.Values)
            {
                if (reg.Bound)
                    continue;
                if (Exists(reg.Def.Code))
                    Bind(reg);
                else
                    waiting = true;
            }

            for (int i = Loads.Count - 1; i >= 0; i--)
            {
                var load = Loads[i];
                if (Registrations.TryGetValue(load.Code, out var reg) && IsReady(reg))
                {
                    Loads.RemoveAt(i);
                    Info($"Loading '{load.Code}' runs now ({load.What}).");
                    try { load.Replay(); }
                    catch (Exception ex) { Error($"Deferred load of '{load.Code}' ({load.What}) threw: {ex}"); }
                }
                else if (Now() >= load.Deadline)
                {
                    Loads.RemoveAt(i);
                    Error($"Gave up loading '{load.Code}' ({load.What}) after {WaitSeconds:0} s: the property was not built. Its saved file is kept.");
                }
            }

            if (_heatmapStart != null)
            {
                var pending = new List<string>();
                foreach (var reg in Registrations.Values)
                    if (!IsReady(reg))
                        pending.Add(reg.Def.Code);
                if (pending.Count == 0 || Now() >= _heatmapDeadline)
                {
                    var start = _heatmapStart;
                    _heatmapStart = null;
                    if (pending.Count > 0)
                        Warning($"Heatmap starts after {WaitSeconds:0} s without {string.Join(", ", pending)}; those properties have no temperature view.");
                    else
                        Info("Heatmap starts now; every custom property is ready.");
                    _startingHeatmap = true;
                    try { start(); }
                    catch (Exception ex) { Error($"Starting the heatmap failed; temperature views are missing: {ex}"); }
                    finally { _startingHeatmap = false; }
                }
            }

            return waiting || Loads.Count > 0 || _heatmapStart != null;
        }

        /// <summary>True while the heatmap start is being replayed, so its patch lets it through.</summary>
        internal static bool IsStartingHeatmap => _startingHeatmap;

        private static void Bind(Registration reg)
        {
            reg.Bound = true;
            var code = reg.Def.Code;
            if (PropertyExistsForTesting != null)
            {
                Info($"Custom property '{code}' is in the world.");
                return;
            }
            var property = CustomPropertyWorld.Find(code);
            if (property == null)
                return;
            try
            {
                if (reg.Def.Price > 0f)
                    property.Price = reg.Def.Price;
                if (reg.Def.EmployeeCapacity > 0)
                    property.EmployeeCapacity = reg.Def.EmployeeCapacity;
                if (!string.IsNullOrEmpty(reg.Def.Name) && property.PropertyName != reg.Def.Name)
                    Warning($"Custom property '{code}' is named '{property.PropertyName}' in the world, the definition says '{reg.Def.Name}'. The save file follows the world name.");
                Info($"Custom property '{code}' is in the world (price {property.Price}, {property.EmployeeCapacity} employee(s)).");
            }
            catch (Exception ex)
            {
                Error($"Applying the definition of custom property '{code}' failed: {ex}");
            }
        }

        internal static bool Exists(string code) =>
            PropertyExistsForTesting != null ? PropertyExistsForTesting(code) : CustomPropertyWorld.Find(code) != null;

        private static bool IsReady(Registration reg) => Exists(reg.Def.Code) && RootReady(reg);

        private static bool RootReady(Registration reg)
        {
            var root = reg.Def.Root;
            if (root == null)
                return true;
            try
            {
                return root() != null;
            }
            catch (Exception ex)
            {
                if (!reg.RootErrorLogged)
                {
                    reg.RootErrorLogged = true;
                    Error($"Root of custom property '{reg.Def.Code}' threw, so the property counts as not ready: {ex}");
                }
                return false;
            }
        }

        private static float Now() => NowForTesting != null ? NowForTesting() : UnityNow();

        // Kept apart so that the unit tests, which have no Unity runtime, never compile the Unity call.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static float UnityNow() => UnityEngine.Time.unscaledTime;

        // ---- pump ----

        private static void StartPump()
        {
            if (_pumpRunning)
                return;
            _pumpRunning = true;
            if (StartPumpForTesting != null)
            {
                StartPumpForTesting();
                return;
            }
            MelonLoader.MelonCoroutines.Start(PumpRoutine());
        }

        private static IEnumerator PumpRoutine()
        {
            while (true)
            {
                bool more;
                try
                {
                    more = Pump();
                }
                catch (Exception ex)
                {
                    Error($"Waiting for custom properties failed and stopped: {ex}");
                    more = false;
                }
                if (!more)
                    break;
                yield return null;
            }
            _pumpRunning = false;
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;
            _lifecycleHooked = true;
            GameLifecycle.OnPreSceneChange += ClearSceneState;
        }

        // Property objects, deferred loads and the heatmap belong to the loaded world; registrations belong to the mod.
        private static void ClearSceneState()
        {
            if (Loads.Count > 0 || _heatmapStart != null)
                Warning($"Scene change dropped {Loads.Count} deferred load(s){(_heatmapStart != null ? " and the deferred heatmap start" : string.Empty)}.");
            Loads.Clear();
            _heatmapStart = null;
            foreach (var reg in Registrations.Values)
                reg.Bound = false;
            if (Registrations.Count > 0)
                StartPump();
        }

        internal static void ResetForTesting()
        {
            Registrations.Clear();
            Loads.Clear();
            _heatmapStart = null;
            _startingHeatmap = false;
            _pumpRunning = false;
            _lifecycleHooked = true;
            LogSinkForTesting = null;
            PropertyExistsForTesting = null;
            NowForTesting = null;
            StartPumpForTesting = () => { };
        }

        private static void Info(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("info", message); return; }
            Logger.Msg(message);
        }

        private static void Warning(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("warn", message); return; }
            Logger.Warning(message);
        }

        private static void Error(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("error", message); return; }
            Logger.Error(message);
        }
    }
}
