using System;

namespace S1API.Items.Clothing
{
    /// <summary>
    /// Represents the slot where a clothing item can be equipped.
    /// Mirrors ScheduleOne.Clothing.EClothingSlot.
    /// </summary>
    public enum ClothingSlot
    {
        /// <summary>Feet slot (shoes, boots).</summary>
        Feet = 0,
        /// <summary>Bottom slot (pants, shorts).</summary>
        Bottom = 1,
        /// <summary>Waist slot (belts).</summary>
        Waist = 2,
        /// <summary>Top slot (shirts).</summary>
        Top = 3,
        /// <summary>Outerwear slot (jackets, coats).</summary>
        Outerwear = 4,
        /// <summary>Hands slot (gloves).</summary>
        Hands = 5,
        /// <summary>Neck slot (necklaces, scarves).</summary>
        Neck = 6,
        /// <summary>Eyes slot (glasses, sunglasses).</summary>
        Eyes = 7,
        /// <summary>Head slot (hats, caps, helmets).</summary>
        Head = 8,
        /// <summary>Wrist slot (watches, bracelets).</summary>
        Wrist = 9
    }
}