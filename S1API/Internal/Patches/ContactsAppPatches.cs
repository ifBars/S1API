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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches and helpers related to adding and positioning custom NPCs in Contacts app.
    /// </summary>
    [HarmonyPatch]
    internal class ContactsAppPatches
    {
        private static bool _contactsReady;
        private static bool? _hasCustomNpcTypesCache;

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
            catch
            {
                // On error, assume no custom NPCs to avoid breaking phone apps
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
            if (SceneManager.GetActiveScene().name == "Tutorial") return true;
            
            // If no custom NPCs exist, allow original Start to run normally
            if (!HasCustomNpcTypes())
                return true;
            
            if (!_contactsReady)
            {
                MelonCoroutines.Start(WaitForNPCs(__instance));
                return false;
            }
            _contactsReady = false;

            return true;
        }

        /// <summary>
        /// Stops the updates until contacts are ready
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(S1ContactsApp.ContactsApp), "Update")]
        private static bool ContactsApp_Update_Prefix(S1ContactsApp.ContactsApp __instance)
        {
            // skip patch if in the tutorial
            if (SceneManager.GetActiveScene().name == "Tutorial") return true;
            
            // If no custom NPCs exist, allow original Update to run normally
            if (!HasCustomNpcTypes())
                return true;
            
            return _contactsReady;
        }

        /// <summary>
        /// Waits for custom NPCs to be ready and present in the scene before adding relation circles.
        /// </summary>
        private static IEnumerator WaitForNPCs(S1ContactsApp.ContactsApp contactsApp)
        {
            yield return new WaitWhile((Func<bool>)(() => NPCPatches.CustomNpcsReady == false));

            var customNPCs = NPC.All.Where(n => n.IsCustomNPC).ToList();
            
            // Get the original Start method once for reuse
            var originalStart = typeof(S1ContactsApp.ContactsApp)
                .GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (originalStart == null)
            {
                Debug.LogError("[ContactsPatches] Couldn't find Start");
                yield break;
            }
            
            // Safety check: if no custom NPCs exist after waiting, skip relation circles logic
            if (customNPCs.Count == 0)
            {
                _contactsReady = true;
                originalStart.Invoke(contactsApp, null);
                yield break;
            }
            
            yield return new WaitUntil((Func<bool>)(() =>
            {
                var allSceneNPCs = Object.FindObjectsOfType<S1NPCs.NPC>(true);
                return customNPCs.All(npc => allSceneNPCs.Any(sn => sn.ID == npc.ID));
            }));
            
            // Wait until relationship data has been applied from prefab for all custom NPCs
            yield return new WaitUntil((Func<bool>)(() =>
            {
                return customNPCs.All(npc => npc.RelationshipDataAppliedFromPrefab);
            }));

            AddRelationCircles(contactsApp);

            _contactsReady = true;
            originalStart.Invoke(contactsApp, null);
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
            var placeholders = new System.Collections.Generic.Dictionary<NPC, S1Relations.RelationCircle>();

            // create circle game objects for all custom NPCs
            foreach (var npc in customNPCs)
            {
                if (!regionUIs.TryGetValue(npc.S1NPC.Region, out var regionUI) || regionUI?.Container == null)
                    continue;

                var existing = regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true)
                    .FirstOrDefault(c => c.AssignedNPC_ID == npc.S1NPC.ID);
                if (existing != null)
                    continue;

                var template = regionUI.Container.GetComponentInChildren<S1Relations.RelationCircle>(true)
                               ?? contactsApp.CirclesContainer.GetComponentInChildren<S1Relations.RelationCircle>(true);
                if (template == null)
                    continue;

                var go = Object.Instantiate(template.gameObject, regionUI.Container);
                go.name = npc.ID;
                var circle = go.GetComponent<S1Relations.RelationCircle>();
                circle.AssignedNPC = npc.S1NPC;
                circle.AssignedNPC_ID = npc.S1NPC.ID;
                EnableDealerIndicator(circle, npc);
                placeholders[npc] = circle;
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

            // placement order
            var graph = BuildGraph(customNPCs);
            var nativeIds = new System.Collections.Generic.HashSet<string>();
            foreach (var kv in regionCircles)
            foreach (var circ in kv.Value)
                if (!string.IsNullOrEmpty(circ.AssignedNPC_ID))
                    nativeIds.Add(circ.AssignedNPC_ID);

            var order = ComputeInsertionOrder(customNPCs, graph, nativeIds);

            foreach (var regionGroup in order.GroupBy(n => n.S1NPC.Region))
            {
                if (!regionUIs.TryGetValue(regionGroup.Key, out var regionUI) || regionUI?.Container == null)
                    continue;

                var circlesInRegion = regionUI.Container.GetComponentsInChildren<S1Relations.RelationCircle>(true);

                var circleById = circlesInRegion
                    .Where(c => !string.IsNullOrEmpty(c.AssignedNPC_ID))
                    .ToDictionary(c => c.AssignedNPC_ID, c => c);

                var rectTransforms = circlesInRegion.ToDictionary(
                    c => c,
                    c => c.GetComponent<RectTransform>()
                );

                foreach (var npc in regionGroup)
                {
                    var circle = circlesInRegion.FirstOrDefault(c => c.AssignedNPC_ID == npc.S1NPC.ID);
                    if (circle == null)
                        continue;

                    // rebuild edges each time (necessary - graph is dynamic)
                    var existingEdges = GetAllEdges(circlesInRegion, rectTransforms);

                    var newPos = ComputePlacement(circle, circlesInRegion, circleById, rectTransforms, existingEdges);
                    rectTransforms[circle].anchoredPosition = newPos;
                }
            }
        }

        /// <summary>
        /// Builds a graph of connections between custom NPCs.
        /// </summary>
        private static System.Collections.Generic.Dictionary<NPC, System.Collections.Generic.List<NPC>> BuildGraph(
            System.Collections.Generic.List<NPC> customs)
        {
            var byId = customs.ToDictionary(n => n.S1NPC.ID, n => n);
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
        /// Uses a scoring system to evaluate candidate positions based on:
        /// - Edge crossings (heavily penalized at 1000x weight)
        /// - Lines passing through other NPC circles (1.5x weight)
        /// - Connection edge lengths (0.5x weight - prefers shorter)
        /// - Crowding near other NPCs (2x weight)
        /// For connected NPCs, generates candidates in multiple directions around their connection anchors.
        /// For unconnected NPCs, places them in rows above the existing layout.
        /// </summary>
        private static Vector2 ComputePlacement(
            S1Relations.RelationCircle circle,
            S1Relations.RelationCircle[] regionCircles,
            System.Collections.Generic.Dictionary<string, S1Relations.RelationCircle> circleById,
            System.Collections.Generic.Dictionary<S1Relations.RelationCircle, RectTransform> rectTransforms,
            System.Collections.Generic.List<(Vector2, Vector2)> existingEdges)
        {
            var anchors = GetConnectionPositions(circle, circleById, rectTransforms);

            var allPositions = regionCircles
                .Select(c => rectTransforms[c].anchoredPosition)
                .ToList();

            if (allPositions.Count == 0)
                return Vector2.zero;

            var spacing = EstimateGridSpacing(allPositions);
            var center = Mean(allPositions);
            var (right, up) = EstimateLocalBasis(allPositions, center);

            if (anchors.Count > 0)
            {
                var targetCenter = Mean(anchors);
                var candidates = new System.Collections.Generic.List<Vector2>();

                var dirs = new[]
                {
                    right, -right, up, -up,
                    right + up, right - up, -right + up, -right - up,
                    (right + up * 2f).normalized, (right * 2f + up).normalized,
                    (-right + up * 2f).normalized, (-right * 2f + up).normalized,
                    (right - up * 2f).normalized, (right * 2f - up).normalized,
                    (-right - up * 2f).normalized, (-right * 2f - up).normalized
                };

                var minSpacing = spacing * 0.7f;

                for (var r = spacing * 0.8f; r <= spacing * 3.5f; r += spacing * 0.2f)
                {
                    foreach (var d in dirs)
                    {
                        var candidate = targetCenter + d * r;
                        candidate = SnapToLattice(candidate, targetCenter, spacing, right, up);

                        // small jitter for variation
                        var jittered = candidate + Random.insideUnitCircle * 5f;

                        if (IsPositionFree(jittered, allPositions, minSpacing))
                        {
                            candidates.Add(jittered);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    return PickBestPosition(candidates, anchors, allPositions, existingEdges, spacing);
                }

                return targetCenter + new Vector2(0, -spacing * 1.5f);
            }

            // unconnected NPCs
            var maxY = allPositions.Max(p => p.y);
            var xCenter = center.x;
            var minSpacingUnconnected = spacing * 0.8f;

            for (var attempt = 0; attempt < 20; attempt++)
            {
                var y = maxY + spacing * 2f + (attempt / 3) * spacing * 1.2f;
                var xOffset = (attempt % 3 - 1) * spacing * 0.8f;
                var candidate = new Vector2(xCenter + xOffset, y);
                candidate = SnapToLattice(candidate, new Vector2(xCenter, y), spacing, right, up);

                if (IsPositionFree(candidate, allPositions, minSpacingUnconnected))
                    return candidate;
            }

            return new Vector2(xCenter, maxY + spacing * 2f);
        }

        /// <summary>
        /// Gets the positions of all connected NPCs for a given circle.
        /// </summary>
        private static System.Collections.Generic.List<Vector2> GetConnectionPositions(
            S1Relations.RelationCircle circle,
            System.Collections.Generic.Dictionary<string, S1Relations.RelationCircle> circleById,
            System.Collections.Generic.Dictionary<S1Relations.RelationCircle, RectTransform> rectTransforms)
        {
            var positions = new System.Collections.Generic.List<Vector2>();
            var conns = circle.AssignedNPC?.RelationData?.Connections;

            if (conns == null || conns.Count == 0)
                return positions;

            foreach (var conn in conns)
            {
                if (conn == null || string.IsNullOrEmpty(conn.ID))
                    continue;

                if (circleById.TryGetValue(conn.ID, out var circ))
                {
                    positions.Add(rectTransforms[circ].anchoredPosition);
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
        /// Estimates the grid spacing based on distances between points.
        /// </summary>
        private static float EstimateGridSpacing(System.Collections.Generic.List<Vector2> pts)
        {
            if (pts.Count < 2) return 200f;
            var dists = new System.Collections.Generic.List<float>();
            for (var i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                var others = pts.Where((q, idx) => idx != i).OrderBy(q => Vector2.Distance(p, q)).Take(3);
                dists.AddRange(others.Select(q => Vector2.Distance(p, q)));
            }

            dists.Sort();
            var median = dists.Count > 0 ? dists[dists.Count / 2] : 200f;
            return Mathf.Clamp(median, 150f, 250f);
        }

        /// <summary>
        /// Estimates the local coordinate system basis vectors from point distribution.
        /// </summary>
        private static (Vector2, Vector2) EstimateLocalBasis(System.Collections.Generic.List<Vector2> pts,
            Vector2 center)
        {
            float xx = 0, yy = 0, xy = 0;
            foreach (var p in pts)
            {
                var d = p - center;
                xx += d.x * d.x;
                yy += d.y * d.y;
                xy += d.x * d.y;
            }

            var theta = 0.5f * Mathf.Atan2(2f * xy, xx - yy);
            var right = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            var up = new Vector2(-right.y, right.x);
            return (right, up);
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
            float spacing)
        {
            var bestScore = float.MaxValue;
            var bestPos = candidates[0];

            foreach (var candidate in candidates)
            {
                var crossings = CountCrossings(candidate, anchors, existingEdges);
                var avgEdgeLength = anchors.Average(a => Vector2.Distance(candidate, a));
                var crowding = CalculateCrowdingPenalty(candidate, allPositions, spacing);
                var circleIntersections = CalculateCircleIntersectionPenalty(candidate, anchors, allPositions, spacing);

                // Combined score (lower is better)
                var score =
                    crossings * 50000f + // Heavy penalty - line crossings
                    circleIntersections * 4.8f + // Penalty - lines through circles
                    avgEdgeLength * 0.35f + // Prefer shorter connections
                    crowding * 0.65f; // Penalty - tight clusters

                if (!(score < bestScore)) continue;
                bestScore = score;
                bestPos = candidate;
            }

            return bestPos;
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

        [HarmonyPatch(typeof(S1ContactsApp.ContactsDetailPanel), "Open")]
        [HarmonyPostfix]
        private static void ContactsDetailPanel_Open_Postfix(S1ContactsApp.ContactsDetailPanel __instance)
        {
            if (__instance == null) return;
            if (__instance.transform == null) return;
            if (__instance.TypeLabel.font == null) return;
            var gameObject = __instance.transform.Find("Connections")?.gameObject;
            if (gameObject == null)
            {
                gameObject = new GameObject("Connections");
                gameObject.transform.SetParent(__instance.transform, false);
                var text = gameObject.AddComponent<Text>();
                text.font = __instance.TypeLabel.font;
            }

            var textComp = gameObject.GetComponent<Text>();
            if (textComp == null)
            {
                Debug.LogWarning("Connections Text Component not found!");
                return;
            }

            var npcConnections = __instance.SelectedNPC?.RelationData?.Connections;
            if (npcConnections == null || npcConnections.Count == 0)
            {
                textComp.text = "Connections: None";
                return;
            }
#if MONOMELON
            var connsText = string.Join(", ", npcConnections.Where(c => c != null).Select(c =>
#elif IL2CPPMELON
            var connsText = string.Join(", ", npcConnections._items.Where(c => c != null).Select(c =>
#endif
            {
                if (c.RelationData == null)
                    return "???";
                var isCustomer = NPC.IsCustomerType(c.GetType());

                // Dealers/Suppliers have to be unlocked to see the name
                if (!isCustomer)
                {
                    return c.RelationData.Unlocked
                        ? c.fullName
                        : "???";
                }
                // Customers can be known via mutual connections
                return c.RelationData.IsKnown()
                    ? c.fullName
                    : "???";
            }));
            textComp.text = $"Connections: {connsText}";
        }
    }
}
