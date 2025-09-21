#if (IL2CPPMELON)
using S1Dialogue = Il2CppScheduleOne.Dialogue;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Dialogue = ScheduleOne.Dialogue;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Entities.Dialogue
{
    /// <summary>
    /// Builder for composing an NPC-specific dialogue database without requiring asset bundles.
    /// Public surface only accepts strings and arrays; no game types exposed.
    /// </summary>
    public sealed class DialogueDatabaseBuilder
    {
        private readonly List<(string key, string[] lines)> _genericEntries = new List<(string, string[])>();
        private readonly Dictionary<string, List<(string key, string[] lines)>> _modules = new Dictionary<string, List<(string, string[])>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Adds a generic entry (available under the Generic module).
        /// </summary>
        public DialogueDatabaseBuilder WithGeneric(string key, params string[] lines)
        {
            if (!string.IsNullOrEmpty(key) && lines != null)
                _genericEntries.Add((key, lines));
            return this;
        }

        /// <summary>
        /// Adds an entry to a named module (e.g., "Reactions", "SmallTalk").
        /// </summary>
        public DialogueDatabaseBuilder WithModuleEntry(string moduleName, string key, params string[] lines)
        {
            if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(key) || lines == null)
                return this;
            if (!_modules.TryGetValue(moduleName, out var list))
            {
                list = new List<(string, string[])>();
                _modules[moduleName] = list;
            }
            list.Add((key, lines));
            return this;
        }

        /// <summary>
        /// INTERNAL: Produces a ScriptableObject database and runtime module specs to attach at runtime.
        /// </summary>
        internal BuildResult BuildInternal()
        {
            var db = ScriptableObject.CreateInstance<S1Dialogue.DialogueDatabase>();

            // Build GenericEntries
            var generic = new List<S1Dialogue.Entry>();
            foreach (var (key, lines) in _genericEntries)
            {
                var chain = new S1Dialogue.DialogueChain { Lines = lines ?? Array.Empty<string>() };
                generic.Add(new S1Dialogue.Entry { Key = key, Chains = new[] { chain } });
            }
            // Assign generic entries list
            db.GenericEntries = ToIl2CppList(generic);

            // Modules will be added as runtime components; hand back specs
            var moduleSpecs = new List<ModuleSpec>();
            foreach (var kvp in _modules)
            {
                var entries = new List<S1Dialogue.Entry>();
                foreach (var (key, lines) in kvp.Value)
                {
                    var chain = new S1Dialogue.DialogueChain { Lines = lines ?? Array.Empty<string>() };
                    entries.Add(new S1Dialogue.Entry { Key = key, Chains = new[] { chain } });
                }
                moduleSpecs.Add(new ModuleSpec(kvp.Key, entries));
            }

            return new BuildResult(db, moduleSpecs);
        }

        /// <summary>
        /// INTERNAL: Result wrapper.
        /// </summary>
        internal sealed class BuildResult
        {
            internal readonly S1Dialogue.DialogueDatabase Database;
            internal readonly List<ModuleSpec> ModuleSpecs;

            internal BuildResult(S1Dialogue.DialogueDatabase database, List<ModuleSpec> moduleSpecs)
            {
                Database = database;
                ModuleSpecs = moduleSpecs;
            }
        }

        /// <summary>
        /// INTERNAL: Module description for runtime attachment.
        /// </summary>
        internal sealed class ModuleSpec
        {
            internal readonly string ModuleName;
            internal readonly List<S1Dialogue.Entry> Entries;

            internal ModuleSpec(string moduleName, List<S1Dialogue.Entry> entries)
            {
                ModuleName = moduleName;
                Entries = entries;
            }
        }

#if (IL2CPPMELON)
        private static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            if (source == null)
                return list;
            for (int i = 0; i < source.Count; i++)
                list.Add(source[i]);
            return list;
        }
#else
        private static System.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            return source;
        }
#endif
    }
}


