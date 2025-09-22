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

namespace S1API.Entities
{
    /// <summary>
    /// Builder for composing a per-NPC prefab prior to network spawn.
    /// Use to predeclare networked components (Customer, ScheduleManager, Actions, etc.).
    /// </summary>
    public sealed class NPCPrefabBuilder
    {
        private readonly GameObject prefabRoot;
        private readonly System.Type ownerType;

        internal NPCPrefabBuilder(GameObject prefabRoot, System.Type ownerType)
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
            return this;
        }

        /// <summary>
        /// Plan and predeclare schedule actions on the prefab using the API schedule builder.
        /// The plan is applied at runtime to activate and configure precreated actions.
        /// </summary>
        public NPCPrefabBuilder WithSchedule(Action<PrefabScheduleBuilder> configure)
        {
            if (configure == null)
                return this;

            var planner = new PrefabScheduleBuilder();
            configure(planner);

            var specs = planner.Build();
            NPC.RegisterSchedulePlanForType(ownerType, specs);

            // Pre-create actions based on the plan to keep FishNet indices stable
            PrecreateActionsForSpecs(specs);
            return this;
        }

        private void PrecreateActionsForSpecs(System.Collections.Generic.List<IScheduleActionSpec> specs)
        {
            if (specs == null || specs.Count == 0)
                return;

            var mgr = EnsureScheduleManager();

            int walkTo = 0, stayInBuilding = 0, locationDialogue = 0, useVending = 0, driveToCarPark = 0, dealSignal = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                var s = specs[i];
                if (s is WalkToSpec) walkTo++;
                else if (s is StayInBuildingSpec) stayInBuilding++;
                else if (s is LocationDialogueSpec) locationDialogue++;
                else if (s is UseVendingMachineSpec) useVending++;
                else if (s is DriveToCarParkSpec) driveToCarPark++;
                else if (s is EnsureDealSignalSpec) dealSignal = Math.Max(dealSignal, 1);
            }

            if (dealSignal > 0)
                EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WaitForDelivery>(count: 1, namePrefix: "DealSignal");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_WalkToLocation>(walkTo, "WalkTo");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_StayInBuilding>(stayInBuilding, "StayInBuilding");
            EnsurePrefabAction<S1NPCsSchedules.NPCEvent_LocationDialogue>(locationDialogue, "LocationDialogue");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_UseVendingMachine>(useVending, "UseVending");
            EnsurePrefabAction<S1NPCsSchedules.NPCSignal_DriveToCarPark>(driveToCarPark, "DriveToCarPark");
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


