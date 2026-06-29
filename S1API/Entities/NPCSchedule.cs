#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1GameTime = Il2CppScheduleOne.GameTime;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Economy = Il2CppScheduleOne.Economy;
using Il2CppFishNet;
using Il2CppFishNet.Object;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1GameTime = ScheduleOne.GameTime;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Economy = ScheduleOne.Economy;
using FishNet;
using FishNet.Object;
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
    /// Schedules are defined in <see cref="NPC.ConfigurePrefab"/> using <see cref="NPCPrefabBuilder.WithSchedule"/> and managed at runtime via this wrapper.
    /// </remarks>
    public sealed class NPCSchedule
    {
        internal readonly NPC NPC;
        private static bool _loggedDealSignalTypeMissing;

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
        internal T AddActionInternal<T>(int startTime, string name = null) where T : S1NPCsSchedules.NPCAction
        {
            EnsureManager();
            if (Manager == null)
                return null;

            // Prefer a pre-created, inactive action instance of this type to avoid changing component indices
            var pool = Manager.GetComponentsInChildren<T>(true);
            T chosen = null;
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
        /// Ensures that a deal-wait signal exists under the schedule manager for customer handover functionality.
        /// </summary>
        /// <remarks>
        /// This method ensures that a <see cref="S1NPCsSchedules.NPCSignal_WaitForDelivery"/> component
        /// exists on the NPC's schedule manager. This signal is required for customer NPCs to
        /// properly handle deal interactions and handovers with the player.
        /// 
        /// If the signal already exists, it will be properly initialized and wired to the
        /// customer component. If it doesn't exist, a warning will be logged indicating that
        /// it should be added via <see cref="NPC.ConfigurePrefab"/>.
        /// 
        /// The deal signal allows the NPC to wait for deliveries and toggle customer handover states.
        /// </remarks>
        public void EnsureDealSignal()
        {
            var manager = Manager;
            if (manager == null)
            {
                EnsureManager();
                manager = Manager;
                if (manager == null)
                    return;
            }

            var existing = FindDealSignal(manager);
            if (existing != null)
            {
                TryNetworkInitialize(existing);
                // Also reflect into Customer so base game logic can reference it consistently
                TryWireCustomerDealSignal(existing);
                TryAssignDealSignalField(existing);
                return;
            }
            // Do not create new network behaviours at runtime; require prefab declaration
            UnityEngine.Debug.LogWarning("[S1API] DealSignal missing on prefab. Please add via NPC.ConfigurePrefab(builder.EnsureDealSignal()).");
        }

        private static Type? GetDealSignalType()
        {
            return ReflectionUtils.GetTypeByName("ScheduleOne.NPCs.Schedules.NPCSignal_WaitForDelivery")
                   ?? ReflectionUtils.GetTypeByName("Il2CppScheduleOne.NPCs.Schedules.NPCSignal_WaitForDelivery");
        }

        private Component? FindDealSignal(S1NPCs.NPCScheduleManager manager)
        {
            var dealSignalType = GetDealSignalType();
            if (dealSignalType == null)
            {
                if (!_loggedDealSignalTypeMissing)
                {
                    _loggedDealSignalTypeMissing = true;
                    UnityEngine.Debug.LogWarning("[S1API] DealSignal type is not available in this game build. Customer deal signal wiring was skipped.");
                }

                return null;
            }

            var components = manager.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && dealSignalType.IsInstanceOfType(component))
                    return component;
            }

            return null;
        }

        private void TryWireCustomerDealSignal(object signal)
        {
            try
            {
                var customer = NPC.gameObject.GetComponent<S1Economy.Customer>();
                if (customer == null) return;
                ReflectionUtils.TrySetFieldOrProperty(customer, "DealSignal", signal);
            }
            catch { /* ignore */ }
        }

        private void TryAssignDealSignalField(object signal)
        {
            try
            {
                var customer = NPC.gameObject.GetComponent<S1Economy.Customer>();
                if (customer == null || signal == null)
                    return;

                ReflectionUtils.TrySetFieldOrProperty(customer, "DealSignal", signal);
            }
            catch { /* ignore */ }
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
        internal S1NPCs.NPCScheduleManager Manager => NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);

        /// <summary>
        /// INTERNAL: The owning NPC instance.
        /// </summary>
        internal NPC Owner => NPC;

        /// <summary>
        /// INTERNAL: Warm FishNet caches on a dynamically added NetworkBehaviour.
        /// </summary>
        private void TryNetworkInitialize(object behaviour)
        {
            if (behaviour == null)
                return;
            try
            {
                var networkObject = NPC.gameObject.GetComponent<NetworkObject>();
                var transportManager = InstanceFinder.TransportManager;
                if (networkObject == null || transportManager == null)
                    return;

                SetNonPublicInstanceField(behaviour, "_networkObjectCache", networkObject);
                SetNonPublicInstanceField(behaviour, "_transportManagerCache", transportManager);
            }
            catch { }
        }

        private static void SetNonPublicInstanceField(object target, string fieldName, object value)
        {
            try
            {
                if (target == null || string.IsNullOrEmpty(fieldName)) return;
                var type = target.GetType();
                System.Reflection.FieldInfo field = null;
                while (type != null && field == null)
                {
                    field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    type = type.BaseType;
                }
                field?.SetValue(target, value);
            }
            catch { }
        }
    }
}


