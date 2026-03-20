#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1ContactsApp = Il2CppScheduleOne.UI.Phone.ContactsApp;
using S1Map = Il2CppScheduleOne.Map;
using S1Relations = Il2CppScheduleOne.UI.Relations;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1ContactsApp = ScheduleOne.UI.Phone.ContactsApp;
using S1Map = ScheduleOne.Map;
using S1Relations = ScheduleOne.UI.Relations;
using System;
using System.Collections.Generic;
#endif
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using S1API.Entities;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches and helpers related to adding and positioning custom NPCs in Contacts app.
    /// </summary>
    [HarmonyPatch]
    internal class ContactsAppPatches
    {
        private static readonly Log Logger = new Log("ContactsAppPatches");
        private static bool _startCalled;
        private static bool? _hasCustomNpcTypesCache;

        /// <summary>
        /// Resets static state so the contacts app initializes correctly across save loads.
        /// </summary>
        internal static void ResetState()
        {
            _startCalled = false;
            _hasCustomNpcTypesCache = null;
        }

        /// <summary>
        /// Checks if any custom NPC types exist (excluding S1API internal types).
        /// Caches the result to avoid repeated reflection calls.
        /// </summary>
        private static bool HasCustomNpcTypes()
        {
            if (_hasCustomNpcTypesCache.HasValue)
                return _hasCustomNpcTypesCache.Value;

            try
            {
                var baseType = typeof(NPC);
                var baseAssembly = baseType.Assembly;
                var customTypes = ReflectionUtils.GetDerivedClasses<NPC>();

                // Filter out S1API internal types - only count mod-defined NPC types
                bool hasCustom = customTypes.Any(t => t != null && t.Assembly != baseAssembly && !t.IsAbstract);
                
                _hasCustomNpcTypesCache = hasCustom;
                return hasCustom;
            }
            catch (System.Exception ex)
            {
                // On error, assume no custom NPCs to avoid breaking phone apps
                Logger.Error($"Error in HasCustomNpcTypes: {ex}");
                _hasCustomNpcTypesCache = false;
                return false;
            }
        }

        /// <summary>
        /// Intercepts ContactsApp.Start to wait for custom NPCs before initialization.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(S1ContactsApp.ContactsApp), "Start")]
        private static bool ContactsApp_Start_Prefix(S1ContactsApp.ContactsApp __instance)
        {
            // skip patch if in the tutorial
            if (SceneManager.GetActiveScene().name == "Tutorial")
                return true;

            // If no custom NPCs exist, allow original Start to run normally
            var hasCustomTypes = HasCustomNpcTypes();

            if (!hasCustomTypes)
                return true;

            if (NPCPatches.CustomNpcsReady)
            {
                var allNPCs = NPC.All.ToList();
                var customNPCs = allNPCs.Where(n => n.IsCustomNPC).ToList();
                var physicalCustomNPCs = customNPCs.Where(n => n.IsPhysical).ToList();
                if (physicalCustomNPCs.Count == 0)
                    return true;
            }

            if (!_startCalled)
            {
                _startCalled = true;
                MelonCoroutines.Start(WaitForNPCs(__instance));
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Waits for custom NPCs to be ready and present in the scene before adding relation circles.
        /// </summary>
        private static IEnumerator WaitForNPCs(S1ContactsApp.ContactsApp contactsApp)
        {
            // Wait for custom NPCs to be ready - use polling for Il2Cpp compatibility
            while (!NPCPatches.CustomNpcsReady)
            {
                yield return null;
            }

            // Check for physical custom NPCs immediately after CustomNpcsReady
            // If there are no physical NPCs, we can skip all the waiting and proceed immediately
            var allNPCs = NPC.All.ToList();
            var customNPCs = allNPCs.Where(n => n.IsCustomNPC && n.IsPhysical).ToList();

            var startMethod = typeof(S1ContactsApp.ContactsApp)
                .GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            if (startMethod == null)
            {
                Logger.Error("Couldn't find ContactsApp.Start method via reflection");
                yield break;
            }
            
            // Safety check: if no physical custom NPCs exist after waiting, skip relation circles logic
            if (customNPCs.Count == 0)
            {
                try
                {
                    startMethod.Invoke(contactsApp, null);
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"Error invoking Start: {ex}");
                }
                yield break;
            }
            
            yield return new WaitUntil((Func<bool>)(() =>
            {
                var allSceneNPCs = Object.FindObjectsOfType<S1NPCs.NPC>(true);
                return customNPCs.All(npc => allSceneNPCs.Any(sn => sn.ID == npc.ID));
            }));

            yield return new WaitUntil((Func<bool>)(() =>
            {
                return customNPCs.All(npc => npc.RelationshipDataAppliedFromPrefab);
            }));

            // Wait for all custom NPCs to have valid S1NPC references with RelationData
            yield return new WaitUntil((Func<bool>)(() =>
            {
                return customNPCs.All(npc =>
                    npc.S1NPC != null &&
                    npc.S1NPC.RelationData != null);
            }));

            // Additional frame delay to let everything settle
            yield return null;
            yield return null;

            // Wait for mugshots to be generated before creating circles
            // This ensures HeadshotImg.sprite gets the correct mugshot, not the default icon
            yield return new WaitUntil((Func<bool>)(() => NPCAppearance.MugshotsProcessingComplete));

            // Run Start() FIRST so it doesn't call LoadNPCData() on our custom circles
            try
            {
                startMethod.Invoke(contactsApp, null);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error invoking Start: {ex}");
            }

            // Add our circles AFTER Start() so they aren't processed by LoadNPCData()
            AddRelationCircles(contactsApp);
        }

        /// <summary>
        /// Creates and positions relation circles for all custom NPCs.
        /// </summary>
        private static void AddRelationCircles(S1ContactsApp.ContactsApp contactsApp)
        {
            var customNPCs = NPC.All
                .Where(n => n.IsCustomNPC &&
                            (NPC.IsCustomerType(n.GetType()) || NPC.IsDealerType(n.GetType())))
                .ToList();

            var regionUIs = contactsApp.RegionUIs.ToDictionary(r => r.Region, r => r);

            // Track IDs of circles we create so we don't use them as templates
            var createdCircleIds = new System.Collections.Generic.HashSet<string>();

            // Create circle game objects for all custom NPCs
            foreach (var npc in customNPCs)
            {
                // Validate NPC is fully initialized
                if (npc.S1NPC == null || npc.S1NPC.RelationData == null)
                {
                    Logger.Warning($"Skipping NPC {npc.ID} - S1NPC={npc.S1NPC != null}, RelationData={npc.S1NPC?.RelationData != null}");
                    continue;
                }

                if (!regionUIs.TryGetValue(npc.S1NPC.Region, out var regionUI) || regionUI?.Container == null)
                {
                    Logger.Warning($"Skipping NPC {npc.ID} - region {npc.S1NPC.Region} not found in regionUIs");
                    continue;
                }

                var existing = regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true)
                    .FirstOrDefault(c => c.AssignedNPC_ID == npc.S1NPC.ID);
                if (existing != null)
                    continue;

                // Find a base game template - exclude circles we've already created
                var allCirclesInRegion = regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true);

                var template = allCirclesInRegion
                    .FirstOrDefault(c => !createdCircleIds.Contains(c.AssignedNPC_ID))
                    ?? contactsApp.CirclesContainer.GetComponentInChildren<S1Relations.RelationCircle>(true);

                if (template == null)
                    continue;

                var go = Object.Instantiate(template.gameObject, regionUI.Container);
                go.name = npc.ID;
                var circle = go.GetComponent<S1Relations.RelationCircle>();

                // Set ID first, then call AssignNPC to properly set up everything
                // AssignNPC handles: UnassignNPC (cleanup), event handlers, HeadshotImg, display refresh
                circle.AssignedNPC_ID = npc.S1NPC.ID;
                circle.AssignNPC(npc.S1NPC);

                // Track this circle so we don't use it as a template
                createdCircleIds.Add(npc.S1NPC.ID);

                // Call the same methods that the game's Select method calls
                var zoomMethod = typeof(S1ContactsApp.ContactsApp).GetMethod("ZoomToRect",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                var cachedCircle = circle;
                var cachedNPC = npc.S1NPC; // Cache the NPC reference directly
                circle.onClicked = (Action)delegate
                {
                    contactsApp.DetailPanel.Open(cachedNPC);

                    // Zoom to the circle
                    if (zoomMethod != null)
                        zoomMethod.Invoke(contactsApp, new object[] { cachedCircle.Rect });

                    // Update selection indicator
                    contactsApp.SelectionIndicator.position = cachedCircle.Rect.position;
                };

                EnableDealerIndicator(circle, npc);
            }

            var regionCircles =
                new System.Collections.Generic.Dictionary<S1Map.EMapRegion,
                    System.Collections.Generic.List<S1Relations.RelationCircle>>();
            foreach (var regionUI in contactsApp.RegionUIs)
            {
                var list = regionUI.Container == null
                    ? new System.Collections.Generic.List<S1Relations.RelationCircle>()
                    : regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true).ToList();
                regionCircles[regionUI.Region] = list;
            }

            // Precompute stable grid parameters from native circles only
            var customNpcIds = new System.Collections.Generic.HashSet<string>(
                customNPCs.Where(n => n.S1NPC != null).Select(n => n.S1NPC.ID));
            var regionGridParams = new System.Collections.Generic.Dictionary<S1Map.EMapRegion,
                (float spacing, Vector2 center, Vector2 right, Vector2 up, Vector4 nativeBounds)>();
            foreach (var kv in regionCircles)
            {
                var nativePositions = kv.Value
                    .Where(c => !string.IsNullOrEmpty(c.AssignedNPC_ID) && !customNpcIds.Contains(c.AssignedNPC_ID))
                    .Select(c => c.GetComponent<RectTransform>().anchoredPosition)
                    .ToList();
                if (nativePositions.Count >= 2)
                {
                    var sp = EstimateGridSpacing(nativePositions);
                    var origin = nativePositions[0];

                    // Compute native bounding box (minX, maxX, minY, maxY) with 1 grid-cell padding
                    var nMinX = nativePositions.Min(p => p.x) - sp;
                    var nMaxX = nativePositions.Max(p => p.x) + sp;
                    var nMinY = nativePositions.Min(p => p.y) - sp;
                    var nMaxY = nativePositions.Max(p => p.y) + sp;
                    var bounds = new Vector4(nMinX, nMaxX, nMinY, nMaxY);

                    regionGridParams[kv.Key] = (sp, origin, Vector2.right, Vector2.up, bounds);
                }
                else
                {
                    regionGridParams[kv.Key] = (200f, Vector2.zero, Vector2.right, Vector2.up, new Vector4(-1000, 1000, -1000, 1000));
                }
            }

            // placement order
            try
            {
                var graph = BuildGraph(customNPCs);
                var nativeIds = new System.Collections.Generic.HashSet<string>();
                foreach (var kv in regionCircles)
                foreach (var circ in kv.Value)
                    if (!string.IsNullOrEmpty(circ.AssignedNPC_ID) && !customNpcIds.Contains(circ.AssignedNPC_ID))
                        nativeIds.Add(circ.AssignedNPC_ID);

                var order = ComputeInsertionOrder(customNPCs, graph, nativeIds);

                foreach (var regionGroup in order.GroupBy(n => n.S1NPC.Region))
                {
                    if (!regionUIs.TryGetValue(regionGroup.Key, out var regionUI) || regionUI?.Container == null)
                        continue;

                    var circlesInRegion = regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true);
                    var circleById = circlesInRegion
                        .Where(c => !string.IsNullOrEmpty(c.AssignedNPC_ID))
                        .GroupBy(c => c.AssignedNPC_ID)
                        .ToDictionary(g => g.Key, g => g.First());

                    var rectTransforms = new System.Collections.Generic.Dictionary<S1Relations.RelationCircle, RectTransform>();
                    foreach (var c in circlesInRegion)
                    {
                        if (!rectTransforms.ContainsKey(c))
                            rectTransforms[c] = c.GetComponent<RectTransform>();
                    }

                    var placedIds = new System.Collections.Generic.HashSet<string>();
                    // All native circles are already placed
                    foreach (var c in circlesInRegion)
                        if (!string.IsNullOrEmpty(c.AssignedNPC_ID) && !customNpcIds.Contains(c.AssignedNPC_ID))
                            placedIds.Add(c.AssignedNPC_ID);

                    foreach (var npc in regionGroup)
                    {
                        try
                        {
                            var circle = circlesInRegion.FirstOrDefault(c => c.AssignedNPC_ID == npc.S1NPC.ID);
                            if (circle == null)
                            {
                                Logger.Warning($"  No circle found for {npc.S1NPC.ID}");
                                continue;
                            }

                            var existingEdges = GetAllEdges(circlesInRegion, rectTransforms);
                            var gp = regionGridParams[regionGroup.Key];
                            var newPos = ComputePlacement(circle, circlesInRegion, circleById, rectTransforms, existingEdges,
                                gp.spacing, gp.center, gp.right, gp.up, gp.nativeBounds, placedIds);
                            rectTransforms[circle].anchoredPosition = newPos;
                            placedIds.Add(npc.S1NPC.ID);
                        }
                        catch (System.Exception ex)
                        {
                            Logger.Error($"Error placing {npc.S1NPC?.ID}: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Error in placement phase: {ex.Message}\n{ex.StackTrace}");
            }

            // Create connection lines for custom NPCs
            CreateConnectionLines(contactsApp, customNPCs, regionUIs);
        }

        /// <summary>
        /// Creates connection lines between custom NPCs and their connections.
        /// </summary>
        private static void CreateConnectionLines(
            S1ContactsApp.ContactsApp contactsApp,
            System.Collections.Generic.List<NPC> customNPCs,
            System.Collections.Generic.Dictionary<S1Map.EMapRegion, S1ContactsApp.ContactsApp.RegionUI> regionUIs)
        {
            if (contactsApp.ConnectionPrefab == null)
            {
                Logger.Warning("ConnectionPrefab is null, cannot create connection lines");
                return;
            }

            // Track connections we've already created to avoid duplicates
            var createdConnections = new System.Collections.Generic.HashSet<string>();

            foreach (var npc in customNPCs)
            {
                if (npc.S1NPC?.RelationData?.Connections == null)
                    continue;

                if (!regionUIs.TryGetValue(npc.S1NPC.Region, out var regionUI) || regionUI?.Container == null)
                    continue;

                var circlesInRegion = regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true);
                var npcCircle = circlesInRegion.FirstOrDefault(c => c.AssignedNPC_ID == npc.S1NPC.ID);
                if (npcCircle == null)
                    continue;

#if MONOMELON
                var connections = npc.S1NPC.RelationData.Connections;
#elif IL2CPPMELON
                var connections = npc.S1NPC.RelationData.Connections._items;
#endif

                foreach (var connectedNPC in connections)
                {
                    if (connectedNPC == null)
                        continue;

                    // Skip if different region
                    if (connectedNPC.Region != npc.S1NPC.Region)
                        continue;

                    // Create a unique key for this connection (order-independent)
                    var id1 = npc.S1NPC.ID;
                    var id2 = connectedNPC.ID;
                    var connKey = string.Compare(id1, id2, System.StringComparison.Ordinal) < 0
                        ? $"{id1}->{id2}"
                        : $"{id2}->{id1}";

                    // Skip if connection already exists
                    if (createdConnections.Contains(connKey))
                        continue;

                    // Find circle for connected NPC
                    var otherCircle = circlesInRegion.FirstOrDefault(c => c.AssignedNPC_ID == connectedNPC.ID);
                    if (otherCircle == null)
                        continue;

                    // Mark connection as created
                    createdConnections.Add(connKey);

                    // Get the connections container for this region
                    var connectionsContainer = regionUI.ConnectionsContainer ?? contactsApp.ConnectionsContainer;
                    if (connectionsContainer == null)
                        continue;

                    // Create the connection line (same logic as game's Start method)
                    var connectionGO = Object.Instantiate(contactsApp.ConnectionPrefab, connectionsContainer);
                    var connectionRect = connectionGO.GetComponent<RectTransform>();

                    // Position at midpoint between circles
                    connectionRect.anchoredPosition = (otherCircle.Rect.anchoredPosition + npcCircle.Rect.anchoredPosition) / 2f;

                    // Calculate rotation
                    Vector3 direction = otherCircle.Rect.anchoredPosition - npcCircle.Rect.anchoredPosition;
                    float angle = -Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                    connectionRect.localRotation = Quaternion.Euler(0f, 0f, angle);

                    // Set length
                    connectionRect.sizeDelta = new Vector2(
                        connectionRect.sizeDelta.x,
                        Vector3.Distance(otherCircle.Rect.anchoredPosition, npcCircle.Rect.anchoredPosition));

                    connectionGO.name = $"{npc.S1NPC.ID} -> {connectedNPC.ID}";

                    // Set up click handlers for the connection endpoints
                    var startButton = connectionRect.Find("StartButton")?.GetComponent<Button>();
                    var endButton = connectionRect.Find("EndButton")?.GetComponent<Button>();

                    var zoomMethod = typeof(S1ContactsApp.ContactsApp).GetMethod("ZoomToRect",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (startButton != null && zoomMethod != null)
                    {
                        var cachedOtherRect = otherCircle.Rect;
                        startButton.onClick.AddListener((UnityEngine.Events.UnityAction)delegate
                        {
                            zoomMethod.Invoke(contactsApp, new object[] { cachedOtherRect });
                        });
                    }

                    if (endButton != null && zoomMethod != null)
                    {
                        var cachedNpcRect = npcCircle.Rect;
                        endButton.onClick.AddListener((UnityEngine.Events.UnityAction)delegate
                        {
                            zoomMethod.Invoke(contactsApp, new object[] { cachedNpcRect });
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Builds a graph of connections between custom NPCs.
        /// </summary>
        private static System.Collections.Generic.Dictionary<NPC, System.Collections.Generic.List<NPC>> BuildGraph(
            System.Collections.Generic.List<NPC> customs)
        {
            var byId = new System.Collections.Generic.Dictionary<string, NPC>();
            foreach (var n in customs)
            {
                if (n.S1NPC?.ID == null) continue;
                if (byId.ContainsKey(n.S1NPC.ID))
                {
                    Logger.Warning($"Duplicate custom NPC ID '{n.S1NPC.ID}' - each mod should use unique IDs. Skipping duplicate.");
                    continue;
                }
                byId[n.S1NPC.ID] = n;
            }
            var graph = customs.ToDictionary(n => n, n => new System.Collections.Generic.List<NPC>());
            foreach (var n in customs)
            {
                var conns = n.S1NPC.RelationData?.Connections;
                if (conns == null) continue;
                foreach (var c in conns)
                {
                    if (c == null || c.ID == null) continue;
                    if (!byId.TryGetValue(c.ID, out var other)) continue;
                    if (!graph[n].Contains(other)) graph[n].Add(other);
                    if (!graph[other].Contains(n)) graph[other].Add(n);
                }
            }

            return graph;
        }

        /// <summary>
        /// Computes optimal insertion order for custom NPCs based on their connections.
        /// </summary>
        private static System.Collections.Generic.List<NPC> ComputeInsertionOrder(
            System.Collections.Generic.List<NPC> customs,
            System.Collections.Generic.Dictionary<NPC, System.Collections.Generic.List<NPC>> graph,
            System.Collections.Generic.HashSet<string> nativeIds)
        {
            var dist = new System.Collections.Generic.Dictionary<NPC, int>();
            var q = new System.Collections.Generic.Queue<NPC>();

            foreach (var n in customs)
            {
                var conns = n.S1NPC.RelationData?.Connections;
#if MONOMELON
            var hasNative = conns != null && conns.Any(c => c != null && nativeIds.Contains(c.ID));
#elif IL2CPPMELON
                var hasNative = conns != null && conns._items.Any(c => c != null && nativeIds.Contains(c.ID));
#endif
                if (!hasNative) continue;
                dist[n] = 0;
                q.Enqueue(n);
            }

            if (q.Count == 0 && customs.Count > 0)
            {
                dist[customs[0]] = 0;
                q.Enqueue(customs[0]);
            }

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                var dcur = dist[cur];
                foreach (var nb in graph[cur])
                    if (!dist.ContainsKey(nb))
                    {
                        dist[nb] = dcur + 1;
                        q.Enqueue(nb);
                    }
            }

            return customs.OrderBy(n => dist.ContainsKey(n) ? dist[n] : int.MaxValue)
                .ThenByDescending(n => graph[n].Count)
                .ToList();
        }

        /// <summary>
        /// Computes the optimal placement position for a custom NPC's relation circle.
        /// Connected NPCs are placed on grid points near their anchors, scored by edge length,
        /// crossings, crowding, and out-of-bounds penalties to prefer visible horizontal space.
        /// Unconnected NPCs fill available grid cells within native bounds first.
        /// </summary>
        private static Vector2 ComputePlacement(
            S1Relations.RelationCircle circle,
            S1Relations.RelationCircle[] regionCircles,
            System.Collections.Generic.Dictionary<string, S1Relations.RelationCircle> circleById,
            System.Collections.Generic.Dictionary<S1Relations.RelationCircle, RectTransform> rectTransforms,
            System.Collections.Generic.List<(Vector2, Vector2)> existingEdges,
            float spacing, Vector2 gridCenter, Vector2 right, Vector2 up,
            Vector4 nativeBounds,
            System.Collections.Generic.HashSet<string> placedIds = null)
        {
            var anchors = GetConnectionPositions(circle, circleById, rectTransforms, placedIds);

            var allPositions = regionCircles
                .Select(c => rectTransforms[c].anchoredPosition)
                .ToList();

            if (allPositions.Count == 0)
                return Vector2.zero;

            var rn = right.normalized;
            var un = up.normalized;
            var minSpacing = spacing * 0.7f;

            if (anchors.Count > 0)
            {
                var targetCenter = Mean(anchors);
                var candidates = new System.Collections.Generic.List<Vector2>();

                // Project targetCenter onto grid coordinates relative to gridCenter
                var relTarget = targetCenter - gridCenter;
                var targetGx = Mathf.Round(Vector2.Dot(relTarget, rn) / spacing);
                var targetGy = Mathf.Round(Vector2.Dot(relTarget, un) / spacing);

                // Wider horizontal search (6) to use available space to the right/left
                const int searchRadiusX = 6;
                const int searchRadiusY = 3;
                var seen = new System.Collections.Generic.HashSet<long>();
                for (var gx = -searchRadiusX; gx <= searchRadiusX; gx++)
                {
                    for (var gy = -searchRadiusY; gy <= searchRadiusY; gy++)
                    {
                        var ix = (long)(targetGx + gx);
                        var iy = (long)(targetGy + gy);
                        var key = ix * 100000L + iy;
                        if (!seen.Add(key))
                            continue;

                        var candidate = gridCenter + (targetGx + gx) * spacing * rn + (targetGy + gy) * spacing * un;
                        if (IsPositionFree(candidate, allPositions, minSpacing))
                            candidates.Add(candidate);
                    }
                }

                if (candidates.Count > 0)
                {
                    return PickBestPosition(candidates, anchors, allPositions, existingEdges, spacing, nativeBounds);
                }

                // Fallback: snap targetCenter to nearest free grid point
                var fallback = SnapToLattice(targetCenter, gridCenter, spacing, right, up);
                return fallback;
            }

            // Unconnected NPCs - place on grid edge so they don't look connected to neighbors
            // Prefer positions just outside the cluster (1 cell beyond native bounds)
            var boundsMinGx = Mathf.FloorToInt((nativeBounds.x - gridCenter.x) / spacing);
            var boundsMaxGx = Mathf.CeilToInt((nativeBounds.y - gridCenter.x) / spacing);
            var boundsMinGy = Mathf.FloorToInt((nativeBounds.z - gridCenter.y) / spacing);
            var boundsMaxGy = Mathf.CeilToInt((nativeBounds.w - gridCenter.y) / spacing);

            var centroid = Mean(allPositions);
            var unconnectedCandidates = new System.Collections.Generic.List<Vector2>();

            // Scan native bounds + 2 cell padding to find all free positions
            const int padding = 2;
            for (var gy = boundsMinGy - padding; gy <= boundsMaxGy + padding; gy++)
            {
                for (var gx = boundsMinGx - padding; gx <= boundsMaxGx + padding; gx++)
                {
                    var candidate = gridCenter + gx * spacing * rn + gy * spacing * un;
                    if (IsPositionFree(candidate, allPositions, minSpacing))
                        unconnectedCandidates.Add(candidate);
                }
            }

            if (unconnectedCandidates.Count > 0)
            {
                // Score all candidates, then pick randomly from the top-scoring ones
                // to spread unconnected NPCs around different edges
                var scored = new System.Collections.Generic.List<(Vector2 pos, float score)>();

                foreach (var candidate in unconnectedCandidates)
                {
                    var adjacentCount = 0;
                    foreach (var pos in allPositions)
                    {
                        var dist = Vector2.Distance(candidate, pos);
                        if (dist < spacing * 1.2f && dist > 0.01f)
                            adjacentCount++;
                    }

                    // Must have exactly 1 adjacent neighbor — skip positions with 0 (isolated) or 3+ (interior)
                    if (adjacentCount == 0)
                        continue;

                    // Penalize positions surrounded by neighbors (they look connected)
                    var neighborPenalty = adjacentCount <= 1 ? 0f : adjacentCount * 100f;

                    // Penalize positions near existing connection lines (they look connected)
                    var linePenalty = 0f;
                    var circleRadius = spacing * 0.4f;
                    foreach (var (from, to) in existingEdges)
                    {
                        // Point-to-segment distance
                        var ab = to - from;
                        var ac = candidate - from;
                        var t = Mathf.Clamp01(Vector2.Dot(ac, ab) / Vector2.Dot(ab, ab));
                        var closest = from + t * ab;
                        var distToLine = Vector2.Distance(candidate, closest);
                        if (distToLine < circleRadius)
                            linePenalty += (circleRadius - distToLine) / circleRadius * 500f;
                    }

                    // Prefer closer edge positions over far-flung ones
                    var distToCentroid = Vector2.Distance(candidate, centroid);

                    var score = neighborPenalty + linePenalty + distToCentroid * 2f;
                    scored.Add((candidate, score));
                }

                scored.Sort((a, b) => a.score.CompareTo(b.score));

                // Gather all candidates within a small tolerance of the best score
                var bestScore = scored[0].score;
                var topCandidates = scored
                    .Where(s => s.score <= bestScore + 50f)
                    .Select(s => s.pos)
                    .ToList();

                // Use a deterministic seed based on circle ID so placement is stable per-NPC
                var hash = circle.AssignedNPC_ID?.GetHashCode() ?? 0;
                var idx = Mathf.Abs(hash) % topCandidates.Count;
                return topCandidates[idx];
            }

            return gridCenter;
        }

        /// <summary>
        /// Gets the positions of all connected NPCs for a given circle.
        /// </summary>
        private static System.Collections.Generic.List<Vector2> GetConnectionPositions(
            S1Relations.RelationCircle circle,
            System.Collections.Generic.Dictionary<string, S1Relations.RelationCircle> circleById,
            System.Collections.Generic.Dictionary<S1Relations.RelationCircle, RectTransform> rectTransforms,
            System.Collections.Generic.HashSet<string> placedIds = null)
        {
            var positions = new System.Collections.Generic.List<Vector2>();
            var seen = new System.Collections.Generic.HashSet<string>();
            var myId = circle.AssignedNPC_ID;

            // Outgoing connections: NPCs this circle connects to (only if already placed)
            var conns = circle.AssignedNPC?.RelationData?.Connections;
            if (conns != null)
            {
                foreach (var conn in conns)
                {
                    if (conn == null || string.IsNullOrEmpty(conn.ID))
                        continue;

                    // Only use as anchor if the target has been placed (or no tracking)
                    if (placedIds != null && !placedIds.Contains(conn.ID))
                        continue;

                    if (circleById.TryGetValue(conn.ID, out var circ) && seen.Add(conn.ID))
                    {
                        positions.Add(rectTransforms[circ].anchoredPosition);
                    }
                }
            }

            // Reverse connections: NPCs that connect TO this circle (only if already placed)
            if (!string.IsNullOrEmpty(myId))
            {
                foreach (var kv in circleById)
                {
                    if (kv.Key == myId || seen.Contains(kv.Key))
                        continue;

                    // Only use as anchor if the other NPC has been placed
                    if (placedIds != null && !placedIds.Contains(kv.Key))
                        continue;

                    var otherConns = kv.Value.AssignedNPC?.RelationData?.Connections;
                    if (otherConns == null)
                        continue;

                    foreach (var c in otherConns)
                    {
                        if (c != null && c.ID == myId && seen.Add(kv.Key))
                        {
                            positions.Add(rectTransforms[kv.Value].anchoredPosition);
                            break;
                        }
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Checks if a position is free from other circles.
        /// </summary>
        private static bool IsPositionFree(Vector2 pos, System.Collections.Generic.List<Vector2> existing,
            float minDist)
        {
            foreach (var e in existing)
                if (Vector2.Distance(e, pos) < minDist)
                    return false;
            return true;
        }

        /// <summary>
        /// Calculates the mean position of a collection of points.
        /// </summary>
        private static Vector2 Mean(System.Collections.Generic.IEnumerable<Vector2> pts)
        {
            var list = pts.ToList();
            return new Vector2(list.Average(p => p.x), list.Average(p => p.y));
        }

        /// <summary>
        /// Estimates the grid spacing by finding the smallest axis-aligned step between points.
        /// Only considers horizontal and vertical distances to avoid inflated diagonal measurements.
        /// </summary>
        private static float EstimateGridSpacing(System.Collections.Generic.List<Vector2> pts)
        {
            if (pts.Count < 2) return 200f;
            var steps = new System.Collections.Generic.List<float>();
            for (var i = 0; i < pts.Count; i++)
            {
                for (var j = i + 1; j < pts.Count; j++)
                {
                    var dx = Mathf.Abs(pts[i].x - pts[j].x);
                    var dy = Mathf.Abs(pts[i].y - pts[j].y);
                    // Only consider axis-aligned pairs (one axis differs, the other is close to zero)
                    if (dx > 50f && dy < 10f) steps.Add(dx);
                    if (dy > 50f && dx < 10f) steps.Add(dy);
                }
            }

            if (steps.Count == 0) return 200f;
            steps.Sort();
            // The smallest axis-aligned step is the grid spacing
            return Mathf.Clamp(steps[0], 100f, 300f);
        }

        /// <summary>
        /// Snaps a position to the nearest lattice point based on the local coordinate system.
        /// </summary>
        private static Vector2 SnapToLattice(Vector2 pos, Vector2 origin, float grid, Vector2 right, Vector2 up)
        {
            var rn = right.normalized;
            var un = up.normalized;
            var rel = pos - origin;
            var xr = Vector2.Dot(rel, rn);
            var yr = Vector2.Dot(rel, un);
            xr = Mathf.Round(xr / grid) * grid;
            yr = Mathf.Round(yr / grid) * grid;
            return origin + xr * rn + yr * un;
        }

        /// <summary>
        /// Gets all existing edges (connections) in the region using cached RectTransforms.
        /// </summary>
        private static System.Collections.Generic.List<(Vector2 from, Vector2 to)> GetAllEdges(
            S1Relations.RelationCircle[] regionCircles,
            System.Collections.Generic.Dictionary<S1Relations.RelationCircle, RectTransform> rectTransforms)
        {
            var edges = new System.Collections.Generic.List<(Vector2, Vector2)>();
            var byId = regionCircles
                .Where(c => !string.IsNullOrEmpty(c.AssignedNPC_ID))
                .ToDictionary(c => c.AssignedNPC_ID, c => c);

            foreach (var circle in regionCircles)
            {
                if (circle.AssignedNPC?.RelationData?.Connections == null)
                    continue;

                var fromPos = rectTransforms[circle].anchoredPosition;

                foreach (var conn in circle.AssignedNPC.RelationData.Connections)
                {
                    if (conn == null || string.IsNullOrEmpty(conn.ID))
                        continue;

                    if (!byId.TryGetValue(conn.ID, out var targetCircle)) continue;
                    var toPos = rectTransforms[targetCircle].anchoredPosition;
                    edges.Add((fromPos, toPos));
                }
            }

            return edges;
        }

        /// <summary>
        /// Checks if two line segments intersect.
        /// </summary>
        private static bool LineSegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            var d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);

            if (Mathf.Abs(d) < 0.001f)
                return false; // parallel

            var t = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
            var u = ((b1.x - a1.x) * (a2.y - a1.y) - (b1.y - a1.y) * (a2.x - a1.x)) / d;

            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }

        /// <summary>
        /// Counts how many existing edges would be crossed by new edges from this position.
        /// </summary>
        private static int CountCrossings(Vector2 newPos, System.Collections.Generic.List<Vector2> anchors,
            System.Collections.Generic.List<(Vector2, Vector2)> existingEdges)
        {
            var crossings = 0;

            foreach (var anchor in anchors)
            {
                foreach (var (from, to) in existingEdges)
                {
                    // skip if edge shares an endpoint with our new edge
                    if (Vector2.Distance(from, newPos) < 1f || Vector2.Distance(to, newPos) < 1f ||
                        Vector2.Distance(from, anchor) < 1f || Vector2.Distance(to, anchor) < 1f)
                        continue;

                    if (LineSegmentsIntersect(newPos, anchor, from, to))
                        crossings++;
                }
            }

            return crossings;
        }

        /// <summary>
        /// Calculates a "crowding penalty" for positions near multiple NPCs.
        /// </summary>
        private static float CalculateCrowdingPenalty(Vector2 pos,
            System.Collections.Generic.List<Vector2> allPositions, float spacing)
        {
            var penalty = 0f;
            var criticalRadius = spacing * 1.25f;

            foreach (var existing in allPositions)
            {
                var dist = Vector2.Distance(pos, existing);
                if (dist < criticalRadius && dist > 0.01f)
                {
                    // exp penalty for tight clusters
                    penalty += Mathf.Pow((criticalRadius - dist) / criticalRadius, 2) * 100f;
                }
            }

            return penalty;
        }

        /// <summary>
        /// Scores a candidate position and picks the best one.
        /// </summary>
        private static Vector2 PickBestPosition(
            System.Collections.Generic.List<Vector2> candidates,
            System.Collections.Generic.List<Vector2> anchors,
            System.Collections.Generic.List<Vector2> allPositions,
            System.Collections.Generic.List<(Vector2, Vector2)> existingEdges,
            float spacing,
            Vector4 nativeBounds)
        {
            var bestScore = float.MaxValue;
            var bestPos = candidates[0];

            foreach (var candidate in candidates)
            {
                var crossings = CountCrossings(candidate, anchors, existingEdges);
                var avgEdgeLength = anchors.Average(a => Vector2.Distance(candidate, a));
                var crowding = CalculateCrowdingPenalty(candidate, allPositions, spacing);
                var circleIntersections = CalculateCircleIntersectionPenalty(candidate, anchors, allPositions, spacing);
                var outOfBounds = CalculateOutOfBoundsPenalty(candidate, nativeBounds, spacing);

                // Combined score (lower is better)
                var score =
                    avgEdgeLength * 10f + // Primary: prefer short connections
                    crossings * 200f + // Light penalty for line crossings
                    circleIntersections * 1f + // Minor: lines through circles
                    crowding * 0.5f + // Minor: tight clusters
                    outOfBounds; // Strong penalty for going outside visible area

                if (!(score < bestScore)) continue;
                bestScore = score;
                bestPos = candidate;
            }

            return bestPos;
        }

        /// <summary>
        /// Penalizes positions that extend beyond the native circle bounding box.
        /// nativeBounds = (minX, maxX, minY, maxY) with 1 grid-cell padding already applied.
        /// </summary>
        private static float CalculateOutOfBoundsPenalty(Vector2 pos, Vector4 nativeBounds, float spacing)
        {
            var penalty = 0f;

            // How far outside native bounds in each direction (in grid-cell units)
            if (pos.x < nativeBounds.x)
                penalty += Mathf.Pow((nativeBounds.x - pos.x) / spacing, 2);
            else if (pos.x > nativeBounds.y)
                penalty += Mathf.Pow((pos.x - nativeBounds.y) / spacing, 2);

            if (pos.y < nativeBounds.z)
                penalty += Mathf.Pow((nativeBounds.z - pos.y) / spacing, 2) * 2f; // Extra penalty for going below
            else if (pos.y > nativeBounds.w)
                penalty += Mathf.Pow((pos.y - nativeBounds.w) / spacing, 2);

            return penalty * 500f;
        }

        /// <summary>
        /// Calculates penalty for connection lines passing near/through other NPC circles.
        /// </summary>
        private static float CalculateCircleIntersectionPenalty(Vector2 newPos,
            System.Collections.Generic.List<Vector2> anchors,
            System.Collections.Generic.List<Vector2> allPositions, float spacing)
        {
            var penalty = 0f;
            var circleRadius = spacing * 0.25f; // approx radius of an NPC circle
            var clearanceRadius = circleRadius * 1.3f;

            foreach (var anchor in anchors)
            {
                foreach (var otherPos in allPositions)
                {
                    // skip if line terminates here
                    if (Vector2.Distance(otherPos, newPos) < 0.8f || Vector2.Distance(otherPos, anchor) < 0.8f)
                        continue;

                    float distToLine = DistanceFromPointToLineSegment(otherPos, newPos, anchor);

                    if (distToLine < clearanceRadius)
                    {
                        // exponential penalty for passing through circles
                        var penetration = (clearanceRadius - distToLine) / clearanceRadius;
                        penalty += Mathf.Pow(penetration, 2) * 500f;
                    }
                }
            }

            return penalty;
        }

        /// <summary>
        /// Calculates the shortest distance from a point to a line segment.
        /// </summary>
        private static float DistanceFromPointToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            var line = lineEnd - lineStart;
            var lineLength = line.magnitude;

            if (lineLength < 0.001f)
                return Vector2.Distance(point, lineStart);

            var lineNorm = line / lineLength;
            var pointVector = point - lineStart;

            // project point onto line
            var t = Vector2.Dot(pointVector, lineNorm);

            // clamp to segment
            t = Mathf.Clamp(t, 0f, lineLength);

            var closest = lineStart + lineNorm * t;
            return Vector2.Distance(point, closest);
        }

        /// <summary>
        /// Enables the dealer indicator for dealer NPCs if a matching child exists on the relation circle.
        /// </summary>
        private static void EnableDealerIndicator(S1Relations.RelationCircle circle, NPC npc)
        {
            if (circle == null || npc == null)
                return;

            var indicator = circle.transform?.Find("DealerIndicator");
            if (indicator == null)
                return;

            var isDealer = NPC.IsDealerType(npc.GetType());
            indicator.gameObject.SetActive(isDealer);
        }

    }
}
