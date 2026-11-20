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
        /// <see cref="BuildEventArgs.Storage"/> is null for items that are not storage containers.
        /// </remarks>
        public static event Action<BuildEventArgs>? OnGridItemCreated;

        /// <summary>
        /// Event raised after a surface item (table-top item) is created.
        /// </summary>
        /// <remarks>
        /// <see cref="BuildEventArgs.Storage"/> is null for items that are not storage containers.
        /// </remarks>
        public static event Action<BuildEventArgs>? OnSurfaceItemCreated;

        /// <summary>
        /// Event raised after a buildable item component is initialized.
        /// This event fires for all buildable items and can be used for additional setup.
        /// </summary>
        /// <remarks>
        /// <see cref="BuildEventArgs.Storage"/> is null for items that are not storage containers.
        /// </remarks>
        public static event Action<BuildEventArgs>? OnBuildableItemInitialized;

        /// <summary>
        /// Event raised when the placement ghost of a grid, surface or procedural grid item is created, before the
        /// player sees it.
        /// </summary>
        /// <remarks>
        /// <see cref="GhostCreatedEventArgs.GameObject"/> is the ghost; filter by
        /// <see cref="GhostCreatedEventArgs.ItemId"/>.
        /// The game has already disabled colliders, navigation and networking on the ghost hierarchy that exists when
        /// the handler runs. A part a handler adds is not covered: the mod must disable those components on that part
        /// itself.
        /// Each handler runs on its own: one that throws is logged and the others still run.
        /// </remarks>
        public static event Action<GhostCreatedEventArgs>? OnGhostCreated;

        /// <summary>
        /// INTERNAL: Receives handler errors instead of the MelonLoader log in unit tests.
        /// </summary>
        internal static Action<string>? ErrorSinkForTesting;

        /// <summary>
        /// INTERNAL: Raises the OnGhostCreated event, one handler at a time.
        /// Called by Harmony patches in S1API.Internal.Patches.BuildingPatches.
        /// </summary>
        internal static void RaiseGhostCreated(GhostCreatedEventArgs args)
        {
            var handlers = OnGhostCreated;
            if (args == null || handlers == null)
                return;

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<GhostCreatedEventArgs>)handler)(args);
                }
                catch (Exception ex)
                {
                    var message = $"Exception in OnGhostCreated handler {handler.Method.DeclaringType?.FullName}.{handler.Method.Name} for item '{args.ItemId}': {ex}";
                    if (ErrorSinkForTesting != null)
                        ErrorSinkForTesting(message);
                    else
                        Logger.Error(message);
                }
            }
        }

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
