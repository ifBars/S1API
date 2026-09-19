using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
#if MONOMELON
using S1Clothing = ScheduleOne.Clothing;
using S1DevUtilities = ScheduleOne.DevUtilities;
#elif IL2CPPMELON
using S1Clothing = Il2CppScheduleOne.Clothing;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#endif

namespace S1API.Items.Clothing
{
    /// <summary>
    /// Provides read-only access to the base game's clothing slot and color metadata.
    /// </summary>
    public static class ClothingMetadataCatalog
    {
        private static readonly IReadOnlyList<ClothingSlotMetadata> EmptySlots =
            new ReadOnlyCollection<ClothingSlotMetadata>(
                Array.Empty<ClothingSlotMetadata>());
        private static readonly IReadOnlyList<ClothingColorMetadata> EmptyColors =
            new ReadOnlyCollection<ClothingColorMetadata>(
                Array.Empty<ClothingColorMetadata>());
        private static IClothingMetadataProvider _provider =
            new NativeClothingMetadataProvider();

        /// <summary>
        /// Gets a read-only snapshot of all available clothing slot metadata.
        /// Returns an empty list before the base game's clothing utility is ready.
        /// </summary>
        public static IReadOnlyList<ClothingSlotMetadata> Slots =>
            GetSlots();

        /// <summary>
        /// Gets a read-only snapshot of all available clothing color metadata.
        /// Returns an empty list before the base game's clothing utility is ready.
        /// </summary>
        public static IReadOnlyList<ClothingColorMetadata> Colors =>
            GetColors();

        /// <summary>
        /// Gets metadata for a clothing slot.
        /// </summary>
        /// <param name="slot">The clothing slot to look up.</param>
        /// <returns>
        /// The slot metadata, or <see langword="null"/> when the slot is unavailable
        /// or the base game's clothing utility is not ready.
        /// </returns>
        public static ClothingSlotMetadata? GetSlot(ClothingSlot slot)
        {
            TryGetSlot(slot, out ClothingSlotMetadata? metadata);
            return metadata;
        }

        /// <summary>
        /// Tries to get metadata for a clothing slot.
        /// </summary>
        /// <param name="slot">The clothing slot to look up.</param>
        /// <param name="metadata">The resolved metadata when available.</param>
        /// <returns><see langword="true"/> when metadata was found.</returns>
        public static bool TryGetSlot(
            ClothingSlot slot,
            out ClothingSlotMetadata? metadata)
        {
            if (!Enum.IsDefined(typeof(ClothingSlot), slot))
            {
                metadata = null;
                return false;
            }

            return _provider.TryGetSlot(slot, out metadata);
        }

        /// <summary>
        /// Gets metadata for a clothing color.
        /// </summary>
        /// <param name="color">The clothing color to look up.</param>
        /// <returns>
        /// The color metadata, or <see langword="null"/> when the color is unavailable
        /// or the base game's clothing utility is not ready.
        /// </returns>
        public static ClothingColorMetadata? GetColor(ClothingColor color)
        {
            TryGetColor(color, out ClothingColorMetadata? metadata);
            return metadata;
        }

        /// <summary>
        /// Tries to get metadata for a clothing color.
        /// </summary>
        /// <param name="color">The clothing color to look up.</param>
        /// <param name="metadata">The resolved metadata when available.</param>
        /// <returns><see langword="true"/> when metadata was found.</returns>
        public static bool TryGetColor(
            ClothingColor color,
            out ClothingColorMetadata? metadata)
        {
            if (!Enum.IsDefined(typeof(ClothingColor), color))
            {
                metadata = null;
                return false;
            }

            return _provider.TryGetColor(color, out metadata);
        }

        internal static void ResetForTesting(IClothingMetadataProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        internal static void RestoreProviderForTesting()
        {
            _provider = new NativeClothingMetadataProvider();
        }

        private static IReadOnlyList<ClothingSlotMetadata> GetSlots()
        {
            var metadata = new List<ClothingSlotMetadata>();
            foreach (ClothingSlot slot in Enum.GetValues(typeof(ClothingSlot)))
            {
                if (_provider.TryGetSlot(slot, out ClothingSlotMetadata? entry)
                    && entry != null)
                {
                    metadata.Add(entry);
                }
            }

            return metadata.Count == 0
                ? EmptySlots
                : new ReadOnlyCollection<ClothingSlotMetadata>(metadata);
        }

        private static IReadOnlyList<ClothingColorMetadata> GetColors()
        {
            var metadata = new List<ClothingColorMetadata>();
            foreach (ClothingColor color in Enum.GetValues(typeof(ClothingColor)))
            {
                if (_provider.TryGetColor(color, out ClothingColorMetadata? entry)
                    && entry != null)
                {
                    metadata.Add(entry);
                }
            }

            return metadata.Count == 0
                ? EmptyColors
                : new ReadOnlyCollection<ClothingColorMetadata>(metadata);
        }
    }

    internal interface IClothingMetadataProvider
    {
        bool TryGetSlot(
            ClothingSlot slot,
            out ClothingSlotMetadata? metadata);

        bool TryGetColor(
            ClothingColor color,
            out ClothingColorMetadata? metadata);
    }

    internal sealed class NativeClothingMetadataProvider :
        IClothingMetadataProvider
    {
        public bool TryGetSlot(
            ClothingSlot slot,
            out ClothingSlotMetadata? metadata)
        {
            metadata = null;

            try
            {
                var configuration = Resources
                    .FindObjectsOfTypeAll<S1Clothing.ClothingConfiguration>()
                    .FirstOrDefault();
                if (configuration == null)
                    return false;

                S1Clothing.ClothingConfiguration.ClothingSlotData nativeMetadata =
                    configuration.GetSlotData((S1Clothing.EClothingSlot)slot);
                if (nativeMetadata.IsNull())
                    return false;

                metadata = new ClothingSlotMetadata(
                    slot,
                    nativeMetadata.Name ?? slot.ToString(),
                    nativeMetadata.Icon);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetColor(
            ClothingColor color,
            out ClothingColorMetadata? metadata)
        {
            metadata = null;

            try
            {
                var configuration = Resources
                    .FindObjectsOfTypeAll<S1Clothing.ClothingConfiguration>()
                    .FirstOrDefault();
                if (configuration == null)
                    return false;

                S1Clothing.ClothingConfiguration.ColorData nativeMetadata =
                    configuration.GetColorData((S1Clothing.EClothingColor)color);
                if (nativeMetadata.IsNull())
                    return false;

                metadata = new ClothingColorMetadata(
                    color,
                    color.ToString(),
                    nativeMetadata.ActualColor,
                    nativeMetadata.LabelColor);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
