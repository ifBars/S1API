#if (IL2CPPMELON)
using S1Building = Il2CppScheduleOne.Building;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1Building = ScheduleOne.Building;
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif
using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items.Buildable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Extracts presentation-only furniture clones without exposing native prefabs or materials.
    /// </summary>
    internal static class FurnitureVisualCloner
    {
        internal static FurnitureCloneSource CreateSource(
            S1ItemFramework.BuildableItemDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(definition.ID))
                throw new ArgumentException("Furniture donor has no stable item ID.", nameof(definition));
            if (definition.BuiltItem == null)
            {
                throw new ArgumentException(
                    $"Furniture donor '{definition.ID}' has no placed-item prefab.",
                    nameof(definition));
            }

            S1EntityFramework.BuildableItem builtItem = definition.BuiltItem;
            FurniturePlacementMode placementMode;
            IReadOnlyList<FurnitureFootprintCoordinate>? footprint = null;
            FurnitureSurfaceType surfaceTypes = FurnitureSurfaceType.Wall;
            bool allowSurfaceRotation = true;

            if (CrossType.IsExact<S1EntityFramework.GridItem>(builtItem) &&
                CrossType.Is(builtItem, out S1EntityFramework.GridItem gridItem))
            {
                placementMode = FurniturePlacementMode.Grid;
                footprint = ExtractFootprint(gridItem, definition.ID);
            }
            else if (CrossType.IsExact<S1EntityFramework.SurfaceItem>(builtItem) &&
                     CrossType.Is(builtItem, out S1EntityFramework.SurfaceItem surfaceItem))
            {
                placementMode = FurniturePlacementMode.Surface;
                surfaceTypes = ExtractSurfaceTypes(surfaceItem, definition.ID);
                allowSurfaceRotation = surfaceItem.AllowRotation;
            }
            else
            {
                throw new ArgumentException(
                    $"Furniture donor '{definition.ID}' uses '{builtItem.GetType().Name}'. " +
                    "Only presentation-only GridItem and SurfaceItem donors are supported.",
                    nameof(definition));
            }

            GameObject model = CreatePresentationClone(builtItem.gameObject, definition.ID);
            model.hideFlags = HideFlags.HideAndDontSave;
            model.SetActive(false);
            Object.DontDestroyOnLoad(model);
            return new FurnitureCloneSource(
                definition.ID,
                model,
                placementMode,
                footprint,
                surfaceTypes,
                allowSurfaceRotation,
                FurnitureBuildSoundMapper.FromNative(definition.BuildSoundType),
                definition.StackLimit,
                definition.BasePurchasePrice,
                definition.ResellMultiplier,
                definition.Icon);
        }

        internal static GameObject CloneOwnedVisual(GameObject source)
        {
            GameObject clone = InactiveObjectCloner.CloneGameObject(source);
            try
            {
                CloneRendererMaterials(clone);
                return clone;
            }
            catch
            {
                Object.DestroyImmediate(clone);
                throw;
            }
        }

        private static GameObject CreatePresentationClone(GameObject donor, string donorId)
        {
            GameObject clone = InactiveObjectCloner.CloneGameObject(donor);
            try
            {
                clone.name = $"{donorId}_FurnitureVisualSource";
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                StripRuntimeComponents(clone);
                EnsureRenderable(clone, donorId);
                CloneRendererMaterials(clone);
                return clone;
            }
            catch
            {
                Object.DestroyImmediate(clone);
                throw;
            }
        }

        private static void StripRuntimeComponents(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int index = components.Length - 1; index >= 0; index--)
            {
                Component component = components[index];
                if (component == null || IsPresentationComponent(component))
                    continue;

                Object.DestroyImmediate(component);
            }

            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component != null && !IsPresentationComponent(component))
                {
                    throw new InvalidOperationException(
                        $"Furniture visual extraction could not remove '{component.GetType().Name}'.");
                }
            }
        }

        private static bool IsPresentationComponent(Component component)
        {
            return CrossType.Is(component, out Transform _) ||
                   CrossType.Is(component, out Renderer _) ||
                   CrossType.Is(component, out MeshFilter _) ||
                   CrossType.Is(component, out LODGroup _) ||
                   CrossType.Is(component, out Animator _) ||
                   CrossType.Is(component, out Animation _);
        }

        private static void EnsureRenderable(GameObject root, string donorId)
        {
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter? meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    return;
            }

            if (root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                return;

            throw new ArgumentException(
                $"Furniture donor '{donorId}' has no supported presentation renderer.",
                nameof(donorId));
        }

        private static void CloneRendererMaterials(GameObject root)
        {
            var materialClones = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] sourceMaterials = renderer.sharedMaterials;
                var ownedMaterials = new Material[sourceMaterials.Length];
                for (int index = 0; index < sourceMaterials.Length; index++)
                {
                    Material source = sourceMaterials[index];
                    if (source == null)
                        continue;
                    if (!materialClones.TryGetValue(source, out Material? owned))
                    {
                        owned = new Material(source)
                        {
                            name = source.name + "_S1API_FurnitureVariant",
                        };
                        materialClones.Add(source, owned);
                    }

                    ownedMaterials[index] = owned;
                }

                renderer.sharedMaterials = ownedMaterials;
            }
        }

        private static IReadOnlyList<FurnitureFootprintCoordinate> ExtractFootprint(
            S1EntityFramework.GridItem gridItem,
            string donorId)
        {
            if (gridItem.CoordinateFootprintTilePairs == null ||
                gridItem.CoordinateFootprintTilePairs.Count == 0)
            {
                throw new ArgumentException(
                    $"Furniture donor '{donorId}' has no grid footprint.",
                    nameof(gridItem));
            }

            var footprint = new List<FurnitureFootprintCoordinate>(
                gridItem.CoordinateFootprintTilePairs.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < gridItem.CoordinateFootprintTilePairs.Count; index++)
            {
                var coordinate = gridItem.CoordinateFootprintTilePairs[index].coord;
                if (coordinate == null)
                {
                    throw new ArgumentException(
                        $"Furniture donor '{donorId}' has an unsupported grid footprint.",
                        nameof(gridItem));
                }
                if (coordinate.x < 0 || coordinate.y < 0 ||
                    !seen.Add($"{coordinate.x}:{coordinate.y}"))
                {
                    throw new ArgumentException(
                        $"Furniture donor '{donorId}' has an unsupported grid footprint.",
                        nameof(gridItem));
                }

                footprint.Add(new FurnitureFootprintCoordinate(coordinate.x, coordinate.y));
            }

            return footprint;
        }

        private static FurnitureSurfaceType ExtractSurfaceTypes(
            S1EntityFramework.SurfaceItem surfaceItem,
            string donorId)
        {
            if (surfaceItem.ValidSurfaceTypes == null || surfaceItem.ValidSurfaceTypes.Count == 0)
            {
                throw new ArgumentException(
                    $"Furniture donor '{donorId}' has no supported surfaces.",
                    nameof(surfaceItem));
            }

            FurnitureSurfaceType surfaceTypes = FurnitureSurfaceType.None;
            for (int index = 0; index < surfaceItem.ValidSurfaceTypes.Count; index++)
            {
                S1Building.Surface.ESurfaceType surfaceType = surfaceItem.ValidSurfaceTypes[index];
                switch (surfaceType)
                {
                    case S1Building.Surface.ESurfaceType.Wall:
                        surfaceTypes |= FurnitureSurfaceType.Wall;
                        break;
                    case S1Building.Surface.ESurfaceType.Roof:
                        surfaceTypes |= FurnitureSurfaceType.Roof;
                        break;
                    default:
                        throw new ArgumentException(
                            $"Furniture donor '{donorId}' uses an unsupported surface type.",
                            nameof(surfaceItem));
                }
            }

            return surfaceTypes;
        }
    }
}
