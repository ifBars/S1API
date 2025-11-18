#if (IL2CPPMELON)
using S1Levelling = Il2CppScheduleOne.Levelling;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Levelling = ScheduleOne.Levelling;
#endif

using UnityEngine;

namespace S1API.Leveling
{
    /// <summary>
    /// Represents an entry that becomes available when a rank threshold is met.
    /// </summary>
    public sealed class Unlockable
    {
        /// <summary>
        /// INTERNAL: The native unlockable reference.
        /// </summary>
        internal S1Levelling.Unlockable Native { get; }

        /// <summary>
        /// Creates an unlockable tied to a specific rank and tier.
        /// </summary>
        /// <param name="rank">Rank+tier requirement.</param>
        /// <param name="title">Display title shown in UI.</param>
        /// <param name="icon">Display icon.</param>
        public Unlockable(FullRank rank, string title, Sprite icon)
        {
            Native = new S1Levelling.Unlockable(rank.ToNative(), title, icon);
        }

        /// <summary>
        /// INTERNAL: Wraps an existing native unlockable.
        /// </summary>
        internal Unlockable(S1Levelling.Unlockable unlockable)
        {
            Native = unlockable;
        }

        /// <summary>
        /// Rank requirement for this unlockable.
        /// </summary>
        public FullRank Rank
        {
            get => FullRank.FromNative(Native.Rank);
            set => Native.Rank = value.ToNative();
        }

        /// <summary>
        /// Display title used in UI elements.
        /// </summary>
        public string Title
        {
            get => Native.Title;
            set => Native.Title = value;
        }

        /// <summary>
        /// Icon associated with the unlockable entry.
        /// </summary>
        public Sprite Icon
        {
            get => Native.Icon;
            set => Native.Icon = value;
        }
    }
}
