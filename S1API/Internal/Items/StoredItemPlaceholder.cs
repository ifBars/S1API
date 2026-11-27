#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Storage = Il2CppScheduleOne.Storage;
using S1Tiles = Il2CppScheduleOne.Tiles;
#elif (MONOMELON)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
using S1Storage = ScheduleOne.Storage;
using S1Tiles = ScheduleOne.Tiles;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Items
{
    /// <summary>
    /// INTERNAL: The StoredItem an item gets when its builder was given none through <c>WithStoredItem</c>.
    /// </summary>
    /// <remarks>
    /// Every storage with a visualizer (shelves, racks, closets) redraws its contents when a slot changes and reads
    /// the footprint of each item's StoredItem for that. A bare StoredItem has no footprint tiles, so
    /// <c>StoredItem.SizeX</c>/<c>SizeY</c> throw inside <c>ItemSlot.SetStoredItem</c>. The drop then stops halfway:
    /// the item is already in the storage slot, but the inventory never loses it, and every later drop into that
    /// storage throws the same way.
    ///
    /// The placeholder is a copy of a small vanilla item's StoredItem, which keeps its footprint tiles and build
    /// point. The vanilla model is removed and the item's icon stands in its place as two crossed sprites, so it
    /// reads from any side of a shelf. Without an icon the item is stored without a visible model.
    /// </remarks>
    internal static class StoredItemPlaceholder
    {
        private static readonly Log Logger = new Log("StoredItemPlaceholder");

        /// <summary>
        /// Vanilla items whose StoredItem fills one storage tile. The first one found with footprint tiles and a
        /// build point is copied.
        /// </summary>
        internal static readonly string[] TemplateIds =
        {
            "cuke", "energydrink", "iodine", "paracetamol", "banana", "donut", "megabean", "mouthwash"
        };

        private const string IconChildName = "S1API_StoredItemIcon";

        private static GameObject? _root;

        // Item ID -> the icon sprites of its placeholder prefab and the spot they stand on, so a late icon change
        // reaches shelf copies made after it.
        private static readonly Dictionary<string, IconStand> IconStands =
            new Dictionary<string, IconStand>(StringComparer.OrdinalIgnoreCase);

        private sealed class IconStand
        {
            internal readonly List<SpriteRenderer> Renderers = new List<SpriteRenderer>(2);
            internal Bounds Spot;
        }

        /// <summary>
        /// INTERNAL: Builds the placeholder StoredItem prefab for an item. Returns null, with an error logged, when no
        /// vanilla template is available or the copy fails.
        /// </summary>
        internal static S1Storage.StoredItem? Create(string itemId, string? displayName, Sprite? icon)
        {
            var template = FindTemplate(out string templateId);
            if (template == null)
            {
                Logger.Error(
                    $"No vanilla StoredItem to copy for '{itemId}' (tried {string.Join(", ", TemplateIds)}). " +
                    "Putting this item into a shelf or closet will throw; give it a StoredItem with WithStoredItem(...).");
                return null;
            }

            try
            {
                var copy = Object.Instantiate(template.gameObject, EnsureRoot().transform);
                copy.name = $"{(string.IsNullOrEmpty(displayName) ? itemId : displayName)}_StoredItem";

                Bounds spot = StripModel(copy);
                AddIcon(itemId, copy.transform, spot, icon);

                var storedItem = copy.GetComponent<S1Storage.StoredItem>();
                Logger.Msg(
                    $"'{itemId}' has no StoredItem of its own; using a copy of '{templateId}' " +
                    (icon != null ? "with the item icon." : "without a model until the item gets an icon."));
                return storedItem;
            }
            catch (Exception ex)
            {
                Logger.Error($"Building the StoredItem placeholder for '{itemId}' from '{templateId}' failed: {ex}");
                return null;
            }
        }

        /// <summary>
        /// INTERNAL: Puts a new icon on the placeholder prefab of an item, if it has one. Shelf copies made before the
        /// change keep the old icon until the storage draws them again.
        /// </summary>
        internal static void RefreshIcon(string? itemId, Sprite? icon)
        {
            if (string.IsNullOrEmpty(itemId) || !IconStands.TryGetValue(itemId!, out var stand))
                return;

            for (int i = 0; i < stand.Renderers.Count; i++)
            {
                if (stand.Renderers[i] != null)
                    stand.Renderers[i].sprite = icon;
            }

            PlaceIcon(stand, icon);
        }

        /// <summary>
        /// INTERNAL: Uniform scale that fits a sprite of <paramref name="spriteWidth"/> by
        /// <paramref name="spriteHeight"/> world units into a spot of <paramref name="spotWidth"/> by
        /// <paramref name="spotHeight"/>. Returns 0 when either side is empty.
        /// </summary>
        internal static float FitScale(float spriteWidth, float spriteHeight, float spotWidth, float spotHeight)
        {
            if (spriteWidth <= 0f || spriteHeight <= 0f || spotWidth <= 0f || spotHeight <= 0f)
                return 0f;
            return Math.Min(spotWidth / spriteWidth, spotHeight / spriteHeight);
        }

        private static S1Storage.StoredItem? FindTemplate(out string templateId)
        {
            templateId = string.Empty;
            for (int i = 0; i < TemplateIds.Length; i++)
            {
                var definition = S1Registry.GetItem(TemplateIds[i]);
                if (definition == null)
                    continue;
                if (!CrossType.Is(definition, out S1ItemFramework.StorableItemDefinition storable) ||
                    storable.StoredItem == null)
                    continue;

                var stored = storable.StoredItem;
                if (FootprintTileCount(stored) <= 0 || BuildPoint(stored) == null)
                {
                    Logger.Msg($"Vanilla '{TemplateIds[i]}' StoredItem has no footprint tiles or build point; trying the next one.");
                    continue;
                }

                templateId = TemplateIds[i];
                return stored;
            }

            return null;
        }

        private static GameObject EnsureRoot()
        {
            if (_root != null)
                return _root;

            // Inactive, so the copies never run Awake themselves; storages instantiate them as prefabs.
            _root = new GameObject("S1API_StoredItemPlaceholders");
            _root.SetActive(false);
            Object.DontDestroyOnLoad(_root);
            return _root;
        }

        /// <summary>
        /// Removes the vanilla model from the copy and returns where it stood, in the copy's local space. Footprint
        /// tiles are placement markers, not model, and stay.
        /// </summary>
        private static Bounds StripModel(GameObject copy)
        {
            Transform root = copy.transform;
            Bounds spot = default;
            bool hasSpot = false;

            foreach (var filter in copy.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null || filter.GetComponent<S1Tiles.FootprintTile>() != null)
                    continue;
                Encapsulate(ref spot, ref hasSpot, root.worldToLocalMatrix * filter.transform.localToWorldMatrix,
                    filter.sharedMesh.bounds);
            }

            foreach (var lod in copy.GetComponentsInChildren<LODGroup>(true))
            {
                if (lod != null)
                    Object.DestroyImmediate(lod);
            }

            foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.GetComponent<S1Tiles.FootprintTile>() != null)
                    continue;
                Object.DestroyImmediate(renderer);
            }

            if (!hasSpot)
                spot = new Bounds(new Vector3(0f, 0.05f, 0f), new Vector3(0.1f, 0.1f, 0.1f));
            return spot;
        }

        /// <summary>
        /// Stands the icon on the spot of the removed model as two crossed sprites.
        /// </summary>
        private static void AddIcon(string itemId, Transform root, Bounds spot, Sprite? icon)
        {
            var stand = new IconStand { Spot = spot };
            IconStands[itemId] = stand;

            for (int i = 0; i < 2; i++)
            {
                var child = new GameObject(IconChildName);
                child.transform.SetParent(root, false);
                child.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                var sprite = child.AddComponent<SpriteRenderer>();
                sprite.sprite = icon;
                stand.Renderers.Add(sprite);
            }

            PlaceIcon(stand, icon);
        }

        private static void PlaceIcon(IconStand stand, Sprite? icon)
        {
            Bounds spot = stand.Spot;
            Vector2 spriteSize = icon != null ? (Vector2)icon.bounds.size : Vector2.one;
            float scale = FitScale(spriteSize.x, spriteSize.y, Mathf.Max(spot.size.x, spot.size.z), spot.size.y);
            if (scale <= 0f)
                scale = 0.1f;

            // Bottom of the sprite on the bottom of the removed model.
            var position = new Vector3(spot.center.x, spot.min.y + spriteSize.y * scale * 0.5f, spot.center.z);
            for (int i = 0; i < stand.Renderers.Count; i++)
            {
                if (stand.Renderers[i] == null)
                    continue;
                var transform = stand.Renderers[i].transform;
                transform.localPosition = position;
                transform.localScale = Vector3.one * scale;
            }
        }

        private static void Encapsulate(ref Bounds bounds, ref bool has, Matrix4x4 toRoot, Bounds local)
        {
            Vector3 c = local.center;
            Vector3 e = local.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z));
                Vector3 p = toRoot.MultiplyPoint3x4(corner);
                if (!has)
                {
                    bounds = new Bounds(p, Vector3.zero);
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(p);
                }
            }
        }

#if (IL2CPPMELON)
        private static int FootprintTileCount(S1Storage.StoredItem stored) =>
            stored._coordinateFootprintTilePairs?.Count ?? 0;

        private static Transform? BuildPoint(S1Storage.StoredItem stored) => stored.buildPoint;
#elif (MONOMELON)
        private static readonly FieldInfo? PairsField = typeof(S1Storage.StoredItem).GetField(
            "_coordinateFootprintTilePairs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo? BuildPointField = typeof(S1Storage.StoredItem).GetField(
            "buildPoint", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static int FootprintTileCount(S1Storage.StoredItem stored) =>
            PairsField?.GetValue(stored) is ICollection pairs ? pairs.Count : 0;

        private static Transform? BuildPoint(S1Storage.StoredItem stored) =>
            BuildPointField?.GetValue(stored) as Transform;
#endif
    }
}
