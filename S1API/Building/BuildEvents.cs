using System;
using S1API.Logging;

namespace S1API.Building
{
    /// <summary>
    /// Provides events for the building system, allowing mods to customize items as they're placed.
    /// Subscribe to these events instead of using Harmony patches on BuildManager.
    /// </summary>
    /// <example>
    /// <code>
    /// // Customize materials when items are placed
    /// BuildEvents.OnGridItemCreated += (args) =>
    /// {
    ///     if (args.ItemId == "my_custom_rack")
    ///     {
    ///         MaterialHelper.ReplaceMaterials(args.GameObject, ...);
    ///     }
    /// };
    /// </code>
    /// </example>
    public static class BuildEvents
    {
        private static readonly Log Logger = new Log("BuildEvents");

        /// <summary>
        /// Event raised after a grid item (wall-mounted or floor-placed) is created in the world.
        /// This is the most common build event for furniture and storage items.
        /// </summary>
        /// <remarks>
        /// Subscribers receive a BuildEventArgs containing the item and GameObject.
        /// The GameObject can be modified to change appearance or behavior.
        /// </remarks>
        public static event Action<BuildEventArgs> OnGridItemCreated;

        /// <summary>
        /// Event raised after a surface item (table-top item) is created.
        /// </summary>
        public static event Action<BuildEventArgs> OnSurfaceItemCreated;

        /// <summary>
        /// Event raised after a buildable item component is initialized.
        /// This event fires for all buildable items and can be used for additional setup.
        /// </summary>
        public static event Action<BuildEventArgs> OnBuildableItemInitialized;

        /// <summary>
        /// INTERNAL: Raises the OnGridItemCreated event.
        /// Called by Harmony patches in S1API.Internal.Patches.BuildingPatches.
        /// </summary>
        internal static void RaiseGridItemCreated(BuildEventArgs args)
        {
            if (args == null)
                return;

            try
            {
                OnGridItemCreated?.Invoke(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in OnGridItemCreated handler for item '{args.ItemId}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Raises the OnSurfaceItemCreated event.
        /// Called by Harmony patches in S1API.Internal.Patches.BuildingPatches.
        /// </summary>
        internal static void RaiseSurfaceItemCreated(BuildEventArgs args)
        {
            if (args == null)
                return;

            try
            {
                OnSurfaceItemCreated?.Invoke(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in OnSurfaceItemCreated handler for item '{args.ItemId}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Raises the OnBuildableItemInitialized event.
        /// Called by Harmony patches in S1API.Internal.Patches.BuildingPatches.
        /// </summary>
        internal static void RaiseBuildableItemInitialized(BuildEventArgs args)
        {
            if (args == null)
                return;

            try
            {
                OnBuildableItemInitialized?.Invoke(args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in OnBuildableItemInitialized handler for item '{args.ItemId}': {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
