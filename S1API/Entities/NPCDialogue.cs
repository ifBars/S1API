#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using NativeAction = Il2CppSystem.Action;
#elif MONOMELON
using S1Dialogue = ScheduleOne.Dialogue;
using NativeAction = System.Action;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;
using S1API.Entities.Dialogue;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing dialogue wrapper for an NPC. Provides helpers to create interactive conversations with branching dialogue trees,
    /// choice-based interactions, and dynamic responses. Use <see cref="BuildAndRegisterContainer"/> to define custom conversations.
    /// </summary>
    /// <remarks>
    /// Dialogue configuration is done in <see cref="NPC.OnCreated"/>. Use <see cref="BuildAndSetDatabase"/> for dialogue entries and <see cref="BuildAndRegisterContainer"/> for conversation flows.
    /// Subscribe to choice, node, and completion events for dynamic dialogue behavior.
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
            _choiceCallbacks.Add(choiceLabel, callback);
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
            _nodeCallbacks.Add(nodeLabel, callback);
            return this;
        }

        /// <summary>
        /// Register a callback to run when a conversation starts with this NPC.
        /// This fires before the dialogue is displayed, allowing you to prepare or rebuild dialogue containers.
        /// </summary>
        public NPCDialogue OnConversationStart(Action callback)
        {
            if (callback == null)
                return this;
            EnsureHandler();
            EnsureEventHooks();
            _conversationStartCallbacks.Add(callback);
            return this;
        }

        /// <summary>
        /// Register a callback to run when any dialogue interaction handled by this NPC ends.
        /// </summary>
        /// <remarks>
        /// The callback is handler-wide and does not identify the ending container. It runs when the native
        /// handler ends an interaction, including explicit calls to <see cref="End"/>. Register it against
        /// the active NPC handler; it does not persist through destruction and recreation of the handler.
        /// </remarks>
        /// <param name="callback">The callback to invoke when the dialogue handler ends an interaction.</param>
        /// <returns>This dialogue wrapper.</returns>
        public NPCDialogue OnDialogueEnded(Action callback)
        {
            if (callback == null)
                return this;
            EnsureHandler();
            EnsureEventHooks();
            _dialogueEndedCallbacks.Add(callback);
            return this;
        }

        /// <summary>
        /// Removes all registered dialogue callbacks for this NPC.
        /// </summary>
        public void ClearCallbacks()
        {
            _choiceCallbacks.Clear();
            _nodeCallbacks.Clear();
            _conversationStartCallbacks.Clear();
            _dialogueEndedCallbacks.Clear();
            RemoveEventHooks();
        }

        /// <summary>
        /// Enables or disables a controller-level dialogue choice by its destination container name.
        /// </summary>
        /// <remarks>
        /// This changes only the live <c>DialogueController</c> choice state. It does not modify node choices,
        /// save data, or network state. The name is matched case-insensitively against the choice's destination
        /// container, and the method returns <see langword="false"/> when that controller choice is unavailable.
        /// </remarks>
        /// <param name="dialogueContainerName">The name of the dialogue container opened by the choice.</param>
        /// <param name="enabled">Whether the choice should be enabled.</param>
        /// <returns><see langword="true"/> when a matching controller choice was updated; otherwise <see langword="false"/>.</returns>
        public bool SetChoiceEnabled(string dialogueContainerName, bool enabled)
        {
            if (string.IsNullOrEmpty(dialogueContainerName))
                return false;

            var controller = Handler?.GetComponent<S1Dialogue.DialogueController>();
            var choices = controller?.Choices;
            if (choices == null)
                return false;

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice == null || choice.Conversation == null)
                    continue;

                if (!NPCDialoguePolicy.MatchesChoiceContainer(choice.Conversation.name, dialogueContainerName))
                    continue;

                choice.Enabled = enabled;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts a dialogue by container name present on the NPC's handler.
        /// </summary>
        public void Start(string containerName, bool enableBehaviour = true, string entryNodeLabel = "ENTRY")
        {
            if (string.IsNullOrEmpty(containerName))
                return;
            EnsureHandler();
            StartDialogueCompat(containerName, enableBehaviour, entryNodeLabel);
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
        internal S1Dialogue.DialogueHandler? Handler
        {
            get
            {
                var handler = FindHandler();
                if (handler != null && HasCallbacks)
                    EnsureEventHooks(handler);
                return handler;
            }
        }

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
            EnsureEventHooks(FindHandler());
        }

        private void EnsureEventHooks(S1Dialogue.DialogueHandler? handler)
        {
            if (handler == null)
                return;

            if (_hookedHandler == handler)
                return;

            RemoveEventHooks();

            NativeAction? dialogueEndedDispatcher = CreateDialogueEndedDispatcher();
            if (dialogueEndedDispatcher == null)
                return;

            _hookedHandler = handler;
            _nativeDialogueEndedDispatcher = dialogueEndedDispatcher;

            // Handler events are invoked from DialogueHandler.ChoiceCallback and DialogueCallback
            try
            {
                global::S1API.Utils.EventHelper.AddListener(Internal_OnChoice, handler.onDialogueChoiceChosen);
                global::S1API.Utils.EventHelper.AddListener(Internal_OnNode, handler.onDialogueNodeDisplayed);
                global::S1API.Utils.EventHelper.AddListener(Internal_OnConversationStart, handler.onConversationStart);
                handler.OnDialogueEnd += dialogueEndedDispatcher;
            }
            catch
            {
                RemoveEventHooks();
            }
        }

        private S1Dialogue.DialogueHandler? FindHandler() =>
            NPC.gameObject.GetComponentInChildren<S1Dialogue.DialogueHandler>(true);

        private bool HasCallbacks =>
            _choiceCallbacks.HasCallbacks
            || _nodeCallbacks.HasCallbacks
            || _conversationStartCallbacks.HasCallbacks
            || _dialogueEndedCallbacks.HasCallbacks;

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
            var runtimeModules = GetRuntimeModules();
            try { runtimeModules?.Clear(); } catch { }

            // Remove any DialogueModule components attached locally (these are typically the generic module)
            var localModules = Handler.gameObject.GetComponents<S1Dialogue.DialogueModule>();
            for (int i = 0; i < localModules.Length; i++)
                Object.Destroy(localModules[i]);

            // Create a fresh Generic module on the NPC and seed with database GenericEntries
            var generic = Handler.gameObject.AddComponent<S1Dialogue.DialogueModule>();
            generic.ModuleType = S1Dialogue.EDialogueModule.Generic;
            generic.Entries = db.GenericEntries;
            runtimeModules?.Add(generic);

            // Append database-provided modules (scene/prefab modules)
            if (db.Modules != null)
            {
                for (int i = 0; i < db.Modules.Count; i++)
                {
                    try { runtimeModules?.Add(db.Modules[i]); } catch { }
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
                ReflectionUtils.TrySetFieldOrProperty(Handler, "Database", built.Database);
                RebuildRuntimeModules(built.Database);
            }

            // Attach module components with provided names and entries
            if (built.ModuleSpecs != null)
            {
                foreach (var spec in built.ModuleSpecs)
                {
                    var mod = Handler.gameObject.AddComponent<S1Dialogue.DialogueModule>();
                    // Try to map a known enum name; fallback keeps as custom
                    if (System.Enum.TryParse(spec.ModuleName, true, out S1Dialogue.EDialogueModule moduleType))
                        mod.ModuleType = moduleType;
                    mod.Entries = ToIl2CppEntryList(spec.Entries);
                    EnsureRuntimeModulesList();
                    try { GetRuntimeModules()?.Add(mod); } catch { }
                }
            }

            // Ensure database bound to handler after changes
            try { (Handler.Database ?? built.Database)?.Initialize(Handler); } catch { }
        }

        private void EnsureRuntimeModulesList()
        {
            try
            {
                if (GetRuntimeModules() == null)
                {
#if IL2CPPMELON
                    ReflectionUtils.TrySetFieldOrProperty(Handler, "RuntimeModules", new Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueModule>());
#else
                    runtimeModulesProperty?.SetValue(Handler, new List<S1Dialogue.DialogueModule>());
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
#if IL2CPPMELON
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
            var runtimeModules = GetRuntimeModules();
            try
            {
                // Add a Generic module if DB has generic entries
                if (db.GenericEntries != null && db.GenericEntries.Count > 0)
                {
                    var generic = Handler.gameObject.AddComponent<S1Dialogue.DialogueModule>();
                    generic.ModuleType = S1Dialogue.EDialogueModule.Generic;
                    generic.Entries = db.GenericEntries;
                    runtimeModules?.Add(generic);
                }
            }
            catch { }

            if (db.Modules != null)
            {
                for (int i = 0; i < db.Modules.Count; i++)
                {
                    try { runtimeModules?.Add(db.Modules[i]); } catch { }
                }
            }
        }

        /// <summary>
        /// Builds a DialogueContainer with choice-based flow and registers it by name.
        /// Use this to define custom conversations for this NPC entirely from code.
        /// 
        /// If a custom NPC, you must also call <see cref="BuildAndSetDatabase(Action{DialogueDatabaseBuilder})">BuildAndSetDatabase</see>
        /// for this to work
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
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.Conversation>;
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
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.Conversation>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list == null)
                return false;
            S1Dialogue.Conversation? container = null;
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
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.Conversation>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list == null)
                return false;
            S1Dialogue.Conversation? container = null;
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
                try { global::S1API.Utils.EventHelper.RemoveListener((System.Action)ClearOnce, Handler.onConversationStart); } catch { }
            }
            try { global::S1API.Utils.EventHelper.AddListener((System.Action)ClearOnce, Handler.onConversationStart); } catch { }

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
            var list = dialogueContainersField?.GetValue(Handler) as List<S1Dialogue.Conversation>;
#else
            var list = Handler.dialogueContainers;
#endif
            if (list == null)
                return false;
            S1Dialogue.Conversation? container = null;
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
            return StartDialogueCompat(container, enableBehaviour, entryNodeLabel);
        }

        private void Internal_OnChoice(string choiceLabel)
        {
            _choiceCallbacks.Invoke(choiceLabel);
        }

        private void Internal_OnNode(string nodeLabel)
        {
            _nodeCallbacks.Invoke(nodeLabel);
        }

        private void Internal_OnConversationStart()
        {
            _conversationStartCallbacks.InvokeAll();
        }

        private void Internal_OnDialogueEnded()
        {
            _dialogueEndedCallbacks.InvokeAll();
        }

        private NativeAction? CreateDialogueEndedDispatcher()
        {
#if IL2CPPMELON
            return DelegateSupport.ConvertDelegate<NativeAction>(new Action(Internal_OnDialogueEnded));
#else
            return Internal_OnDialogueEnded;
#endif
        }

        private void RemoveEventHooks()
        {
            var handler = _hookedHandler;
            var dialogueEndedDispatcher = _nativeDialogueEndedDispatcher;

            _hookedHandler = null;
            _nativeDialogueEndedDispatcher = null;

            if (handler == null)
                return;

            try { global::S1API.Utils.EventHelper.RemoveListener(Internal_OnChoice, handler.onDialogueChoiceChosen); } catch { }
            try { global::S1API.Utils.EventHelper.RemoveListener(Internal_OnNode, handler.onDialogueNodeDisplayed); } catch { }
            try { global::S1API.Utils.EventHelper.RemoveListener(Internal_OnConversationStart, handler.onConversationStart); } catch { }
            if (dialogueEndedDispatcher != null)
            {
                try { handler.OnDialogueEnd -= dialogueEndedDispatcher; } catch { }
            }
        }

#if MONOMELON
        private FieldInfo dialogueContainersField = typeof(S1Dialogue.DialogueHandler).GetField("dialogueContainers", BindingFlags.NonPublic | BindingFlags.Instance);
        private PropertyInfo runtimeModulesProperty = typeof(S1Dialogue.DialogueHandler).GetProperty("runtimeModules", BindingFlags.NonPublic | BindingFlags.Instance);
#else
        // In IL2CPP, dialogueContainers is a property, not a field
#endif
        private readonly NPCDialogueCallbackRegistry _choiceCallbacks = new NPCDialogueCallbackRegistry();
        private readonly NPCDialogueCallbackRegistry _nodeCallbacks = new NPCDialogueCallbackRegistry();
        private readonly NPCDialogueCallbackRegistry _conversationStartCallbacks = new NPCDialogueCallbackRegistry();
        private readonly NPCDialogueCallbackRegistry _dialogueEndedCallbacks = new NPCDialogueCallbackRegistry();
        private S1Dialogue.DialogueHandler? _hookedHandler;
        private NativeAction? _nativeDialogueEndedDispatcher;

#if IL2CPPMELON
        private Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueModule>? GetRuntimeModules()
        {
            var handler = Handler;
            if (handler == null)
                return null;

            return ReflectionUtils.TryGetFieldOrProperty(handler, "RuntimeModules") as Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueModule>
                ?? ReflectionUtils.TryGetFieldOrProperty(handler, "runtimeModules") as Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueModule>;
        }
#else
        private List<S1Dialogue.DialogueModule>? GetRuntimeModules()
        {
            var handler = Handler;
            if (handler == null)
                return null;

            return ReflectionUtils.TryGetFieldOrProperty(handler, "runtimeModules") as List<S1Dialogue.DialogueModule>
                ?? ReflectionUtils.TryGetFieldOrProperty(handler, "RuntimeModules") as List<S1Dialogue.DialogueModule>;
        }
#endif

        private bool StartDialogueCompat(string containerName, bool enableBehaviour = true, string entryNodeLabel = "ENTRY")
        {
            if (Handler == null || string.IsNullOrEmpty(containerName))
                return false;

            Handler.StartDialogue(containerName, enableBehaviour, entryNodeLabel);
            return true;
        }

        private bool StartDialogueCompat(S1Dialogue.Conversation container, bool enableBehaviour = true, string entryNodeLabel = "ENTRY")
        {
            if (Handler == null || container == null)
                return false;

            Handler.StartDialogue(container, enableBehaviour, entryNodeLabel);
            return true;
        }

#if IL2CPPMELON
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
