#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1GameTime = Il2CppScheduleOne.GameTime;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Behaviour = Il2CppScheduleOne.NPCs.Behaviour;
#elif MONOMELON
using S1NPCs = ScheduleOne.NPCs;
using S1GameTime = ScheduleOne.GameTime;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Behaviour = ScheduleOne.NPCs.Behaviour;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using UnityEngine;
using S1API.Entities.Schedule;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing scheduling wrapper for an NPC. Exposes the underlying schedule manager to enable, disable,
    /// and manage scheduled actions and curfew modes. Schedule configuration must be done in <see cref="NPC.ConfigurePrefab"/>.
    /// </summary>
    /// <remarks>
    /// Use this to control NPC movement patterns, building visits, and timed activities.
    /// Schedules are defined in <see cref="NPC.ConfigurePrefab"/> using <c>NPCPrefabBuilder.WithSchedule(...)</c> and managed at runtime via this wrapper.
    /// </remarks>
    public sealed class NPCSchedule
    {
        internal readonly NPC NPC;
        private static bool _loggedRemovedDealSignal;

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
            ScheduleBehaviour?.Enable_Server();
        }

        /// <summary>
        /// Disables the NPC's schedule.
        /// </summary>
        public void Disable()
        {
            Manager?.DisableSchedule();
            ScheduleBehaviour?.Disable_Server();
        }

        private S1Behaviour.ScheduleBehaviour? ScheduleBehaviour =>
            NPC.gameObject.GetComponentInChildren<S1Behaviour.ScheduleBehaviour>(true);

        /// <summary>
        /// Initializes/sorts the order of the schedules on this NPC.
        /// This method is responsible for adding times to the schedule names.
        /// </summary>
        internal void InitializeActions()
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
        internal T? AddActionInternal<T>(int startTime, string? name = null) where T : S1NPCsSchedules.NPCAction
        {
            EnsureManager();
            if (Manager == null)
                return null;

            // Prefer a pre-created, inactive action instance of this type to avoid changing component indices
            var pool = Manager.GetComponentsInChildren<T>(true);
            T? chosen = null;
            for (int i = 0; i < pool.Length; i++)
            {
                var candidate = pool[i];
                if (candidate == null)
                    continue;
                if (!candidate.gameObject.activeSelf)
                {
                    chosen = candidate;
                    break;
                }
            }

            // Fallback to any instance if all are active (do NOT create new components at runtime)
            if (chosen == null)
            {
                if (pool.Length > 0)
                {
                    chosen = pool[0];
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[S1API] No available pre-created actions of type {typeof(T).Name}. Add more via NPC.ConfigurePrefab.");
                    return null;
                }
            }

            if (chosen == null)
                return null;

            if (!string.IsNullOrEmpty(name))
                chosen.gameObject.name = name;

            chosen.SetStartTime(startTime);
            if (!chosen.gameObject.activeSelf)
                chosen.gameObject.SetActive(true);
            chosen.enabled = true;

            // Note: InitializeActions() and EnforceState() are called after all actions are added in FinalizeNetworkSpawn
            // Calling them here causes inconsistent sort results when building the schedule
            return chosen;
        }

        /// <summary>
        /// INTERNAL: Adds an action via an S1API spec. Use from builder.Add(spec).
        /// </summary>
        internal void AddActionFromSpec(IScheduleActionSpec spec)
        {
            if (spec == null)
                return;
            spec.ApplyTo(this);
        }

        /// <summary>
        /// Retained for source compatibility with game versions that used a deal-wait schedule signal.
        /// </summary>
        /// <remarks>
        /// Schedule I 0.4.6 removed <c>NPCSignal_WaitForDelivery</c>. Customer deal attendance is
        /// configured automatically when <see cref="NPC.IsCustomer"/> is <c>true</c>. This method
        /// now performs no runtime work and logs one compatibility warning per process.
        /// </remarks>
        [Obsolete("NPCSignal_WaitForDelivery was removed in game version 0.4.6. Override NPC.IsCustomer; customer deal attendance is configured automatically.")]
        public void EnsureDealSignal()
        {
            if (_loggedRemovedDealSignal)
                return;

            _loggedRemovedDealSignal = true;
            UnityEngine.Debug.LogWarning(
                "[S1API] EnsureDealSignal is obsolete in Schedule I 0.4.6. " +
                "Customer deal attendance is configured automatically; this call is now a compatibility no-op.");
        }


        /// <summary>
        /// Removes all actions under the schedule manager with optional filtering by action type.
        /// </summary>
        /// <param name="includeSignals">Whether to remove signal-type actions (e.g., WalkTo, DriveToCarPark). Default is <c>true</c>.</param>
        /// <param name="includeEvents">Whether to remove event-type actions (e.g., StayInBuilding, LocationDialogue). Default is <c>true</c>.</param>
        /// <remarks>
        /// This method removes all schedule actions from the NPC's schedule manager. Actions are
        /// disabled and reset instead of being destroyed to maintain FishNet network component
        /// indices and avoid network synchronization issues.
        /// 
        /// After clearing actions, the schedule manager is re-initialized to update the
        /// action order and timing.
        /// 
        /// Use with caution as this will completely reset the NPC's scheduled behavior.
        /// </remarks>
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
                    if (a != null && a.gameObject != null)
                    {
                        // Disable and reset timing instead of destroying to keep FishNet indices stable
                        a.gameObject.SetActive(false);
                        a.enabled = false;
                        try { a.SetStartTime(0); } catch { }
                    }
                }
            }
            Manager.InitializeActions();
        }

        /// <summary>
        /// Returns the names of all currently configured actions, including inactive and disabled ones.
        /// </summary>
        /// <returns>A read-only list of action names. Returns an empty list if no schedule manager exists.</returns>
        /// <remarks>
        /// This method retrieves the names of all schedule actions currently configured on the NPC,
        /// regardless of their active state. This can be useful for debugging or monitoring
        /// the NPC's schedule configuration.
        /// 
        /// The returned list includes both signal-type and event-type actions.
        /// </remarks>
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
        /// INTERNAL: Ensures a schedule manager exists on the NPC root.
        /// </summary>
        internal void EnsureManager()
        {
            var mgr = NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (mgr == null)
                UnityEngine.Debug.LogWarning("[S1API] NPCScheduleManager is missing. Ensure it is added in NPC.ConfigurePrefab.");
        }

        /// <summary>
        /// INTERNAL: Direct access to the underlying manager.
        /// </summary>
        internal S1NPCs.NPCScheduleManager? Manager => NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);

        /// <summary>
        /// INTERNAL: The owning NPC instance.
        /// </summary>
        internal NPC Owner => NPC;
    }
}


