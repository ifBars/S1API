using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
#if MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Vehicles = ScheduleOne.Vehicles;
#elif IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#endif

namespace S1API.Vehicles
{
    /// <summary>
    /// Provides read-only access to the base game's vehicle color metadata.
    /// </summary>
    public static class VehicleColorCatalog
    {
        private static readonly IReadOnlyList<VehicleColorMetadata> EmptyColors =
            new ReadOnlyCollection<VehicleColorMetadata>(
                Array.Empty<VehicleColorMetadata>());
        private static IVehicleColorMetadataProvider _provider =
            new NativeVehicleColorMetadataProvider();

        /// <summary>
        /// Gets a read-only snapshot of the available vehicle color metadata in native library order.
        /// Returns an empty list before the base game's vehicle color library is ready.
        /// </summary>
        public static IReadOnlyList<VehicleColorMetadata> Colors =>
            GetColors();

        /// <summary>
        /// Gets metadata for a vehicle color.
        /// </summary>
        /// <param name="color">The vehicle color to look up.</param>
        /// <returns>
        /// The color metadata, or <see langword="null"/> when the color is unavailable
        /// or the base game's vehicle color library is not ready.
        /// </returns>
        public static VehicleColorMetadata? GetColor(VehicleColor color)
        {
            TryGetColor(color, out VehicleColorMetadata? metadata);
            return metadata;
        }

        /// <summary>
        /// Tries to get metadata for a vehicle color.
        /// </summary>
        /// <param name="color">The vehicle color to look up.</param>
        /// <param name="metadata">The resolved metadata when available.</param>
        /// <returns><see langword="true"/> when metadata was found.</returns>
        public static bool TryGetColor(
            VehicleColor color,
            out VehicleColorMetadata? metadata)
        {
            if (!Enum.IsDefined(typeof(VehicleColor), color))
            {
                metadata = null;
                return false;
            }

            return _provider.TryGetColor(color, out metadata);
        }

        internal static void ResetForTesting(IVehicleColorMetadataProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        internal static void RestoreProviderForTesting()
        {
            _provider = new NativeVehicleColorMetadataProvider();
        }

        private static IReadOnlyList<VehicleColorMetadata> GetColors()
        {
            if (!_provider.TryGetColors(out IReadOnlyList<VehicleColorMetadata>? metadata)
                || metadata == null
                || metadata.Count == 0)
            {
                return EmptyColors;
            }

            return new ReadOnlyCollection<VehicleColorMetadata>(
                new List<VehicleColorMetadata>(metadata));
        }
    }

    internal interface IVehicleColorMetadataProvider
    {
        bool TryGetColors(out IReadOnlyList<VehicleColorMetadata>? metadata);

        bool TryGetColor(
            VehicleColor color,
            out VehicleColorMetadata? metadata);
    }

    internal sealed class NativeVehicleColorMetadataProvider :
        IVehicleColorMetadataProvider
    {
        public bool TryGetColors(out IReadOnlyList<VehicleColorMetadata>? metadata)
        {
            metadata = null;

            try
            {
                if (!S1DevUtilities.Singleton<S1Vehicles.Modification.VehicleColors>
                        .InstanceExists)
                {
                    return false;
                }

                S1Vehicles.Modification.VehicleColors vehicleColors =
                    S1DevUtilities.Singleton<S1Vehicles.Modification.VehicleColors>
                        .Instance;
                if (vehicleColors.colorLibrary == null)
                    return false;

                var colors = new List<VehicleColorMetadata>(
                    vehicleColors.colorLibrary.Count);
                for (int i = 0; i < vehicleColors.colorLibrary.Count; i++)
                {
                    S1Vehicles.Modification.VehicleColors.VehicleColorData? nativeColor =
                        vehicleColors.colorLibrary[i];
                    if (nativeColor == null)
                        continue;

                    VehicleColor color = (VehicleColor)nativeColor.color;
                    if (!Enum.IsDefined(typeof(VehicleColor), color))
                        continue;

                    colors.Add(new VehicleColorMetadata(
                        color,
                        nativeColor.colorName,
                        nativeColor.MaterialColor,
                        nativeColor.UIColor));
                }

                if (colors.Count == 0)
                    return false;

                metadata = new ReadOnlyCollection<VehicleColorMetadata>(colors);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetColor(
            VehicleColor color,
            out VehicleColorMetadata? metadata)
        {
            metadata = null;

            if (!TryGetColors(out IReadOnlyList<VehicleColorMetadata>? colors)
                || colors == null)
            {
                return false;
            }

            for (int i = 0; i < colors.Count; i++)
            {
                VehicleColorMetadata entry = colors[i];
                if (entry.Color == color)
                {
                    metadata = entry;
                    return true;
                }
            }

            return false;
        }
    }
}
