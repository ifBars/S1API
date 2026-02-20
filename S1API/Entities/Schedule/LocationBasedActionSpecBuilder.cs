using UnityEngine;
using S1API.Entities.Equippables;
using S1API.Graffiti;
using S1API.Map;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Fluent sub-builder for composing a <see cref="LocationBasedActionSpec"/> with type-safe arrive behavior selection.
    /// </summary>
    public sealed class LocationBasedActionSpecBuilder
    {
        private readonly PrefabScheduleBuilder _plan;
        private readonly LocationBasedActionSpec _spec;
        private bool _isCommitted;

        /// <summary>
        /// INTERNAL: Creates a location-based action sub-builder.
        /// </summary>
        /// <param name="plan">Owning schedule builder.</param>
        /// <param name="destination">Destination position for the location-based action.</param>
        /// <param name="startTime">Action start time in minutes from midnight.</param>
        /// <param name="durationMinutes">Action duration in minutes.</param>
        internal LocationBasedActionSpecBuilder(PrefabScheduleBuilder plan, Vector3 destination, int startTime, int durationMinutes)
        {
            _plan = plan;
            _spec = new LocationBasedActionSpec
            {
                Destination = destination,
                StartTime = startTime,
                DurationMinutes = durationMinutes
            };
        }

        /// <summary>
        /// Sets the distance threshold within which the NPC is considered to have arrived.
        /// </summary>
        /// <param name="value">Arrival threshold in world units.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder Within(float value)
        {
            _spec.Within = value;
            return this;
        }

        /// <summary>
        /// Sets whether the NPC should face destination direction while walking.
        /// </summary>
        /// <param name="value">Whether to face destination direction.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder FaceDestinationDirection(bool value = true)
        {
            _spec.FaceDestinationDirection = value;
            return this;
        }

        /// <summary>
        /// Sets whether to warp the NPC to destination if the action is skipped.
        /// </summary>
        /// <param name="value">Whether to warp if skipped.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder WarpIfSkipped(bool value = true)
        {
            _spec.WarpIfSkipped = value;
            return this;
        }

        /// <summary>
        /// Sets an explicit action name for the generated location-based schedule action.
        /// </summary>
        /// <param name="value">Custom action name.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder Named(string value)
        {
            _spec.Name = value;
            return this;
        }

        /// <summary>
        /// For Graffiti: pick a spray surface in the given region. Call before <see cref="OnArriveGraffiti"/>.
        /// </summary>
        /// <param name="region">The map region to search for surfaces.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder WithSpraySurfaceInRegion(Region region)
        {
            _spec.GraffitiRegion = region;
            return this;
        }

        /// <summary>
        /// For Graffiti: use a specific spray surface by GUID. Call before <see cref="OnArriveGraffiti"/>.
        /// </summary>
        /// <param name="guid">The surface GUID (from <see cref="SpraySurface.GUID"/>).</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder WithSpraySurface(System.Guid guid)
        {
            _spec.GraffitiSurfaceGuid = guid;
            return this;
        }

        /// <summary>
        /// For Graffiti: use a specific spray surface. Call before <see cref="OnArriveGraffiti"/>.
        /// </summary>
        /// <param name="surface">The spray surface (e.g. from <see cref="GraffitiManager.FindNearestUntaggedSurface"/>).</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder WithSpraySurface(SpraySurface surface)
        {
            if (surface != null)
                _spec.GraffitiSurfaceGuid = surface.GUID;
            return this;
        }

        /// <summary>
        /// For HoldItem: specify which item to hold. Call before <see cref="OnArriveHoldItem"/>.
        /// </summary>
        /// <param name="equippablePath">Use <see cref="EquippablePath"/> (e.g. <see cref="EquippablePath.Phone_Lowered"/>) or pass custom path string.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder WithItem(EquippablePath equippablePath)
        {
            _spec.EquippableAssetPath = equippablePath.ResourcePath;
            return this;
        }

        /// <summary>
        /// For Drinking: specify which drink to use. Call before <see cref="OnArriveDrinking"/>.
        /// </summary>
        /// <param name="drinkEquippablePath">Use <see cref="EquippablePath"/> (e.g. <see cref="EquippablePath.Beer"/>) or pass custom path string.</param>
        /// <returns>This sub-builder for chaining.</returns>
        public LocationBasedActionSpecBuilder WithDrink(EquippablePath drinkEquippablePath)
        {
            _spec.DrinkEquippablePath = drinkEquippablePath.ResourcePath;
            return this;
        }

        /// <summary>
        /// Finalizes this location-based action with no arrive behavior.
        /// </summary>
        /// <returns>The parent schedule builder for continued chaining.</returns>
        public PrefabScheduleBuilder OnArriveNone() =>
            Commit(LocationArriveBehaviour.None);

        /// <summary>
        /// Finalizes this location-based action to trigger smoke break behavior on arrival.
        /// </summary>
        /// <returns>The parent schedule builder for continued chaining.</returns>
        public PrefabScheduleBuilder OnArriveSmokeBreak() =>
            Commit(LocationArriveBehaviour.SmokeBreak);

        /// <summary>
        /// Finalizes this location-based action to trigger graffiti behavior on arrival.
        /// </summary>
        /// <returns>The parent schedule builder for continued chaining.</returns>
        public PrefabScheduleBuilder OnArriveGraffiti() =>
            Commit(LocationArriveBehaviour.Graffiti);

        /// <summary>
        /// Finalizes this location-based action to trigger drinking action on arrival.
        /// </summary>
        /// <returns>The parent schedule builder for continued chaining.</returns>
        public PrefabScheduleBuilder OnArriveDrinking() =>
            Commit(LocationArriveBehaviour.Drinking);

        /// <summary>
        /// Finalizes this location-based action to trigger item holding action on arrival.
        /// </summary>
        /// <returns>The parent schedule builder for continued chaining.</returns>
        public PrefabScheduleBuilder OnArriveHoldItem() =>
            Commit(LocationArriveBehaviour.HoldItem);

        private PrefabScheduleBuilder Commit(LocationArriveBehaviour arriveBehaviour)
        {
            if (_isCommitted)
                return _plan;

            _spec.ArriveBehaviour = arriveBehaviour;
            _plan.Add(_spec);
            _isCommitted = true;
            return _plan;
        }
    }
}
