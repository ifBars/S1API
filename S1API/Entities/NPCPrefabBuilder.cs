#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Economy = ScheduleOne.Economy;
#endif

using System;
using System.Reflection;
using UnityEngine;
using S1API.Entities.Schedule;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using System.Collections.Generic;

namespace S1API.Entities
{
    /// <summary>
    /// Builder for composing a per-NPC prefab prior to network spawn.
    /// Use to predeclare networked components (Customer, ScheduleManager, Actions, etc.).
    /// </summary>
    public sealed class NPCPrefabBuilder
    {
        private readonly GameObject prefabRoot;
        private readonly Type ownerType;

        internal NPCPrefabBuilder(GameObject prefabRoot, Type ownerType)
        {
            this.prefabRoot = prefabRoot;
            this.ownerType = ownerType;
        }

        /// <summary>
        /// Ensures an <see cref="S1NPCs.NPCScheduleManager"/> exists under the prefab root.
        /// Returns the manager instance for further configuration.
        /// </summary>
        private S1NPCs.NPCScheduleManager EnsureScheduleManager()
        {
            var mgr = prefabRoot.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (mgr == null)
            {
                var go = new GameObject("NPCSchedule");
                go.transform.SetParent(prefabRoot.transform, false);
                mgr = go.AddComponent<S1NPCs.NPCScheduleManager>();
            }
            return mgr;
        }

        /// <summary>
        /// Ensures a Customer component exists on the prefab.
        /// </summary>
        public NPCPrefabBuilder EnsureCustomer()
        {
            var customer = prefabRoot.GetComponent<S1Economy.Customer>();
            if (customer == null)
            {
                customer = prefabRoot.AddComponent<S1Economy.Customer>();
                customer.enabled = true;
            }
            // Mark this NPC type as a Customer-bearing type so pre-registration adds Customer on template
            NPC.RegisterCustomerType(ownerType);
            return this;
        }

        /// <summary>
        /// Plan and predeclare schedule actions on the prefab using the API schedule builder.
        /// The plan is applied at runtime to activate and configure precreated actions.
        /// Mirrors the clean style of other builder APIs and guards against exceptions.
        /// </summary>
        public NPCPrefabBuilder WithSchedule(Action<PrefabScheduleBuilder> configure)
        {
            if (configure == null)
                return this;

            try
            {
                var planner = new PrefabScheduleBuilder();
                configure(planner);
                var specs = planner.Build();
                return WithSchedule(specs);
            }
            catch
            {
                return this;
            }
        }

        /// <summary>
        /// Declares a schedule using a prebuilt set of specs.
        /// Use this when composing plans externally or sharing between NPC types.
        /// </summary>
        public NPCPrefabBuilder WithSchedule(IEnumerable<IScheduleActionSpec> specs)
        {
            if (specs == null)
                return this;

            try
            {
                // Materialize once to avoid multiple enumeration and allow counting for precreation
                var list = specs as List<IScheduleActionSpec> ?? new List<IScheduleActionSpec>(specs);
                if (list.Count == 0)
                    return this;

                NPC.RegisterSchedulePlanForType(ownerType, list);
                // Pre-create actions based on the plan to keep FishNet indices stable
                PrecreateActionsForSpecs(list);
            }
            catch { }

            return this;
        }

        /// <summary>
        /// Declares a schedule using a params array of specs for convenience.
        /// </summary>
        public NPCPrefabBuilder WithSchedule(params IScheduleActionSpec[] specs)
        {
            if (specs == null || specs.Length == 0)
                return this;
            return WithSchedule((IEnumerable<IScheduleActionSpec>)specs);
        }

        /// <summary>
        /// Declares default CustomerData for this NPC type. Ensures a Customer component exists
        /// on the prefab and assigns the composed data as its starting configuration.
        /// Save/load will override these values when present.
        /// </summary>
        public NPCPrefabBuilder WithCustomerDefaults(Action<CustomerDataBuilder> configure)
        {
            if (configure == null)
                return this;

            EnsureCustomer();
            var customer = prefabRoot.GetComponent<S1Economy.Customer>();
            if (customer != null)
            {
                try
                {
                    var builder = new CustomerDataBuilder();
                    configure(builder);
                    var data = builder.BuildInternal();
#if MONOMELON
                    var field = typeof(S1Economy.Customer).GetField("customerData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    field?.SetValue(customer, data);
#else
                    customer.customerData = data;
#endif
                }
                catch { }
            }

            NPC.RegisterCustomerDefaultsForType(ownerType, configure);
            return this;
        }

        /// <summary>
        /// Declares default relationship settings (delta, unlock type, connections) for this NPC type.
        /// Applied to the instance after spawn and before save-data hydration.
        /// </summary>
        public NPCPrefabBuilder WithRelationshipDefaults(Action<NPCRelationshipDataBuilder> configure)
        {
            if (configure != null)
                NPC.RegisterRelationshipDefaultsForType(ownerType, configure);
            return this;
        }

        /// <summary>
        /// Sets the spawn position and rotation for this NPC type.
        /// Applied every time the NPC is spawned (both new games and loaded games).
        /// </summary>
        public NPCPrefabBuilder WithSpawnPosition(Vector3 position, Quaternion rotation)
        {
            NPC.RegisterSpawnPositionForType(ownerType, position, rotation);
            return this;
        }

        /// <summary>
        /// Sets the spawn position for this NPC type with default rotation.
        /// Applied every time the NPC is spawned (both new games and loaded games).
        /// </summary>
        public NPCPrefabBuilder WithSpawnPosition(Vector3 position)
        {
            return WithSpawnPosition(position, Quaternion.identity);
        }

        private void PrecreateActionsForSpecs(List<IScheduleActionSpec> specs)
        {
            if (specs == null || specs.Count == 0)
                return;

            var mgr = EnsureScheduleManager();

            int walkTo = 0, stayInBuilding = 0, locationDialogue = 0, useVending = 0, driveToCarPark = 0, dealSignal = 0, useATM = 0, handleDeal = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                var s = specs[i];
                if (s is WalkToSpec) walkTo++;
                else if (s is StayInBuildingSpec) stayInBuilding++;
                else if (s is LocationDialogueSpec) locationDialogue++;
                else if (s is UseVendingMachineSpec) useVending++;
                else if (s is DriveToCarParkSpec) driveToCarPark++;
                else if (s is EnsureDealSignalSpec) dealSignal = Math.Max(dealSignal, 1);
                else if (s is UseATMSpec) useATM++;
                else if (s is HandleDealSpec) handleDeal++;
            }

            if (dealSignal > 0)
                EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WaitForDelivery>(count: 1, namePrefix: "DealSignal");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WalkToLocation>(walkTo, "WalkTo");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_StayInBuilding>(stayInBuilding, "StayInBuilding");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_LocationDialogue>(locationDialogue, "LocationDialogue");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseVendingMachine>(useVending, "UseVending");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_DriveToCarPark>(driveToCarPark, "DriveToCarPark");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseATM>(useATM, "UseATM");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_HandleDeal>(handleDeal, "HandleDeal");
        }

        private void EnsurePrefabAction<T>(int count, string namePrefix) where T : S1NPCsSchedules.NPCAction
        {
            if (count <= 0)
                return;
            var mgr = EnsureScheduleManager();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject(string.IsNullOrEmpty(namePrefix) ? typeof(T).Name : ($"{namePrefix}_{i + 1}"));
                go.transform.SetParent(mgr.transform, false);
                var comp = go.AddComponent<T>();
                try
                {
                    var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                    var npcField = typeof(T).GetField("npc", BindingFlags.NonPublic | BindingFlags.Instance);
                    npcField?.SetValue(comp, baseNpc);
                    var schedField = typeof(T).GetField("schedule", BindingFlags.NonPublic | BindingFlags.Instance);
                    schedField?.SetValue(comp, mgr);
                }
                catch { }
                go.SetActive(false);
                comp.enabled = false;
            }
        }
    }
}