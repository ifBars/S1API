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
using UnityEngine;
using S1API.Entities.Schedule;

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
        /// Initializes the order/names of the schedules on this NPC.
        /// This method is responsible for adding times to the schedule names.
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
            TryNetworkInitialize(action);

            // Let the manager pick up and sort the new action immediately
            Manager.InitializeActions();
            Manager.EnforceState();
            return action;
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
                TryNetworkInitialize(existing);
                // Also reflect into Customer so base game logic can reference it consistently
                TryWireCustomerDealSignal(existing);
                return;
            }

            var dealGo = new GameObject("DealSignal");
            dealGo.transform.SetParent(Manager.transform, false);
            var signal = dealGo.AddComponent<S1NPCsSchedules.NPCSignal_WaitForDelivery>();
            TryNetworkInitialize(signal);
            dealGo.SetActive(false);

            // Wire to Customer component if present
            TryWireCustomerDealSignal(signal);

            Manager.InitializeActions();
        }

        private void TryWireCustomerDealSignal(S1NPCsSchedules.NPCSignal_WaitForDelivery signal)
        {
            try
            {
                var customer = NPC.gameObject.GetComponent<S1Economy.Customer>();
                if (customer == null) return;
                var field = typeof(S1Economy.Customer).GetField("DealSignal", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                field?.SetValue(customer, signal);
            }
            catch { /* ignore */ }
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


