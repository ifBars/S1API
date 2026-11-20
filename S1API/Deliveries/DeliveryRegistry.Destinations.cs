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
        /// <param name="available">
        /// Called each time the list is built; return false to stop S1API from adding the property. A property that
        /// vanilla already lists (an owned property that accepts deliveries) stays listed.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static IDisposable AddDestination(PropertyWrapper property, Func<bool> available) =>
            DeliveryDestinationRuntime.AddDestination(property, available);

        /// <summary>
        /// Adds a loading dock to a property once <paramref name="dock"/> returns a transform with a native
        /// <c>LoadingDock</c> component on it or below it.
        /// </summary>
        /// <remarks>
        /// S1API asks the provider whenever the property's docks matter: the destination list, the bay check, an
        /// arrival and <see cref="RefreshLoadingDocks"/>. Return null while the dock does not exist. The dock gets its
        /// own slot at the end of the property's dock list and keeps that number while the same save is played, also
        /// across reloads of it in the same game process, because deliveries and saves store the dock number. A game
        /// restart forgets the slots: the docks get their numbers again in the order they appear, so docks that appear
        /// in another order than before get other numbers. Loading another save drops the slots. When a dock that
        /// S1API did not add takes a reserved number after a reload, the registration and the deliveries saved to that
        /// number move to a new number at the end of the list. While the provider returns null, and after the handle
        /// is disposed, the slot stays reserved: it never
        /// counts as free, the delivery app does not offer it, and only the same dock can take it again (the same
        /// object, or a dock from the same mod with the same GUID, or with the same hierarchy path when it has no
        /// GUID). A mod that builds its dock again after a reload should give it the same GUID every time, or it gets a
        /// new dock number. A dock the property already lists is used as it is and never removed.
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
        /// yet. The van keeps the delivered items, and a save keeps them. S1API retries held deliveries every game
        /// minute, after <see cref="RefreshLoadingDocks"/> and when a hold is disposed. An arrival whose loading dock
        /// does not exist is held the same way without a registered hold. A hold that disposes itself while it runs
        /// does not hold.
        /// <para>
        /// Runs on every peer, so a hold must give the same answer on the host and on every client. In co-op the host
        /// decides: a hold on the host keeps the arrival, its notification and the van back for everyone. A hold that
        /// only a client applies keeps only that client's van back; the host's status change and notification still
        /// reach that client.
        /// </para>
        /// </remarks>
        /// <param name="hold">Return true to hold this delivery's arrival.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hold"/> is null.</exception>
        public static IDisposable AddArrivalHold(Func<Delivery, bool> hold) =>
            DeliveryDestinationRuntime.AddArrivalHold(hold);

        /// <summary>
        /// Asks every registered loading dock provider again, attaches the docks that exist now and retries
        /// held arrivals.
        /// </summary>
        /// <remarks>
        /// Call it when a mod has just built or removed a dock, so waiting deliveries do not wait for the next game
        /// minute. Called from inside a dock provider, it does not ask that provider again; the provider's own return
        /// value counts. Called from inside an arrival hold, it does not check the delivery that hold is deciding;
        /// the hold's own return value counts.
        /// </remarks>
        public static void RefreshLoadingDocks() => DeliveryDestinationRuntime.Refresh("RefreshLoadingDocks");
    }
}
