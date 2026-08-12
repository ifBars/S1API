using System.Collections.Generic;
using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Builder-owned presentation and safe scalar defaults extracted from a furniture donor.
    /// </summary>
    internal sealed class FurnitureCloneSource
    {
        internal FurnitureCloneSource(
            string donorId,
            GameObject model,
            FurniturePlacementMode placementMode,
            IReadOnlyList<FurnitureFootprintCoordinate>? footprint,
            FurnitureSurfaceType surfaceTypes,
            bool allowSurfaceRotation,
            BuildSoundType buildSound,
            int stackLimit,
            float purchasePrice,
            float resellMultiplier,
            Sprite? icon)
        {
            DonorId = donorId;
            Model = model;
            PlacementMode = placementMode;
            Footprint = footprint;
            SurfaceTypes = surfaceTypes;
            AllowSurfaceRotation = allowSurfaceRotation;
            BuildSound = buildSound;
            StackLimit = stackLimit;
            PurchasePrice = purchasePrice;
            ResellMultiplier = resellMultiplier;
            Icon = icon;
        }

        internal string DonorId { get; }
        internal GameObject Model { get; }
        internal FurniturePlacementMode PlacementMode { get; }
        internal IReadOnlyList<FurnitureFootprintCoordinate>? Footprint { get; }
        internal FurnitureSurfaceType SurfaceTypes { get; }
        internal bool AllowSurfaceRotation { get; }
        internal BuildSoundType BuildSound { get; }
        internal int StackLimit { get; }
        internal float PurchasePrice { get; }
        internal float ResellMultiplier { get; }
        internal Sprite? Icon { get; }
    }

    internal readonly struct FurnitureFootprintCoordinate
    {
        internal FurnitureFootprintCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        internal int X { get; }
        internal int Y { get; }
    }
}
