using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
#if MONOMELON
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using FishNet.Connection;
using FishNet.Managing.Object;
using FishNet.Object;
#else
using Il2CppFishNet;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Managing.Scened;
using Il2CppFishNet.Transporting;
using Il2CppFishNet.Connection;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
#endif
using S1API.Entities;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// INTERNAL: Centralizes network readiness tracking for NPC spawning and pre-registers NPC prefabs
    /// on both server and client when the Main scene initializes.
    /// </summary>
    internal static class NPCNetworkBootstrap
    {
        private static readonly Logging.Log Logger = new Logging.Log("NPCNetworkBootstrap");
        private static bool mainSceneInitialized;
        private static bool networkObserved;
        private static bool clientsReady;
        private static bool connectionObjectsReady;
        private static bool prefabsWarmupScheduled;
        private static float mainSceneInitTime;
        private static readonly List<PendingSpawn> PendingSpawns = new List<PendingSpawn>();
        private static bool readinessLogInitialized;
        private static bool lastLogInMain;
        private static bool lastLogServerUp;
        private static bool lastLogClientUp;
        private static bool lastLogRemoteReady;
        private static bool lastLogHasRemote;
        private static bool lastLogClientsReady;
        private static float lastStateLogTime;
        private static bool pendingSpawnBlockedLogged;

        public static bool ClientsReadyToSpawnNpcs =>
            mainSceneInitialized &&
            NPC.PrefabsConfiguredForLocalProcess &&
            connectionObjectsReady &&
            clientsReady;

        internal static void ResetFlags()
        {
            mainSceneInitialized = false;
            networkObserved = false;
            clientsReady = false;
            connectionObjectsReady = false;
            prefabsWarmupScheduled = false;
            mainSceneInitTime = 0f;
            PendingSpawns.Clear();
            readinessLogInitialized = false;
            pendingSpawnBlockedLogged = false;
        }

        internal static void EnsurePrefabsWarmup()
        {
            if (NPC.PrefabsConfiguredForLocalProcess)
                return;

            if (prefabsWarmupScheduled)
                return;

            prefabsWarmupScheduled = true;
            MelonCoroutines.Start(PrefabWarmupCoroutine());
        }

        internal static void RegisterPendingNetworkSpawn(NPC? owner, NetworkObject netObject, float activationDelay, float spawnDelay)
        {
            if (owner == null || netObject == null)
                return;

            try
            {
                var nm = InstanceFinder.NetworkManager;
                if (nm != null && !nm.IsServer)
                    return;

                EnsurePrefabsWarmup();

                for (var i = PendingSpawns.Count - 1; i >= 0; i--)
                {
                    var entry = PendingSpawns[i];
                    if (entry == null || entry.NetObject == null || entry.NetObject == netObject)
                        PendingSpawns.RemoveAt(i);
                }

                var now = Time.realtimeSinceStartup;
                var activateAt = now + Math.Max(0f, activationDelay);
                var spawnAt = now + Math.Max(spawnDelay, activationDelay);
                PendingSpawns.Add(new PendingSpawn(owner, netObject, activateAt, spawnAt));

                TryProcessPendingSpawns();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1API] Failed to register pending NPC spawn: {ex.Message}");
            }
        }

        public static void OnMainSceneInitialized()
        {
            ResetFlags();
            mainSceneInitialized = true;
            mainSceneInitTime = Time.realtimeSinceStartup;

            // Ensure NPC prefabs are configured on both server and client.
            // If spawnables are not yet available, schedule a warmup to retry shortly.
            try
            {
                var nm = InstanceFinder.NetworkManager;
                var spawnables = nm?.SpawnablePrefabs;
                if (spawnables != null)
                {
                    NPC.PreRegisterAllNpcPrefabs();
                }
                else
                {
                    EnsurePrefabsWarmup();
                }
            }
            catch
            {
                EnsurePrefabsWarmup();
            }

            // Start readiness monitor (will no-op on clients)
            MelonCoroutines.Start(ReadinessMonitor());
        }

        private static IEnumerator PrefabWarmupCoroutine()
        {
            var start = Time.realtimeSinceStartup;
            var timeout = 20f;

            while (!NPC.PrefabsConfiguredForLocalProcess && (Time.realtimeSinceStartup - start) < timeout)
            {
                NetworkManager nm = null;
                PrefabObjects spawnables = null;
                try
                {
                    nm = InstanceFinder.NetworkManager;
                    spawnables = nm?.SpawnablePrefabs;
                }
                catch { }

                if (spawnables != null)
                {
                    try
                    {
                        NPC.PreRegisterAllNpcPrefabs();
                        break;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[S1API] Prefab warmup failed: {ex.Message}");
                        break;
                    }
                }

                yield return new WaitForSeconds(0.25f);
            }

            prefabsWarmupScheduled = false;
            TryProcessPendingSpawns();
        }

        private static IEnumerator ReadinessMonitor()
        {
            NetworkManager nm = null;
            var start = Time.realtimeSinceStartup;
            // Wait for NetworkManager
            while (nm == null)
            {
                nm = InstanceFinder.NetworkManager;
                if (nm != null) break;
                if (Time.realtimeSinceStartup - start > 10f) yield break;
                yield return new WaitForSeconds(0.1f);
            }

            // Only the server should evaluate readiness and process spawns
            if (nm != null && !nm.IsServer)
                yield break;

            // Observe connection/scene events where possible
            if (!networkObserved)
            {
                networkObserved = true;
                try
                {
                    // Periodically evaluate readiness
                    MelonCoroutines.Start(PeriodicEvaluate());
                }
                catch { }
            }

            // Fallback: timeout-based readiness (server-only)
            var maxWait = 12f;
            while (!clientsReady && (Time.realtimeSinceStartup - start) < maxWait)
            {
                EvaluateReadiness();
                yield return new WaitForSeconds(0.25f);
            }
            EvaluateReadiness();
            TryProcessPendingSpawns();
        }

        private static IEnumerator PeriodicEvaluate()
        {
            while (true)
            {
                EvaluateReadiness();
                TryProcessPendingSpawns();
                yield return new WaitForSeconds(0.25f);
            }
        }

        private static void EvaluateReadiness()
        {
            var nm = InstanceFinder.NetworkManager;
            if (nm == null || !NPC.PrefabsConfiguredForLocalProcess)
            {
                clientsReady = false;
                connectionObjectsReady = false;
                return;
            }

            var remoteConnectionsReady = AreRemoteConnectionsReady(nm, out var hasRemoteClients);
            connectionObjectsReady = remoteConnectionsReady;

            var inMain = false;
            try { inMain = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main"; } catch { }

            // Heuristic: consider clients ready once transport is running, online scene is Main,
            // and any remote connections have finished loading their initial object.
            var serverUp = nm.IsServer && nm.ServerManager != null && nm.ServerManager.Started;
            var clientUpIfHost = !nm.IsServer || nm.IsClient || hasRemoteClients;

            clientsReady = inMain && serverUp && clientUpIfHost && remoteConnectionsReady;
            LogReadinessState(inMain, serverUp, clientUpIfHost, remoteConnectionsReady, hasRemoteClients);
        }

        private static bool HasRemoteClients()
        {
            try
            {
                var nm = InstanceFinder.NetworkManager;
                if (nm == null) return false;
                if (!nm.IsServer) return false;
                var conns = nm.ServerManager?.Clients;
                if (conns == null) return false;
                var count = 0;
                foreach (var kvp in conns)
                {
                    var c = kvp.Value;
                    if (c == null) continue;
                    if (c.IsLocalClient) continue;
                    count++;
                }
                return count > 0;
            }
            catch { return false; }
        }

        private static bool AreRemoteConnectionsReady(NetworkManager nm, out bool hasRemoteClients)
        {
            hasRemoteClients = false;

            try
            {
                if (nm == null)
                    return false;

                var serverManager = nm.ServerManager;
                if (serverManager == null)
                    return false;

                var clients = serverManager.Clients;
                if (clients == null)
                    return true;

                foreach (var kvp in clients)
                {
                    var conn = kvp.Value;
                    if (conn == null)
                        continue;
                    if (!conn.IsValid)
                        continue;
                    if (conn.IsLocalClient)
                        continue;

                    hasRemoteClients = true;
                    if (!conn.Authenticated)
                        return false;
                    if (conn.FirstObject == null)
                        return false;
                }

                // Additional delay to ensure all clients have completed prefab configuration
                // This prevents server from spawning before clients finish ConfigurePrefab
                if (!hasRemoteClients) return true;
                var timeSinceMainScene = Time.realtimeSinceStartup - mainSceneInitTime;
                return !(timeSinceMainScene < 5f); // Wait at least 5 seconds after main scene init
            }
            catch
            {
                return false;
            }
        }

        private static void TryProcessPendingSpawns()
        {
            if (PendingSpawns.Count == 0)
            {
                pendingSpawnBlockedLogged = false;
                return;
            }

            var nm = InstanceFinder.NetworkManager;
            if (nm == null || !nm.IsServer)
                return;

            if (!ClientsReadyToSpawnNpcs)
            {
                if (!pendingSpawnBlockedLogged)
                {
                    pendingSpawnBlockedLogged = true;
                }
                return;
            }

            pendingSpawnBlockedLogged = false;

            var serverManager = nm.ServerManager;
            if (serverManager == null)
                return;
                
            var now = Time.realtimeSinceStartup;

            for (var i = PendingSpawns.Count - 1; i >= 0; i--)
            {
                var pending = PendingSpawns[i];
                if (pending == null)
                {
                    PendingSpawns.RemoveAt(i);
                    continue;
                }

                var netObject = pending.NetObject;
                var owner = pending.Owner;
                var ownerId = owner?.ID ?? owner?.gameObject?.name ?? "<unknown>";

                if (netObject == null)
                {
                    PendingSpawns.RemoveAt(i);
                    continue;
                }

                var go = netObject.gameObject;
                if (go == null || netObject.IsSpawned)
                {
                    PendingSpawns.RemoveAt(i);
                    continue;
                }

                if (!pending.ActivationApplied && now >= pending.ActivateAt)
                {
                    pending.ActivationApplied = true;
                }

                if (now < pending.SpawnAt)
                    continue;

                try
                {
                    owner?.PrepareForNetworkSpawn();
                }
                catch (Exception prepEx)
                {
                    Logger.Warning($"[NPCNetworkBootstrap] PrepareForNetworkSpawn threw for '{ownerId}': {prepEx.Message}");
                }

                try
                {
                    serverManager.Spawn(netObject, null, default(UnityEngine.SceneManagement.Scene));
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[S1API] Failed to spawn pending NPC '{go.name}': {ex.Message}");
                    Logger.Warning($"[NPCNetworkBootstrap] Spawn exception stack: {ex.StackTrace}");
                    continue;
                }

                try
                {
                    owner?.FinalizeNetworkSpawn();
                }
                catch (Exception finalizeEx)
                {
                    Logger.Warning($"[NPCNetworkBootstrap] FinalizeNetworkSpawn threw for '{ownerId}': {finalizeEx.Message}");
                }

                PendingSpawns.RemoveAt(i);
            }
        }

        private static void LogReadinessState(bool inMain, bool serverUp, bool clientUpIfHost, bool remoteConnectionsReady, bool hasRemoteClients)
        {
            var now = Time.realtimeSinceStartup;
            var stateChanged = !readinessLogInitialized ||
                               lastLogInMain != inMain ||
                               lastLogServerUp != serverUp ||
                               lastLogClientUp != clientUpIfHost ||
                               lastLogRemoteReady != remoteConnectionsReady ||
                               lastLogHasRemote != hasRemoteClients ||
                               lastLogClientsReady != clientsReady;

            if (!stateChanged && (clientsReady || !((now - lastStateLogTime) >= 5f))) return;
            readinessLogInitialized = true;
            lastLogInMain = inMain;
            lastLogServerUp = serverUp;
            lastLogClientUp = clientUpIfHost;
            lastLogRemoteReady = remoteConnectionsReady;
            lastLogHasRemote = hasRemoteClients;
            lastLogClientsReady = clientsReady;
            lastStateLogTime = now;
        }

        private sealed class PendingSpawn
        {
            internal readonly NPC Owner;
            internal readonly NetworkObject NetObject;
            internal readonly float ActivateAt;
            internal readonly float SpawnAt;
            internal bool ActivationApplied;

            internal PendingSpawn(NPC owner, NetworkObject netObject, float activateAt, float spawnAt)
            {
                Owner = owner;
                NetObject = netObject;
                ActivateAt = activateAt;
                SpawnAt = spawnAt;
                ActivationApplied = false;
            }
        }
    }
}
