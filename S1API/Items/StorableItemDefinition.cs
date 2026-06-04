#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

using S1API.Leveling;
using UnityEngine;

namespace S1API.Items
{
    /// <summary>
    /// Represents an item definition that can be purchased, sold, and stored in inventories.
    /// Extends <see cref="ItemDefinition"/> with economic properties.
    /// </summary>
    /// <remarks>
    /// In Schedule One, all items are StorableItemDefinition or subclasses thereof.
    /// The base ItemDefinition class is abstract and not used directly in gameplay.
    /// </remarks>
    public class StorableItemDefinition : ItemDefinition
    {
        /// <summary>
        /// INTERNAL: Wraps an existing native storable item definition.
        /// </summary>
        internal StorableItemDefinition(S1ItemFramework.StorableItemDefinition definition) 
            : base(definition)
        {
            S1StorableItemDefinition = definition;
        }

        /// <summary>
        /// INTERNAL: A reference to the native game storable item definition.
        /// </summary>
        internal S1ItemFramework.StorableItemDefinition S1StorableItemDefinition { get; }

        /// <summary>
        /// The base purchase price for this item in shops.
        /// </summary>
        public float BasePurchasePrice
        {
            get => S1StorableItemDefinition.BasePurchasePrice;
            set => S1StorableItemDefinition.BasePurchasePrice = value;
        }

        /// <summary>
        /// The resell multiplier (0.0 to 1.0) that determines how much of the purchase price
        /// can be recovered when selling the item.
        /// </summary>
        public float ResellMultiplier
        {
            get => S1StorableItemDefinition.ResellMultiplier;
            set => S1StorableItemDefinition.ResellMultiplier = value;
        }

        /// <summary>
        /// Gets whether this item is currently unlocked (available for purchase/use).
        /// </summary>
        public bool IsUnlocked =>
            S1StorableItemDefinition.IsUnlocked;

        /// <summary>
        /// Whether purchasing this item requires the player to be at or above a certain level.
        /// </summary>
        public bool RequiresLevelToPurchase
        {
            get => S1StorableItemDefinition.RequiresLevelToPurchase;
            set => S1StorableItemDefinition.RequiresLevelToPurchase = value;
        }

        /// <summary>
        /// The required player level to purchase this item, if <see cref="RequiresLevelToPurchase"/> is true.
        /// </summary>
        public FullRank RequiredRank
        {
            get => FullRank.FromNative(S1StorableItemDefinition.RequiredRank);
            set => S1StorableItemDefinition.RequiredRank = value.ToNative();
        }

        /// <summary>
        /// Gets whether this item has a StationItem assigned (used by station/minigame tasks, e.g., Chemistry Station).
        /// </summary>
        public bool HasStationItem =>
            S1StorableItemDefinition.StationItem != null;

        /// <summary>
        /// Gets the StationItem prefab GameObject for this item, if any.
        /// </summary>
        /// <remarks>
        /// This is primarily used for debugging and tooling. Prefer configuring StationItem via
        /// <see cref="StorableItemDefinitionBuilder.WithStationItem"/> during build/registration.
        /// </remarks>
        public GameObject? StationItemPrefab =>
            S1StorableItemDefinition.StationItem?.gameObject;
    }
}
