#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
using S1Player = Il2CppScheduleOne.PlayerScripts.Player;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1Economy = ScheduleOne.Economy;
using S1Player = ScheduleOne.PlayerScripts.Player;
using S1NPCsActions = ScheduleOne.NPCs.Actions;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
#endif

using System;
using System.Reflection;
using UnityEngine;
using FishNet.Object;
#if (IL2CPPMELON)
using Il2CppFishNet;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Managing.Object;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Object;
#endif

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing customer wrapper for an <see cref="NPC"/>.
    /// Provides helpers to configure and interact with the base game's Customer behaviour.
    /// </summary>
    public sealed class NPCCustomer
    {
        internal readonly NPC NPC;

        internal NPCCustomer(NPC npc)
        {
            NPC = npc;
        }

        /// <summary>
        /// Returns whether this NPC currently has a Customer component.
        /// </summary>
        public bool IsCustomer => Component != null;

        /// <summary>
        /// Ensures this NPC has a Customer component, creating one if absent.
        /// </summary>
        public void EnsureCustomer()
        {
            if (Component == null)
            {
                var c = NPC.gameObject.AddComponent<S1Economy.Customer>();
                // Wire critical references before initialization
                WireCoreReferences(c);
                c.enabled = true;
                c.InitializeSaveable();
                TryNetworkInitialize(c);
                EnsureScheduleArtifacts();
            }
            else
            {
                WireCoreReferences(Component);
                TryNetworkInitialize(Component);
                EnsureScheduleArtifacts();
            }
        }

        /// <summary>
        /// Assigns Customer settings from a Resources path to a CustomerData asset.
        /// Returns true on success.
        /// </summary>
        public bool SetCustomerDataByResource(string resourcePath)
        {
            EnsureCustomer();
            if (Component == null || string.IsNullOrEmpty(resourcePath))
                return false;
            var data = UnityEngine.Resources.Load<S1Economy.CustomerData>(resourcePath);
            if (data == null)
                return false;
            customerDataField?.SetValue(Component, data);
            return true;
        }

        /// <summary>
        /// Builds a CustomerData object from code and applies it to this NPC's customer component.
        /// </summary>
        public void BuildAndSetCustomerData(Action<CustomerDataBuilder> configure)
        {
            if (configure == null)
                return;
            EnsureCustomer();
            if (Component == null)
                return;
            var builder = new CustomerDataBuilder();
            configure(builder);
            var data = builder.BuildInternal();
            customerDataField?.SetValue(Component, data);
        }

        /// <summary>
        /// Marks this customer as unlocked (visible to the player systems).
        /// </summary>
        public void Unlock()
        {
            EnsureCustomer();
            if (Component == null)
                return;

            var relation = NPC.S1NPC.RelationData;
            if (relation != null && !relation.Unlocked)
            {
                relation.Unlock(S1NPCs.Relation.NPCRelationData.EUnlockType.DirectApproach, false);
            }
        }

        /// <summary>
        /// Forces a deal offer attempt (based on the base game's heuristics).
        /// </summary>
        public void ForceDealOffer()
        {
            if (Component == null)
                return;
            Component.ForceDealOffer();
        }

        /// <summary>
        /// Requests a product from the specified player (or local player if null).
        /// </summary>
        public void RequestProduct(Player player = null)
        {
            if (Component == null)
                return;
            if (player == null)
            {
                Component.RequestProduct(S1Player.Local);
            }
            else
            {
                Component.RequestProduct(player.S1Player);
            }
        }

        /// <summary>
        /// Sets whether the customer is awaiting delivery.
        /// </summary>
        public void SetAwaitingDelivery(bool awaiting)
        {
            if (Component == null)
                return;
            Component.SetIsAwaitingDelivery(awaiting);
        }

        /// <summary>
        /// INTERNAL: Direct access to underlying customer component.
        /// </summary>
        internal S1Economy.Customer Component => NPC.gameObject.GetComponent<S1Economy.Customer>();

        /// <summary>
        /// INTERNAL: Ensures the newly added Customer component is initialized with FishNet.
        /// Safe to call before or after the NPC NetworkObject is spawned.
        /// </summary>
        private void TryNetworkInitialize(S1Economy.Customer customer)
        {
            if (customer == null)
                return;
            try
            {
                // Initialize the NetworkBehaviour lifecycle if it hasn't been already.
                customer.NetworkInitializeIfDisabled();

                // If the NPC is already spawned, make sure late-added behaviours are ready on server/clients.
                var no = NPC.gameObject.GetComponent<NetworkObject>();
                if (no != null && no.IsSpawned)
                {
                    // Nothing extra is required for late-added NetworkBehaviours in FishNet
                    // beyond NetworkInitializeIfDisabled. Keep here for future compatibility.
                }
            }
            catch (Exception)
            {
                // no-op: avoid crashing mods if FishNet state isn't ready yet
            }
        }

        /// <summary>
        /// INTERNAL: Wires basic references Customer expects when added at runtime.
        /// </summary>
        private void WireCoreReferences(S1Economy.Customer customer)
        {
            try
            {
                // Set protected NPC property via reflection so Customer has a back-reference immediately
                var npcProp = typeof(S1Economy.Customer).GetProperty("NPC", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var setMethod = npcProp?.GetSetMethod(true);
                setMethod?.Invoke(customer, new object[] { NPC.S1NPC });
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// INTERNAL: Ensures schedule manager and a DealSignal action exist so Customer can function.
        /// </summary>
        private void EnsureScheduleArtifacts()
        {
            try
            {
                var schedule = NPC.gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
                if (schedule == null)
                {
                    var go = new GameObject("NPCSchedule");
                    go.transform.SetParent(NPC.gameObject.transform, false);
                    schedule = go.AddComponent<S1NPCs.NPCScheduleManager>();
                }

                var existing = schedule.GetComponentInChildren<S1NPCsSchedules.NPCSignal_WaitForDelivery>(true);
                if (existing == null)
                {
                    var dealGo = new GameObject("DealSignal");
                    dealGo.transform.SetParent(schedule.transform, false);
                    existing = dealGo.AddComponent<S1NPCsSchedules.NPCSignal_WaitForDelivery>();
                }
                // Keep disabled by default; Customer will toggle as needed
                if (existing != null && existing.gameObject != null)
                    existing.gameObject.SetActive(false);
            }
            catch (Exception)
            {
                // no-op; safe fallback if schedule system is not present
            }
        }

        private FieldInfo customerDataField = typeof(S1Economy.Customer).GetField("customerData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }
}


