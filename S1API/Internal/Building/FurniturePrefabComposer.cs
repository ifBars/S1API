#if (IL2CPPMELON)
using S1Building = Il2CppScheduleOne.Building;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Storage = Il2CppScheduleOne.Storage;
using S1Tiles = Il2CppScheduleOne.Tiles;
#elif MONOMELON
using S1Building = ScheduleOne.Building;
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
using S1Storage = ScheduleOne.Storage;
using S1Tiles = ScheduleOne.Tiles;
#endif
using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Items.Buildable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Preserves the game's serialized build, save, and networking graph while replacing only
    /// presentation, bounds, footprint, and surface-placement configuration.
    /// </summary>
    internal static class FurniturePrefabComposer
    {
        private const float GridTileSize = 0.5f;

        internal static FurnitureComposition Compose(
            string id,
            GameObject model,
            FurniturePlacementMode placementMode,
            int footprintWidth,
            int footprintDepth,
            FurnitureSurfaceType surfaceTypes,
            bool allowSurfaceRotation,
            IReadOnlyList<FurnitureFootprintCoordinate>? donorFootprint,
            bool centerModelOnFootprint,
            bool isolateMaterials)
        {
            string templateId = placementMode == FurniturePlacementMode.Grid
                ? FurnitureTemplateCatalog.GridItemId
                : FurnitureTemplateCatalog.SurfaceItemId;
            S1ItemFramework.BuildableItemDefinition template = GetTemplateDefinition(templateId);

            S1EntityFramework.BuildableItem builtItem = InactiveObjectCloner.CloneComponent(
                template.BuiltItem,
                parent: null);
            builtItem.gameObject.name = $"{id}_BuiltItem";
            DisableTemplateRenderers(builtItem.gameObject);

            Vector3 visualOffset = placementMode == FurniturePlacementMode.Grid &&
                                   centerModelOnFootprint
                ? new Vector3(
                    (footprintWidth - 1) * GridTileSize * 0.5f,
                    0f,
                    (footprintDepth - 1) * GridTileSize * 0.5f)
                : Vector3.zero;
            GameObject builtModel = AddModelClone(
                model,
                builtItem.transform,
                visualOffset,
                BuildableGhostRuntime.FurnitureVisualName,
                isolateMaterials);
            ConfigureBoundsAndCulling(builtItem, builtModel, placementMode);

            if (placementMode == FurniturePlacementMode.Grid)
            {
                if (!CrossType.Is(builtItem, out S1EntityFramework.GridItem gridItem))
                    throw new InvalidOperationException($"Furniture template '{templateId}' is not a native GridItem.");

                ConfigureGridFootprint(
                    gridItem,
                    footprintWidth,
                    footprintDepth,
                    donorFootprint);
            }
            else
            {
                if (!CrossType.Is(builtItem, out S1EntityFramework.SurfaceItem surfaceItem))
                    throw new InvalidOperationException($"Furniture template '{templateId}' is not a native SurfaceItem.");

                ConfigureSurfacePlacement(surfaceItem, surfaceTypes, allowSurfaceRotation);
            }

            RuntimePrefabCache.Store(builtItem.gameObject);

            S1Storage.StoredItem storedItem = InactiveObjectCloner.CloneComponent(
                template.StoredItem,
                parent: null);
            storedItem.gameObject.name = $"{id}_StoredItem";
            DisableTemplateRenderers(storedItem.gameObject);
            AddModelClone(
                model,
                storedItem.transform,
                Vector3.zero,
                BuildableGhostRuntime.FurnitureVisualName,
                isolateMaterials);
            RuntimePrefabCache.Store(storedItem.gameObject);

            return new FurnitureComposition(
                template,
                builtItem,
                storedItem,
                new Equippable(template.Equippable));
        }

        private static S1ItemFramework.BuildableItemDefinition GetTemplateDefinition(string templateId)
        {
            object? item = S1Registry.GetItem(templateId);
            if (item == null || !CrossType.Is(item, out S1ItemFramework.BuildableItemDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Native furniture template '{templateId}' is unavailable. Build custom furniture after the item registry is initialized.");
            }

            if (definition.BuiltItem == null)
                throw new InvalidOperationException($"Native furniture template '{templateId}' has no placed-item prefab.");
            if (definition.StoredItem == null)
                throw new InvalidOperationException($"Native furniture template '{templateId}' has no stored-item prefab.");
            if (definition.Equippable == null)
                throw new InvalidOperationException($"Native furniture template '{templateId}' has no native equippable prefab.");

            return definition;
        }

        private static GameObject AddModelClone(
            GameObject model,
            Transform parent,
            Vector3 localPosition,
            string name,
            bool isolateMaterials)
        {
            GameObject clone = isolateMaterials
                ? FurnitureVisualCloner.CloneOwnedVisual(model)
                : InactiveObjectCloner.CloneGameObject(model);
            clone.name = name;
            clone.transform.SetParent(parent, false);
            clone.transform.localPosition = localPosition;
            clone.transform.localRotation = Quaternion.identity;
            clone.SetActive(true);
            return clone;
        }

        private static void DisableTemplateRenderers(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private static void ConfigureBoundsAndCulling(
            S1EntityFramework.BuildableItem builtItem,
            GameObject model,
            FurniturePlacementMode placementMode)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new ArgumentException("Furniture model must contain at least one Renderer.", nameof(model));

            Bounds builtItemBounds = CalculateCombinedBounds(renderers, builtItem.transform);
            Bounds modelBounds = CalculateCombinedBounds(renderers, model.transform);

            BoxCollider boundingCollider = builtItem.BoundingCollider;
            if (boundingCollider == null)
                boundingCollider = builtItem.gameObject.AddComponent<BoxCollider>();
            boundingCollider.center = builtItemBounds.center;
            boundingCollider.size = builtItemBounds.size;
            builtItem.BoundingCollider = boundingCollider;

            var collision = model.AddComponent<BoxCollider>();
            collision.center = modelBounds.center;
            collision.size = modelBounds.size;

            builtItem.GameObjectsToCull = new[] { model };
#if (IL2CPPMELON)
            builtItem.MeshesToCull = new Il2CppSystem.Collections.Generic.List<MeshRenderer>();
#else
            builtItem.MeshesToCull = new System.Collections.Generic.List<MeshRenderer>();
#endif
            foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                builtItem.MeshesToCull.Add(renderer);

#if (IL2CPPMELON)
            var outlineRenderers = new Il2CppSystem.Collections.Generic.List<GameObject>();
#else
            var outlineRenderers = new System.Collections.Generic.List<GameObject>();
#endif
            foreach (Renderer renderer in renderers)
                outlineRenderers.Add(renderer.gameObject);
            if (!ReflectionUtils.TrySetFieldOrProperty(builtItem, "OutlineRenderers", outlineRenderers))
                throw new InvalidOperationException("Native furniture template does not expose its outline renderer list.");

            // Grid build points are ground anchors. Moving one to the model center lowers the
            // footprint below the tile detectors, which makes the ghost invisible and invalid.
            Vector3 boundsCenterWorld = builtItem.transform.TransformPoint(builtItemBounds.center);
            if (placementMode == FurniturePlacementMode.Surface && builtItem.BuildPoint != null)
                builtItem.BuildPoint.position = boundsCenterWorld;
            if (builtItem.MidAirCenterPoint != null)
                builtItem.MidAirCenterPoint.position = boundsCenterWorld;
        }

        private static Bounds CalculateCombinedBounds(
            Renderer[] renderers,
            Transform relativeTo)
        {
            Bounds combined = default;
            bool hasPoint = false;

            foreach (Renderer renderer in renderers)
            {
                if (!TryGetRendererLocalBounds(renderer, out Bounds rendererBounds))
                    continue;

                Vector3 center = rendererBounds.center;
                Vector3 extents = rendererBounds.extents;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3(
                        (corner & 1) == 0 ? -extents.x : extents.x,
                        (corner & 2) == 0 ? -extents.y : extents.y,
                        (corner & 4) == 0 ? -extents.z : extents.z);
                    Vector3 point = relativeTo.InverseTransformPoint(
                        renderer.transform.TransformPoint(center + offset));
                    if (!hasPoint)
                    {
                        combined = new Bounds(point, Vector3.zero);
                        hasPoint = true;
                    }
                    else
                    {
                        combined.Encapsulate(point);
                    }
                }
            }

            if (!hasPoint)
            {
                throw new ArgumentException(
                    "Furniture model must contain a MeshRenderer with a mesh or a SkinnedMeshRenderer.",
                    nameof(renderers));
            }

            return combined;
        }

        private static bool TryGetRendererLocalBounds(
            Renderer renderer,
            out Bounds bounds)
        {
            MeshFilter? meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                bounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            SkinnedMeshRenderer? skinnedRenderer = renderer.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer != null)
            {
                bounds = skinnedRenderer.localBounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private static void ConfigureGridFootprint(
            S1EntityFramework.GridItem gridItem,
            int width,
            int depth,
            IReadOnlyList<FurnitureFootprintCoordinate>? donorFootprint)
        {
            if (gridItem.CoordinateFootprintTilePairs == null ||
                gridItem.CoordinateFootprintTilePairs.Count == 0 ||
                gridItem.CoordinateFootprintTilePairs[0].footprintTile == null)
            {
                throw new InvalidOperationException("Native grid furniture template has no footprint tile to clone.");
            }

            S1Tiles.FootprintTile templateTile = gridItem.CoordinateFootprintTilePairs[0].footprintTile;
            Transform oldFootprintRoot = templateTile.transform.parent;
            if (oldFootprintRoot != null && oldFootprintRoot != gridItem.transform)
                oldFootprintRoot.gameObject.SetActive(false);

            var footprintRoot = new GameObject("FurnitureFootprint");
            footprintRoot.transform.SetParent(gridItem.transform, false);
            footprintRoot.SetActive(true);

#if (IL2CPPMELON)
            var pairs = new Il2CppSystem.Collections.Generic.List<S1Tiles.CoordinateFootprintTilePair>();
#else
            var pairs = new System.Collections.Generic.List<S1Tiles.CoordinateFootprintTilePair>();
#endif
            if (donorFootprint != null)
            {
                foreach (FurnitureFootprintCoordinate coordinate in donorFootprint)
                    AddFootprintTile(coordinate.X, coordinate.Y);
            }
            else
            {
                for (int x = 0; x < width; x++)
                for (int y = 0; y < depth; y++)
                    AddFootprintTile(x, y);
            }

            gridItem.CoordinateFootprintTilePairs = pairs;

            void AddFootprintTile(int x, int y)
            {
                S1Tiles.FootprintTile tile = InactiveObjectCloner.CloneComponent(
                    templateTile,
                    footprintRoot.transform);
                tile.name = $"FootprintTile_{x}_{y}";
                tile.X = x;
                tile.Y = y;
                tile.transform.localPosition = new Vector3(x * GridTileSize, 0f, y * GridTileSize);
                tile.gameObject.SetActive(true);

#if (IL2CPPMELON)
                var pair = new S1Tiles.CoordinateFootprintTilePair();
                pair.coord = new S1Tiles.Coordinate(x, y);
                pair.footprintTile = tile;
#else
                var pair = new S1Tiles.CoordinateFootprintTilePair
                {
                    coord = new S1Tiles.Coordinate(x, y),
                    footprintTile = tile,
                };
#endif
                pairs.Add(pair);
            }
        }

        private static void ConfigureSurfacePlacement(
            S1EntityFramework.SurfaceItem surfaceItem,
            FurnitureSurfaceType surfaceTypes,
            bool allowRotation)
        {
#if (IL2CPPMELON)
            var nativeTypes = new Il2CppSystem.Collections.Generic.List<S1Building.Surface.ESurfaceType>();
#else
            var nativeTypes = new System.Collections.Generic.List<S1Building.Surface.ESurfaceType>();
#endif
            if ((surfaceTypes & FurnitureSurfaceType.Wall) != 0)
                nativeTypes.Add(S1Building.Surface.ESurfaceType.Wall);
            if ((surfaceTypes & FurnitureSurfaceType.Roof) != 0)
                nativeTypes.Add(S1Building.Surface.ESurfaceType.Roof);

            surfaceItem.ValidSurfaceTypes = nativeTypes;
            surfaceItem.AllowRotation = allowRotation;
        }
    }
}
