#if (IL2CPPMELON)
using S1Building = Il2CppScheduleOne.Building;
#elif MONOMELON
using S1Building = ScheduleOne.Building;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Applies opt-in S1API visual configuration after the native placement system creates a ghost.
    /// </summary>
    internal static class BuildableGhostRuntime
    {
        internal const string FurnitureVisualName = "FurnitureVisual";
        internal const string FurnitureGhostVisualName = "FurnitureGhostVisual";

        private static readonly Dictionary<string, Action<GameObject>> Configurators =
            new Dictionary<string, Action<GameObject>>(StringComparer.OrdinalIgnoreCase);

        internal static void RegisterVisual(
            string itemId,
            Func<Transform, GameObject> visualFactory,
            bool replaceExistingVisual)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Buildable item ID cannot be null or whitespace.", nameof(itemId));
            if (visualFactory == null)
                throw new ArgumentNullException(nameof(visualFactory));

            Configurators[itemId] = ghostRoot =>
                CreateVisual(ghostRoot, visualFactory, replaceExistingVisual);
        }

        internal static void RegisterVisualSource(
            string itemId,
            GameObject visualSource,
            string ghostVisualName,
            bool replaceExistingVisual)
        {
            if (visualSource == null)
                throw new ArgumentNullException(nameof(visualSource));
            if (string.IsNullOrWhiteSpace(ghostVisualName))
                throw new ArgumentException("Ghost visual name cannot be null or whitespace.", nameof(ghostVisualName));

            RegisterVisual(
                itemId,
                parent =>
                {
                    GameObject visual = UnityEngine.Object.Instantiate(
                        visualSource,
                        parent,
                        false);
                    visual.name = ghostVisualName;
                    return visual;
                },
                replaceExistingVisual);
        }

        internal static bool TryConfigure(string? itemId, GameObject? ghostRoot)
        {
            if (string.IsNullOrWhiteSpace(itemId) || ghostRoot == null)
                return false;
            if (!Configurators.TryGetValue(itemId, out Action<GameObject>? configure))
                return false;

            configure(ghostRoot);
            return true;
        }

        private static void CreateVisual(
            GameObject ghostRoot,
            Func<Transform, GameObject> visualFactory,
            bool replaceExistingVisual)
        {
            Renderer[] existingRenderers = ghostRoot.GetComponentsInChildren<Renderer>(true);
            bool[] previousStates = new bool[existingRenderers.Length];
            GameObject? createdVisual = null;

            try
            {
                if (replaceExistingVisual)
                {
                    for (int index = 0; index < existingRenderers.Length; index++)
                    {
                        previousStates[index] = existingRenderers[index].enabled;
                        existingRenderers[index].enabled = false;
                    }
                }

                GameObject visual = visualFactory(ghostRoot.transform);
                if (visual == null)
                    throw new InvalidOperationException("The buildable ghost visual factory returned null.");

                createdVisual = visual;
                if (!visual.transform.IsChildOf(ghostRoot.transform))
                    visual.transform.SetParent(ghostRoot.transform, false);

                visual.SetActive(true);
                PrepareVisualForGhost(visual);
            }
            catch
            {
                if (createdVisual != null)
                    UnityEngine.Object.Destroy(createdVisual);

                if (replaceExistingVisual)
                {
                    for (int index = 0; index < existingRenderers.Length; index++)
                    {
                        if (existingRenderers[index] != null)
                            existingRenderers[index].enabled = previousStates[index];
                    }
                }

                throw;
            }
        }

        private static void PrepareVisualForGhost(GameObject visual)
        {
            S1Building.BuildManager? buildManager = S1Building.BuildManager.Instance;
            if (buildManager == null)
                throw new InvalidOperationException("The native build manager is unavailable while creating a placement ghost.");

            // Native ghost preparation runs before S1API adds this visual. Apply the same
            // non-interactive runtime contract to the late-added hierarchy.
            buildManager.DisableColliders(visual);
            buildManager.DisableNavigation(visual);
            buildManager.DisableNetworking(visual);
            buildManager.DisableCanvases(visual);
            buildManager.DisableLights(visual);
        }

    }
}
