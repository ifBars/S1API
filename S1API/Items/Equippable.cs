#if (IL2CPPMELON)
using S1Equipping = Il2CppScheduleOne.Equipping;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Equipping = ScheduleOne.Equipping;
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

namespace S1API.Items
{
    /// <summary>
    /// Represents an equippable component that can be attached to items.
    /// Provides a wrapper around the game's native equippable system.
    /// </summary>
    public class Equippable
    {
        /// <summary>
        /// INTERNAL: A reference to the native game equippable component.
        /// </summary>
        internal S1Equipping.Equippable S1Equippable { get; }

        /// <summary>
        /// INTERNAL: Wraps an existing native equippable component.
        /// </summary>
        internal Equippable(S1Equipping.Equippable equippable)
        {
            S1Equippable = equippable;
        }

        /// <summary>
        /// Gets or sets whether the player can interact with objects when this item is equipped.
        /// </summary>
        public bool CanInteractWhenEquipped
        {
            get => S1Equippable.CanInteractWhenEquipped;
            set => S1Equippable.CanInteractWhenEquipped = value;
        }

        /// <summary>
        /// Gets or sets whether the player can pick up items when this item is equipped.
        /// </summary>
        public bool CanPickUpWhenEquipped
        {
            get => S1Equippable.CanPickUpWhenEquipped;
            set => S1Equippable.CanPickUpWhenEquipped = value;
        }

        /// <summary>
        /// Called when this item is equipped by the player.
        /// Override this in derived classes to implement custom equip behavior.
        /// </summary>
        /// <param name="item">The item instance being equipped.</param>
        public virtual void Equip(ItemInstance item)
        {
            var s1Instance = item?.S1ItemInstance;
            if (s1Instance != null)
            {
                S1Equippable.Equip(s1Instance);
            }
        }

        /// <summary>
        /// Called when this item is unequipped by the player.
        /// Override this in derived classes to implement custom unequip behavior.
        /// </summary>
        public virtual void Unequip()
        {
            S1Equippable.Unequip();
        }
    }
}

