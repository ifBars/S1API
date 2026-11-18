using System;
using S1API.Internal.Deliveries;
using S1API.Property;
using UnityEngine;

namespace S1API.Deliveries
{
    public static partial class DeliveryRegistry
    {
        /// <summary>
        /// Offers a property as a destination in the phone's delivery app while <paramref name="available"/> returns true.
        /// </summary>
        /// <remarks>
        /// Vanilla lists only owned properties that accept deliveries. A registered property is added to that list
        /// each time the app builds it. The game still refuses an order to a property without a loading dock,
        /// so pair this with <see cref="AddLoadingDock"/> when the property has none of its own.
        /// Dispose the returned handle to stop offering the property.
        /// </remarks>
        /// <param name="property">The property to offer.</param>
        /// <param name="available">Called each time the list is built; return false to hide the property.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static IDisposable AddDestination(PropertyWrapper property, Func<bool> available) =>
            DeliveryDestinationRuntime.AddDestination(property, available);

        /// <summary>
        /// Adds a loading dock to a property once <paramref name="dock"/> returns a transform with a native
        /// <c>LoadingDock</c> component on it or below it.
        /// </summary>
        /// <remarks>
        /// S1API asks the provider whenever the property's docks matter: the destination list, the bay check, an
        /// arrival and <see cref="RefreshLoadingDocks"/>. Return null while the dock does not exist yet. The dock is
        /// appended to the property's dock list, so its index stays stable while it is registered.
        /// Dispose the returned handle to remove the dock again.
        /// </remarks>
        /// <param name="property">The property that owns the dock.</param>
        /// <param name="dock">Returns the dock's transform, or null while it does not exist.</param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static IDisposable AddLoadingDock(PropertyWrapper property, Func<Transform?> dock) =>
            DeliveryDestinationRuntime.AddLoadingDock(property, dock);

        /// <summary>
        /// Holds the arrival of a delivery while <paramref name="hold"/> returns true for it.
        /// </summary>
        /// <remarks>
        /// A held delivery keeps the status "arrived", so a save stores it as arrived, but its van does not appear
        /// yet. S1API retries held deliveries every game minute, after <see cref="RefreshLoadingDocks"/> and when a
        /// hold is disposed. An arrival whose loading dock does not exist is held the same way without a
        /// registered hold. Runs on every peer.
        /// </remarks>
        /// <param name="hold">Return true to hold this delivery's arrival.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hold"/> is null.</exception>
        public static IDisposable AddArrivalHold(Func<Delivery, bool> hold) =>
            DeliveryDestinationRuntime.AddArrivalHold(hold);

        /// <summary>
        /// Asks every registered loading dock provider again, attaches the docks that exist now and retries
        /// held arrivals.
        /// </summary>
        /// <remarks>Call it when a mod has just built a dock, so waiting deliveries do not wait for the next game minute.</remarks>
        public static void RefreshLoadingDocks() => DeliveryDestinationRuntime.Refresh("RefreshLoadingDocks");
    }
}
