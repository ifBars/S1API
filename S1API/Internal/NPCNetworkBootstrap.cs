using System;
using System.Collections;
using MelonLoader;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using S1API.Entities;
using UnityEngine;

namespace S1API.Internal
{
    /// <summary>
    /// INTERNAL: Centralizes network readiness tracking for NPC spawning and pre-registers NPC prefabs
    /// on both server and client when the Main scene initializes.
    /// </summary>
    internal static class NPCNetworkBootstrap
    {
        private static bool mainSceneInitialized;
        private static bool networkObserved;
        private static bool clientsReady;
        private static float lastStateLog;

        public static bool ClientsReadyToSpawnNpcs => mainSceneInitialized && (clientsReady || !HasRemoteClients());

        public static void ResetFlags()
        {
            mainSceneInitialized = false;
            networkObserved = false;
            clientsReady = false;
            lastStateLog = 0f;
        }

        public static void OnMainSceneInitialized()
        {
            ResetFlags();
            mainSceneInitialized = true;

            // Pre-register NPC prefabs on both server and client before any spawn occurs
            try { NPC.PreRegisterAllNpcPrefabs(); } catch { }

            // Start readiness monitor
            MelonCoroutines.Start(ReadinessMonitor());
        }

        private static IEnumerator ReadinessMonitor()
        {
            NetworkManager nm = null;
            float start = Time.realtimeSinceStartup;
            // Wait for NetworkManager
            while (nm == null)
            {
                nm = InstanceFinder.NetworkManager;
                if (nm != null) break;
                if (Time.realtimeSinceStartup - start > 10f) yield break;
                yield return new WaitForSeconds(0.1f);
            }

            // Observe connection/scene events where possible
            if (!networkObserved)
            {
                networkObserved = true;
                try
                {
                    var cm = nm.ClientManager;
                    var sm = nm.ServerManager;
                    var scened = nm.SceneManager;

                    // Periodically evaluate readiness
                    MelonCoroutines.Start(PeriodicEvaluate());
                }
                catch { }
            }

            // Fallback: timeout-based readiness
            float maxWait = 12f;
            while (!clientsReady && (Time.realtimeSinceStartup - start) < maxWait)
            {
                EvaluateReadiness();
                yield return new WaitForSeconds(0.25f);
            }
            EvaluateReadiness();
        }

        private static IEnumerator PeriodicEvaluate()
        {
            while (true)
            {
                EvaluateReadiness();
                yield return new WaitForSeconds(0.25f);
            }
        }

        private static void EvaluateReadiness()
        {
            var nm = InstanceFinder.NetworkManager;
            if (nm == null)
            {
                clientsReady = false;
                return;
            }

            // If no remote clients, we can proceed
            if (!HasRemoteClients())
            {
                clientsReady = true;
                return;
            }

            bool inMain = false;
            try { inMain = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main"; } catch { }

            // Heuristic: consider clients ready once transport is running and online scene is Main
            bool serverUp = nm.IsServer && nm.ServerManager != null && nm.ServerManager.Started;
            bool clientUpIfHost = !nm.IsServer || nm.IsClient;

            clientsReady = inMain && serverUp && clientUpIfHost;

            // Log once every few seconds if still not ready
            if (!clientsReady)
            {
                if (Time.realtimeSinceStartup - lastStateLog > 3f)
                {
                    MelonLogger.Msg($"[S1API] Waiting for client readiness: inMain={inMain} serverUp={serverUp} hasRemote={HasRemoteClients()} hostClient={nm.IsClient}");
                    lastStateLog = Time.realtimeSinceStartup;
                }
            }
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
                int count = 0;
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
    }
}


