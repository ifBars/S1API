using System;
using UnityEngine;
using S1API.Items;

#if (IL2CPPMELON)
using S1Building = Il2CppScheduleOne.Building;
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1Tiles = Il2CppScheduleOne.Tiles;
using Surface = Il2CppScheduleOne.Building.Surface;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Building = ScheduleOne.Building;
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1Tiles = ScheduleOne.Tiles;
using Surface = ScheduleOne.Building.Surface;
#endif

namespace S1API.Building
{
    /// <summary>
    /// Provides control over the building system.
    /// </summary>
    public static class BuildManager
    {
        /// <summary>
        /// Gets whether the player is currently in building mode.
        /// </summary>
        public static bool IsBuilding => S1Building.BuildManager.Instance.isBuilding;

        /// <summary>
        /// Gets the current build handler GameObject, if any.
        /// </summary>
        public static GameObject? CurrentBuildHandler => S1Building.BuildManager.Instance.currentBuildHandler;

        /// <summary>
        /// Starts building mode with the specified item.
        /// </summary>
        /// <param name="item">The item instance to build.</param>
        public static void StartBuilding(ItemInstance item) =>
            S1Building.BuildManager.Instance.StartBuilding(item.S1ItemInstance);

        /// <summary>
        /// Stops the current building mode.
        /// </summary>
        public static void StopBuilding() =>
            S1Building.BuildManager.Instance.StopBuilding();

        /// <summary>
        /// Creates a grid item from an item instance.
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="grid">The target grid.</param>
        /// <param name="originCoordinate">The origin coordinate on the grid.</param>
        /// <param name="rotation">The rotation (0, 90, 180, 270).</u003e
        /// <param name="guid">Optional GUID.</u003e
        /// <returns>The created grid item GameObject.</u003e
        public static GameObject CreateGridItem(ItemInstance item, S1Tiles.Grid grid, Vector2 originCoordinate, 
            int rotation, string guid = "")
        {
            var gridItem = S1Building.BuildManager.Instance.CreateGridItem(item.S1ItemInstance, grid, originCoordinate, rotation, guid);
            return gridItem.gameObject;
        }

        /// <summary>
        /// Creates a surface item from an item instance.
        /// </summary>
        /// <param name="item">The item to create.</u003e
        /// <param name="parentSurface">The parent surface.</u003e
        /// <param name="relativePosition">Position relative to the surface.</u003e
        /// <param name="relativeRotation">Rotation relative to the surface.</u003e
        /// <param name="guid">Optional GUID.</u003e
        /// <returns>The created surface item GameObject.</u003e
        public static GameObject CreateSurfaceItem(ItemInstance item, Surface parentSurface, 
            Vector3 relativePosition, Quaternion relativeRotation, string guid = "")
        {
            var surfaceItem = S1Building.BuildManager.Instance.CreateSurfaceItem(item.S1ItemInstance, parentSurface, relativePosition, relativeRotation, guid);
            return surfaceItem.gameObject;
        }
    }
}
