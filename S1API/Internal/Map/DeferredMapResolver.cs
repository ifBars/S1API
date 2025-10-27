using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;
using S1API.Logging;
using S1API.Map;

namespace S1API.Internal.Map
{
    /// <summary>
    /// INTERNAL: Manages deferred resolutions for map entities (buildings, parking lots, delivery locations, etc.)
    /// that are referenced during ConfigurePrefab but don't exist until Main scene loads.
    /// </summary>
    internal static class DeferredMapResolver
    {
        private static readonly Log Logger = new Log("DeferredMapResolver");
        private static readonly List<DeferredLookup> PendingLookups = new List<DeferredLookup>();
        private static bool MainSceneLoaded = false;

        /// <summary>
        /// Registers a deferred lookup that will be resolved when Main scene loads.
        /// </summary>
        public static void RegisterDeferredLookup(DeferredLookup lookup)
        {
            if (lookup == null)
                return;

            lock (PendingLookups)
            {
                if (MainSceneLoaded && !lookup.IsResolved)
                {
                    // Already in Main scene - try to resolve immediately
                    TryResolveLookup(lookup);
                }
                else if (!PendingLookups.Contains(lookup))
                {
                    PendingLookups.Add(lookup);
                }
            }
        }

        /// <summary>
        /// Attempts to resolve all pending lookups immediately.
        /// Called when Main scene has been initialized and registries are populated.
        /// </summary>
        public static void ResolveAll()
        {
            if (MainSceneLoaded)
                return;

            MainSceneLoaded = true;
            Logger.Msg("Main scene loaded - resolving pending map entity lookups");

            List<DeferredLookup> pendingCopy;
            lock (PendingLookups)
            {
                pendingCopy = new List<DeferredLookup>(PendingLookups);
            }

            int resolvedCount = 0;
            int failedCount = 0;

            foreach (var lookup in pendingCopy)
            {
                if (lookup.IsResolved)
                    continue;

                try
                {
                    if (TryResolveLookup(lookup))
                        resolvedCount++;
                    else
                        failedCount++;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to resolve deferred lookup: {ex.Message}");
                    failedCount++;
                }
            }

            lock (PendingLookups)
            {
                PendingLookups.RemoveAll(l => l.IsResolved);
            }
        }

        /// <summary>
        /// Attempts to resolve a single lookup by invoking its callback if the target is found.
        /// </summary>
        private static bool TryResolveLookup(DeferredLookup lookup)
        {
            object resolved = null;

            // Try typed lookup first
            if (lookup.IdentifierType != null)
            {
                resolved = ResolveTypedLookup(lookup.IdentifierType);
            }
            // Otherwise try name-based lookup
            else if (!string.IsNullOrEmpty(lookup.IdentifierName))
            {
                // Name-based lookups are handled by the specific registries
                // We can't generically resolve them here without knowing the type
                lookup.MarkResolved();
                return false;
            }

            if (resolved != null)
            {
                try
                {
                    lookup.Callback(resolved);
                    lookup.MarkResolved();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Callback failed for deferred lookup: {ex.Message}");
                    lookup.MarkResolved();
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves a typed lookup by delegating to the appropriate registry.
        /// </summary>
        private static object ResolveTypedLookup(Type identifierType)
        {
            try
            {
                // Try ParkingLotRegistry.Get<T>()
                var parkingLotGetMethod = typeof(ParkingLotRegistry).GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                if (parkingLotGetMethod != null)
                {
                    var genericMethod = parkingLotGetMethod.MakeGenericMethod(identifierType);
                    var result = genericMethod.Invoke(null, null);
                    if (result is ParkingLotWrapper parkingLot && parkingLot != null)
                    {
                        return parkingLot;
                    }
                }
                
                // Try Building.Get<T>()
                var buildingGetMethod = typeof(Building).GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                if (buildingGetMethod != null)
                {
                    var genericMethod = buildingGetMethod.MakeGenericMethod(identifierType);
                    var result = genericMethod.Invoke(null, null);
                    if (result is Building building && building != null)
                    {
                        return building;
                    }
                }
                
                // Try DeliveryLocation.Get<T>()
                var deliveryLocationGetMethod = typeof(DeliveryLocation).GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                if (deliveryLocationGetMethod != null)
                {
                    var genericMethod = deliveryLocationGetMethod.MakeGenericMethod(identifierType);
                    var result = genericMethod.Invoke(null, null);
                    if (result is DeliveryLocation deliveryLocation && deliveryLocation != null)
                    {
                        return deliveryLocation;
                    }
                }
            }
            catch { }
            
            return null;
        }

        /// <summary>
        /// Clears all pending lookups. Called during scene cleanup.
        /// </summary>
        public static void Clear()
        {
            lock (PendingLookups)
            {
                PendingLookups.Clear();
            }
            MainSceneLoaded = false;
            Logger.Msg("Cleared deferred lookups");
        }

        /// <summary>
        /// Attempts to resolve a deferred lookup immediately.
        /// Used when we're already in Main scene and want to get the result right away.
        /// </summary>
        public static void TryResolve(DeferredLookup lookup)
        {
            if (lookup == null || lookup.IsResolved)
                return;

            TryResolveLookup(lookup);
        }

        /// <summary>
        /// Checks if we're currently in a scene where map entities exist.
        /// </summary>
        public static bool IsMainScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                return scene != null && string.Equals(scene.name, "Main", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if we're currently in a scene where map entities don't exist yet.
        /// </summary>
        public static bool IsMenuScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                return scene != null && !string.Equals(scene.name, "Main", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true; // Assume menu scene if we can't check
            }
        }
    }
}

