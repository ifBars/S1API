#if (IL2CPPMELON)
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1Equipping = Il2CppScheduleOne.Equipping.Framework;
using S1Interaction = Il2CppScheduleOne.Interaction;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1Noise = Il2CppScheduleOne.Noise;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Relation = Il2CppScheduleOne.NPCs.Relation;
using S1Responses = Il2CppScheduleOne.NPCs.Responses;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1ContactApps = Il2CppScheduleOne.UI.Phone.ContactsApp;
using S1WorkspacePopup = Il2CppScheduleOne.UI.WorldspacePopup;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1Behaviour = Il2CppScheduleOne.NPCs.Behaviour;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1Vision = Il2CppScheduleOne.Vision;
using S1VoiceOver = Il2CppScheduleOne.VoiceOver;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Employees = Il2CppScheduleOne.Employees;
using S1Combat = Il2CppScheduleOne.Combat;
using S1Tools = Il2CppScheduleOne.Tools;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1MapBase = Il2CppScheduleOne.Map;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Money = Il2CppScheduleOne.Money;
using ConversationCategoryList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Messaging.EConversationCategory>;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
using S1Dialogue = ScheduleOne.Dialogue;
using S1Equipping = ScheduleOne.Equipping.Framework;
using S1Interaction = ScheduleOne.Interaction;
using S1Messaging = ScheduleOne.Messaging;
using S1Noise = ScheduleOne.Noise;
using S1Economy = ScheduleOne.Economy;
using S1Relation = ScheduleOne.NPCs.Relation;
using S1Responses = ScheduleOne.NPCs.Responses;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1ContactApps = ScheduleOne.UI.Phone.ContactsApp;
using S1WorkspacePopup = ScheduleOne.UI.WorldspacePopup;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1Behaviour = ScheduleOne.NPCs.Behaviour;
using S1Vehicles = ScheduleOne.Vehicles;
using S1Vision = ScheduleOne.Vision;
using S1VoiceOver = ScheduleOne.VoiceOver;
using S1NPCs = ScheduleOne.NPCs;
using S1Employees = ScheduleOne.Employees;
using S1Combat = ScheduleOne.Combat;
using S1Tools = ScheduleOne.Tools;
using S1Items = ScheduleOne.ItemFramework;
using S1MapBase = ScheduleOne.Map;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Registry = ScheduleOne.Registry;
using S1Money = ScheduleOne.Money;
using ConversationCategoryList = System.Collections.Generic.List<ScheduleOne.Messaging.EConversationCategory>;
#endif

#if IL2CPPMELON
using S1Type = Il2CppSystem.Type;
using Il2CppInterop.Runtime;
#else
using S1Type = System.Type;
#endif

#if IL2CPPMELON
using Il2CppSystem.Collections.Generic;
#else
using System.Collections.Generic;
#endif

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using HarmonyLib;
#if (IL2CPPMELON)
using Il2CppFishNet;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
#elif MONOMELON
using FishNet;
using FishNet.Managing.Object;
using FishNet.Object;
#endif
using MelonLoader;
using S1API.Entities.Actions;
using S1API.Entities.Behaviour;
using S1API.Entities.Interfaces;
using S1API.Entities.Schedule;
using S1API.Entities.Customer;
using S1API.Entities.Dealer;
using S1API.Entities.Supplier;
using S1API.Entities.Relation;
using S1API.Internal;
using S1API.Internal.Abstraction;
using S1API.Internal.Entities;
using S1API.Internal.Utils;
using S1API.Map;
using S1API.Messaging;
using S1API.Logging;
using S1API.Vehicles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace S1API.Entities
{
    /// <summary>
    /// Abstract base class for creating custom NPCs with modular architecture supporting both physical and non-physical NPCs.
    /// Physical NPCs are visible in the game world with 3D models, movement, and direct interaction.
    /// Non-physical NPCs are invisible contacts primarily used for messaging and phone interactions.
    /// </summary>
    /// <remarks>
    /// NPCs provide access to component systems: <see cref="Appearance"/>, <see cref="Dialogue"/>, <see cref="Schedule"/>,
    /// <see cref="Customer"/>, <see cref="Relationship"/>, <see cref="Inventory"/>, and <see cref="Movement"/>.
    /// Customer, relationship, and schedule configuration must be done in <see cref="ConfigurePrefab"/> for proper save/load behavior.
    /// </remarks>
    public abstract class NPC : Saveable, IEntity, IHealth
    {
        private static readonly Log Logger = new Log("NPC");
        // Protected members intended to be used by modders.
        // Intended to be used from within the class / derived classes ONLY.
        private static readonly System.Collections.Generic.Dictionary<System.Type, GameObject> TypeToPrefab = new System.Collections.Generic.Dictionary<System.Type, GameObject>();
        private static readonly object TemplateLoadLock = new object();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.List<IScheduleActionSpec>> TypeToSchedulePlan = new System.Collections.Generic.Dictionary<System.Type, System.Collections.Generic.List<IScheduleActionSpec>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<CustomerDataBuilder>> TypeToCustomerDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<CustomerDataBuilder>>();
        internal static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<NPCRelationshipDataBuilder>> TypeToRelationshipDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<NPCRelationshipDataBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<RandomInventoryItemsBuilder>> TypeToRandomInventoryDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<RandomInventoryItemsBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<DealerDataBuilder>> TypeToDealerDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<DealerDataBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, DealerDataBuilder.DealerConfigData> TypeToBuiltDealerDefaults = new System.Collections.Generic.Dictionary<System.Type, DealerDataBuilder.DealerConfigData>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<SupplierDataBuilder>> TypeToSupplierDefaults = new System.Collections.Generic.Dictionary<System.Type, System.Action<SupplierDataBuilder>>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, SupplierDataBuilder.SupplierConfigData> TypeToBuiltSupplierDefaults = new System.Collections.Generic.Dictionary<System.Type, SupplierDataBuilder.SupplierConfigData>();
        internal static readonly System.Collections.Generic.HashSet<System.Type>
            FinalizedCustomNpcTypes =
                new System.Collections.Generic.HashSet<System.Type>();
        private static readonly System.Collections.Generic.Dictionary<System.Type, (Vector3 position, Quaternion rotation)> TypeToSpawnPosition = new System.Collections.Generic.Dictionary<System.Type, (Vector3, Quaternion)>();
        private static readonly System.Collections.Generic.HashSet<System.Type> CustomerTypes = new System.Collections.Generic.HashSet<System.Type>();
        private static readonly System.Collections.Generic.HashSet<System.Type> DealerTypes = new System.Collections.Generic.HashSet<System.Type>();
        private static readonly System.Collections.Generic.HashSet<System.Type> SupplierTypes = new System.Collections.Generic.HashSet<System.Type>();
        private const string DealerPrefabName = "Dealer";
        private const string CivilianNpcPrefabName = "CivilianNPC";
        private const string BaseNpcPrefabName = "BaseNPC";
        private const string BaseEmployeePrefabName = "BaseEmployee";
        private const string PropertyInteriorNavMeshAreaName = "PropertyInterior";
        private const string LadderNavMeshAreaName = "Ladder";
        private static readonly bool LogBetaNpcPrefabDiagnostics = false;
        private static readonly string[] BaseNpcMembersToCopy =
        {
            "NPCData",
            "Scale",
            "Region",
            "BakedGUID",
            "GUID",
            "AggressionController",
            "Movement",
            "DialogueHandler",
            "Avatar",
            "Awareness",
            "Responses",
            "Actions",
            "Behaviour",
            "Inventory",
            "VoiceOverEmitter",
            "Health",
            "Visibility",
            "CurrentVehicle",
            "RelationData",
            "MSGConversation"
        };
        private static readonly string[] ChildNpcReferenceMemberNames =
        {
            "Npc",
            "NPC",
            "npc",
            "nPC",
            "baseNpc",
            "BaseNpc",
            "baseNPC",
            "BaseNPC"
        };
        private static volatile bool _prefabsConfiguredForLocalProcess;
        private static bool _loggedBaseEmployeeNormalization;
        private static bool _loggedExternalPreRegisterAllCall;
        private static bool _loggedExternalPreRegisterTypeCall;
        private static readonly System.Collections.Generic.HashSet<string> WarnedUnsupportedDealerSettings =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        private static int _clientNetworkSpawnHydrationDepth;
#if MONOMELON
        private static readonly FieldInfo BehaviourOwnerField =
            AccessTools.Field(typeof(S1Behaviour.Behaviour), "<beh>k__BackingField")
            ?? throw new MissingFieldException(typeof(S1Behaviour.Behaviour).FullName, "<beh>k__BackingField");
        private static readonly FieldInfo NpcBehaviourOwnerField =
            AccessTools.Field(typeof(S1Behaviour.NPCBehaviour), "<Npc>k__BackingField")
            ?? throw new MissingFieldException(typeof(S1Behaviour.NPCBehaviour).FullName, "<Npc>k__BackingField");
#endif
        internal static bool PrefabsConfiguredForLocalProcess => _prefabsConfiguredForLocalProcess;
        private S1AvatarFramework.Avatar? _runtimeAvatar;
        private bool _hasExplicitIcon;

        internal bool HasExplicitIcon => _hasExplicitIcon;
        
        #region Template Prefab Helpers

        private static GameObject InstantiateTemplateInstance(System.Type npcType, NPC owner)
        {
            GameObject prefab = GetOrCreatePerNpcPrefab(npcType, owner);
            NetworkObject netPrefab = prefab.GetComponent<NetworkObject>() ?? prefab.AddComponent<NetworkObject>();

            NetworkObject? spawnableNetPrefab = null;
            try
            {
                var nm = InstanceFinder.NetworkManager;
                if (nm != null)
                {
                    PrefabObjects spawnablePrefabs = nm.SpawnablePrefabs;
                    if (spawnablePrefabs != null)
                    {
                        int count = spawnablePrefabs.GetObjectCount();
                        for (int i = 0; i < count; i++)
                        {
                            NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                            if (obj != null && obj.gameObject != null && obj.gameObject.name == prefab.name)
                            {
                                spawnableNetPrefab = obj;
                                break;
                            }
                        }
                    }
                }
            }
            catch { /* no-op: fallback to local prefab */ }

            GameObject prefabToUse = spawnableNetPrefab?.gameObject ?? prefab;
            GameObject instance = UnityEngine.Object.Instantiate<GameObject>(prefabToUse);
            try
            {
                var nm = InstanceFinder.NetworkManager;
                bool isServer = nm != null && nm.IsServer;
                var existingNo = instance.GetComponent<NetworkObject>();
                if (isServer)
                {
                    if (existingNo == null)
                        existingNo = instance.AddComponent<NetworkObject>();
                }
                else
                {
                    if (existingNo != null)
                        UnityEngine.Object.Destroy(existingNo);
                }
            }
            catch { }
            if (S1NPCs.NPCManager.InstanceExists && S1NPCs.NPCManager.Instance.NPCContainer != null)
            {
                Transform parent = S1NPCs.NPCManager.Instance.NPCContainer;
                if (parent != null && parent.gameObject != null && parent.gameObject.activeInHierarchy)
                    instance.transform.SetParent(parent, false);
            }
            instance.name = prefab.name;
            S1NPCs.NPC? instanceNpc = GetPreferredNpcComponent(instance);
            if (instanceNpc != null)
                RepairNpcPrefabReferences(instance, instanceNpc);

            return instance;
        }

        private static NetworkObject? FindSpawnablePrefabByName(PrefabObjects spawnablePrefabs, int count, params string[] prefabNames)
        {
            for (int i = 0; i < count; i++)
            {
                NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                if (obj == null || obj.gameObject == null)
                    continue;

                for (int nameIndex = 0; nameIndex < prefabNames.Length; nameIndex++)
                {
                    if (obj.gameObject.name == prefabNames[nameIndex])
                        return obj;
                }
            }

            return null;
        }

        private static NetworkObject? FindSpawnablePrefabWithComponent<T>(PrefabObjects spawnablePrefabs, int count) where T : Component
        {
            for (int i = 0; i < count; i++)
            {
                NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                if (obj != null && obj.gameObject != null && obj.gameObject.GetComponent<T>() != null)
                    return obj;
            }

            return null;
        }

        private static NetworkObject? FindSpawnablePrefabWithPlainNpcComponent(PrefabObjects spawnablePrefabs, int count)
        {
            for (int i = 0; i < count; i++)
            {
                NetworkObject obj = spawnablePrefabs.GetObject(true, i);
                if (obj == null || obj.gameObject == null)
                    continue;

                if (IsGeneratedS1ApiPrefab(obj.gameObject))
                    continue;

                if (FindPlainNpcComponent(obj.gameObject) != null)
                    return obj;
            }

            return null;
        }

        private static bool IsGeneratedS1ApiPrefab(GameObject prefabRoot)
        {
            return prefabRoot != null
                   && !string.IsNullOrEmpty(prefabRoot.name)
                   && prefabRoot.name.StartsWith("S1API_", StringComparison.OrdinalIgnoreCase);
        }

        private static NetworkObject? ResolveNpcSpawnablePrefab(PrefabObjects spawnablePrefabs, int count, NpcRootRole rootRole)
        {
            if (rootRole == NpcRootRole.Dealer)
            {
                NetworkObject? dealerPrefab = FindSpawnablePrefabByName(spawnablePrefabs, count, DealerPrefabName)
                    ?? FindSpawnablePrefabWithComponent<S1Economy.Dealer>(spawnablePrefabs, count);

                if (dealerPrefab != null)
                    return dealerPrefab;
            }

            if (rootRole == NpcRootRole.Supplier)
            {
                NetworkObject? supplierPrefab =
                    FindSpawnablePrefabWithComponent<S1Economy.Supplier>(spawnablePrefabs, count);

                if (supplierPrefab != null)
                    return supplierPrefab;
            }

            return FindSpawnablePrefabByName(
                    spawnablePrefabs,
                    count,
                    CivilianNpcPrefabName,
                    BaseNpcPrefabName)
                ?? FindSpawnablePrefabWithPlainNpcComponent(spawnablePrefabs, count)
                ?? FindSpawnablePrefabByName(spawnablePrefabs, count, BaseEmployeePrefabName)
                ?? FindSpawnablePrefabWithComponent<S1NPCs.NPC>(spawnablePrefabs, count);
        }

        private static S1NPCs.NPC? FindPlainNpcComponent(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return null;

            try
            {
                var employeeInstanceIds = GetEmployeeComponentInstanceIds(prefabRoot);
                var components = prefabRoot.GetComponents<S1NPCs.NPC>();
                foreach (var component in components)
                {
                    if (IsPlainNpcComponent(component, employeeInstanceIds))
                        return component;
                }
            }
            catch
            {
            }

            return null;
        }

        private static S1NPCs.NPC? FindEmployeeNpcComponent(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return null;

            var employee = prefabRoot.GetComponent<S1Employees.Employee>();
            if (employee != null)
                return employee;

            try
            {
                var components = prefabRoot.GetComponents<S1NPCs.NPC>();
                foreach (var component in components)
                {
                    if (IsEmployeeNpcComponent(component))
                        return component;
                }
            }
            catch
            {
            }

            return null;
        }

        private static System.Collections.Generic.HashSet<int> GetEmployeeComponentInstanceIds(GameObject prefabRoot)
        {
            var ids = new System.Collections.Generic.HashSet<int>();
            if (prefabRoot == null)
                return ids;

            try
            {
                var employees = prefabRoot.GetComponents<S1Employees.Employee>();
                foreach (var employee in employees)
                {
                    if (employee != null)
                        ids.Add(employee.GetInstanceID());
                }
            }
            catch
            {
            }

            return ids;
        }

        private static bool IsPlainNpcComponent(S1NPCs.NPC? component)
        {
            return IsPlainNpcComponent(component, null);
        }

        private static bool IsPlainNpcComponent(S1NPCs.NPC? component, System.Collections.Generic.ISet<int>? employeeInstanceIds)
        {
            if (component == null)
                return false;

            if (employeeInstanceIds != null && employeeInstanceIds.Contains(component.GetInstanceID()))
                return false;

            return component.GetType().FullName == typeof(S1NPCs.NPC).FullName;
        }

        private static bool IsEmployeeNpcComponent(S1NPCs.NPC? component)
        {
            if (component == null)
                return false;

            if (component is S1Employees.Employee)
                return true;

            string? fullName = component.GetType().FullName;
            return fullName == "ScheduleOne.Employees.Employee"
                   || fullName == "Il2CppScheduleOne.Employees.Employee";
        }

        private static S1NPCs.NPC? GetPreferredNpcComponent(GameObject prefabRoot)
        {
            return prefabRoot.GetComponent<S1Economy.Supplier>()
                   ?? prefabRoot.GetComponent<S1Economy.Dealer>()
                   ?? FindPlainNpcComponent(prefabRoot)
                   ?? prefabRoot.GetComponent<S1NPCs.NPC>();
        }

        private static void NormalizeBaseEmployeePrefab(
            GameObject prefabRoot,
            string sourcePrefabName,
            NpcRootRole rootRole)
        {
            if (prefabRoot == null || sourcePrefabName != BaseEmployeePrefabName)
                return;

            S1Economy.Dealer? existingDealer = prefabRoot.GetComponent<S1Economy.Dealer>();
            S1NPCs.NPC? existingPlainNpc = FindPlainNpcComponent(prefabRoot);
            S1NPCs.NPC? sourceNpc = FindEmployeeNpcComponent(prefabRoot);
            if (sourceNpc == null && existingPlainNpc != null)
            {
                RepairNpcPrefabReferences(prefabRoot, existingPlainNpc);
                NormalizeBaseEmployeeNavigation(prefabRoot, rootRole);
                if (rootRole == NpcRootRole.Dealer)
                    EnsureDealerComponentOnPrefab(prefabRoot);
                else if (rootRole == NpcRootRole.Supplier)
                    EnsureSupplierComponentOnPrefab(prefabRoot);
                return;
            }

            if (sourceNpc == null && rootRole == NpcRootRole.Dealer && existingDealer != null)
            {
                S1NPCs.NPC coreNpc = existingPlainNpc ?? existingDealer;
                RewireChildNpcReferences(prefabRoot, coreNpc);
                RepairNpcPrefabReferences(prefabRoot, coreNpc);
                LogBaseEmployeeNormalization();
                return;
            }

            if (sourceNpc == null)
                sourceNpc = prefabRoot.GetComponent<S1NPCs.NPC>();

            if (sourceNpc == null)
                return;

            try
            {
                LogBaseEmployeeComponentState("before plain NPC AddComponent", prefabRoot);

                S1NPCs.NPC? replacementNpc = rootRole switch
                {
                    NpcRootRole.Dealer => prefabRoot.GetComponent<S1Economy.Dealer>(),
                    NpcRootRole.Supplier => prefabRoot.GetComponent<S1Economy.Supplier>(),
                    _ => existingPlainNpc
                };

                if (replacementNpc == null)
                {
                    Type replacementType = rootRole switch
                    {
                        NpcRootRole.Dealer => typeof(S1Economy.Dealer),
                        NpcRootRole.Supplier => typeof(S1Economy.Supplier),
                        _ => typeof(S1NPCs.NPC)
                    };
                    LogBetaNpcPrefabDiagnostic($"[S1API][BaseEmployeeFallback] Adding {replacementType.FullName} to cloned prefab '{prefabRoot.name}'. Source={DescribeComponent(sourceNpc)}");
                    replacementNpc = rootRole switch
                    {
                        NpcRootRole.Dealer => prefabRoot.AddComponent<S1Economy.Dealer>(),
                        NpcRootRole.Supplier => prefabRoot.AddComponent<S1Economy.Supplier>(),
                        _ => prefabRoot.AddComponent<S1NPCs.NPC>()
                    };
                    LogBetaNpcPrefabDiagnostic($"[S1API][BaseEmployeeFallback] AddComponent returned {DescribeComponent(replacementNpc)}. IsPlain={IsPlainNpcComponent(replacementNpc)}, IsEmployee={IsEmployeeNpcComponent(replacementNpc)}");
                    LogBetaNpcPrefabDiagnostic($"[S1API][BaseEmployeeFallback] Immediate FindPlainNpcComponent returned {DescribeComponent(FindPlainNpcComponent(prefabRoot))}");
                    LogBaseEmployeeComponentState("after plain NPC AddComponent", prefabRoot);
                }

                if (sourceNpc != replacementNpc)
                {
                    CopyBaseNpcState(sourceNpc, replacementNpc);
                    NPCDataAccess.AssignNewData(replacementNpc, rootRole, sourceNpc);
                    LogBetaNpcPrefabDiagnostic($"[S1API][BaseEmployeeFallback] Copied base NPC state from {DescribeComponent(sourceNpc)} to {DescribeComponent(replacementNpc)}.");
                    RemoveComponentImmediate(sourceNpc);
                    LogBetaNpcPrefabDiagnostic($"[S1API][BaseEmployeeFallback] Removed source NPC component {DescribeComponent(sourceNpc)}.");
                    LogBaseEmployeeComponentState("after source NPC removal", prefabRoot);
                }

                RemoveEmployeeComponentsFromBaseEmployeeFallback(prefabRoot);
                NormalizeBaseEmployeeDialogueComponents(prefabRoot, rootRole);
                LogBaseEmployeeComponentState("after employee cleanup", prefabRoot);
                RewireChildNpcReferences(prefabRoot, replacementNpc);
                RepairNpcPrefabReferences(prefabRoot, replacementNpc);
                NormalizeBaseEmployeeNavigation(prefabRoot, rootRole);
                LogBaseEmployeeNormalization();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to normalize {BaseEmployeePrefabName} source prefab; using inherited NPC component fallback: {ex.Message}");
            }
        }

        private static void LogBaseEmployeeNormalization()
        {
            if (_loggedBaseEmployeeNormalization)
                return;

            _loggedBaseEmployeeNormalization = true;
            Logger.Debug($"[S1API] Normalized {BaseEmployeePrefabName} source prefab for beta NPC fallback.");
        }

        private static void LogBetaNpcPrefabDiagnostic(string message)
        {
            if (LogBetaNpcPrefabDiagnostics)
                Logger.Msg(message);
        }

        private static void CopyBaseNpcState(S1NPCs.NPC sourceNpc, S1NPCs.NPC replacementNpc)
        {
            foreach (string memberName in BaseNpcMembersToCopy)
            {
                object? value = GetGameMember(sourceNpc, memberName);
                if (value != null)
                    SetGameMember(replacementNpc, memberName, value);
            }
        }

        private static void RewireChildNpcReferences(GameObject prefabRoot, S1NPCs.NPC replacementNpc)
        {
            Component[] components = prefabRoot.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null || component == replacementNpc)
                    continue;

                foreach (string memberName in ChildNpcReferenceMemberNames)
                {
                    SetGameMember(component, memberName, replacementNpc);
                }
            }
        }

        private static void RepairNpcPrefabReferences(GameObject prefabRoot, S1NPCs.NPC npc)
        {
            if (prefabRoot == null || npc == null)
                return;

            var movement = prefabRoot.GetComponent<S1NPCs.NPCMovement>()
                           ?? prefabRoot.GetComponentInChildren<S1NPCs.NPCMovement>(true);
            if (movement != null)
            {
                SetGameMember(npc, "Movement", movement);
                SetGameMember(movement, "npc", npc);

                movement.Agent = EnsureRootNavMeshAgent(prefabRoot);

                var speedController = prefabRoot.GetComponent<S1NPCs.NPCSpeedController>()
                                      ?? prefabRoot.GetComponentInChildren<S1NPCs.NPCSpeedController>(true);
                if (speedController != null)
                {
                    movement.SpeedController = speedController;
                    SetGameMember(speedController, "Movement", movement);
                }
            }
        }

        private static UnityEngine.AI.NavMeshAgent EnsureRootNavMeshAgent(GameObject prefabRoot)
        {
            return prefabRoot.GetComponent<UnityEngine.AI.NavMeshAgent>()
                   ?? prefabRoot.AddComponent<UnityEngine.AI.NavMeshAgent>();
        }

        private static void NormalizeBaseEmployeeNavigation(GameObject prefabRoot, NpcRootRole rootRole)
        {
            if (prefabRoot == null || rootRole != NpcRootRole.Plain)
                return;

            var movement = prefabRoot.GetComponent<S1NPCs.NPCMovement>()
                           ?? prefabRoot.GetComponentInChildren<S1NPCs.NPCMovement>(true);
            UnityEngine.AI.NavMeshAgent agent = EnsureRootNavMeshAgent(prefabRoot);
            int propertyInteriorArea = UnityEngine.AI.NavMesh.GetAreaFromName(PropertyInteriorNavMeshAreaName);
            int ladderArea = UnityEngine.AI.NavMesh.GetAreaFromName(LadderNavMeshAreaName);
            agent.areaMask = IncludeNavMeshArea(
                ExcludeNavMeshArea(agent.areaMask, propertyInteriorArea),
                ladderArea);
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.MedQualityObstacleAvoidance;

            if (movement != null)
            {
                movement.SetAgentType(S1NPCs.NPCMovement.EAgentType.Humanoid);
                movement.DefaultObstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
        }

        internal static int ExcludeNavMeshArea(int areaMask, int areaIndex) =>
            areaIndex is >= 0 and < 32
                ? areaMask & ~(1 << areaIndex)
                : areaMask;

        internal static int IncludeNavMeshArea(int areaMask, int areaIndex) =>
            areaIndex is >= 0 and < 32
                ? areaMask | (1 << areaIndex)
                : areaMask;

        private static S1Economy.Dealer? EnsureDealerComponentOnPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return null;

            var existingDealer = prefabRoot.GetComponent<S1Economy.Dealer>();
            if (existingDealer != null)
            {
                RepairDealerPrefabReferences(prefabRoot, existingDealer);
                RepairNpcPrefabReferences(prefabRoot, existingDealer);
                return existingDealer;
            }

            S1NPCs.NPC? sourceNpc = GetPreferredNpcComponent(prefabRoot);
            if (sourceNpc == null)
                return null;

            var dealer = prefabRoot.AddComponent<S1Economy.Dealer>();
            CopyBaseNpcState(sourceNpc, dealer);
            NPCDataAccess.AssignNewData(dealer, NpcRootRole.Dealer, sourceNpc);

            RewireChildNpcReferences(prefabRoot, dealer);
            RepairDealerPrefabReferences(prefabRoot, dealer);
            RepairNpcPrefabReferences(prefabRoot, dealer);
            return dealer;
        }

        private static S1Economy.Supplier? EnsureSupplierComponentOnPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return null;

            SupplierRuntimeCoordinator.EnsurePrefabInfrastructure(prefabRoot);

            var existingSupplier = prefabRoot.GetComponent<S1Economy.Supplier>();
            if (existingSupplier != null)
            {
                // Unity clones ScriptableObject references by identity. Always replace a donor supplier's
                // framework data so custom identity/listings never mutate a vanilla or sibling asset.
                NPCDataAccess.AssignNewData(existingSupplier, NpcRootRole.Supplier, existingSupplier);
                SupplierRuntimeCoordinator.BindPrefabInfrastructure(existingSupplier);
                RepairNpcPrefabReferences(prefabRoot, existingSupplier);
                return existingSupplier;
            }

            S1NPCs.NPC? sourceNpc = GetPreferredNpcComponent(prefabRoot);
            if (sourceNpc == null)
                return null;

            var supplier = prefabRoot.AddComponent<S1Economy.Supplier>();
            CopyBaseNpcState(sourceNpc, supplier);
            NPCDataAccess.AssignNewData(supplier, NpcRootRole.Supplier, sourceNpc);
            RewireChildNpcReferences(prefabRoot, supplier);
            SupplierRuntimeCoordinator.BindPrefabInfrastructure(supplier);
            RepairNpcPrefabReferences(prefabRoot, supplier);

            if (sourceNpc != supplier)
                RemoveComponentImmediate(sourceNpc);

            return supplier;
        }

        private static void RepairDealerPrefabReferences(GameObject prefabRoot, S1Economy.Dealer dealer)
        {
            dealer.HomeEvent ??= prefabRoot
                .GetComponentsInChildren<S1NPCsSchedules.NPCEvent_StayInBuilding>(true)
                .FirstOrDefault(action =>
                    NPCPrefabBuilder.IsDealerHomeEventName(action?.gameObject?.name));

            S1Dialogue.DialogueController_Dealer controller =
                prefabRoot.GetComponentInChildren<S1Dialogue.DialogueController_Dealer>(true);
            if (controller == null)
            {
                S1Dialogue.DialogueController source =
                    prefabRoot.GetComponentInChildren<S1Dialogue.DialogueController>(true);
                GameObject controllerObject = source != null
                    ? source.gameObject
                    : dealer.DialogueHandler.gameObject;

                controller = controllerObject.AddComponent<S1Dialogue.DialogueController_Dealer>();
                if (source != null)
                {
                    controller.IntObj = source.IntObj;
                    controller.GenericDialogue = source.GenericDialogue;
                    controller.DialogueEnabled = source.DialogueEnabled;
                    controller.UseDialogueBehaviour = source.UseDialogueBehaviour;
                    controller.Choices = source.Choices;
                    controller.GreetingOverrides = source.GreetingOverrides;
                    controller.OverrideContainer = source.OverrideContainer;
                    RemoveComponentImmediate(source);
                }
            }

            dealer.DialogueController = controller;
        }

        private static string DescribeComponent(Component? component)
        {
            if (component == null)
                return "<null>";

            try
            {
                var type = component.GetType();
                return $"{type.FullName ?? type.Name}#{component.GetInstanceID()}";
            }
            catch (Exception ex)
            {
                return $"<component describe failed: {ex.Message}>";
            }
        }

        private static void LogBaseEmployeeComponentState(string stage, GameObject prefabRoot)
        {
            if (!LogBetaNpcPrefabDiagnostics)
                return;

            if (prefabRoot == null)
            {
                Logger.Msg($"[S1API][BaseEmployeeFallback] {stage}: prefabRoot=<null>");
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.Append($"[S1API][BaseEmployeeFallback] {stage}: root='{prefabRoot.name}', activeSelf={prefabRoot.activeSelf}, activeInHierarchy={prefabRoot.activeInHierarchy}");

                var employeeInstanceIds = GetEmployeeComponentInstanceIds(prefabRoot);
                var npcComponents = prefabRoot.GetComponents<S1NPCs.NPC>();
                sb.Append($", npcComponents={npcComponents.Length}[");
                for (int i = 0; i < npcComponents.Length; i++)
                {
                    if (i > 0)
                        sb.Append("; ");

                    var npcComponent = npcComponents[i];
                    sb.Append(DescribeComponent(npcComponent));
                    sb.Append($", plain={IsPlainNpcComponent(npcComponent, employeeInstanceIds)}, employee={employeeInstanceIds.Contains(npcComponent.GetInstanceID()) || IsEmployeeNpcComponent(npcComponent)}, dealer={npcComponent is S1Economy.Dealer}");
                }
                sb.Append(']');

                var employeeComponents = prefabRoot.GetComponents<S1Employees.Employee>();
                sb.Append($", employeeComponents={employeeComponents.Length}[");
                for (int i = 0; i < employeeComponents.Length; i++)
                {
                    if (i > 0)
                        sb.Append("; ");
                    sb.Append(DescribeComponent(employeeComponents[i]));
                }
                sb.Append(']');

                var dealerComponents = prefabRoot.GetComponents<S1Economy.Dealer>();
                sb.Append($", dealerComponents={dealerComponents.Length}[");
                for (int i = 0; i < dealerComponents.Length; i++)
                {
                    if (i > 0)
                        sb.Append("; ");
                    sb.Append(DescribeComponent(dealerComponents[i]));
                }
                sb.Append(']');

                Logger.Msg(sb.ToString());
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API][BaseEmployeeFallback] Failed to log component state at '{stage}': {ex.Message}");
            }
        }

        private static void RemoveEmployeeComponentsFromBaseEmployeeFallback(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return;

            if (FindPlainNpcComponent(prefabRoot) == null)
                return;

            try
            {
                int removed = 0;
                var employees = prefabRoot.GetComponents<S1Employees.Employee>();
                foreach (var employee in employees)
                {
                    if (employee == null)
                        continue;

                    RemoveComponentImmediate(employee);
                    removed++;
                }

                var npcComponents = prefabRoot.GetComponents<S1NPCs.NPC>();
                foreach (var npcComponent in npcComponents)
                {
                    if (npcComponent == null || !IsEmployeeNpcComponent(npcComponent))
                        continue;

                    RemoveComponentImmediate(npcComponent);
                    removed++;
                }

                if (removed > 0)
                    Logger.Debug($"[S1API] Removed {removed} Employee component(s) from {BaseEmployeePrefabName} NPC fallback prefab.");
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to remove Employee component(s) from {BaseEmployeePrefabName} fallback prefab: {ex.Message}");
            }
        }

        private static void NormalizeBaseEmployeeDialogueComponents(
            GameObject prefabRoot,
            NpcRootRole rootRole)
        {
            if (prefabRoot == null || rootRole != NpcRootRole.Plain)
                return;

            try
            {
                var employeeControllers =
                    prefabRoot.GetComponentsInChildren<S1Dialogue.DialogueController_Employee>(true);
                foreach (S1Dialogue.DialogueController_Employee employeeController in employeeControllers)
                {
                    if (employeeController == null)
                        continue;

                    GameObject controllerObject = employeeController.gameObject;
                    var civilianController = controllerObject.GetComponent<S1Dialogue.DialogueController>();
                    if (civilianController == null || civilianController == employeeController)
                    {
                        civilianController = controllerObject.AddComponent<S1Dialogue.DialogueController>();
                        civilianController.IntObj = employeeController.IntObj;
                        civilianController.GenericDialogue = employeeController.GenericDialogue;
                        civilianController.DialogueEnabled = employeeController.DialogueEnabled;
                        civilianController.UseDialogueBehaviour = employeeController.UseDialogueBehaviour;
                        // Customer and other runtime components rebuild their own role-specific dialogue state.
                        civilianController.Choices = new List<S1Dialogue.DialogueController.DialogueChoice>();
                        civilianController.GreetingOverrides = new List<S1Dialogue.DialogueController.GreetingOverride>();
                        civilianController.OverrideContainer = null;
                    }

                    RemoveComponentImmediate(employeeController);
                    Logger.Debug(
                        $"[S1API][BaseEmployeeFallback][Dialogue] Replaced employee dialogue controller on " +
                        $"'{controllerObject.name}' with the base civilian controller.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"[S1API][BaseEmployeeFallback][Dialogue] Failed to normalize employee dialogue components: {ex.Message}");
            }
        }

        private static void RemoveComponentImmediate(Component component)
        {
            try
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
            catch
            {
                UnityEngine.Object.Destroy(component);
            }
        }

        private static GameObject GetOrCreatePerNpcPrefab(System.Type npcType, NPC? owner)
        {
            if (npcType == null)
                throw new Exception("NPC type is null for prefab resolution.");

            if (TypeToPrefab.TryGetValue(npcType, out var cached) && cached != null)
            {
                FinalizeSupplierPrefabIfNeeded(npcType, cached);
                MarkPrefabsConfigured();
                return cached;
            }

            lock (TemplateLoadLock)
            {
                if (TypeToPrefab.TryGetValue(npcType, out cached) && cached != null)
                {
                    FinalizeSupplierPrefabIfNeeded(npcType, cached);
                    MarkPrefabsConfigured();
                    return cached;
                }

                // Prefer a spawnable prefab provided by the base game.
                var nm = InstanceFinder.NetworkManager;
                if (nm == null)
                    throw new Exception("NetworkManager not found when resolving NPC prefab.");

                PrefabObjects spawnablePrefabs = nm.SpawnablePrefabs;
                if (spawnablePrefabs == null)
                    throw new Exception("SpawnablePrefabs not available on NetworkManager.");

                NetworkObject? chosen = null;
                int count = spawnablePrefabs.GetObjectCount();
                
                NpcRootRole rootRole = GetDeclaredRootRole(npcType);
                chosen = ResolveNpcSpawnablePrefab(spawnablePrefabs, count, rootRole);

                if (chosen == null)
                {
                    throw new Exception($"Failed to locate a suitable NPC spawnable prefab ({DealerPrefabName}, {CivilianNpcPrefabName}, {BaseNpcPrefabName}, {BaseEmployeePrefabName}, or any with NPC component).");
                }

                string sourcePrefabName = chosen.gameObject != null ? chosen.gameObject.name : string.Empty;
                LogBetaNpcPrefabDiagnostic($"[S1API][NPCPrefabSelection] Type={npcType.FullName}, RootRole={rootRole}, SourcePrefab='{sourcePrefabName}', Source={DescribeComponent(chosen)}");
                if (chosen.gameObject != null)
                    LogBaseEmployeeComponentState("selected source prefab before clone", chosen.gameObject);

                string prefabName = GetPrefabNameForType(npcType);
                bool restoreChosenActive = false;
                bool chosenWasActive = false;
                if (sourcePrefabName == BaseEmployeePrefabName && chosen.gameObject != null)
                {
                    chosenWasActive = chosen.gameObject.activeSelf;
                    restoreChosenActive = chosenWasActive;
                    if (chosenWasActive)
                        chosen.gameObject.SetActive(false);
                }

                // Build a unique per-NPC prefab based on type
                NetworkObject prefabNO;
                try
                {
                    prefabNO = UnityEngine.Object.Instantiate<NetworkObject>(chosen);
                    // Keep every component added during normalization dormant. Identity-dependent
                    // supplier resources are finalized only after ConfigurePrefab has supplied the ID.
                    prefabNO.gameObject?.SetActive(false);
                    LogBetaNpcPrefabDiagnostic($"[S1API][NPCPrefabSelection] Cloned source '{sourcePrefabName}' into '{prefabNO.gameObject?.name ?? "<null>"}' for type {npcType.FullName}.");
                    if (prefabNO.gameObject != null)
                        LogBaseEmployeeComponentState("cloned prefab before normalization", prefabNO.gameObject);
                }
                finally
                {
                    if (restoreChosenActive && chosen.gameObject != null)
                        chosen.gameObject.SetActive(chosenWasActive);
                }

                NormalizeBaseEmployeePrefab(prefabNO.gameObject, sourcePrefabName, rootRole);
                prefabNO.gameObject.name = prefabName;

                // Ensure template prefab does not execute runtime logic or remain in NPC registry
                try
                {
                    // Deactivate template instance to prevent Awake/Start side effects
                    if (prefabNO != null && prefabNO.gameObject != null)
                    {
                        prefabNO.gameObject.SetActive(false);
                        
                        // Handle registry cleanup for both NPC and Dealer components
                        var dealerComp = prefabNO.gameObject.GetComponent<S1Economy.Dealer>();
                        var npcComp = dealerComp != null ? dealerComp as S1NPCs.NPC : prefabNO.gameObject.GetComponent<S1NPCs.NPC>();
                        
                        if (npcComp != null)
                        {
                            var reg = S1NPCs.NPCManager.NPCRegistry;
                            if (reg != null && reg.Count > 0)
                            {
                                for (int i = reg.Count - 1; i >= 0; i--)
                                {
                                    if (reg[i] == npcComp)
                                    {
                                        reg.RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                // Let the NPC subclass declare required components on the prefab (Customer, actions, etc.)
                if (prefabNO is null)
                    throw new InvalidOperationException("NPC prefab is missing its NetworkObject.");
                var prefabRoot = prefabNO.gameObject ?? throw new InvalidOperationException("NPC prefab is missing its GameObject.");
                var builder = new NPCPrefabBuilder(prefabRoot, npcType);
                if (owner != null)
                {
                    owner.ConfigurePrefab(builder);
                }
                else
                {
                    InvokeConfigurePrefabWithoutInstance(npcType, builder);
                }

                // ConfigurePrefab may declare a specialized root role even when the virtual property was not overridden.
                rootRole = GetDeclaredRootRole(npcType);
                switch (rootRole)
                {
                    case NpcRootRole.Dealer:
                    {
                        var dealerComponent = EnsureDealerComponentOnPrefab(prefabNO.gameObject);
                        var dealerDefaults = BuildDealerDefaultsForType(npcType);
                        if (dealerComponent != null && dealerDefaults != null)
                            TryApplyDealerDefaults(dealerComponent, dealerDefaults);
                        break;
                    }
                    case NpcRootRole.Supplier:
                    {
                        var supplierComponent = EnsureSupplierComponentOnPrefab(prefabNO.gameObject);
                        var supplierDefaults = BuildSupplierDefaultsForType(npcType);
                        if (supplierComponent != null && supplierDefaults != null)
                            TryApplySupplierDefaults(supplierComponent, supplierDefaults);
                        SupplierRuntimeCoordinator.FinalizePrefabInfrastructure(
                            prefabNO.gameObject,
                            supplierDefaults?.PersistentId);
                        break;
                    }
                }

                // Ensure schedule actions exist on the template so NetworkBehaviour indices are stable
                try
                {
                    EnsureScheduleActionsOnPrefab(prefabNO.gameObject);
                }
                catch { }

                if (sourcePrefabName == BaseEmployeePrefabName)
                {
                    RemoveEmployeeComponentsFromBaseEmployeeFallback(prefabNO.gameObject);
                }

                // If we are pre-registering without an instance owner, ensure baseline Customer exists when applicable
                if (owner == null)
                {
                    try
                    {
                        // Only add Customer for types that opted-in via EnsureCustomer
                        if (IsCustomerType(npcType))
                        {
                            var existingCustomer = prefabNO.gameObject.GetComponent<S1Economy.Customer>();
                            if (existingCustomer == null)
                            {
                                existingCustomer = prefabNO.gameObject.AddComponent<S1Economy.Customer>();
                            }

                            // Apply defaults if the mod registered them
                            var defaults = GetCustomerDefaultsForType(npcType);
                            if (defaults != null && existingCustomer != null)
                            {
                                var data = BuildCustomerDefaultsForType(npcType);
                                if (data != null)
                                    TrySetCustomerDataOnComponent(existingCustomer, data);
                            }
                        }
                        
                    }
                    catch { }
                }

                RepairBehaviourOwnership(prefabRoot, GetPreferredNpcComponent(prefabRoot));

                // Register as spawnable so FishNet assigns stable behaviour indices and can network-spawn
                try
                {
                    if (spawnablePrefabs != null)
                    {
                        bool alreadyRegistered = false;
                        int existingCount = spawnablePrefabs.GetObjectCount();
                        for (int i = 0; i < existingCount; i++)
                        {
                            NetworkObject existing = spawnablePrefabs.GetObject(true, i);
                            if (existing != null && existing.gameObject != null && existing.gameObject.name == prefabName)
                            {
                                alreadyRegistered = true;
                                break;
                            }
                        }

                        if (!alreadyRegistered)
                            spawnablePrefabs.AddObject(prefabNO);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to register {prefabName} in SpawnablePrefabs: {ex.Message}");
                }

                // Organize the prefab in the scene hierarchy to avoid clutter
                NPCPrefabContainer.OrganizePrefab(prefabNO.gameObject, npcType.Name);

                TypeToPrefab[npcType] = prefabNO.gameObject;
                MarkPrefabsConfigured();
                return prefabNO.gameObject;
            }
        }

        private static void FinalizeSupplierPrefabIfNeeded(System.Type npcType, GameObject prefabRoot)
        {
            if (prefabRoot == null || GetDeclaredRootRole(npcType) != NpcRootRole.Supplier)
                return;

            SupplierRuntimeCoordinator.FinalizePrefabInfrastructure(
                prefabRoot,
                BuildSupplierDefaultsForType(npcType)?.PersistentId);
        }

        private static NpcRootRole GetDeclaredRootRole(System.Type npcType)
        {
            bool isDealer = IsDealerType(npcType);
            bool isSupplier = IsSupplierType(npcType);
            bool isPhysical = false;

            try
            {
                NPC tempInstance = (NPC)FormatterServices.GetUninitializedObject(npcType);
                isDealer |= tempInstance.IsDealer;
                isSupplier |= tempInstance.IsSupplier;
                isPhysical = tempInstance.IsPhysical;
            }
            catch
            {
            }

            if (isDealer && isSupplier)
            {
                throw new InvalidOperationException(
                    $"Custom NPC type '{npcType.FullName}' cannot be both a dealer and a supplier root.");
            }

            if (isSupplier)
            {
                if (!isPhysical)
                {
                    throw new InvalidOperationException(
                        $"Custom supplier type '{npcType.FullName}' must override IsPhysical to return true.");
                }

                RegisterSupplierType(npcType);
                return NpcRootRole.Supplier;
            }
            if (isDealer)
            {
                RegisterDealerType(npcType);
                return NpcRootRole.Dealer;
            }
            return NpcRootRole.Plain;
        }

        private static void InvokeConfigurePrefabWithoutInstance(System.Type npcType, NPCPrefabBuilder builder)
        {
            if (npcType == null || builder == null)
                return;

            // Skip if the type did not override ConfigurePrefab
            MethodInfo? configureMethod = npcType.GetMethod("ConfigurePrefab", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (configureMethod == null || configureMethod.DeclaringType == typeof(NPC))
                return;

            NPC? tempInstance = null;
            try
            {
                tempInstance = (NPC)FormatterServices.GetUninitializedObject(npcType);
                configureMethod.Invoke(tempInstance, new object[] { builder });
            }
            finally
            {
                tempInstance = null;
            }
        }

        private static string GetPrefabNameForType(System.Type npcType)
        {
            // Avoid path separators, keep name concise, deterministic per type
            string typeName = npcType != null ? npcType.Name : "UnknownNPC";
            return $"S1API_{typeName}";
        }

        internal static bool TryGetConfiguredNpcId(System.Type npcType, out string id)
        {
            id = string.Empty;
            if (npcType == null)
                return false;

            if (TypeToPrefab.TryGetValue(npcType, out GameObject? prefab) && prefab != null)
            {
                var identity = prefab.GetComponent<NPCPrefabIdentity>();
                if (!string.IsNullOrWhiteSpace(identity?.Id))
                {
                    id = identity.Id;
                    return true;
                }
            }

            if (!NPCPrefabIdentity.TryGetIdentityFromRegistry(
                    GetPrefabNameForType(npcType),
                    out var resolvedId,
                    out _,
                    out _,
                    out _) || string.IsNullOrWhiteSpace(resolvedId))
            {
                return false;
            }

            id = resolvedId;
            return true;
        }

        /// <summary>
        /// INTERNAL: Creates a wrapper for a network-spawned custom NPC on clients.
        /// Called when a client receives an NPC that was spawned on the server.
        /// </summary>
        internal static NPC? CreateWrapperForNetworkSpawnedNPC(S1NPCs.NPC baseNpc)
        {
            if (baseNpc == null)
                return null;

            try
            {
                // Check if this is a custom S1API NPC by looking for NPCPrefabIdentity or prefab name
                var identity = baseNpc.GetComponent<NPCPrefabIdentity>();
                string prefabName = baseNpc.gameObject.name;
                
                // Remove "(Clone)" suffix if present
                if (prefabName.EndsWith("(Clone)"))
                    prefabName = prefabName.Substring(0, prefabName.Length - 7);

                // Check if it's an S1API prefab
                if (!prefabName.StartsWith("S1API_", StringComparison.Ordinal) && identity == null)
                    return null;

                // Extract type name from prefab name
                string? typeName = prefabName.StartsWith("S1API_", StringComparison.Ordinal)
                    ? prefabName.Substring(6) // Remove "S1API_" prefix
                    : null;

                if (string.IsNullOrEmpty(typeName))
                    return null;

                // Find the NPC type in loaded assemblies
                System.Type? npcType = null;
                var baseType = typeof(NPC);
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < asms.Length && npcType == null; ai++)
                {
                    var asm = asms[ai];
                    if (asm == baseType.Assembly)
                        continue; // Skip S1API assembly (internal wrappers)

                    System.Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    for (int ti = 0; ti < types.Length; ti++)
                    {
                        var t = types[ti];
                        if (t == null || t.IsAbstract || !baseType.IsAssignableFrom(t))
                            continue;
                        if (t.Name == typeName)
                        {
                            npcType = t;
                            break;
                        }
                    }
                }

                if (npcType == null)
                    return null;

                // Check if wrapper already exists
                for (int i = 0; i < All.Count; i++)
                {
                    var existing = All[i];
                    if (existing != null && existing.S1NPC == baseNpc)
                        return existing;
                }

                // Create uninitialized instance (avoids constructor which creates new GameObject)
                NPC wrapper = (NPC)FormatterServices.GetUninitializedObject(npcType);
                
                // Use reflection to set readonly fields/properties
                bool s1NpcSet = Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(wrapper, "S1NPC", baseNpc);
                bool isCustomNpcSet = Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(wrapper, "IsCustomNPC", true);
                
                // gameObject is a readonly auto-property, need to find and set its backing field
                bool gameObjectSet = false;
                var allFields = Internal.Utils.ReflectionUtils.GetAllFields(npcType, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                
                for (int fi = 0; fi < allFields.Length; fi++)
                {
                    var field = allFields[fi];
                    // Try compiler-generated backing field name or direct field name
                    if ((field.Name == "<gameObject>k__BackingField" || field.Name == "gameObject") && 
                        field.FieldType == typeof(GameObject))
                    {
                        try
                        {
                            field.SetValue(wrapper, baseNpc.gameObject);
                            gameObjectSet = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Exception setting gameObject backing field '{field.Name}': {ex.Message}");
                        }
                    }
                }
                
                // Fallback: try the property setter if field wasn't found
                if (!gameObjectSet)
                    gameObjectSet = Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(wrapper, "gameObject", baseNpc.gameObject);

                // Validate that critical fields were set
                if (!s1NpcSet)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Could not set S1NPC field/property for '{baseNpc.ID}'.");
                    return null;
                }
                if (!gameObjectSet)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Could not set gameObject field/property for '{baseNpc.ID}'. Tried backing field and property setter.");
                    return null;
                }

                // Verify the wrapper has the correct references
                if (wrapper.S1NPC != baseNpc)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: S1NPC field not set correctly for '{baseNpc.ID}'.");
                    return null;
                }
                if (wrapper.gameObject != baseNpc.gameObject)
                {
                    Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: gameObject field not set correctly for '{baseNpc.ID}'.");
                    return null;
                }

                InitializeWrapperStateFromNetworkSpawn(wrapper, baseNpc);

                // Add to All list
                All.Add(wrapper);
                ReconcileAllCustomNpcRelationshipConnections();

                return wrapper;
            }
            catch (Exception ex)
            {
                Logger.Warning($"CreateWrapperForNetworkSpawnedNPC: Exception creating wrapper for '{baseNpc?.ID ?? "<null>"}': {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private static void InitializeWrapperStateFromNetworkSpawn(NPC wrapper, S1NPCs.NPC baseNpc)
        {
            if (wrapper == null || baseNpc == null)
                return;

            try
            {
                var runtimeAvatar = baseNpc.Avatar ?? baseNpc.gameObject?.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                wrapper._runtimeAvatar = runtimeAvatar;
                wrapper.Appearance = new NPCAppearance(wrapper, runtimeAvatar);
                wrapper.RestoreRuntimeAvatarAppearance();

                try
                {
                    var registry = S1NPCs.NPCManager.NPCRegistry;
                    if (registry != null && !registry.Contains(baseNpc))
                        registry.Add(baseNpc);
                }
                catch { }

                var identity = baseNpc.gameObject?.GetComponent<NPCPrefabIdentity>();
                if (identity != null)
                {
                    // Connections are prefab configuration, not replicated relationship state.
                    // Rebuild only the graph locally so save-loaded delta/unlock state remains intact.
                    identity.ApplyRelationshipConnectionsTo(baseNpc);
                    wrapper._hasExplicitIcon = identity.Icon != null;
                }

                wrapper.RefreshMessagingIcons();
                wrapper._relationshipDataAppliedFromPrefab = identity != null && baseNpc.RelationData != null;
            }
            catch (Exception ex)
            {
                Logger.Warning($"InitializeWrapperStateFromNetworkSpawn: Failed for '{baseNpc?.ID ?? "<null>"}': {ex.Message}");
            }
        }

        public void RefreshMessagingIcons()
        {
            try
            {
                Sprite? sprite = Icon;
                if (sprite == null)
                    return;

                var convo = S1NPC?.MSGConversation;
                if (convo == null)
                    return;

                var entryRect = convo.entry ?? ResolveConversationRect(convo, "entry");
                var containerRect = ResolveConversationRect(convo, "container");

                TryApplyIconToRect(entryRect, sprite);
                TryApplyIconToRect(containerRect, sprite);
            }
            catch (Exception ex)
            {
                Logger.Warning($"RefreshMessagingIcons failed for '{GetSafeNpcId()}': {ex.Message}");
            }
        }

        private void TryApplyIconToRect(RectTransform? rect, Sprite sprite)
        {
            if (rect == null || sprite == null)
                return;

            ApplyIconToPath(rect, null, sprite);
            ApplyIconToPath(rect, "Icon", sprite);
            ApplyIconToPath(rect, "IconMask/Icon", sprite);
        }

        private static void ApplyIconToPath(RectTransform root, string? childPath, Sprite sprite)
        {
            if (root == null || sprite == null)
                return;

            Transform target = string.IsNullOrEmpty(childPath) ? root : root.Find(childPath);
            if (target == null)
                return;

            var image = target.GetComponent<Image>();
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = true;
        }

        private static RectTransform? ResolveConversationRect(S1Messaging.MSGConversation convo, string memberName)
        {
            if (convo == null || string.IsNullOrEmpty(memberName))
                return null;

            var value = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(convo, memberName);
            return value as RectTransform;
        }

        internal static void RegisterSchedulePlanForType(System.Type npcType, System.Collections.Generic.List<IScheduleActionSpec> specs)
        {
            if (npcType == null || specs == null)
                return;
            TypeToSchedulePlan[npcType] = specs;
        }

        /// <summary>
        /// Compatibility shim for manually pre-registering a per-type NPC prefab.
        /// </summary>
        /// <remarks>S1API owns prefab registration and retry timing. Mods should not call this method.</remarks>
        /// <param name="npcType">The custom NPC type whose prefab S1API will pre-register.</param>
        [Obsolete("S1API automatically pre-registers NPC prefabs. Remove this call.", false)]
        public static void PreRegisterPrefabForType(System.Type npcType)
        {
            if (!_loggedExternalPreRegisterTypeCall)
            {
                _loggedExternalPreRegisterTypeCall = true;
                Logger.Warning(
                    "[S1API][NPCPrefabRegistration] A mod called the legacy NPC.PreRegisterPrefabForType API. " +
                    "S1API already owns prefab registration and retry timing; remove this call from mod initialization.");
            }

            PreRegisterPrefabForTypeInternal(npcType);
        }

        internal static void PreRegisterPrefabForTypeInternal(System.Type npcType)
        {
            try
            {
                GetOrCreatePerNpcPrefab(npcType, null);
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException (from reflection) to reveal the actual cause
                var inner = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException : ex;
                var msg = inner?.Message ?? ex.Message;
                var trace = inner?.StackTrace ?? ex.StackTrace;
                Logger.Warning($"[S1API] Failed to pre-register NPC prefab for {npcType?.Name}: {msg}");
                if (!string.IsNullOrEmpty(trace))
                    Logger.Warning($"[S1API] Stack trace: {trace}");
            }
        }

        /// <summary>
        /// Compatibility shim for manually scanning and pre-registering NPC prefabs.
        /// </summary>
        /// <remarks>S1API owns prefab registration and retry timing. Mods should not call this method.</remarks>
        [Obsolete("S1API automatically pre-registers NPC prefabs. Remove this call.", false)]
        public static void PreRegisterAllNpcPrefabs()
        {
            if (!_loggedExternalPreRegisterAllCall)
            {
                _loggedExternalPreRegisterAllCall = true;
                Logger.Warning(
                    "[S1API][NPCPrefabRegistration] A mod called the legacy NPC.PreRegisterAllNpcPrefabs API. " +
                    "S1API already scans and registers NPC prefabs when FishNet is ready; remove this call from mod initialization.");
            }

            PreRegisterAllNpcPrefabsInternal();
        }

        internal static void PreRegisterAllNpcPrefabsInternal()
        {
            try
            {
                // Only pre-register when SpawnablePrefabs is available; otherwise, a warmup will retry shortly
                var nm = InstanceFinder.NetworkManager;
                var spawnables = nm?.SpawnablePrefabs;
                if (spawnables == null)
                    return;

                var baseType = typeof(NPC);
                var baseAssembly = baseType.Assembly;
                var candidateTypes = new System.Collections.Generic.List<System.Type>();
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int ai = 0; ai < asms.Length; ai++)
                {
                    var asm = asms[ai];
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    for (int ti = 0; ti < types.Length; ti++)
                    {
                        var t = types[ti];
                        if (t == null || t.IsAbstract)
                            continue;
                        if (baseType.IsAssignableFrom(t))
                        {
                            // Skip internal S1API NPC wrappers; only pre-register mod-defined types
                            if (t.Assembly == baseAssembly)
                                continue;
                            candidateTypes.Add(t);
                        }
                    }
                }

                foreach (System.Type type in candidateTypes.OrderBy(
                             candidate => candidate.FullName,
                             StringComparer.Ordinal))
                {
                    PreRegisterPrefabForTypeInternal(type);
                }

                SupplierRuntimeCoordinator.EnsureAllDeliveryPrefabsRegistered();

                // Prefabs are configured for this process once registration has been attempted with spawnables present
                MarkPrefabsConfigured();
            }
            catch (Exception ex)
            {
                Logger.Error($"[S1API] PreRegisterAllNpcPrefabs failed: {ex.Message}");
                Logger.Error($"[S1API] Stack Trace: {ex.StackTrace}");
            }
        }

        internal static void RegisterCustomerDefaultsForType(System.Type npcType, System.Action<CustomerDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToCustomerDefaults[npcType] = configure;
        }

        internal static void RegisterCustomerType(System.Type npcType)
        {
            if (npcType == null)
                return;
            CustomerTypes.Add(npcType);
        }

        internal static bool IsCustomerType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return CustomerTypes.Contains(npcType);
        }

        // Helper accessors for loader-time default application
        internal static bool HasCustomerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return TypeToCustomerDefaults.TryGetValue(npcType, out var cfg) && cfg != null;
        }

        internal static System.Action<CustomerDataBuilder>? GetCustomerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            TypeToCustomerDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static S1Economy.CustomerData? BuildCustomerDefaultsForType(System.Type npcType)
        {
            var cfg = GetCustomerDefaultsForType(npcType);
            if (cfg == null)
                return null;
            var builder = new CustomerDataBuilder();
            cfg(builder);
            return builder.BuildInternal();
        }

        internal static bool TrySetCustomerDataOnComponent(S1Economy.Customer customerComponent, S1Economy.CustomerData data)
        {
            if (customerComponent == null || data == null)
                return false;
            try
            {
#if MONOMELON
                var field = typeof(S1Economy.Customer).GetField("customerData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                field?.SetValue(customerComponent, data);
                var field2 = typeof(S1Economy.Customer).GetField("currentAffinityData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                field2?.SetValue(customerComponent, data.DefaultAffinityData);
#else
                customerComponent.customerData = data;
                customerComponent.currentAffinityData = data.DefaultAffinityData;
#endif
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryApplyDealerDefaults(S1Economy.Dealer? dealerComponent, DealerDataBuilder.DealerConfigData data)
        {
            if (dealerComponent is null || data is null)
                return false;
            try
            {
                NPCDataAccess.ApplyDealerDefaults(dealerComponent, data);
                string dealerId = string.Empty;
                try
                {
                    dealerId = dealerComponent.ID ?? dealerComponent?.name ?? "<unknown-dealer>";
                }
                catch
                {
                    dealerId = "<unknown-dealer>";
                }

                Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(dealerComponent, "SigningFee", data.SigningFee);
                Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(dealerComponent, "Cut", data.Cut);
#if MONOMELON
                var dealerTypeField = typeof(S1Economy.Dealer).GetField("DealerType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (dealerTypeField != null)
                {
                    var dealerTypeEnum = Enum.Parse(typeof(S1Economy.EDealerType), data.DealerType.ToString());
                    dealerTypeField.SetValue(dealerComponent, dealerTypeEnum);
                }
#else
                Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(dealerComponent, "DealerType", (S1Economy.EDealerType)(int)data.DealerType);
#endif
                // Note: SellInsufficientQualityItems and SellExcessQualityItems were removed in v0.4.3

                // Store Home building reference in NPCPrefabIdentity for resolution in Main scene
                // This runs in Menu scene where buildings aren't available yet
                string? buildingNameToStore = null;
                if (data.Home != null)
                {
                    // Try to get name from Building wrapper (works even for deferred wrappers)
                    buildingNameToStore = data.Home.Name;
                }
                else if (!string.IsNullOrEmpty(data.HomeName))
                {
                    buildingNameToStore = data.HomeName;
                }

                if (!string.IsNullOrEmpty(buildingNameToStore))
                {
                    var dealerObject = dealerComponent?.gameObject;
                    if (dealerObject is null)
                    {
                        Logger.Warning($"[NPC] TryApplyDealerDefaults: Dealer {dealerId} has no GameObject. Building name '{buildingNameToStore}' will not be stored.");
                        return false;
                    }

                    // Store building name in NPCPrefabIdentity for deferred resolution
                    // Get identity from the NPC GameObject (Dealer inherits from NPC, so dealerComponent IS the NPC)
                    // Use gameObject.GetComponent to ensure we get the component from the root GameObject
                    var identity = dealerObject.GetComponent<Internal.Entities.NPCPrefabIdentity>();
                    if (identity != null)
                    {
                        // Set component field (works on Mono, may be null on Il2Cpp)
                        identity.DealerHomeBuildingName = buildingNameToStore;
                        
                        // Get prefab name - normalize to match RegisterToStaticCache behavior
                        string prefabName = dealerObject.name;
                        if (prefabName.EndsWith("(Clone)"))
                            prefabName = prefabName.Substring(0, prefabName.Length - 7);
                        
                        // Register to static cache for Il2Cpp support (this stores in registry)
                        identity.RegisterToStaticCache(prefabName);
                    }
                    else
                    {
                        Logger.Warning($"[NPC] TryApplyDealerDefaults: NPCPrefabIdentity component not found on {dealerObject.name} for dealer {dealerId}. Building name '{buildingNameToStore}' will not be stored.");
                    }
                }
                else
                {
                    Logger.Warning($"[NPC] TryApplyDealerDefaults: No building name to store for dealer {dealerId}. Home={data.Home != null}, HomeName={data.HomeName ?? "null"}");
                }
                
                // Note: CompletedDealsVariable would need to be set via other means
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[NPC] TryApplyDealerDefaults: Exception applying dealer defaults: {ex.Message}");
                Logger.Error($"[NPC] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        internal static void RegisterRelationshipDefaultsForType(System.Type npcType, System.Action<NPCRelationshipDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToRelationshipDefaults[npcType] = configure;
        }

        internal static void RegisterDealerDefaultsForType(System.Type npcType, System.Action<DealerDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;

            TypeToDealerDefaults[npcType] = configure;

            try
            {
                var builder = new DealerDataBuilder();
                configure(builder);
                var built = builder.BuildInternal();
                TypeToBuiltDealerDefaults[npcType] = built;
                WarnForUnsupportedDealerSettings(DescribeNpcTypeOwner(npcType), built);
            }
            catch (Exception ex)
            {
                TypeToBuiltDealerDefaults.Remove(npcType);
                Logger.Warning($"[S1API] Failed to cache dealer defaults for '{npcType.Name}': {ex.Message}");
            }
        }

        private static void WarnForUnsupportedDealerSettings(
            string dealerOwner,
            DealerDataBuilder.DealerConfigData data)
        {
            if (data.InsufficientQualityConfigured || data.ExcessQualityConfigured)
            {
                var configuredOptions = new System.Collections.Generic.List<string>();
                if (data.InsufficientQualityConfigured)
                    configuredOptions.Add("AllowInsufficientQuality");
                if (data.ExcessQualityConfigured)
                    configuredOptions.Add("AllowExcessQuality");

                WarnUnsupportedDealerSettingOnce(
                    dealerOwner,
                    "quality",
                    $"{string.Join(" and ", configuredOptions)} cannot be applied because the native dealer quality fields were removed. " +
                    "Remove these calls; S1API has preserved them as compatibility no-ops.");
            }

            if (data.CompletedDealsVariableConfigured)
            {
                WarnUnsupportedDealerSettingOnce(
                    dealerOwner,
                    "completed-deals-variable",
                    $"WithCompletedDealsVariable('{data.CompletedDealsVariable}') is not supported for custom dealers and is not applied. " +
                    "Remove this call or track completed deals in the mod's own save data.");
            }
        }

        private static void WarnUnsupportedDealerSettingOnce(
            string dealerOwner,
            string settingKey,
            string guidance)
        {
            string normalizedOwner = string.IsNullOrWhiteSpace(dealerOwner)
                ? "<unknown-dealer>"
                : dealerOwner;
            string warningKey = normalizedOwner + "|" + settingKey;
            lock (WarnedUnsupportedDealerSettings)
            {
                if (!WarnedUnsupportedDealerSettings.Add(warningKey))
                    return;
            }

            Logger.Warning($"[S1API][DealerConfiguration] Dealer '{normalizedOwner}': {guidance}");
        }

        private static string DescribeNpcTypeOwner(System.Type npcType)
        {
            string typeName = npcType.FullName ?? npcType.Name;
            string assemblyName = npcType.Assembly.GetName().Name ?? "<unknown-assembly>";
            return $"{typeName} (assembly={assemblyName})";
        }

        internal static void RegisterDealerType(System.Type npcType)
        {
            if (npcType == null)
                return;

            if (SupplierTypes.Contains(npcType))
            {
                throw new InvalidOperationException(
                    $"Custom NPC type '{npcType.FullName}' cannot be both a dealer and a supplier root.");
            }

            DealerTypes.Add(npcType);
        }

        internal static bool IsDealerType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return DealerTypes.Contains(npcType);
        }

        internal static bool HasDealerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return TypeToDealerDefaults.TryGetValue(npcType, out var cfg) && cfg != null;
        }

        internal static System.Action<DealerDataBuilder>? GetDealerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            TypeToDealerDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static DealerDataBuilder.DealerConfigData? GetBuiltDealerDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;

            TypeToBuiltDealerDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static DealerDataBuilder.DealerConfigData? BuildDealerDefaultsForType(System.Type npcType)
        {
            var cached = GetBuiltDealerDefaultsForType(npcType);
            if (cached != null)
                return cached;

            var cfg = GetDealerDefaultsForType(npcType);
            if (cfg == null)
                return null;

            var builder = new DealerDataBuilder();
            cfg(builder);
            var built = builder.BuildInternal();

            if (npcType != null)
                TypeToBuiltDealerDefaults[npcType] = built;

            return built;
        }

        internal static void RegisterSupplierDefaultsForType(
            System.Type npcType,
            System.Action<SupplierDataBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;

            // Validate the complete callback before replacing a previously valid configuration.
            // Invalid supplier data must fail prefab configuration at its source, not be retained
            // and invoked a second time later during runtime construction.
            var builder = new SupplierDataBuilder();
            configure(builder);
            SupplierDataBuilder.SupplierConfigData built = builder.BuildInternal();

            TypeToSupplierDefaults[npcType] = configure;
            TypeToBuiltSupplierDefaults[npcType] = built;
        }

        internal static void RegisterSupplierType(System.Type npcType)
        {
            if (npcType == null)
                return;

            if (DealerTypes.Contains(npcType))
            {
                throw new InvalidOperationException(
                    $"Custom NPC type '{npcType.FullName}' cannot be both a dealer and a supplier root.");
            }

            SupplierTypes.Add(npcType);
        }

        internal static bool IsSupplierType(System.Type npcType)
        {
            return npcType != null && SupplierTypes.Contains(npcType);
        }

        internal static SupplierDataBuilder.SupplierConfigData? BuildSupplierDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;

            if (TypeToBuiltSupplierDefaults.TryGetValue(npcType, out var cached) && cached != null)
                return cached;

            if (!TypeToSupplierDefaults.TryGetValue(npcType, out var configure) || configure == null)
                return null;

            var builder = new SupplierDataBuilder();
            configure(builder);
            var built = builder.BuildInternal();
            TypeToBuiltSupplierDefaults[npcType] = built;
            return built;
        }

        internal static bool TryApplySupplierDefaults(
            S1Economy.Supplier supplierComponent,
            SupplierDataBuilder.SupplierConfigData data)
        {
            try
            {
                return NPCDataAccess.ApplySupplierDefaults(supplierComponent, data);
            }
            catch (Exception ex)
            {
                Logger.Error($"[NPC] Failed to apply supplier defaults: {ex.Message}");
                return false;
            }
        }

        internal static void RegisterRandomInventoryDefaultsForType(System.Type npcType, System.Action<RandomInventoryItemsBuilder> configure)
        {
            if (npcType == null || configure == null)
                return;
            TypeToRandomInventoryDefaults[npcType] = configure;
        }

        internal static bool HasRandomInventoryDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return false;
            return TypeToRandomInventoryDefaults.TryGetValue(npcType, out var cfg) && cfg != null;
        }

        internal static System.Action<RandomInventoryItemsBuilder>? GetRandomInventoryDefaultsForType(System.Type npcType)
        {
            if (npcType == null)
                return null;
            TypeToRandomInventoryDefaults.TryGetValue(npcType, out var cfg);
            return cfg;
        }

        internal static RandomInventoryItemsBuilder.InventoryDefaultsData? BuildRandomInventoryDefaultsForType(System.Type npcType)
        {
            var cfg = GetRandomInventoryDefaultsForType(npcType);
            if (cfg == null)
                return null;
            var builder = new RandomInventoryItemsBuilder();
            cfg(builder);
            return builder.BuildInternal();
        }

        internal static void RegisterSpawnPositionForType(System.Type npcType, Vector3 position, Quaternion rotation)
        {
            if (npcType == null)
                return;
            TypeToSpawnPosition[npcType] = (position, rotation);
        }

        private static void MarkPrefabsConfigured()
        {
            _prefabsConfiguredForLocalProcess = true;
        }

#endregion
        
        #region Protected Members

        /// <summary>
        /// A list of text responses you've added to your NPC.
        /// </summary>
        protected readonly System.Collections.Generic.List<Response> Responses = new System.Collections.Generic.List<Response>();

        /// <summary>
        /// Base constructor for a new NPC. Identity is configured via <see cref="ConfigurePrefab"/> using <see cref="NPCPrefabBuilder.WithIdentity"/> and optionally <see cref="NPCPrefabBuilder.WithIcon"/>.
        /// </summary>
        /// <remarks>
        /// Not intended for direct instancing. Create your derived class and let S1API handle instancing.
        /// Identity information (ID, firstName, lastName, icon) must be provided in <see cref="ConfigurePrefab"/> using the builder methods.
        /// </remarks>
        protected NPC()
        {
            IsCustomNPC = true;

            gameObject = InstantiateTemplateInstance(this.GetType(), this);
            gameObject.SetActive(false);

            S1NPCs.NPC? prefabNpc = GetPreferredNpcComponent(gameObject);
            if (prefabNpc == null)
                throw new Exception("NPC template is missing the core ScheduleOne.NPCs.NPC component.");

            NPCDataAccess.InitializeCurrentDataForConstruction(prefabNpc);
            S1NPC = prefabNpc;

            S1AvatarFramework.Avatar? runtimeAvatar = S1NPC.Avatar ?? gameObject.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
            _runtimeAvatar = runtimeAvatar;

            // EnsureTextMeshProFonts();

            // Read identity from NPCPrefabIdentity component (set by ConfigurePrefab via WithIdentity/WithIcon)
            var identity = gameObject.GetComponent<NPCPrefabIdentity>();
            string? id = null;
            string? firstName = null;
            string? lastName = null;
            Sprite? icon = null;

            if (identity != null)
            {
                string prefabName = gameObject.name;
                if (prefabName.EndsWith("(Clone)"))
                    prefabName = prefabName.Substring(0, prefabName.Length - 7);
                identity.PrefabName = prefabName;

                if (NPCPrefabIdentity.TryGetIdentityFromRegistry(prefabName, out string? regId, out string? regFirstName, out string? regLastName, out Sprite? regIcon))
                {
                    id = regId;
                    firstName = regFirstName;
                    lastName = regLastName;
                    icon = regIcon;
                }
                else
                {
                    // Fallback to component-backed values if the registry is unavailable.
                    id = identity.Id;
                    firstName = identity.FirstName;
                    lastName = identity.LastName;
                    icon = identity.Icon;
                }
            }

            NPCDataAccess.ApplyIdentity(S1NPC, id, firstName, lastName);
            if (icon != null)
            {
                NPCDataAccess.ApplyIcon(S1NPC, icon);
                _hasExplicitIcon = true;
            }

            // Use default icon if none was set
            if (Icon == null)
                NPCDataAccess.ApplyIcon(S1NPC, S1DevUtilities.PlayerSingleton<S1ContactApps.ContactsApp>.Instance.AppIcon);

            AssignPersistentGuid(id);
            
            if (IsPhysical)
                ResetConversationCategoriesToDefaults();
            else
                EnsureMessageConversationReady(resetDefaults: true);
            InitializeHealthComponent();
            InitializeAwarenessComponent();
            InitializeBehaviourComponents();
            InitializeVisionComponents();
            InitializeInteractables();
            InitializeInventoryComponent();
            InitializeRelationshipData();
            InitializeNetworkBehaviours();

            identity?.ApplyAppearanceTo(S1NPC, _runtimeAvatar);
            Appearance = new NPCAppearance(this, _runtimeAvatar);
            RestoreRuntimeAvatarAppearance();

            string displayName = FirstName;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = !string.IsNullOrWhiteSpace(id) ? id : GetPrefabNameForType(GetType());
            gameObject.name = displayName;

            // Ensure the base game NPC is added to the registry manually since Awake isn't called when inactive
            if (!S1NPCs.NPCManager.NPCRegistry.Contains(S1NPC))
            {
                S1NPCs.NPCManager.NPCRegistry.Add(S1NPC);
            }

            All.Add(this);
        }

        /// <summary>
        /// Backwards-compatible constructor for non-physical NPCs that provides identity directly via parameters.
        /// This constructor is intended for backwards compatibility with mods that used the old constructor pattern.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This constructor is marked as obsolete. For new code, use the parameterless constructor and configure identity
        /// via <see cref="ConfigurePrefab"/> using <see cref="NPCPrefabBuilder.WithIdentity"/> and optionally <see cref="NPCPrefabBuilder.WithIcon"/>.
        /// </para>
        /// <para>
        /// This constructor is appropriate for non-physical NPCs (where <see cref="IsPhysical"/> returns <c>false</c>) that
        /// don't require prefab configuration. Physical NPCs should use <see cref="ConfigurePrefab"/> for proper network spawn support.
        /// </para>
        /// </remarks>
        /// <param name="id">Unique identifier for the NPC.</param>
        /// <param name="firstName">The first name for the NPC.</param>
        /// <param name="lastName">The last name for the NPC. Can be null.</param>
        /// <param name="icon">The icon sprite for the NPC. Can be null to use default.</param>
        [Obsolete("Use the parameterless constructor and configure identity via ConfigurePrefab with NPCPrefabBuilder.WithIdentity. This constructor is provided for backwards compatibility with non-physical NPCs.")]
        protected NPC(string id, string? firstName, string? lastName, Sprite? icon = null) : this()
        {
            bool hasId = !string.IsNullOrEmpty(id);
            bool hasFirstName = !string.IsNullOrEmpty(firstName);
            bool hasLastName = !string.IsNullOrEmpty(lastName);

            NPCDataAccess.ApplyIdentity(S1NPC, id, firstName, lastName);
            AssignPersistentGuid(id);
            if (icon != null)
            {
                NPCDataAccess.ApplyIcon(S1NPC, icon);
                _hasExplicitIcon = true;
            }

            var identity = gameObject.GetComponent<NPCPrefabIdentity>();
            if (identity != null)
            {
                if (hasId)
                    identity.Id = id!;
                if (hasFirstName)
                    identity.FirstName = firstName!;
                if (hasLastName)
                    identity.LastName = lastName!;
                if (icon != null)
                    identity.Icon = icon;

                identity.RegisterToStaticCache(gameObject.name);
            }

            if (Icon == null)
                NPCDataAccess.ApplyIcon(S1NPC, S1DevUtilities.PlayerSingleton<S1ContactApps.ContactsApp>.Instance.AppIcon);

            string displayName = FirstName;
            if (string.IsNullOrEmpty(displayName))
                displayName = hasId ? id! : "UnknownNPC";
            gameObject.name = displayName;

            // Update the message conversation's contact name if it was already created
            if (S1NPC.MSGConversation != null)
            {
                try
                {
                    // Update contactName field/property in MSGConversation
                    string newContactName = GetNpcFullName();
                    if (string.IsNullOrEmpty(newContactName))
                        newContactName = hasFirstName ? firstName! : (hasId ? id! : "Unknown");
                    
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(S1NPC.MSGConversation, "contactName", newContactName);

                    // Refresh the UI to show the updated name
                    RefreshMessagingIcons();
                    
                    // Update the entry name text if UI exists by calling SetIsKnown with current value
                    var setIsKnownMethod = typeof(S1Messaging.MSGConversation).GetMethod("SetIsKnown", BindingFlags.Public | BindingFlags.Instance);
                    if (setIsKnownMethod != null)
                    {
                        var isKnownValue = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(S1NPC.MSGConversation, "IsSenderKnown");
                        bool isKnown = isKnownValue is bool known ? known : true;
                        setIsKnownMethod.Invoke(S1NPC.MSGConversation, new object[] { isKnown });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to update MSGConversation contactName for '{GetSafeNpcId()}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Override to configure NPC components and default behavior before the NPC is spawned.
        /// Called during prefab creation to set up spawn position, customer behavior, relationships, and schedules.
        /// </summary>
        /// <remarks>
        /// Customer, relationship, and schedule configuration must be done here for proper save/load behavior and network compatibility.
        /// Use the builder pattern for fluent configuration. Runtime initialization should be done in <see cref="OnCreated"/> instead.
        /// </remarks>
        /// <param name="builder">Prefab builder for configuring this NPC type.</param>
        protected virtual void ConfigurePrefab(NPCPrefabBuilder builder) { }

        /// <summary>
        /// Called when a text message response is loaded from the save file.
        /// Override to re-attach callbacks to loaded responses.
        /// </summary>
        /// <param name="response">The response that was loaded from save data.</param>
        protected virtual void OnResponseLoaded(Response response) { }

        /// <summary>
        /// Called when the NPC is fully created and spawned. Override for runtime initialization after all components are set up.
        /// Use this to configure appearance, dialogue systems, subscribe to events, enable schedule system, and set basic properties.
        /// </summary>
        /// <remarks>
        /// Called after the NPC is instantiated and all components are initialized. Appearance, dialogue, and schedule setup should be done here rather than in the constructor.
        /// </remarks>
        protected override void OnCreated()
        {
            if (_hasExplicitIcon)
                RefreshMessagingIcons();
            else
                Appearance.GenerateMugshot();

            RestoreRuntimeAvatarAppearance();
            RepairNpcPrefabReferences(gameObject, S1NPC);
            // Adding a movement component when NPC is created prevents it from disabling
            if (S1NPC.Movement == null)
                SetGameMember(S1NPC, "Movement", gameObject.GetComponent<S1NPCs.NPCMovement>());

            if (S1NPC.Movement != null)
                S1NPC.Movement.enabled = true;
        }

        #endregion

        // Public members intended to be used by modders.
        // Can be used inside your derived class, or outside via instance reference.
        #region Public Members

        /// <summary>
        /// INTERNAL: Tracking for the GameObject associated with this NPC.
        /// Not intended for use by modders!
        /// </summary>
        public GameObject gameObject { get; }

        /// <summary>
        /// The world position of the NPC.
        /// </summary>
        public Vector3 Position
        {
            get => gameObject.transform.position;
            set => S1NPC.Movement.Warp(value);
        }

        /// <summary>
        /// The transform of the NPC.
        /// Please do not set the properties of this transform.
        /// </summary>
        public Transform Transform =>
            gameObject.transform;

        /// <summary>
        /// List of all NPCs within the base game and modded.
        /// </summary>
        public static readonly System.Collections.Generic.List<NPC> All = new System.Collections.Generic.List<NPC>();

        /// <summary>
        /// Whether all custom NPCs have been instantiated and finalized.
        /// This flag is set to true once all custom NPC types have been spawned and initialized.
        /// Mods can check this to ensure custom NPCs are ready before performing operations that depend on them.
        /// </summary>
        public static bool CustomNpcsReady
        {
            get => Internal.Patches.NPCPatches.CustomNpcsReady;
            internal set => Internal.Patches.NPCPatches.CustomNpcsReady = value;
        }

        /// <summary>
        /// The first name of this NPC.
        /// </summary>
        public string FirstName
        {
            get => NPCDataAccess.GetFirstName(S1NPC);
            set => NPCDataAccess.ApplyFirstName(S1NPC, value);
        }

        /// <summary>
        /// The last name of this NPC.
        /// </summary>
        public string LastName
        {
            get => NPCDataAccess.GetLastName(S1NPC);
            set => NPCDataAccess.ApplyLastName(S1NPC, value);
        }

        /// <summary>
        /// The full name of this NPC.
        /// If there is no last name, it will just return the first name.
        /// </summary>
        public string FullName =>
            GetNpcFullName();

        /// <summary>
        /// The unique identifier to assign to this NPC.
        /// Used when saving and loading. Probably other things within the base game code.
        /// </summary>
        public string ID
        {
            get => NPCDataAccess.GetId(S1NPC);
            protected set => NPCDataAccess.ApplyId(S1NPC, value);
        }

        /// <summary>
        /// Static NPC ID for this NPC type. Used to resolve connections during prefab configuration
        /// when NPC instances are not yet available. Override this in derived classes to provide
        /// the NPC ID string (e.g., "kyle_cooley", "ludwig_meyer").
        /// </summary>
        /// <remarks>
        /// For built-in NPC wrappers, this should return the ID string that matches the base game NPC.
        /// For custom NPCs, this should return the ID configured via <see cref="NPCPrefabBuilder.WithIdentity"/>.
        /// </remarks>
        public static string? NPCId => null;

        /// <summary>
        /// The icon assigned to this NPC.
        /// </summary>
        public Sprite? Icon
        {
            get => NPCDataAccess.GetIcon(S1NPC);
            set
            {
                _hasExplicitIcon = value != null;
                NPCDataAccess.ApplyIcon(S1NPC, value);
                RefreshMessagingIcons();
                Internal.Patches.ContactsAppPatches.RefreshContactIcon(S1NPC);
            }
        }

        internal void ApplyGeneratedIcon(Sprite icon)
        {
            if (_hasExplicitIcon)
                return;

            NPCDataAccess.ApplyIcon(S1NPC, icon);
            RefreshMessagingIcons();
            Internal.Patches.ContactsAppPatches.RefreshContactIcon(S1NPC);
        }

        /// <summary>
        /// Whether the NPC is currently conscious or not.
        /// </summary>
        public bool IsConscious =>
            S1NPC.IsConscious;

        /// <summary>
        /// Whether the NPC is currently inside a building or not.
        /// </summary>
        public bool IsInBuilding =>
            S1NPC.isInBuilding;

        /// <summary>
        /// Whether the NPC is currently inside a vehicle or not.
        /// </summary>
        public bool IsInVehicle =>
            S1NPC.IsInVehicle;

        /// <summary>
        /// Whether the NPC is currently panicking or not.
        /// </summary>
        public bool IsPanicking =>
            S1NPC.IsPanicked;

        /// <summary>
        /// Whether the NPC is currently unsettled or not.
        /// </summary>
        public bool IsUnsettled =>
            S1NPC.isUnsettled;

        /// <summary>
        /// UNCONFIRMED: Whether the NPC is currently visible to the player or not.
        /// If you confirm this, please let us know so we can update the documentation!
        /// </summary>
        public bool IsVisible =>
            S1NPC.isVisible;

        /// <summary>
        /// Determines if the NPC is visible in the game world. Override as true for physical NPCs with 3D models, movement, and direct interaction.
        /// </summary>
        /// <remarks>
        /// Physical NPCs (<c>true</c>): Visible in world, have collision detection, can move and follow schedules, can be damaged/healed.
        /// Non-physical NPCs (<c>false</c>): Invisible, primarily for messaging and phone contacts, cannot move or be directly interacted with.
        /// </remarks>
        public virtual bool IsPhysical => false;
        
        /// <summary>
        /// Determines if the NPC has dealer functionality. Override as true for NPCs that should be dealers.
        /// </summary>
        /// <remarks>
        /// Dealer NPCs (<c>true</c>): Can manage customers, handle contracts, accept cash payments, and track inventory for sales.
        /// When true, the NPC prefab will use the "Dealer" network prefab instead of "CivilianNPC".
        /// Non-dealer NPCs (<c>false</c>): Regular NPCs without dealer-specific functionality.
        /// </remarks>
        public virtual bool IsDealer => false;

        /// <summary>
        /// Determines whether this NPC uses the native supplier root and supplier framework data.
        /// </summary>
        /// <remarks>
        /// Supplier NPCs can provide dead-drop orders, meetings, delivery unlocks, and debt tracking.
        /// A custom NPC cannot be both a dealer and a supplier.
        /// </remarks>
        public virtual bool IsSupplier => false;

        internal void EnsureMessageConversationReady(bool resetDefaults)
        {
            try
            {
                var categories = resetDefaults
                    ? ResetConversationCategoriesToDefaults()
                    : EnsureConversationCategoriesInitialized();

                EnsureMessageConversationInstance(categories);
            }
            catch (Exception ex)
            {
                if (IsCustomNPC && (ex is ArgumentNullException || ex.Message.Contains("ArgumentNullException")))
                    return;

                Logger.Warning($"EnsureMessageConversationReady exception for '{GetSafeNpcId()}': {ex.Message}");
            }
        }

        private ConversationCategoryList EnsureConversationCategoriesInitialized()
        {
            var categories = GetConversationCategories();

            if (categories.Count == 0)
            {
                ResetConversationCategoriesToDefaults(categories);
            }

            return categories;
        }

        private ConversationCategoryList ResetConversationCategoriesToDefaults()
        {
            var categories = GetConversationCategories();
            categories.Clear();

            ResetConversationCategoriesToDefaults(categories);
            return categories;
        }

        private void ResetConversationCategoriesToDefaults(ConversationCategoryList categories)
        {
            if (categories == null)
                return;

            if (ShouldUseSupplierCategory())
            {
                categories.Add(S1Messaging.EConversationCategory.Supplier);
            }
            else if (ShouldUseDealerCategory())
            {
                categories.Add(S1Messaging.EConversationCategory.Dealer);
            }
            else
            {
                categories.Add(S1Messaging.EConversationCategory.Customer);
            }

            SetConversationCategories(categories);
        }

        internal void SetConversationCategory(
            S1Messaging.EConversationCategory category,
            bool ensureUi = true)
        {
            var categories = new ConversationCategoryList();
            categories.Add(category);
            SetConversationCategories(categories);

            if (S1NPC.MSGConversation == null)
                return;

            S1NPC.MSGConversation.SetCategories(categories);
            if (ensureUi)
                S1NPC.MSGConversation.EnsureUIExists();
        }

        private bool ShouldUseDealerCategory()
        {
            bool useDealer = false;

            try
            {
                useDealer = IsDealer;
            }
            catch
            {
            }

            if (!useDealer)
            {
                try
                {
                    useDealer = IsDealerType(GetType());
                }
                catch
                {
                }
            }

            return useDealer;
        }

        private bool ShouldUseSupplierCategory()
        {
            bool useSupplier = false;

            try
            {
                useSupplier = IsSupplier;
            }
            catch
            {
            }

            if (!useSupplier)
            {
                try
                {
                    useSupplier = IsSupplierType(GetType());
                }
                catch
                {
                }
            }

            return useSupplier;
        }

        private void EnsureMessageConversationInstance(ConversationCategoryList categories)
        {
            if (S1NPC == null)
                return;

            if (S1NPC.MSGConversation == null)
            {
#if IL2CPPMELON
                S1NPC.CreateMessageConversation();
#elif MONOMELON
                MethodInfo createConvoMethod = AccessTools.Method(typeof(S1NPCs.NPC), "CreateMessageConversation");
                createConvoMethod?.Invoke(S1NPC, null);
#endif
                if (S1NPC.MSGConversation == null)
                {
                    Logger.Warning($"EnsureMessageConversationInstance: creation failed for '{GetSafeNpcId()}'.");
                }
            }

            var convo = S1NPC.MSGConversation;
            if (convo == null)
            {
                Logger.Warning($"EnsureMessageConversationInstance: conversation still null for '{GetSafeNpcId()}'.");
                return;
            }

            if (categories == null)
            {
                Logger.Warning($"EnsureMessageConversationInstance: categories null for '{GetSafeNpcId()}'.");
                return;
            }

            try
            {
                convo.SetCategories(categories);
                _messaging?.EnsureConversationHook();
            }
            catch (Exception ex)
            {
                Logger.Warning($"EnsureMessageConversationInstance: failed to apply categories for '{GetSafeNpcId()}': {ex.Message}");
            }
        }

        private static object? GetGameMember(object? target, string memberName)
        {
            return target == null
                ? null
                : Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(target, memberName);
        }

        private static T? GetGameMember<T>(object? target, string memberName)
        {
            var value = GetGameMember(target, memberName);
            return value is T typed ? typed : default;
        }

        private static bool SetGameMember(object? target, string memberName, object? value)
        {
            if (target == null)
                return false;

            return Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(target, memberName, value)
                   || Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(target, $"<{memberName}>k__BackingField", value)
                   || Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(target, $"_{memberName}_k__BackingField", value);
        }

        private string GetNpcString(string memberName)
        {
            return GetGameMember<string>(S1NPC, memberName) ?? string.Empty;
        }

        private string GetSafeNpcId()
        {
            try
            {
                string id = NPCDataAccess.GetId(S1NPC);
                if (!string.IsNullOrEmpty(id))
                    return id;
            }
            catch
            {
            }

            try
            {
                var identity = gameObject != null ? gameObject.GetComponent<NPCPrefabIdentity>() : null;
                if (!string.IsNullOrEmpty(identity?.Id))
                    return identity.Id;
            }
            catch
            {
            }

            return "<null>";
        }

        private void SetNpcMember(string memberName, object? value)
        {
            SetGameMember(S1NPC, memberName, value);
        }

        private string GetNpcFullName()
        {
            var fullName = GetGameMember<string>(S1NPC, "fullName");
            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            var firstName = FirstName;
            var lastName = LastName;
            return string.IsNullOrWhiteSpace(lastName)
                ? firstName
                : $"{firstName} {lastName}".Trim();
        }

        private ConversationCategoryList GetConversationCategories()
        {
            var categories = new ConversationCategoryList();
            foreach (S1Messaging.EConversationCategory category in NPCDataAccess.GetConversationCategories(S1NPC))
                categories.Add(category);
            return categories;
        }

        private void SetConversationCategories(ConversationCategoryList categories)
        {
            var values = new System.Collections.Generic.List<S1Messaging.EConversationCategory>(categories.Count);
            for (int i = 0; i < categories.Count; i++)
                values.Add(categories[i]);
            NPCDataAccess.ApplyConversationCategories(S1NPC, values);
        }

        private static UnityEvent? GetInventoryContentsChanged(S1NPCs.NPCInventory? inventory)
        {
            return GetGameMember(inventory, "onContentsChanged") as UnityEvent;
        }

        private static S1Interaction.InteractableObject? GetInventoryPickpocketInteractable(S1NPCs.NPCInventory? inventory)
        {
            return GetGameMember(inventory, "PickpocketIntObj") as S1Interaction.InteractableObject;
        }

        private static void SetInventoryPickpocketInteractable(S1NPCs.NPCInventory? inventory, S1Interaction.InteractableObject interactable)
        {
            SetGameMember(inventory, "PickpocketIntObj", interactable);
        }

        private static void SetInventoryMember(S1NPCs.NPCInventory? inventory, string memberName, object? value)
        {
            SetGameMember(inventory, memberName, value);
        }

        private static object? GetInventoryMember(S1NPCs.NPCInventory? inventory, string memberName)
        {
            return GetGameMember(inventory, memberName);
        }
        
        /// <summary>
        /// How aggressive this NPC is towards others.
        /// </summary>
        public float Aggressiveness
        {
            get => S1NPC.Aggression;
            set => SetNpcMember("Aggression", value);
        }

        /// <summary>
        /// The region the NPC is associated with.
        /// Note: Not the region they're in currently. Just the region they're designated to.
        /// </summary>
        public Region Region
        {
            get => (Region)S1NPC.Region;
            set
            {
                // Map S1API.Map.Region to base game's EMapRegion safely
                try
                {
                    S1NPC.Region = (S1MapBase.EMapRegion)(int)value;
                }
                catch
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// Sets the scale of the NPC.
        /// </summary>
        public float Scale
        {
            get => S1NPC.Scale;
            set => S1NPC.SetScale(value);
        }

        /// <summary>
        /// Whether the NPC is knocked out or not.
        /// </summary>
        public bool IsKnockedOut =>
            S1NPC.Health.IsKnockedOut;

        /// <summary>
        /// UNCONFIRMED: Whether the NPC requires the region unlocked in order to deal to.
        /// If you confirm this, please let us know so we can update the documentation!
        /// </summary>
        public bool RequiresRegionUnlocked
        {
#if IL2CPPMELON
            get => DefaultRequiresRegionUnlocked;
            set { /* no-op under IL2CPP; constant in base game so non accessible */ }
#else
            get => _requiresRegionUnlockedField != null && (bool)_requiresRegionUnlockedField.GetValue(S1NPC)!;
            set { _requiresRegionUnlockedField?.SetValue(S1NPC, value); }
#endif
        }

        /// <summary>
        /// The enterable building the NPC is currently in, if any.
        /// </summary>
        public Map.Building? CurrentBuilding
        {
            get
            {
                object? currentBuilding = S1NPC.CurrentBuilding;
                return currentBuilding == null
                    ? null
                    : Map.Building.All.FirstOrDefault(building => ReferenceEquals(building._gameBuilding, currentBuilding));
            }
        }

        /// <summary>
        /// The current vehicle the NPC is occupying, if any.
        /// </summary>
        public LandVehicle? CurrentVehicle =>
            S1NPC.CurrentVehicle != null ? new LandVehicle(S1NPC.CurrentVehicle) : null;

        // TODO: Add Inventory (currently missing NPCInventory abstraction)
        // public ??? Inventory { get; set; }

        /// <summary>
        /// The current health the NPC has.
        /// </summary>
        public float CurrentHealth =>
            S1NPC.Health.Health;

        /// <summary>
        /// The maximum health the NPC has.
        /// </summary>
        public float MaxHealth
        {
            get => S1NPC.Health.MaxHealth;
            set => SetGameMember(S1NPC.Health, "MaxHealth", value);
        }

        /// <summary>
        /// Whether the NPC is dead or not.
        /// </summary>
        public bool IsDead =>
            S1NPC.Health.IsDead;

        /// <summary>
        /// Whether the NPC is invincible or not.
        /// </summary>
        public bool IsInvincible
        {
            get => GetGameMember<bool>(S1NPC.Health, "Invincible");
            set => SetGameMember(S1NPC.Health, "Invincible", value);
        }

        /// <summary>
        /// Revives the NPC. For a network-spawned custom NPC, the server uses the native revive method.
        /// A client call does not alter local state.
        /// Unspawned custom NPCs use a temporary compatibility fallback until FishNet initializes.
        /// </summary>
        public void Revive() =>
            S1NPC.Health.Revive();

        /// <summary>
        /// Deals damage to the NPC.
        /// </summary>
        /// <param name="amount">The amount of damage to deal.</param>
        public void Damage(int amount)
        {
            if (amount <= 0)
                return;

            S1NPC.Health.TakeDamage(amount, true);
        }

        /// <summary>
        ///  Heals the NPC.
        /// </summary>
        /// <param name="amount">The amount of health to heal.</param>
        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            float actualHealAmount = Mathf.Min(amount, S1NPC.Health.MaxHealth - S1NPC.Health.Health);
            S1NPC.Health.TakeDamage(-actualHealAmount, false);
        }

        /// <summary>
        /// Kills the NPC.
        /// </summary>
        public void Kill() =>
            S1NPC.Health.TakeDamage(S1NPC.Health.MaxHealth);

        /// <summary>
        /// Causes the NPC to become unsettled.
        /// UNCONFIRMED: Will panic them for a short duration.
        /// </summary>
        /// <param name="duration">Length of time they should stay unsettled.</param>
        public void Unsettle(float duration) =>
            _unsettleMethod.Invoke(S1NPC, new object[] { duration });

        /// <summary>
        /// Smoothly scales the NPC over lerpTime.
        /// </summary>
        /// <param name="scale">The scale you want set.</param>
        /// <param name="lerpTime">The time to scale over.</param>
        public void LerpScale(float scale, float lerpTime) =>
            S1NPC.SetScale(scale, lerpTime);

        /// <summary>
        /// Requests that the NPC become panicked.
        /// </summary>
        public void Panic() =>
            S1NPC.SetPanicked_Server();

        /// <summary>
        /// Causes the NPC to stop panicking, if they are currently.
        /// </summary>
        public void StopPanicking() =>
            _removePanicMethod.Invoke(S1NPC, new object[] { });

        /// <summary>
        /// Knocks the NPC out.
        /// NOTE: Does not work for invincible NPCs.
        /// </summary>
        public void KnockOut() =>
            S1NPC.Health.KnockOut();

        /// <summary>
        /// Tells the NPC to travel to a specific position in world space.
        /// </summary>
        /// <param name="position">The position to travel to.</param>
        public void Goto(Vector3 position) =>
            S1NPC.Movement.SetDestination(position);

        /// <summary>
        /// Clears the NPC's conversation categories, removing any badge (C/S/D) from the messages UI.
        /// This makes the NPC appear like Uncle Nelson - present in messages but without a category badge.
        /// </summary>
        public void ClearConversationCategories()
        {
            try
            {
                var categories = new ConversationCategoryList();
                SetConversationCategories(categories);
                S1NPC.MSGConversation?.SetCategories(categories);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to clear conversation categories for {ID}: {ex.Message}");
            }
        }

        // TODO: Add OnEnterVehicle listener (currently missing LandVehicle abstraction)
        // public event Action OnEnterVehicle { }

        // TODO: Add OnExitVehicle listener (currently missing LandVehicle abstraction)
        // public event Action OnExitVehicle { }

        // TODO: Add OnExplosionHeard listener (currently missing NoiseEvent abstraction)
        // public event Action OnExplosionHeard { }

        // TODO: Add OnGunshotHeard listener (currently missing NoiseEvent abstraction)
        // public event Action OnGunshotHeard { }

        // TODO: Add OnHitByCar listener (currently missing LandVehicle abstraction)
        // public event Action OnHitByCar { }

        // TODO: Add OnNoticedDrugDealing listener (currently missing Player abstraction)
        // public event Action OnNoticedDrugDealing { }

        // TODO: Add OnNoticedGeneralCrime listener (currently missing Player abstraction)
        // public event Action OnNoticedGeneralCrime { }

        // TODO: Add OnNoticedPettyCrime listener (currently missing Player abstraction)
        // public event Action OnNoticedPettyCrime { }

        // TODO: Add OnPlayerViolatingCurfew listener (currently missing Player abstraction)
        // public event Action OnPlayerViolatingCurfew { }

        // TODO: Add OnNoticedSuspiciousPlayer listener (currently missing Player abstraction)
        // public event Action OnNoticedSuspiciousPlayer { }

        /// <summary>
        /// Called when the NPC died.
        /// </summary>
        public event Action OnDeath
        {
            add => global::S1API.Utils.EventHelper.AddListener(value, S1NPC.Health.onDie);
            remove => global::S1API.Utils.EventHelper.RemoveListener(value, S1NPC.Health.onDie);
        }

        /// <summary>
        /// Called when the NPC's inventory contents change.
        /// </summary>
        public event Action OnInventoryChanged
        {
            add
            {
                var evt = GetInventoryContentsChanged(S1NPC.Inventory);
                if (evt != null)
                    global::S1API.Utils.EventHelper.AddListener(value, evt);
            }
            remove
            {
                var evt = GetInventoryContentsChanged(S1NPC.Inventory);
                if (evt != null)
                    global::S1API.Utils.EventHelper.RemoveListener(value, evt);
            }
        }

        /// <summary>
        /// Access to the appearance customization system for visual avatar management.
        /// </summary>
        public NPCAppearance Appearance { get; private set; }

        /// <summary>
        /// Access to the movement system for controlling NPC movement and navigation.
        /// </summary>
        public NPCMovement Movement => new NPCMovement(this);
        
        /// <summary>
        /// The current <see cref="CombatBehaviour"/> instance.
        /// </summary>
        public CombatBehaviour CombatBehaviour => new CombatBehaviour(this);

        /// <summary>
        /// Access to the smoking action for this NPC.
        /// </summary>
        public NPCSmoking Smoking => _smoking ?? (_smoking = new NPCSmoking(this));

        /// <summary>
        /// Access to the spray painting action for this NPC.
        /// </summary>
        public NPCSprayPainting SprayPainting => _sprayPainting ?? (_sprayPainting = new NPCSprayPainting(this));

        /// <summary>
        /// Access to the drinking action for this NPC.
        /// </summary>
        public NPCDrinking Drinking => _drinking ?? (_drinking = new NPCDrinking(this));

        /// <summary>
        /// Access to the item holding action for this NPC.
        /// </summary>
        public NPCItemHolding ItemHolding => _itemHolding ?? (_itemHolding = new NPCItemHolding(this));

        /// <summary>
        /// Access to the dialogue system for interactive conversations and dialogue trees.
        /// </summary>
        public NPCDialogue Dialogue => _dialogue ?? (_dialogue = new NPCDialogue(this));

        /// <summary>
        /// Access to the schedule system for movement and activity scheduling.
        /// </summary>
        public NPCSchedule Schedule => _schedule ?? (_schedule = new NPCSchedule(this));

        /// <summary>
        /// Access to the inventory system for item management.
        /// </summary>
        public NPCInventory Inventory => _inventory ?? (_inventory = new NPCInventory(this));

        /// <summary>
        /// Access to the customer behavior system for NPCs that act as business customers.
        /// </summary>
        public NPCCustomer Customer => _customer ?? (_customer = new NPCCustomer(this));

        /// <summary>
        /// Access to the dealer system for NPCs that act as product distributors.
        /// </summary>
        public NPCDealer Dealer => _dealer ?? (_dealer = new NPCDealer(this));

        /// <summary>
        /// Access to the supplier system for NPCs that provide dead drops and supplier meetings.
        /// </summary>
        public NPCSupplier Supplier => _supplier ?? (_supplier = new NPCSupplier(this));

        /// <summary>
        /// Access to the relationship system for social connections and relationships with the player.
        /// </summary>
        public NPCRelationship Relationship => _relationship ?? (_relationship = new NPCRelationship(this));

        /// <summary>
        /// Gets access to the NPC's phone messaging state, events, and message helpers.
        /// </summary>
        public NPCMessaging Messaging => _messaging ?? (_messaging = new NPCMessaging(this));

        /// <summary>
        /// Sends a text message from this NPC to the players.
        /// Supports responses with callbacks for additional logic.
        /// </summary>
        /// <param name="message">The message you want the player to see. Unity rich text is allowed.</param>
        /// <param name="responses">Instances of <see cref="Response"/> to display.</param>
        /// <param name="responseDelay">The delay between when the message is sent and when the player can reply.</param>
        /// <param name="network">Whether this should propagate to all players or not.</param>
        public void SendTextMessage(string message, Response[]? responses = null, float responseDelay = 1f, bool network = true)
        {
            bool effectiveNetwork = network && _clientNetworkSpawnHydrationDepth == 0;

            if (S1NPC.MSGConversation == null)
            {
                Logger.Warning($"SendTextMessage: MSGConversation null before send for '{GetSafeNpcId()}'. Trying to ensure.");
                EnsureMessageConversationReady(resetDefaults: false);
            }

            if (S1NPC.MSGConversation == null)
            {
                Logger.Warning($"SendTextMessage: Conversation is unavailable for '{GetSafeNpcId()}'.");
                return;
            }

            S1NPC.MSGConversation.SendMessage(
                new S1Messaging.Message(
                    message,
                    S1Messaging.Message.ESenderType.Other,
                    true,
                    UnityEngine.Random.Range(int.MinValue, int.MaxValue)),
                notify: true,
                network: effectiveNetwork);
            if (responses == null || responses.Length == 0)
            {
                if (S1NPC.MSGConversation == null)
                {
                    Logger.Warning($"SendTextMessage: Conversation still null after send for '{GetSafeNpcId()}'.");
                }
                return;
            }

            if (S1NPC.MSGConversation == null)
            {
                Logger.Warning($"SendTextMessage: Unable to show responses because MSGConversation is null for '{GetSafeNpcId()}'.");
                return;
            }

            S1NPC.MSGConversation.ClearResponses();
            Responses.Clear();

            List<S1Messaging.Response> responsesList = new List<S1Messaging.Response>();

            foreach (Response response in responses)
            {
                Responses.Add(response);
                responsesList.Add(response.S1Response);
            }

            S1NPC.MSGConversation.ShowResponses(
                responsesList,
                responseDelay,
                effectiveNetwork
            );
        }

        /// <summary>
        /// Set's whether the text message can be deleted/hidden
        /// </summary>
        public bool ConversationCanBeHidden
        {
            get => NPCDataAccess.GetConversationCanBeHidden(S1NPC);
            set => NPCDataAccess.ApplyConversationCanBeHidden(S1NPC, value);
        }

        /// <summary>
        /// Sets an equippable item for the NPC.
        /// </summary>
        /// <param name="assetPath">The asset path to the equippable item. Use <see cref="Equippables.EquippablePath"/> or <see cref="Equippables.Misc"/>.</param>
        public void SetEquippable(string assetPath) => S1NPC.SetEquippable_Return(assetPath);

        /// <summary>
        /// Sets an equippable item for the NPC using a type-safe path.
        /// </summary>
        /// <param name="equippablePath">Use <see cref="Equippables.EquippablePath"/> (e.g. <see cref="Equippables.EquippablePath.Phone_Lowered"/>).</param>
        public void SetEquippable(Equippables.EquippablePath equippablePath) => SetEquippable(equippablePath.ResourcePath);

        /// <summary>
        /// Gets the instance of an NPC.
        /// Supports base NPCs as well as other mod NPCs.
        /// For base NPCs, <see cref="NPCs"/>.
        /// </summary>
        /// <typeparam name="T">The NPC class to get the instance of.</typeparam>
        /// <returns></returns>
        public static NPC? Get<T>() where T : NPC =>
            All.FirstOrDefault(npc => npc.GetType() == typeof(T)) ?? TryCreateBuiltInWrapper(typeof(T));

        /// <summary>
        /// Gets the instance of an NPC by its unique ID.
        /// Supports already-materialized wrappers and built-in NPC wrappers.
        /// </summary>
        /// <param name="npcId">The S1 NPC ID.</param>
        /// <returns>The matching wrapper instance, or null if one could not be resolved.</returns>
        public static NPC? Get(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId))
                return null;

            var existing = All.FirstOrDefault(npc =>
                string.Equals(npc.ID, npcId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            var builtInType = Internal.Utils.NPCTypeUtils.TryResolveBuiltInNPCType(npcId);
            return builtInType == null ? null : TryCreateBuiltInWrapper(builtInType);
        }

        /// <summary>
        /// INTERNAL: Lazily creates built-in NPC wrappers (base-game NPCs) when they haven't been materialized yet.
        /// Avoids instantiating custom mod NPCs to prevent unintended prefab creation during ConfigurePrefab.
        /// </summary>
        /// <param name="npcType">Target NPC wrapper type.</param>
        /// <returns>The wrapper instance if created; otherwise, null.</returns>
        private static NPC? TryCreateBuiltInWrapper(System.Type npcType)
        {
            if (npcType == null || npcType.Assembly != typeof(NPC).Assembly || npcType.IsAbstract)
                return null;

            try
            {
#if IL2CPPMELON
                // Allow non-public constructors on Il2Cpp
                return (NPC?)System.Activator.CreateInstance(npcType, true);
#else
                var ctor = npcType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                if (ctor != null)
                    return (NPC?)ctor.Invoke(null);
#endif
            }
            catch
            {
                // Swallow exceptions to avoid breaking prefab configuration when base NPCs are not yet available.
            }

            return null;
        }

        #endregion

        // Internal members used by S1API.
        // Please do not attempt to use these members!
        #region Internal Members

        /// <summary>
        /// INTERNAL: Reference to the NPC on the S1 side.
        /// </summary>
        internal readonly S1NPCs.NPC S1NPC;

        /// <summary>
        /// INTERNAL: Whether relationship data has been applied from prefab.
        /// Used to determine when NPC initialization is complete.
        /// </summary>
        internal bool RelationshipDataAppliedFromPrefab => _relationshipDataAppliedFromPrefab;

        /// <summary>
        /// INTERNAL: Whether native relationship state was hydrated from save data.
        /// </summary>
        internal bool RelationshipLoadedFromSave { get; private set; }

        /// <summary>
        /// INTERNAL: Applies relationship data from a native save payload and retains it through activation.
        /// </summary>
        internal void LoadRelationshipFromSave(
            float relationDelta,
            bool unlocked,
            S1Relation.NPCRelationData.EUnlockType unlockType)
        {
            if (!NPCRelationshipPersistencePolicy.IsValidSavedDelta(relationDelta))
                return;

            _loadedRelationshipDelta = relationDelta;
            _loadedRelationshipUnlocked = unlocked;
            _loadedRelationshipUnlockType = unlockType;
            RelationshipLoadedFromSave = true;
            RestoreLoadedRelationship();
        }

        /// <summary>
        /// INTERNAL: Constructor used for base game NPCs.
        /// </summary>
        /// <param name="npc">Reference to a base game NPC.</param>
        internal NPC(S1NPCs.NPC npc)
        {
            S1NPC = npc;
            gameObject = npc.gameObject;
            _runtimeAvatar = npc.Avatar ?? gameObject.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
            Appearance = new NPCAppearance(this, _runtimeAvatar);
            IsCustomNPC = false;
            All.Add(this);
        }

        /// <summary>
        /// INTERNAL: Initializes the responses that have been added / loaded
        /// </summary>
        internal override void CreateInternal()
        {
            // Assign responses to our tracked responses
            if (S1NPC?.MSGConversation != null && S1NPC.MSGConversation.currentResponses != null)
            {
                foreach (S1Messaging.Response s1Response in S1NPC.MSGConversation.currentResponses)
                {
                    Response response = new Response(s1Response) { Label = s1Response.label, Text = s1Response.text };
                    Responses.Add(response);
                    OnResponseLoaded(response);
                }
            }

            // CRITICAL: Ensure inventory is properly initialized with unlocked slots before spawn
            // This must happen after construction but before the NPC is fully active
            // Without this, base game code calling AddCash will fail with "CanItemFit() returned false"
            try
            {
                Inventory.EnsureInitialized();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPC] CreateInternal: Failed to ensure inventory initialized for '{GetSafeNpcId()}': {ex.Message}");
            }

            // Apply random inventory defaults AFTER slots are initialized
            // This ensures slots exist when adding startup cash/items
            try
            {
                ApplyRandomInventoryDefaults();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPC] CreateInternal: Failed to apply inventory defaults for '{GetSafeNpcId()}': {ex.Message}");
            }

            base.CreateInternal();
        }

        internal void CreateFromClientNetworkSpawn()
        {
            _clientNetworkSpawnHydrationDepth++;
            try
            {
                CreateInternal();
            }
            finally
            {
                _clientNetworkSpawnHydrationDepth--;
            }
        }

        internal override void SaveInternal(string folderPath, ref List<string> extraSaveables)
        {
            // Trim whitespace from SaveFolderName to prevent trailing spaces in folder paths
            // This handles cases where lastName is empty but hasLastName is still true
            string saveFolderName = GetGameMember<string>(S1NPC, "SaveFolderName")?.Trim() ?? "UnknownNPC";
            string npcPath = Path.Combine(folderPath, saveFolderName);
            base.SaveInternal(npcPath, ref extraSaveables);
        }
        #endregion

        // Private members used by the NPC class.
        // Please do not attempt to use these members!
        #region Initialization Helpers

        private void InitializeHealthComponent()
        {
            if (S1NPC.Health == null)
                SetGameMember(S1NPC, "Health", gameObject.GetComponent<S1NPCs.NPCHealth>());
            if (S1NPC.Health == null)
                SetGameMember(S1NPC, "Health", gameObject.AddComponent<S1NPCs.NPCHealth>());

            var health = S1NPC.Health;
            if (health == null)
                return;

            if (health.onDie == null)
                health.onDie = new UnityEvent();
            if (health.onKnockedOut == null)
                health.onKnockedOut = new UnityEvent();

            // S1NPC.Health.Invincible = true;
            SetGameMember(health, "MaxHealth", 100f);
        }

        private void InitializeAwarenessComponent()
        {
            if (S1NPC.Awareness == null)
            {
                SetGameMember(S1NPC, "Awareness", gameObject.GetComponentInChildren<S1NPCs.NPCAwareness>(true));
                if (S1NPC.Awareness == null)
                {
                    GameObject awarenessObject = new GameObject("NPCAwareness");
                    awarenessObject.transform.SetParent(gameObject.transform, false);
                    SetGameMember(S1NPC, "Awareness", awarenessObject.AddComponent<S1NPCs.NPCAwareness>());
                }
            }

            var awareness = S1NPC.Awareness;
            if (awareness == null)
                return;

            if (awareness.onExplosionHeard == null)
                awareness.onExplosionHeard = new UnityEvent<S1Noise.NoiseEvent>();
            if (awareness.onGunshotHeard == null)
                awareness.onGunshotHeard = new UnityEvent<S1Noise.NoiseEvent>();
            if (awareness.onHitByCar == null)
                awareness.onHitByCar = new UnityEvent<S1Vehicles.LandVehicle>();
            if (awareness.onNoticedDrugDealing == null)
                awareness.onNoticedDrugDealing = new UnityEvent<S1PlayerScripts.Player>();
            if (awareness.onNoticedGeneralCrime == null)
                awareness.onNoticedGeneralCrime = new UnityEvent<S1PlayerScripts.Player>();
            if (awareness.onNoticedPettyCrime == null)
                awareness.onNoticedPettyCrime = new UnityEvent<S1PlayerScripts.Player>();
            if (awareness.onNoticedPlayerViolatingCurfew == null)
                awareness.onNoticedPlayerViolatingCurfew = new UnityEvent<S1PlayerScripts.Player>();
            if (awareness.onNoticedSuspiciousPlayer == null)
                awareness.onNoticedSuspiciousPlayer = new UnityEvent<S1PlayerScripts.Player>();

            if (awareness.Listener == null)
                awareness.Listener = gameObject.GetComponent<S1Noise.Listener>() ?? gameObject.AddComponent<S1Noise.Listener>();

            if (S1NPC.Responses == null)
            {
                SetGameMember(S1NPC, "Responses", gameObject.GetComponentInChildren<S1Responses.NPCResponses>(true));
                if (S1NPC.Responses == null)
                {
                    GameObject responsesObject = new GameObject("NPCResponses");
                    responsesObject.transform.SetParent(gameObject.transform, false);
                    SetGameMember(S1NPC, "Responses", responsesObject.AddComponent<S1Responses.NPCResponses_Civilian>());
                }
            }

            // Ensure civilians get the civilian responses implementation so impacts provoke reactions
            if (!(S1NPC.Responses is S1Responses.NPCResponses_Civilian))
            {
                try
                {
                    var respGO = S1NPC.Responses != null ? S1NPC.Responses.gameObject : gameObject;
                    if (S1NPC.Responses != null)
                        UnityEngine.Object.Destroy(S1NPC.Responses);
                    var civilian = respGO.AddComponent<S1Responses.NPCResponses_Civilian>();
                    SetGameMember(S1NPC, "Responses", civilian);
                }
                catch { }
            }

            // Always link Awareness.Responses to the valid NPCResponses_Civilian component
            // This ensures the reference is properly set after instantiation and component creation/replacement
            if (S1NPC.Responses is S1Responses.NPCResponses_Civilian validCivilianResponses)
            {
                awareness.Responses = validCivilianResponses;
            }
        }

        private void InitializeBehaviourComponents()
        {
            if (S1NPC.Behaviour == null)
            {
                S1Behaviour.NPCBehaviour existing =
                    gameObject.GetComponentInChildren<S1Behaviour.NPCBehaviour>(true);
                if (existing == null)
                {
                    GameObject behaviourObject = new GameObject("NPCBehaviour");
                    behaviourObject.transform.SetParent(gameObject.transform, false);
                    existing = behaviourObject.AddComponent<S1Behaviour.NPCBehaviour>();
                }

                SetGameMember(S1NPC, "Behaviour", existing);
            }

            // Ensure NPCActions exists so Responses can trigger behaviours like CallPolice/Face/Combat
            if (S1NPC.Actions == null)
            {
                var existing = S1NPC.GetComponentInChildren<S1NPCs.Actions.NPCActions>(true);
                if (existing == null)
                {
                    GameObject actionsObject = new GameObject("NPCActions");
                    actionsObject.transform.SetParent(gameObject.transform, false);
                    existing = actionsObject.AddComponent<S1NPCs.Actions.NPCActions>();
                }
                SetGameMember(S1NPC, "Actions", existing);
            }

            var npcBehaviour = S1NPC.Behaviour;
            if (npcBehaviour == null)
                return;

            if (npcBehaviour.CoweringBehaviour == null)
            {
                S1Behaviour.CoweringBehaviour existing = npcBehaviour.GetComponentInChildren<S1Behaviour.CoweringBehaviour>(true);
                if (existing == null)
                {
                    GameObject coweringObject = new GameObject("CowingBehaviour");
                    coweringObject.transform.SetParent(npcBehaviour.transform, false);
                    existing = coweringObject.AddComponent<S1Behaviour.CoweringBehaviour>();
                }

                npcBehaviour.CoweringBehaviour = existing;
            }

            npcBehaviour.HeavyFlinchBehaviour =
                npcBehaviour.GetComponentInChildren<S1Behaviour.HeavyFlinchBehaviour>(true);

            if (npcBehaviour.FleeBehaviour == null)
            {
                S1Behaviour.FleeBehaviour existing = npcBehaviour.GetComponentInChildren<S1Behaviour.FleeBehaviour>(true);
                if (existing == null)
                {
                    GameObject fleeObject = new GameObject("FleeBehaviour");
                    fleeObject.transform.SetParent(npcBehaviour.transform, false);
                    existing = fleeObject.AddComponent<S1Behaviour.FleeBehaviour>();
                }

                npcBehaviour.FleeBehaviour = existing;
            }

            // Ensure other behaviours used by Customer flows exist
            if (npcBehaviour.GenericDialogueBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.GenericDialogueBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("GenericDialogueBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.GenericDialogueBehaviour>();
                }
                npcBehaviour.GenericDialogueBehaviour = existing;
            }

            if (npcBehaviour.RequestProductBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.RequestProductBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("RequestProductBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.RequestProductBehaviour>();
                }
                npcBehaviour.RequestProductBehaviour = existing;
            }

            if (npcBehaviour.CallPoliceBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.CallPoliceBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("CallPoliceBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.CallPoliceBehaviour>();
                }
                npcBehaviour.CallPoliceBehaviour = existing;
            }

            if (npcBehaviour.CombatBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Combat.CombatBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("CombatBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Combat.CombatBehaviour>();
                }
                npcBehaviour.CombatBehaviour = existing;
            }

            if (npcBehaviour.CombatBehaviour != null)
            {
                npcBehaviour.CombatBehaviour.TargetVelocityTracker ??=
                    gameObject.GetComponentInChildren<S1Tools.SmoothedVelocityCalculator>(true);
                npcBehaviour.CombatBehaviour.VirtualPunchWeapon ??=
                    gameObject.GetComponentInChildren<S1AvatarEquipping.AvatarMeleeWeapon>(true);
            }

            if (npcBehaviour.StationaryBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.StationaryBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("StationaryBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.StationaryBehaviour>();
                }
                npcBehaviour.StationaryBehaviour = existing;
            }

            if (npcBehaviour.FaceTargetBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.FaceTargetBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("FaceTargetBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.FaceTargetBehaviour>();
                }
                npcBehaviour.FaceTargetBehaviour = existing;
            }

            if (npcBehaviour.ConsumeProductBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.ConsumeProductBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("ConsumeProductBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.ConsumeProductBehaviour>();
                }
                npcBehaviour.ConsumeProductBehaviour = existing;
            }


            if (npcBehaviour.ConsumeProductBehaviour.onConsumeDone == null)
                npcBehaviour.ConsumeProductBehaviour.onConsumeDone = new UnityEvent();

            // UnconsciousBehaviour and DeadBehaviour are required by NPC.IsConscious
            // which is checked during pickpocketing and other interactions
            if (npcBehaviour.UnconsciousBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.UnconsciousBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("UnconsciousBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.UnconsciousBehaviour>();
                }
                npcBehaviour.UnconsciousBehaviour = existing;
            }

            if (npcBehaviour.DeadBehaviour == null)
            {
                var existing = npcBehaviour.GetComponentInChildren<S1Behaviour.DeadBehaviour>(true);
                if (existing == null)
                {
                    GameObject go = new GameObject("DeadBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    existing = go.AddComponent<S1Behaviour.DeadBehaviour>();
                }
                npcBehaviour.DeadBehaviour = existing;
            }

            // Bridge NPCScheduleManager into the Behaviour priority stack. Without this,
            // custom NPCs' schedules run entirely outside the priority system: nothing ever
            // pauses NPCScheduleManager when a higher-priority behaviour (dialogue, combat,
            // flee, etc.) activates, so e.g. NPCs keep walking their schedule while a
            // conversation is in progress. ScheduleBehaviour.Priority is left at the lowest
            // value so every other behaviour preempts it, matching vanilla NPC prefabs where
            // the schedule is the baseline/fallback activity.
            var scheduleManager = gameObject.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (scheduleManager != null)
            {
                var scheduleBehaviour = npcBehaviour.GetComponentInChildren<S1Behaviour.ScheduleBehaviour>(true);
                if (scheduleBehaviour == null)
                {
                    GameObject go = new GameObject("ScheduleBehaviour");
                    go.transform.SetParent(npcBehaviour.transform, false);
                    scheduleBehaviour = go.AddComponent<S1Behaviour.ScheduleBehaviour>();
                }
                scheduleBehaviour.schedule = scheduleManager;
                scheduleBehaviour.Priority = -1;
            }

            RepairBehaviourOwnership(gameObject, S1NPC);

            foreach (S1Behaviour.Behaviour behaviour in
                     npcBehaviour.GetComponentsInChildren<S1Behaviour.Behaviour>(true))
            {
                if (behaviour == null)
                    continue;

                behaviour.onEnable ??= new UnityEvent();
                behaviour.onDisable ??= new UnityEvent();
                behaviour.onBegin ??= new UnityEvent();
                behaviour.onEnd ??= new UnityEvent();
            }

            RefreshBehaviourStack();
        }

        private static void RepairBehaviourOwnership(GameObject prefabRoot, S1NPCs.NPC? npc)
        {
            if (prefabRoot == null || npc == null)
                return;

            S1Behaviour.NPCBehaviour behaviourManager =
                prefabRoot.GetComponentInChildren<S1Behaviour.NPCBehaviour>(true);
            if (behaviourManager == null)
                return;

            SetGameMember(npc, "Behaviour", behaviourManager);

#if IL2CPPMELON
            behaviourManager.Npc = npc;
#else
            NpcBehaviourOwnerField.SetValue(behaviourManager, npc);
#endif

            foreach (S1Behaviour.Behaviour behaviour in
                     behaviourManager.GetComponentsInChildren<S1Behaviour.Behaviour>(true))
            {
                if (behaviour == null)
                    continue;

#if IL2CPPMELON
                behaviour.beh = behaviourManager;
#else
                BehaviourOwnerField.SetValue(behaviour, behaviourManager);
#endif
            }
        }

        private void RefreshBehaviourStack()
        {
            var behaviours = S1NPC.Behaviour.GetComponentsInChildren<S1Behaviour.Behaviour>(true);
#if IL2CPPMELON
            var ordered = new System.Collections.Generic.List<S1Behaviour.Behaviour>();
            foreach (S1Behaviour.Behaviour behaviour in behaviours)
            {
                if (behaviour != null)
                    ordered.Add(behaviour);
            }

            ordered.Sort((left, right) => right.Priority.CompareTo(left.Priority));
            var stack = new Il2CppSystem.Collections.Generic.List<S1Behaviour.Behaviour>();
            foreach (S1Behaviour.Behaviour behaviour in ordered)
                stack.Add(behaviour);
            S1NPC.Behaviour.behaviourStack = stack;
#else
            var stack = new System.Collections.Generic.List<S1Behaviour.Behaviour>(
                behaviours.Where(behaviour => behaviour != null).OrderByDescending(behaviour => behaviour.Priority));

            FieldInfo stackField = AccessTools.Field(typeof(S1Behaviour.NPCBehaviour), "behaviourStack")
                ?? throw new MissingFieldException(typeof(S1Behaviour.NPCBehaviour).FullName, "behaviourStack");
            stackField.SetValue(S1NPC.Behaviour, stack);
#endif
        }

        private void InitializeVisionComponents()
        {
            if (S1NPC.Awareness == null)
                return;

            if (S1NPC.Awareness.VisionCone == null)
            {
                S1Vision.VisionCone existing = gameObject.GetComponentInChildren<S1Vision.VisionCone>(true);
                if (existing == null)
                {
                    GameObject visionObject = new GameObject("VisionCone");
                    visionObject.transform.SetParent(gameObject.transform, false);
                    existing = visionObject.AddComponent<S1Vision.VisionCone>();
                }

                S1NPC.Awareness.VisionCone = existing;
            }

            // Ensure a broad set of visual states are observed so civilians react like base NPCs
            var dsoi = S1NPC.Awareness.VisionCone.DefaultStatesOfInterest;
            if (dsoi == null)
            {
                dsoi = new List<S1Vision.VisionCone.StateContainer>();
                S1NPC.Awareness.VisionCone.DefaultStatesOfInterest = dsoi;
            }
            if (dsoi.Count == 0)
            {
                S1Vision.EVisualState[] defaults = new S1Vision.EVisualState[]
                {
                    S1Vision.EVisualState.PettyCrime,
                    S1Vision.EVisualState.DrugDealing,
                    S1Vision.EVisualState.Vandalizing,
                    S1Vision.EVisualState.Pickpocketing,
                    S1Vision.EVisualState.DisobeyingCurfew,
                    S1Vision.EVisualState.Wanted,
                    S1Vision.EVisualState.Suspicious,
                    S1Vision.EVisualState.Brandishing,
                    S1Vision.EVisualState.DischargingWeapon
                };
                for (int i = 0; i < defaults.Length; i++)
                {
                    dsoi.Add(new S1Vision.VisionCone.StateContainer { state = defaults[i] });
                }
            }

            if (S1NPC.Awareness.VisionCone.QuestionMarkPopup == null)
            {
                S1WorkspacePopup.WorldspacePopup popup =
                    gameObject.GetComponent<S1WorkspacePopup.WorldspacePopup>() ??
                    gameObject.AddComponent<S1WorkspacePopup.WorldspacePopup>();
                S1NPC.Awareness.VisionCone.QuestionMarkPopup = popup;
            }
        }

        private void InitializeInteractables()
        {
            if (gameObject.GetComponentInChildren<S1Interaction.InteractableObject>(true) == null)
                gameObject.AddComponent<S1Interaction.InteractableObject>();
        }

        private void InitializeInventoryComponent()
        {
            if (S1NPC.Inventory == null)
                SetGameMember(S1NPC, "Inventory", gameObject.GetComponentInChildren<S1NPCs.NPCInventory>(true) ?? gameObject.AddComponent<S1NPCs.NPCInventory>());

            if (GetInventoryPickpocketInteractable(S1NPC.Inventory) == null)
            {
                S1Interaction.InteractableObject? talkInteractable = GetPrimaryInteractable();
                S1Interaction.InteractableObject[] interactables = gameObject.GetComponentsInChildren<S1Interaction.InteractableObject>(true);
                S1Interaction.InteractableObject? pickpocket = interactables.FirstOrDefault(io => io != null && io != talkInteractable);
                if (pickpocket == null)
                    pickpocket = gameObject.AddComponent<S1Interaction.InteractableObject>();

                SetInventoryPickpocketInteractable(S1NPC.Inventory, pickpocket);
            }

            // NOTE: ApplyRandomInventoryDefaults() moved to CreateInternal() to ensure slots exist before adding items/cash
        }

        private void InitializeRelationshipData()
        {
            string npcId = GetSafeNpcId();
            bool relationDataExisted = S1NPC.RelationData != null;

            if (S1NPC.RelationData == null)
            {
                S1NPC.RelationData = new S1Relation.NPCRelationData();
            }

            // Ensure the relation data is bound to this NPC and initialized
            try
            {
                if (S1NPC.RelationData != null)
                {
                    S1NPC.RelationData.Init(S1NPC);
                }
                else
                {
                    Logger.Warning($"[NPC] InitializeRelationshipData: RelationData is still null after creation attempt for NPC '{npcId}'");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[NPC] InitializeRelationshipData: Exception during Init() for NPC '{npcId}': {ex.Message}");
                Logger.Error($"[NPC] InitializeRelationshipData: Stack trace: {ex.StackTrace}");
                /* ignore: base game will handle in its own lifecycle if not ready */
            }

            _relationship?.EnsureUnlockedHook();
        }

        private void ApplyRandomInventoryDefaults()
        {
            if (!HasRandomInventoryDefaultsForType(GetType()))
                return;

            try
            {
                string npcId = GetSafeNpcId();
                var data = BuildRandomInventoryDefaultsForType(GetType());
                if (data == null)
                    return;

                var inventory = S1NPC.Inventory;
                if (inventory == null)
                    return;

                // Apply random cash configuration
                if (data.RandomCashMin.HasValue || data.RandomCashMax.HasValue)
                {
                    SetInventoryMember(inventory, "RandomCash", true);
                    if (data.RandomCashMin.HasValue)
                        SetInventoryMember(inventory, "RandomCashMin", data.RandomCashMin.Value);
                    if (data.RandomCashMax.HasValue)
                        SetInventoryMember(inventory, "RandomCashMax", data.RandomCashMax.Value);
                    
                    // Actually add cash to inventory immediately (not just configure for later)
                    // Check if slots exist (Awake has run) before adding cash
                    bool slotsExist = inventory.ItemSlots != null && inventory.ItemSlots.Count > 0;
                    if (slotsExist)
                    {
                        // Check if NPC already has cash (e.g., from save data or previous initialization)
                        int existingCash = 0;
                        try
                        {
                            if (inventory.ItemSlots != null)
                            {
                                for (int i = 0; i < inventory.ItemSlots.Count; i++)
                                {
                                    var slot = inventory.ItemSlots[i];
                                    if (slot?.ItemInstance != null)
                                    {
                                        var itemDef = slot.ItemInstance.Definition;
                                        if (itemDef != null && itemDef.ID == "cash")
                                        {
                                            // Try to get cash value
                                            var valueProp = slot.ItemInstance.GetType().GetProperty("Value");
                                            if (valueProp != null)
                                            {
                                                var value = valueProp.GetValue(slot.ItemInstance);
                                                if (value is int intValue)
                                                    existingCash += intValue;
                                                else if (value is float floatValue)
                                                    existingCash += (int)floatValue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' failed to check existing cash: {ex.Message}");
                        }
                        
                        // Only add cash if NPC doesn't already have any (prevents duplicate cash on reload)
                        if (existingCash == 0)
                        {
                            int minCash = data.RandomCashMin ?? 0;
                            int maxCash = data.RandomCashMax ?? 100;
                            if (maxCash > 0)
                            {
                                int cashAmount = UnityEngine.Random.Range(minCash, maxCash + 1);
                                if (cashAmount > 0)
                                {
                                    try
                                    {
                                        // Use the proper AddCash method on NPCInventory, which handles chunking and network sync correctly
                                        var addCashMethod = inventory.GetType().GetMethod("AddCash", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                        if (addCashMethod != null)
                                        {
                                            addCashMethod.Invoke(inventory, new object[] { (float)cashAmount });
                                        }
                                        else
                                        {
                                            // Fallback to manual insertion if AddCash method not found
                                            var moneyManager = S1DevUtilities.NetworkSingleton<S1Money.MoneyManager>.Instance;
                                            if (moneyManager != null)
                                            {
                                                var cashInstance = moneyManager.GetCashInstance(cashAmount);
                                                if (cashInstance != null)
                                                {
                                                    inventory.InsertItem(cashInstance, network: true);
                                                }
                                                else
                                                {
                                                    Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' MoneyManager.GetCashInstance returned null for amount {cashAmount}");
                                                }
                                            }
                                            else
                                            {
                                                Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' MoneyManager not available to create cash");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' failed to add cash: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }

                // Apply ClearInventoryEachNight setting
                if (data.ClearInventoryEachNight.HasValue)
                    SetInventoryMember(inventory, "ClearInventoryEachNight", data.ClearInventoryEachNight.Value);

                // Apply startup items
                // Always insert items directly and clear StartupItems immediately to prevent Awake from processing them
                // This ensures items are only inserted once, regardless of when ApplyRandomInventoryDefaults() runs
                if (data.StartupItems != null && data.StartupItems.Count > 0)
                {
                    var startupItemsList = new List<S1Items.ItemDefinition>();
                    foreach (var itemId in data.StartupItems)
                    {
                        var def = S1Registry.GetItem(itemId);
                        if (def != null)
                            startupItemsList.Add(def);
                    }

                    if (startupItemsList.Count > 0)
                    {
                        // Check if Awake has already run (slots exist)
                        bool slotsExist = inventory.ItemSlots != null && inventory.ItemSlots.Count > 0;
                        
                        if (slotsExist)
                        {
                            // Awake has run, insert items directly
                            int insertedCount = 0;
                            foreach (var itemDef in startupItemsList)
                            {
                                try
                                {
                                    var itemInstance = itemDef.GetDefaultInstance();
                                    if (itemInstance != null)
                                    {
                                        inventory.InsertItem(itemInstance, network: false);
                                        insertedCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' failed to insert startup item {itemDef.ID}: {ex.Message}");
                                }
                            }
                            
                            // Clear StartupItems to prevent Awake from processing them again
#if (IL2CPPMELON)
                            SetInventoryMember(inventory, "StartupItems", new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>(0));
#else
                            SetInventoryMember(inventory, "StartupItems", Array.Empty<S1Items.ItemDefinition>());
#endif
                        }
                        else
                        {
                            // Awake hasn't run yet, set StartupItems for Awake to process
                            // But first check if StartupItems is already set to avoid overwriting
                            bool startupItemsAlreadySet = false;
                            try
                            {
                                var startupItems = GetInventoryMember(inventory, "StartupItems");
                                if (startupItems != null)
                                {
#if (IL2CPPMELON)
                                    var il2cppArray = startupItems as Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>;
                                    startupItemsAlreadySet = il2cppArray != null && il2cppArray.Length > 0;
#else
                                    var array = startupItems as S1Items.ItemDefinition[];
                                    startupItemsAlreadySet = array != null && array.Length > 0;
#endif
                                }
                            }
                            catch
                            {
                                startupItemsAlreadySet = false;
                            }

                            if (!startupItemsAlreadySet)
                            {
#if (IL2CPPMELON)
                                SetInventoryMember(inventory, "StartupItems", new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<S1Items.ItemDefinition>(startupItemsList.ToArray()));
#else
                                SetInventoryMember(inventory, "StartupItems", startupItemsList.ToArray());
#endif
                            }
                        }
                    }
                    else
                    {
                        Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{npcId}' had StartupItems definitions but none resolved from registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: '{GetSafeNpcId()}' failed with exception: {ex.Message}");
                Logger.Warning($"[NPC] ApplyRandomInventoryDefaults: Stack trace: {ex.StackTrace}");
            }
        }

        private S1Interaction.InteractableObject? GetPrimaryInteractable()
        {
            return gameObject.GetComponentInChildren<S1Interaction.InteractableObject>(true);
        }

        private void InitializeNetworkBehaviours()
        {
            NetworkBehaviour[] behaviours = gameObject.GetComponentsInChildren<NetworkBehaviour>(true);
            foreach (NetworkBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                try
                {
                    // Defer network initialization to FishNet's spawn process. Do not initialize manually.
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to initialize network behaviour {behaviour.GetType().Name}: {ex.Message}");
                }
            }
        }

        private void AssignPersistentGuid(string? npcId)
        {
            Guid guid = NPCPersistentIds.TryGetGuid(npcId, out Guid persistentGuid)
                ? persistentGuid
                : Guid.NewGuid();
            S1NPC.BakedGUID = guid.ToString();
        }

        internal void RegisterPersistentGuidForContractLoad()
        {
            if (!Guid.TryParse(S1NPC.BakedGUID, out Guid guid))
                return;

            try
            {
#if IL2CPPMELON
                S1NPC.SetGUID(new Il2CppSystem.Guid(guid.ToString()));
#else
                S1NPC.SetGUID(guid);
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"[NPC] Failed to register persistent GUID for '{GetSafeNpcId()}': {ex.Message}");
            }
        }

        private void RestoreLoadedRelationship()
        {
            if (!_loadedRelationshipDelta.HasValue || S1NPC.RelationData == null)
                return;

            S1NPC.RelationData.SetRelationship(_loadedRelationshipDelta.Value, false);
            if (_loadedRelationshipUnlocked)
            {
                S1NPC.RelationData.Unlock(
                    _loadedRelationshipUnlockType,
                    notify: false);
            }
        }

        private void RestoreRuntimeAvatarAppearance()
        {
            if (_runtimeAvatar == null)
                return;

            SetGameMember(S1NPC, "Avatar", _runtimeAvatar);
            Appearance.ApplyToAvatar(_runtimeAvatar);
        }

#endregion

        #region Private Members

        internal readonly bool IsCustomNPC;

#if IL2CPPMELON
        private const bool DefaultRequiresRegionUnlocked = true;
#else
        private readonly FieldInfo _requiresRegionUnlockedField = AccessTools.Field(typeof(S1NPCs.NPC), "RequiresRegionUnlocked");
#endif

        private readonly MethodInfo _unsettleMethod = AccessTools.Method(typeof(S1NPCs.NPC), "SetUnsettled");
        private readonly MethodInfo _removePanicMethod = AccessTools.Method(typeof(S1NPCs.NPC), "RemovePanicked");

        private NPCDialogue? _dialogue;
        private NPCSchedule? _schedule;
        private NPCInventory? _inventory;
        private NPCCustomer? _customer;
        private NPCDealer? _dealer;
        private NPCSupplier? _supplier;
        private NPCRelationship? _relationship;
        private NPCMessaging? _messaging;
        private NPCSmoking? _smoking;
        private NPCSprayPainting? _sprayPainting;
        private NPCDrinking? _drinking;
        private NPCItemHolding? _itemHolding;
        private bool _relationshipDataAppliedFromPrefab;
        private float? _loadedRelationshipDelta;
        private bool _loadedRelationshipUnlocked;
        private S1Relation.NPCRelationData.EUnlockType _loadedRelationshipUnlockType;
        private readonly System.Collections.Generic.List<DealerRecommendationSubscription> _recommendationSubscriptions =
            new System.Collections.Generic.List<DealerRecommendationSubscription>();

        /// <summary>
        /// Spawns this NPC's instance on the server using FishNet so it is networked.
        /// No-ops on clients.
        /// </summary>
        private void TrySpawnNetworkInstance()
        {
            try
            {
                var nm = InstanceFinder.NetworkManager;
                if (nm != null && !nm.IsServer)
                    return;

                NetworkObject no = gameObject.GetComponent<NetworkObject>() ?? gameObject.AddComponent<NetworkObject>();
                NPCNetworkBootstrap.RegisterPendingNetworkSpawn(this, no, 0.3f, 0.6f);
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to queue NPC network spawn: {ex.Message}");
            }
        }

        internal bool PrepareForNetworkSpawn()
        {
            try
            {
                if (!TryValidateNativeAwakeReferences(out string diagnostic))
                {
                    Logger.Error(
                        $"[NPC] Refusing to spawn custom NPC '{GetSafeNpcId()}' because its native Awake reference graph is invalid: {diagnostic}");
                    return false;
                }

                NPCDataAccess.PrepareForRuntime(S1NPC);
                RestoreLoadedRelationship();

                var customer = gameObject.GetComponent<S1Economy.Customer>();
                if (customer != null)
                {
                    Customer.EnsureCustomer();
                }

                var supplier = gameObject.GetComponent<S1Economy.Supplier>();
                if (supplier != null && IsSupplierType(GetType()))
                {
                    var defaults = BuildSupplierDefaultsForType(GetType());
                    if (defaults != null)
                        TryApplySupplierDefaults(supplier, defaults);

                    SupplierRuntimeCoordinator.EnsureReady(supplier, ID, defaults);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to prepare NPC runtime data before spawn: {ex.Message}");
                return false;
            }
        }

        private bool TryValidateNativeAwakeReferences(out string diagnostic)
        {
            var missing = new System.Collections.Generic.List<string>();

            if (gameObject.GetComponent<S1NPCs.NPCInventory>() == null)
                missing.Add(nameof(S1NPCs.NPCInventory));
            if (gameObject.GetComponent<S1NPCs.NPCHealth>() == null)
                missing.Add(nameof(S1NPCs.NPCHealth));
            if (gameObject.GetComponent<S1Vision.EntityVisibility>() == null)
                missing.Add(nameof(S1Vision.EntityVisibility));
            if (gameObject.GetComponent<S1NPCs.NPCMovement>() == null)
                missing.Add(nameof(S1NPCs.NPCMovement));
            if (gameObject.GetComponent<S1Equipping.NetworkedEquipper>() == null)
                missing.Add(nameof(S1Equipping.NetworkedEquipper));

            var activeAvatar =
                gameObject.GetComponentInChildren<S1AvatarFramework.Avatar>();
            var anyAvatar =
                gameObject.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
            if (activeAvatar == null)
            {
                missing.Add(
                    anyAvatar == null
                        ? "Avatar"
                        : "Avatar(active)");
            }
            else if (activeAvatar.HeadBone == null)
            {
                missing.Add("Avatar.HeadBone");
            }
            else if (activeAvatar.HeadBone.GetComponentInChildren<S1VoiceOver.VOEmitter>() == null)
            {
                missing.Add(nameof(S1VoiceOver.VOEmitter));
            }

            if (gameObject.GetComponentInChildren<S1Dialogue.DialogueHandler>() == null)
                missing.Add(nameof(S1Dialogue.DialogueHandler));
            if (gameObject.GetComponentInChildren<S1NPCs.NPCAwareness>() == null)
                missing.Add(nameof(S1NPCs.NPCAwareness));
            if (gameObject.GetComponentInChildren<S1Responses.NPCResponses>() == null)
                missing.Add(nameof(S1Responses.NPCResponses));
            if (gameObject.GetComponentInChildren<S1NPCs.Actions.NPCActions>() == null)
                missing.Add(nameof(S1NPCs.Actions.NPCActions));
            if (gameObject.GetComponentInChildren<S1Behaviour.NPCBehaviour>() == null)
                missing.Add(nameof(S1Behaviour.NPCBehaviour));

            diagnostic = missing.Count == 0
                ? string.Empty
                : string.Join(", ", missing);
            return missing.Count == 0;
        }

        internal void FinalizeNetworkSpawn()
        {
            try
            {
                RestoreLoadedRelationship();

                // Ensure NPCAwareness.Responses reference is valid after spawn
                // Network spawning can sometimes break component references
                if (S1NPC.Awareness != null && S1NPC.Responses is S1Responses.NPCResponses_Civilian validResponses)
                {
                    S1NPC.Awareness.Responses = validResponses;
                }

                // Suppliers follow the native lifecycle: they remain hidden while idle and
                // only become visible when the meeting flow places them at a supplier location.
                S1NPC.SetVisible(ShouldBeVisibleAfterSpawn(), networked: false);

                EnsureMessageConversationReady(resetDefaults: false);
                
                // If we're the server, also broadcast to clients via RPC after a delay
                // This ensures the NPC is fully spawned before the RPC is sent
                if (InstanceFinder.IsServer)
                {
                    MelonCoroutines.Start(DelayedVisibilityRPC());
                }

                // If this prefab included a Customer, ensure it's initialized; otherwise, respect non-customer NPCs
                try
                {
                    if (IsCustomNPC)
                    {
                        var hasCustomer = gameObject.GetComponent<S1Economy.Customer>() != null;
                        if (hasCustomer)
                        {
                            // Customer.EnsureCustomer();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to ensure Customer on NPC: {ex.Message}");
                }

                // If this NPC type was registered as a dealer, ensure dealer initialization and category badge
                try
                {
                    if (IsCustomNPC && IsDealerType(GetType()))
                    {
                        Dealer.EnsureDealer();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to ensure Dealer on NPC: {ex.Message}");
                }

                try
                {
                    if (IsCustomNPC && IsSupplierType(GetType()))
                    {
                        Supplier.EnsureSupplier();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to ensure Supplier on NPC: {ex.Message}");
                }

                // Apply any planned schedule specs for this NPC type now that the instance exists
                try
                {
                    var t = GetType();
                    if (TypeToSchedulePlan.TryGetValue(t, out var planned) && planned is { Count: > 0 })
                    {
                        for (int i = 0; i < planned.Count; i++)
                        {
                            var spec = planned[i];
                            if (spec != null)
                            {
                                try
                                {
                                    spec.ApplyTo(Schedule);
                                }
                                catch (Exception specEx)
                                {
                                    Logger.Error($"Failed to apply schedule spec {i} ({spec.GetType().Name}) for NPC type {t.Name}: {specEx.Message}");
                                    Logger.Error($"Stack trace: {specEx.StackTrace}");
                                }
                            }
                        }
                        Schedule.InitializeActions();
                        Schedule.EnforceState();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to apply planned schedule for NPC type {GetType().Name}: {ex.Message}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                }

                // Apply per-type relationship defaults after base fields are present, unless
                // the native relationship payload has already been hydrated from save data.
                if (NPCRelationshipPersistencePolicy.ShouldApplyDefaults(
                    RelationshipLoadedFromSave))
                {
                    try
                    {
                        // First, try to apply relationship data from NPCPrefabIdentity (prefab-level, takes precedence)
                        var identity = gameObject.GetComponent<NPCPrefabIdentity>();
                        bool appliedFromPrefab = false;
                        if (identity != null)
                        {
                            var rel = S1NPC.RelationData;
                            if (rel != null)
                            {
                                bool alreadyUnlocked = rel.Unlocked;
                                identity.ApplyRelationshipDataTo(S1NPC, preserveUnlockState: alreadyUnlocked);
                                appliedFromPrefab = true;
                                _relationshipDataAppliedFromPrefab = true;
                                // Verify unlock state wasn't accidentally overwritten
                                if (alreadyUnlocked && !rel.Unlocked)
                                {
                                    Logger.Warning($"[NPC] FinalizeNetworkSpawn: WARNING - Unlock state was lost for NPC '{GetSafeNpcId()}' after applying prefab defaults. Restoring...");
                                    rel.Unlock(S1Relation.NPCRelationData.EUnlockType.DirectApproach, notify: false);
                                }
                            }
                            else
                            {
                                Logger.Warning($"[Relationship Data] FinalizeNetworkSpawn: RelationData is null for NPC '{GetSafeNpcId()}'");
                            }
                        }
                        
                        // If no prefab-level data was applied, fall back to type-based defaults
                        if (!appliedFromPrefab)
                        {
                            var t = GetType();
                            if (TypeToRelationshipDefaults.TryGetValue(t, out var relCfg) && relCfg != null)
                            {
                                var builder = new NPCRelationshipDataBuilder();
                                relCfg(builder);
                                var rel = S1NPC.RelationData;
                                if (rel != null)
                                {
                                    // Preserve unlock state if NPC is already unlocked (may have been loaded from save)
                                    // This prevents overwriting unlock state if FinalizeNetworkSpawn runs after load
                                    bool alreadyUnlocked = rel.Unlocked;
                                    builder.ApplyTo(rel, S1NPC, preserveUnlockState: alreadyUnlocked);
                                    _relationshipDataAppliedFromPrefab = true; // Mark as applied even if from type defaults
                                    // Verify unlock state wasn't accidentally overwritten
                                    if (alreadyUnlocked && !rel.Unlocked)
                                    {
                                        Logger.Warning($"[NPC] FinalizeNetworkSpawn: WARNING - Unlock state was lost for NPC '{GetSafeNpcId()}' after applying defaults. Restoring...");
                                        // Restore unlock state using stored unlock type, or default to DirectApproach
                                        rel.Unlock(S1Relation.NPCRelationData.EUnlockType.DirectApproach, notify: false);
                                    }
                                }
                                else
                                {
                                    Logger.Warning($"[NPC] FinalizeNetworkSpawn: RelationData is null for NPC '{GetSafeNpcId()}' - cannot apply defaults!");
                                }
                            }
                            else
                            {
                                // No relationship defaults to apply, mark as complete
                                _relationshipDataAppliedFromPrefab = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[NPC] FinalizeNetworkSpawn: Failed to apply relationship defaults for NPC '{GetSafeNpcId()}': {ex.Message}");
                        Logger.Error($"[NPC] FinalizeNetworkSpawn: Stack trace: {ex.StackTrace}");
                    }
                }
                else
                {
                    // NPC was loaded from save, relationship data is already initialized, mark as complete
                    _relationshipDataAppliedFromPrefab = true;
                }

                _relationship?.EnsureUnlockedHook();

                // Note: Random inventory defaults are applied in InitializeInventoryComponent, not here
                // to avoid duplicate item insertion when StartupItems is processed by NPCInventory.Awake

                try
                {
                    ApplyDealerRecommendationDefaults();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to apply dealer recommendation defaults for '{GetSafeNpcId()}': {ex.Message}");
                }

                // Apply spawn position for this NPC type (always applied, regardless of save state)
                try
                {
                    var t = GetType();
                    if (TypeToSpawnPosition.TryGetValue(t, out var spawnData))
                    {
                        Position = spawnData.position;
                        Transform.rotation = spawnData.rotation;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to apply spawn position: {ex.Message}");
                }

                try
                {
                    if (CrossType.Is(S1NPC, out S1Economy.Supplier supplier))
                        SupplierRuntimeCoordinator.ReconcileDeliveryUnlock(supplier);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[S1API] Failed to restore supplier delivery state: {ex.Message}");
                }

                // Check if all custom NPCs are now ready (finalized)
                // This sets the CustomNpcsReady flag once all custom NPCs have been spawned and finalized
                FinalizedCustomNpcTypes.Add(GetType());
                ReconcileAllCustomNpcRelationshipConnections();
                CheckAndSetCustomNpcsReady();
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to finalize NPC after spawn: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if all custom NPCs have been finalized and sets the CustomNpcsReady flag.
        /// This is called from FinalizeNetworkSpawn to signal when all custom NPCs are ready.
        /// </summary>
        internal static void CheckAndSetCustomNpcsReady()
        {
            // If already ready, no need to check again
            if (CustomNpcsReady)
                return;

            try
            {
                // Get all custom NPC types that should exist
                var customNpcTypes = Internal.Utils.ReflectionUtils.GetDerivedClasses<NPC>()
                    .Where(t => t != null && !t.IsAbstract && t.Assembly != typeof(NPC).Assembly)
                    .ToList();

                if (customNpcTypes.Count == 0)
                    return;

                bool allTypesFinalized = customNpcTypes.All(
                    type => FinalizedCustomNpcTypes.Contains(type));

                if (allTypesFinalized)
                    CustomNpcsReady = true;
            }
            catch (Exception ex)
            {
                Logger.Warning($"[NPC] Failed to check CustomNpcsReady status: {ex.Message}");
            }
        }

        internal static void ReconcileAllCustomNpcRelationshipConnections()
        {
            var configuredNpcs = All
                .Where(wrapper => wrapper?.S1NPC != null && wrapper.IsCustomNPC)
                .Select(wrapper => new
                {
                    Wrapper = wrapper,
                    Identity = wrapper.gameObject?.GetComponent<NPCPrefabIdentity>()
                })
                .Where(entry => entry.Identity != null && !string.IsNullOrWhiteSpace(entry.Wrapper.ID))
                .ToArray();

            var declarations =
                new global::System.Collections.Generic.Dictionary<
                    string,
                    global::System.Collections.Generic.IReadOnlyList<string>>(
                    StringComparer.OrdinalIgnoreCase);
            foreach (var entry in configuredNpcs)
            {
                if (!declarations.TryAdd(
                        entry.Wrapper.ID,
                        entry.Identity!.GetConfiguredConnectionIds()))
                {
                    Logger.Warning(
                        $"[Relationship Data] Duplicate custom NPC ID '{entry.Wrapper.ID}' " +
                        "prevents deterministic connection reconciliation for that duplicate.");
                }
            }

            foreach (var entry in configuredNpcs)
            {
                try
                {
                    global::System.Collections.Generic.IReadOnlyList<string> connectionIds =
                        NPCRelationshipGraphPolicy.BuildUndirectedConnectionIds(
                            entry.Wrapper.ID,
                            declarations);
                    if (connectionIds.Count == 0
                        && !entry.Identity!.HasConfiguredConnections())
                        continue;

                    var builder = new NPCRelationshipDataBuilder();
                    builder.WithConnectionsById(connectionIds);
                    var relationData = entry.Wrapper.S1NPC.RelationData;
                    if (relationData == null)
                    {
                        Logger.Warning(
                            $"[Relationship Data] RelationData is null for " +
                            $"'{entry.Wrapper.GetSafeNpcId()}'; skipping connection reconciliation.");
                        continue;
                    }

                    builder.ApplyTo(
                        relationData,
                        entry.Wrapper.S1NPC,
                        preserveUnlockState: true);
                }
                catch (Exception ex)
                {
                    Logger.Warning(
                        $"[Relationship Data] Could not reconcile connections for " +
                        $"'{entry.Wrapper.GetSafeNpcId()}': {ex.Message}");
                }
            }
        }

        private void ApplyDealerRecommendationDefaults()
        {
            var npcType = GetType();
            if (!IsDealerType(npcType))
            {
                ClearDealerRecommendationHooks();
                return;
            }

            var defaults = GetBuiltDealerDefaultsForType(npcType);
            if (defaults == null || defaults.Recommendations.Count == 0)
            {
                ClearDealerRecommendationHooks();
                return;
            }

            ClearDealerRecommendationHooks();

            for (int i = 0; i < defaults.Recommendations.Count; i++)
            {
                var recommendation = defaults.Recommendations[i];
                if (recommendation == null || string.IsNullOrWhiteSpace(recommendation.CustomerId) ||
                    !recommendation.RecommendationTrigger.HasValue)
                {
                    continue;
                }

                var customerNpc = Get(recommendation.CustomerId);
                if (customerNpc == null)
                {
                    Logger.Warning(
                        $"[S1API] Could not resolve recommending customer '{recommendation.CustomerId}' for dealer '{GetSafeNpcId()}'.");
                    continue;
                }

                switch (recommendation.RecommendationTrigger.Value)
                {
                    case DealerRecommendationBuilder.Trigger.DealCompleted:
                    {
                        Action handler = () =>
                        {
                            try
                            {
                                if (!Dealer.HasBeenRecommended())
                                    customerNpc.Customer.RecommendDealer(Dealer);
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning(
                                    $"[S1API] Failed to auto-recommend dealer '{GetSafeNpcId()}' from customer '{customerNpc.ID}': {ex.Message}");
                            }
                        };

                        customerNpc.Customer.OnDealCompleted += handler;
                        _recommendationSubscriptions.Add(new DealerRecommendationSubscription(
                            customerNpc,
                            recommendation.RecommendationTrigger.Value,
                            handler));
                        break;
                    }
                }
            }
        }

        private void ClearDealerRecommendationHooks()
        {
            // Joined-client wrappers are created without running mod constructors so the
            // constructor cannot spawn a duplicate GameObject. Their field initializers are
            // therefore absent, and cleanup must tolerate an uninitialized subscription list.
            if (_recommendationSubscriptions == null)
                return;

            for (int i = 0; i < _recommendationSubscriptions.Count; i++)
            {
                var subscription = _recommendationSubscriptions[i];

                switch (subscription.RecommendationTrigger)
                {
                    case DealerRecommendationBuilder.Trigger.DealCompleted:
                        subscription.Customer.Customer.OnDealCompleted -= subscription.Handler;
                        break;
                }
            }

            _recommendationSubscriptions.Clear();
        }

        internal void CleanupRuntimeHooks()
        {
            ClearDealerRecommendationHooks();
            _messaging?.Cleanup();
        }

        private sealed class DealerRecommendationSubscription
        {
            internal NPC Customer { get; }
            internal DealerRecommendationBuilder.Trigger RecommendationTrigger { get; }
            internal Action Handler { get; }

            internal DealerRecommendationSubscription(
                NPC customer,
                DealerRecommendationBuilder.Trigger recommendationTrigger,
                Action handler)
            {
                Customer = customer;
                RecommendationTrigger = recommendationTrigger;
                Handler = handler;
            }
        }

        /// <summary>
        /// Coroutine to send visibility RPC after a delay to ensure the NPC is fully spawned.
        /// </summary>
        internal bool ShouldBeVisibleAfterSpawn()
        {
            bool isSupplier = IsCustomNPC && IsSupplierType(GetType());
            bool isSupplierMeeting = false;
            if (isSupplier)
            {
                S1Economy.Supplier? supplier =
                    gameObject.GetComponent<S1Economy.Supplier>();
                isSupplierMeeting =
                    supplier?.Status == S1Economy.Supplier.ESupplierStatus.Meeting;
            }

            return ResolveSpawnVisibility(
                IsPhysical,
                isSupplier,
                isSupplierMeeting);
        }

        internal static bool ResolveSpawnVisibility(
            bool isPhysical,
            bool isSupplier,
            bool isSupplierMeeting) =>
            isPhysical && (!isSupplier || isSupplierMeeting);

        internal static bool ShouldApplyLoadedVisibilityBeforeSpawn(
            bool isPhysical,
            bool isSupplier) =>
            isPhysical && !isSupplier;

        private IEnumerator DelayedVisibilityRPC()
        {
            // Wait a frame to ensure the NPC is fully initialized and spawned
            yield return null;
            
            // Additional small delay to ensure network spawn is complete
            yield return new WaitForSeconds(0.1f);
            
            try
            {
                if (S1NPC != null && S1NPC.gameObject != null)
                {
                    // Broadcast visibility to clients via RPC
                    S1NPC.SetVisible(
                        ShouldBeVisibleAfterSpawn(),
                        networked: true);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[S1API] Failed to send visibility RPC for NPC '{GetSafeNpcId()}': {ex.Message}");
            }
        }

        // Removed: component index hack. FishNet assigns NetworkBehaviour indices at spawn based on
        // the behaviours present on the NetworkObject. Forcing ComponentIndex causes mismatches.

#endregion

        /// <summary>
        /// Pre-creates an <see cref="S1NPCs.NPCScheduleManager"/> and all non-abstract <see cref="S1NPCsSchedules.NPCAction"/>s
        /// under the provided prefab root so FishNet assigns stable NetworkBehaviour indices at spawn.
        /// All created action GameObjects are inactive by default; mods can enable/configure them later.
        /// </summary>
        private static void EnsureScheduleActionsOnPrefab(GameObject prefabRoot)
        {
            if (prefabRoot == null)
                return;

            // Ensure manager container
            S1NPCs.NPCScheduleManager existingMgr = prefabRoot.GetComponentInChildren<S1NPCs.NPCScheduleManager>(true);
            if (existingMgr == null)
            {
                GameObject mgrGo = new GameObject("NPCSchedule");
                mgrGo.transform.SetParent(prefabRoot.transform, false);
                existingMgr = mgrGo.AddComponent<S1NPCs.NPCScheduleManager>();
            }

            // Collect action types via reflection when possible
            System.Collections.Generic.List<S1Type> actionTypes = new System.Collections.Generic.List<S1Type>();
#if IL2CPPMELON
            S1Type baseType = Il2CppType.Of<S1NPCsSchedules.NPCAction>();
#else
            S1Type baseType = typeof(S1NPCsSchedules.NPCAction);
#endif
            try
            {
                var asm = baseType.Assembly;
                if (asm != null)
                {
                    var types = asm.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        S1Type t = types[i];
                        if (t == null)
                            continue;
                        if (t.IsAbstract)
                            continue;
                        if (baseType.IsAssignableFrom(t))
                            actionTypes.Add(t);
                    }
                }
            }
            catch
            {
                // Fallback: known concrete action types by simple names in the schedules namespace
                string ns = baseType.Namespace;
                string[] known = new string[]
                {
                    "NPCSignal_WalkToLocation",
                    "NPCSignal_UseVendingMachine",
                    "NPCSignal_UseATM",
                    "NPCSignal_DriveToCarPark",
                    "NPCEvent_StayInBuilding",
                    "NPCEvent_Sit",
                    "NPCEvent_LocationDialogue",
                    "NPCEvent_LocationBasedAction",
                    "NPCEvent_Conversate"
                };
                for (int i = 0; i < known.Length; i++)
                {
                    string full = string.IsNullOrEmpty(ns) ? known[i] : (ns + "." + known[i]);
#if IL2CPPMELON
                    S1Type t = Il2CppSystem.Type.GetType(full);
#else
                    S1Type t = System.Type.GetType(full);
#endif
                    if (t != null && !t.IsAbstract && baseType.IsAssignableFrom(t))
                        actionTypes.Add(t);
                }
            }

            // Add one inactive instance of each action type if not already present
            for (int i = 0; i < actionTypes.Count; i++)
            {
                S1Type t = actionTypes[i];
                if (t == null)
                    continue;

                var existing = existingMgr.GetComponentInChildren(t, true);
                if (existing != null)
                    continue;

                GameObject go = new GameObject(t.Name);
                go.transform.SetParent(existingMgr.transform, false);
                var comp = go.AddComponent(t);

                // Best-effort wire internal references so actions have context even while inactive
                try
                {
                    var baseNpc = prefabRoot.GetComponent<S1NPCs.NPC>();
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(comp, "npc", baseNpc);
                    Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(comp, "schedule", existingMgr);
                }
                catch { }
                go.SetActive(false);
            }
        }
    }
}
