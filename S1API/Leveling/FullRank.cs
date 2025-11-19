#if (IL2CPPMELON)
using S1Levelling = Il2CppScheduleOne.Levelling;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Levelling = ScheduleOne.Levelling;
#endif

using System;

namespace S1API.Leveling
{
    /// <summary>
    /// Represents a rank/tier combination.
    /// </summary>
    public readonly struct FullRank : IEquatable<FullRank>, IComparable<FullRank>
    {
        /// <summary>
        /// Creates a rank/tier combination.
        /// </summary>
        /// <param name="rank">Player rank.</param>
        /// <param name="tier">Tier within the rank (1-5 for most ranks).</param>
        public FullRank(Rank rank, int tier)
        {
            Rank = rank;
            Tier = Math.Max(1, tier);
        }

        /// <summary>
        /// Rank component.
        /// </summary>
        public Rank Rank { get; }

        /// <summary>
        /// Tier component (1-5 for most ranks, unlimited for Kingpin).
        /// </summary>
        public int Tier { get; }

        /// <summary>
        /// Returns the next rank/tier (rolls into the next rank after tier 5).
        /// </summary>
        public FullRank NextRank() =>
            Rank == Rank.Kingpin
                ? new FullRank(Rank.Kingpin, Tier + 1)
                : Tier < 5
                    ? new FullRank(Rank, Tier + 1)
                    : new FullRank(Rank + 1, 1);

        /// <summary>
        /// Converts the rank to a float for interpolation logic.
        /// </summary>
        public float ToFloat() => (float)Rank + (float)Tier / 5f;

        /// <summary>
        /// Returns an index useful for UI progress (0..N).
        /// </summary>
        public int GetRankIndex() => (int)Rank * 5 + (Tier - 1);

        /// <inheritdoc />
        public override string ToString()
        {
            string name = Rank switch
            {
                Rank.StreetRat => "Street Rat",
                Rank.ShotCaller => "Shot Caller",
                Rank.BlockBoss => "Block Boss",
                _ => Rank.ToString()
            };

            string suffix = Tier switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                _ => Tier.ToString()
            };

            return $"{name} {suffix}";
        }

        /// <inheritdoc />
        public bool Equals(FullRank other) => Rank == other.Rank && Tier == other.Tier;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is FullRank other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Rank, Tier);

        /// <inheritdoc />
        public int CompareTo(FullRank other)
        {
            int rankComparison = Rank.CompareTo(other.Rank);
            if (rankComparison != 0)
                return rankComparison;
            return Tier.CompareTo(other.Tier);
        }

        /// <summary>
        /// Greater-than comparison helper.
        /// </summary>
        public static bool operator >(FullRank a, FullRank b) => a.CompareTo(b) > 0;

        /// <summary>
        /// Less-than comparison helper.
        /// </summary>
        public static bool operator <(FullRank a, FullRank b) => a.CompareTo(b) < 0;

        /// <summary>
        /// Greater-or-equal comparison helper.
        /// </summary>
        public static bool operator >=(FullRank a, FullRank b) => a.CompareTo(b) >= 0;

        /// <summary>
        /// Less-or-equal comparison helper.
        /// </summary>
        public static bool operator <=(FullRank a, FullRank b) => a.CompareTo(b) <= 0;

        /// <summary>
        /// Equality check.
        /// </summary>
        public static bool operator ==(FullRank a, FullRank b) => a.Equals(b);

        /// <summary>
        /// Inequality check.
        /// </summary>
        public static bool operator !=(FullRank a, FullRank b) => !a.Equals(b);

        /// <summary>
        /// INTERNAL: Creates a wrapper from the native FullRank.
        /// </summary>
        internal static FullRank FromNative(S1Levelling.FullRank rank) =>
            new FullRank((Rank)rank.Rank, rank.Tier);

        /// <summary>
        /// INTERNAL: Converts to the native FullRank.
        /// </summary>
        internal S1Levelling.FullRank ToNative() =>
            new S1Levelling.FullRank((S1Levelling.ERank)Rank, Tier);
    }
}
