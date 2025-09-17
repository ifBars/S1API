#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1GameTime = Il2CppScheduleOne.GameTime;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1GameTime = ScheduleOne.GameTime;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing scheduling wrapper for an <see cref="NPC"/>.
    /// Exposes the underlying <see cref="S1NPCs.NPCScheduleManager"/> to enable, disable,
    /// and manage scheduled actions and curfew modes.
    /// </summary>
    public sealed class NPCSchedule
    {
        internal readonly NPC NPC;

        internal NPCSchedule(NPC npc)
        {
            NPC = npc;
        }

        /// <summary>
        /// Whether the schedule is currently enabled.
        /// </summary>
        public bool IsEnabled => Manager != null && Manager.ScheduleEnabled;

        /// <summary>
        /// Whether the schedule is currently in curfew mode.
        /// </summary>
        public bool CurfewModeEnabled => Manager != null && Manager.CurfewModeEnabled;

        /// <summary>
        /// Enables the NPC's schedule.
        /// </summary>
        public void Enable()
        {
            EnsureManager();
            Manager?.EnableSchedule();
        }

        /// <summary>
        /// Disables the NPC's schedule.
        /// </summary>
        public void Disable()
        {
            Manager?.DisableSchedule();
        }

        /// <summary>
        /// Initializes and re-reads actions from the scene for this NPC.
        /// </summary>
        public void InitializeActions()
        {
            EnsureManager();
            Manager?.InitializeActions();
        }

        /// <summary>
        /// Forces the manager to enforce state immediately (e.g., after toggles or time jumps).
        /// </summary>
        public void EnforceState()
        {
            Manager?.EnforceState();
        }

        /// <summary>
        /// Sets or clears curfew mode.
        /// </summary>
        public void SetCurfewMode(bool enabled)
        {
            if (Manager == null)
                return;
            Manager.SetCurfewModeEnabled(enabled);
        }

        /// <summary>
        /// Returns the active action label, if any.
        /// </summary>
        public string GetActiveActionName()
        {
            return Manager != null && Manager.ActiveAction != null ? Manager.ActiveAction.name : string.Empty;
        }

        /// <summary>
        /// INTERNAL: Adds a new schedule action instance under this NPC's schedule manager and sets its start time.
        /// </summary>
        internal T AddActionInternal<T>(int startTime, string name = null) where T : S1NPCsSchedules.NPCAction
        {
            EnsureManager();
            if (Manager == null)
                return null;

            var go = new GameObject(string.IsNullOrEmpty(name) ? typeof(T).Name : name);
            go.transform.SetParent(Manager.transform, false);

            var action = go.AddComponent<T>();
            action.SetStartTime(startTime);
            action.NetworkInitializeIfDisabled();

            // Let the manager pick up and sort the new action immediately
            Manager.InitializeActions();
            Manager.EnforceState();
            return action;
        }

        /// <summary>
        /// Ensures a deal-wait signal exists under the schedule manager so customer handovers can be toggled.
        /// </summary>
        public void EnsureDealSignal()
        {
            EnsureManager();
            if (Manager == null)
                return;

            var existing = Manager.GetComponentInChildren<S1NPCsSchedules.NPCSignal_WaitForDelivery>(true);
            if (existing != null)
            {
                existing.NetworkInitializeIfDisabled();
                return;
            }

            var dealGo = new GameObject("DealSignal");
            dealGo.transform.SetParent(Manager.transform, false);
            var signal = dealGo.AddComponent<S1NPCsSchedules.NPCSignal_WaitForDelivery>();
            signal.NetworkInitializeIfDisabled();
            dealGo.SetActive(false);

            Manager.InitializeActions();
        }

        /// <summary>
        /// Adds a timed "walk to location" step.
        /// A destination transform is created and parented under the step.
        /// </summary>
        public void AddWalkTo(Vector3 destination, int startTime, bool faceDestinationDir = true, float threshold = 1f, bool warpIfSkipped = false, string name = null)
        {
            var action = AddActionInternal<S1NPCsSchedules.NPCSignal_WalkToLocation>(startTime, string.IsNullOrEmpty(name) ? "WalkTo" : name);
            if (action == null)
                return;

            var destGo = new GameObject("Destination");
            destGo.transform.SetParent(action.transform, false);
            destGo.transform.position = destination;

            // Orient destination forward towards current NPC position so facing makes sense if requested
            var look = NPC.gameObject.transform.position;
            var forward = (destination - look);
            if (forward.sqrMagnitude > 0.001f)
                destGo.transform.forward = forward.normalized;

            action.Destination = destGo.transform;
            action.FaceDestinationDir = faceDestinationDir;
            action.DestinationThreshold = Mathf.Max(0.01f, threshold);
            action.WarpIfSkipped = warpIfSkipped;
        }

        /// <summary>
        /// Removes all actions under the schedule manager. Signals and/or events can be filtered.
        /// </summary>
        /// <param name="includeSignals">Remove signal-type actions.</param>
        /// <param name="includeEvents">Remove event-type actions.</param>
        public void ClearActions(bool includeSignals = true, bool includeEvents = true)
        {
            if (Manager == null)
                return;

            var actions = Manager.GetComponentsInChildren<S1NPCsSchedules.NPCAction>(true);
            foreach (var a in actions)
            {
                bool isSignal = a is S1NPCsSchedules.NPCSignal;
                bool isEvent = a is S1NPCsSchedules.NPCEvent;
                if ((isSignal && includeSignals) || (isEvent && includeEvents))
                {
                    UnityEngine.Object.Destroy(a.gameObject);
                }
            }
            Manager.InitializeActions();
        }

        /// <summary>
        /// Returns the names of currently configured actions (including inactive/disabled).
        /// </summary>
        public IReadOnlyList<string> GetActionNames()
        {
            if (Manager == null)
                return Array.Empty<string>();
            var actions = Manager.GetComponentsInChildren<S1NPCsSchedules.NPCAction>(true);
            var names = new List<string>(actions.Length);
            for (int i = 0; i < actions.Length; i++)
            {
                names.Add(actions[i] != null ? actions[i].name : string.Empty);
            }
            return names;
        }

        /// <summary>
        /// Starts a builder session to configure actions fluently.
        /// </summary>
        public void Build(System.Action<NPCScheduleBuilder> configure)
        {
            if (configure == null)
                return;
            var builder = new NPCScheduleBuilder(this);
            configure(builder);
            // Ensure manager reflects any changes
            InitializeActions();
            EnforceState();
        }

        /// <summary>
        /// INTERNAL: Ensures a schedule manager exists on the NPC root.
        /// </summary>
        internal void EnsureManager()
        {
            var mgr = NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (mgr == null)
            {
                var go = new GameObject("NPCSchedule");
                go.transform.SetParent(NPC.gameObject.transform, false);
                mgr = go.AddComponent<S1NPCs.NPCScheduleManager>();
            }
        }

        /// <summary>
        /// INTERNAL: Direct access to the underlying manager.
        /// </summary>
        internal S1NPCs.NPCScheduleManager Manager => NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
    }
}


