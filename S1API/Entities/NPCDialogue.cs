#if (IL2CPPMELON)
using S1Dialogue = Il2CppScheduleOne.Dialogue;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Dialogue = ScheduleOne.Dialogue;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;
using S1API.Entities.Dialogue;
using S1API.Internal.Abstraction;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing dialogue wrapper for an NPC. Provides helpers to create interactive conversations with branching dialogue trees,
    /// choice-based interactions, and dynamic responses. Use <see cref="BuildAndRegisterContainer"/> to define custom conversations.
    /// </summary>
    /// <remarks>
    /// Dialogue configuration is done in <see cref="NPC.OnCreated"/>. Use <see cref="BuildAndSetDatabase"/> for dialogue entries and <see cref="BuildAndRegisterContainer"/> for conversation flows.
    /// Subscribe to choice and node events for dynamic dialogue behavior.
    /// </remarks>
    public sealed class NPCDialogue
    {
        /// <summary>
        /// INTERNAL: Reference to the NPC on API side.
        /// </summary>
        internal readonly NPC NPC;

        internal NPCDialogue(NPC npc)
        {
            NPC = npc;
        }

        /// <summary>
        /// Whether a dialogue is currently in progress for this NPC.
        /// </summary>
        public bool IsDialogueInProgress => Handler != null && Handler.IsDialogueInProgress;

        /// <summary>
        /// Register a callback to run when a choice with the given label is selected.
        /// Label must match the DialogueChoiceData.ChoiceLabel in your container.
        /// </summary>
        public NPCDialogue OnChoiceSelected(string choiceLabel, Action callback)
        {
            if (string.IsNullOrEmpty(choiceLabel) || callback == null)
                return this;
            EnsureHandler();
            EnsureEventHooks();
            if (!_choiceCallbacks.TryGetValue(choiceLabel, out var list))
            {
                list = new List<Action>();
                _choiceCallbacks[choiceLabel] = list;
            }
            list.Add(callback);
            return this;
        }

        /// <summary>
        /// Register a callback to run when a dialogue node with the given label is displayed.
        /// </summary>
        public NPCDialogue OnNodeDisplayed(string nodeLabel, Action callback)
        {
            if (string.IsNullOrEmpty(nodeLabel) || callback == null)
                return this;
            EnsureHandler();
            EnsureEventHooks();
            if (!_nodeCallbacks.TryGetValue(nodeLabel, out var list))
            {
                list = new List<Action>();
                _nodeCallbacks[nodeLabel] = list;
            }
            list.Add(callback);
            return this;
        }

        /// <summary>
        /// Removes all registered dialogue callbacks for this NPC.
        /// </summary>
        public void ClearCallbacks()
        {
            _choiceCallbacks.Clear();
            _nodeCallbacks.Clear();
        }

        /// <summary>
        /// Starts a dialogue by container name present on the NPC's handler.
        /// </summary>
        public void Start(string containerName, bool enableBehaviour = true, string entryNodeLabel = "ENTRY")
        {
            if (string.IsNullOrEmpty(containerName))
                return;
            EnsureHandler();
            Handler?.InitializeDialogue(containerName, enableBehaviour, entryNodeLabel);
        }

        /// <summary>
        /// Ends any active dialogue.
        /// </summary>
        public void End()
        {
            Handler?.EndDialogue();
        }

        /// <summary>
        /// Shows worldspace dialogue text at the NPC for a duration.
        /// </summary>
        public void ShowWorldText(string text, float durationSeconds)
        {
            if (string.IsNullOrEmpty(text))
                return;
            EnsureHandler();
            Handler?.ShowWorldspaceDialogue(text, durationSeconds);
        }

        /// <summary>
        /// Plays a reaction by key. If duration is -1 the underlying system decides duration.
        /// </summary>
        public void PlayReaction(string key, float durationSeconds = -1f, bool network = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                Handler?.HideWorldspaceDialogue();
                return;
            }
            EnsureHandler();
            Handler?.PlayReaction(key, durationSeconds, network);
        }

        /// <summary>
        /// Overrides the shown dialogue text (e.g., for temporary notifications).
        /// You generally won't want to use this
        /// </summary>
        public void OverrideText(string text)
        {
            EnsureHandler();
            Handler?.OverrideShownDialogue(text);
        }

        /// <summary>
        /// Stops any active override and resumes normal dialogue display.
        /// </summary>
        public void StopOverride()
        {
            Handler?.StopOverride();
            Handler?.GetComponent<S1Dialogue.DialogueController>().ClearOverrideContainer();
        }

        /// <summary>
        /// INTERNAL: Returns the DialogueHandler instance, if present.
        /// </summary>
        internal S1Dialogue.DialogueHandler Handler => NPC.gameObject.GetComponentInChildren<S1Dialogue.DialogueHandler>(true);

        /// <summary>
        /// INTERNAL: Ensures there is a DialogueHandler component attached.
        /// </summary>
        internal void EnsureHandler()
        {
            if (Handler == null)
                NPC.gameObject.AddComponent<S1Dialogue.DialogueHandler>();
        }

        private void EnsureEventHooks()
        {
            if (Handler == null || _eventsHooked)
                return;
            _eventsHooked = true;
            // Handler events are invoked from DialogueHandler.ChoiceCallback and DialogueCallback
            EventHelper.AddListener(Internal_OnChoice, Handler.onDialogueChoiceChosen);
            EventHelper.AddListener(Internal_OnNode, Handler.onDialogueNodeDisplayed);
        }

        /// <summary>
        /// INTERNAL: Rebuilds runtime modules on the handler to match a new database.
        /// Mirrors the logic in DialogueHandler.Awake for initializing modules.
        /// </summary>
        internal void RebuildRuntimeModules(S1Dialogue.DialogueDatabase db)
        {
            if (Handler == null || db == null)
                return;

            // Bind database first so internal refs are valid
            try { db.Initialize(Handler); } catch { }

            EnsureRuntimeModulesList();

            // Reset and rebuild from DB
            try { Handler.runtimeModules.Clear(); } catch { }

            // Remove any DialogueModule components attached locally (these are typically the generic module)
            var localModules = Handler.gameObject.GetComponents<S1Dialogue.DialogueModule>();
            for (int i = 0; i < localModules.Length; i++)
                Object.Destroy(localModules[i]);

            // Create a fresh Generic module on the NPC and seed with database GenericEntries
            var generic = Handler.gameObject.AddComponent<S1Dialogue.DialogueModule>();
            generic.ModuleType = S1Dialogue.EDialogueModule.Generic;
            generic.Entries = db.GenericEntries;
            Handler.runtimeModules.Add(generic);

            // Append database-provided modules (scene/prefab modules)
            if (db.Modules != null)
            {
                for (int i = 0; i < db.Modules.Count; i++)
                {
                    try { Handler.runtimeModules.Add(db.Modules[i]); } catch { }
                }
            }
        }

        /// <summary>
        /// Builds a dialogue database at runtime from string data and installs it on this NPC.
        /// Does not require asset bundles.
        /// </summary>
        public void BuildAndSetDatabase(Action<DialogueDatabaseBuilder> configure)
        {
            if (configure == null)
                return;
            var builder = new DialogueDatabaseBuilder();
            configure(builder);
            var built = builder.BuildInternal();

            EnsureHandler();
            if (Handler == null)
                return;

            // If a database already exists, append rather than replace
            if (Handler.Database != null)
            {
                AppendIntoExistingDatabase(Handler.Database, built.Database);
                AppendRuntimeModulesFromDatabase(built.Database);
            }
            else
            {
                // Assign database and build modules for first time
                Handler.Database = built.Database;
                RebuildRuntimeModules(built.Database);
            }

            // Attach module components with provided names and entries
            if (built.ModuleSpecs != null)
            {
                foreach (var spec in built.ModuleSpecs)
                {
                    var mod = Handler.gameObject.AddComponent<S1Dialogue.DialogueModule>();
                    // Try to map a known enum name; fallback keeps as custom
                    if (System.Enum.TryParse(typeof(S1Dialogue.EDialogueModule), spec.ModuleName, true, out var enumVal))
                        mod.ModuleType = (S1Dialogue.EDialogueModule)enumVal;
                    mod.Entries = ToIl2CppEntryList(spec.Entries);
                    EnsureRuntimeModulesList();
                    try { Handler.runtimeModules.Add(mod); } catch { }
                }
            }

            // Ensure database bound to handler after changes
            try { (Handler.Database ?? built.Database)?.Initialize(Handler); } catch { }
        }

        private void EnsureRuntimeModulesList()
        {
            try
            {
                if (Handler.runtimeModules == null)
                {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    Handler.runtimeModules = new Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueModule>();
#else
                    runtimeModulesProperty?.SetValue(Handler, new System.Collections.Generic.List<S1Dialogue.DialogueModule>());
#endif
                }
            }
            catch { }
        }

        private void AppendIntoExistingDatabase(S1Dialogue.DialogueDatabase target, S1Dialogue.DialogueDatabase source)
        {
            if (target == null || source == null)
                return;

            // Append generic entries
            try
            {
                if (source.GenericEntries != null)
                {
                    for (int i = 0; i < source.GenericEntries.Count; i++)
                        target.GenericEntries.Add(source.GenericEntries[i]);
                }
            }
            catch { }

            // Append modules
            try
            {
                if (source.Modules != null)
                {
                    // Ensure target modules exists
                    if (target.Modules == null)
                    {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                        target.Modules = new Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueModule>();
#else
                        target.Modules = new System.Collections.Generic.List<S1Dialogue.DialogueModule>();
#endif
                    }
                    for (int i = 0; i < source.Modules.Count; i++)
                        target.Modules.Add(source.Modules[i]);
                }
            }
            catch { }

            try { target.Initialize(Handler); } catch { }
        }

        private void AppendRuntimeModulesFromDatabase(S1Dialogue.DialogueDatabase db)
        {
            if (Handler == null || db == null)
                return;
            EnsureRuntimeModulesList();
            try
            {
                // Add a Generic module if DB has generic entries
                if (db.GenericEntries != null && db.GenericEntries.Count > 0)
                {
                    var generic = Handler.gameObject.AddComponent<S1Dialogue.DialogueModule>();
                    generic.ModuleType = S1Dialogue.EDialogueModule.Generic;
                    generic.Entries = db.GenericEntries;
                    Handler.runtimeModules.Add(generic);
                }
            }
            catch { }

            if (db.Modules != null)
            {
                for (int i = 0; i < db.Modules.Count; i++)
                {
                    try { Handler.runtimeModules.Add(db.Modules[i]); } catch { }
                }
            }
        }

        /// <summary>
        /// Builds a DialogueContainer with choice-based flow and registers it by name.
        /// Use this to define custom conversations for this NPC entirely from code.
        /// </summary>
        public void BuildAndRegisterContainer(string containerName, Action<DialogueContainerBuilder> configure)
        {
            if (string.IsNullOrEmpty(containerName) || configure == null)
                return;
            EnsureHandler();
            if (Handler == null)
                return;

            var contBuilder = new DialogueContainerBuilder();
            configure(contBuilder);
            var container = contBuilder.Build(containerName);

#if MONOMELON
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.DialogueContainer>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list != null)
            {
                int idx = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item != null && item.name == containerName)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0)
                    list[idx] = container;
                else
                    list.Add(container);
            }
        }

        /// <summary>
        /// When the player interacts with this NPC, force using the named container for the next dialogue.
        /// Returns true if the container was found and applied.
        /// </summary>
        public bool UseContainerOnInteract(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                return false;
            EnsureHandler();
            if (Handler == null)
                return false;

#if MONOMELON
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.DialogueContainer>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list == null)
                return false;
            S1Dialogue.DialogueContainer container = null;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item != null && item.name == containerName)
                {
                    container = item;
                    break;
                }
            }
            if (container == null)
                return false;

            var controller = Handler.GetComponent<S1Dialogue.DialogueController>();
            if (controller == null)
                return false;

            controller.SetOverrideContainer(container);
            return true;
        }

        /// <summary>
        /// When the player interacts with this NPC, force using the named container once for the next dialogue.
        /// After the conversation begins, the override is automatically cleared so subsequent interactions use normal flow.
        /// Returns true if the container was found and applied.
        /// </summary>
        public bool UseContainerOnInteractOnce(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                return false;
            EnsureHandler();
            if (Handler == null)
                return false;

#if MONOMELON
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.DialogueContainer>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list == null)
                return false;
            S1Dialogue.DialogueContainer container = null;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item != null && item.name == containerName)
                {
                    container = item;
                    break;
                }
            }
            if (container == null)
                return false;

            var controller = Handler.GetComponent<S1Dialogue.DialogueController>();
            if (controller == null)
                return false;

            controller.SetOverrideContainer(container);

            // Clear the override as soon as the conversation actually starts
            void ClearOnce()
            {
                try { controller.ClearOverrideContainer(); } catch { }
                try { EventHelper.RemoveListener((System.Action)ClearOnce, Handler.onConversationStart); } catch { }
            }
            try { EventHelper.AddListener((System.Action)ClearOnce, Handler.onConversationStart); } catch { }

            return true;
        }

        /// <summary>
        /// Immediately navigates this NPC's dialogue to a specific container and entry node.
        /// Returns true on success.
        /// </summary>
        public bool JumpTo(string containerName, string entryNodeLabel, bool enableBehaviour = false)
        {
            if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(entryNodeLabel))
                return false;
            EnsureHandler();
            if (Handler == null)
                return false;
#if MONOMELON
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.DialogueContainer>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list == null)
                return false;
            S1Dialogue.DialogueContainer container = null;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item != null && item.name == containerName)
                {
                    container = item;
                    break;
                }
            }
            if (container == null)
                return false;
            Handler.InitializeDialogue(container, enableBehaviour, entryNodeLabel);
            return true;
        }

        private void Internal_OnChoice(string choiceLabel)
        {
            if (string.IsNullOrEmpty(choiceLabel))
                return;
            if (_choiceCallbacks.TryGetValue(choiceLabel, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    try { list[i]?.Invoke(); } catch { }
                }
            }
        }

        private void Internal_OnNode(string nodeLabel)
        {
            if (string.IsNullOrEmpty(nodeLabel))
                return;
            if (_nodeCallbacks.TryGetValue(nodeLabel, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    try { list[i]?.Invoke(); } catch { }
                }
            }
        }

#if MONOMELON
        private FieldInfo dialogueContainersField = typeof(S1Dialogue.DialogueHandler).GetField("dialogueContainers", BindingFlags.NonPublic | BindingFlags.Instance);
        private PropertyInfo runtimeModulesProperty = typeof(S1Dialogue.DialogueHandler).GetProperty("runtimeModules", BindingFlags.NonPublic | BindingFlags.Instance);
#else
        // In IL2CPP, dialogueContainers is a property, not a field
#endif
        private readonly Dictionary<string, List<Action>> _choiceCallbacks = new Dictionary<string, List<Action>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Action>> _nodeCallbacks = new Dictionary<string, List<Action>>(StringComparer.OrdinalIgnoreCase);
        private bool _eventsHooked;

#if (IL2CPPMELON || IL2CPPBEPINEX)
        private static Il2CppSystem.Collections.Generic.List<S1Dialogue.Entry> ToIl2CppEntryList(System.Collections.Generic.List<S1Dialogue.Entry> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<S1Dialogue.Entry>();
            if (source == null)
                return list;
            for (int i = 0; i < source.Count; i++)
                list.Add(source[i]);
            return list;
        }
#else
        private static System.Collections.Generic.List<S1Dialogue.Entry> ToIl2CppEntryList(System.Collections.Generic.List<S1Dialogue.Entry> source)
        {
            return source;
        }
#endif
    }
}


