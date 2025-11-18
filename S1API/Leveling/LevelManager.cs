#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using S1Levelling = Il2CppScheduleOne.Levelling;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Levelling = ScheduleOne.Levelling;
#endif

using System;
using System.Collections.Generic;

namespace S1API.Leveling
{
    /// <summary>
    /// Exposes the player progression system, including ranks, tiers, XP, unlockables, and related events.
    /// </summary>
    public static class LevelManager
    {
        private static S1Levelling.LevelManager? _hookedManager;
#if IL2CPPMELON
        private static Il2CppSystem.Action<S1Levelling.FullRank, S1Levelling.FullRank>? _rankChangedIl2Cpp;
        private static Il2CppSystem.Action<S1Levelling.FullRank, S1Levelling.FullRank>? _rankUpIl2Cpp;
#endif

        /// <summary>
        /// Raised whenever XP or rank/tier data is updated, even if the rank/tier values haven't changed.
        /// This fires on every XP change, including when only XP increases without a rank/tier change.
        /// Provides the previous and new rank values (which may be the same).
        /// </summary>
        public static event Action<FullRank, FullRank>? OnXPChanged;

        /// <summary>
        /// Raised when the player's rank or tier actually increases (tier increases within a rank, or rank increases when tier exceeds 5).
        /// This only fires when the rank/tier value changes, not on every XP update.
        /// Provides the previous and new rank values.
        /// </summary>
        public static event Action<FullRank, FullRank>? OnRankUp;

        /// <summary>
        /// Returns true when the underlying levelling manager has been instantiated.
        /// </summary>
        public static bool Exists => Internal != null;

        /// <summary>
        /// The player's current rank.
        /// </summary>
        public static Rank Rank => Internal != null ? (Rank)Internal.Rank : Rank.StreetRat;

        /// <summary>
        /// The player's current tier within their rank.
        /// </summary>
        public static int Tier => Internal?.Tier ?? 1;

        /// <summary>
        /// XP progress within the current tier.
        /// </summary>
        public static int XP => Internal?.XP ?? 0;

        /// <summary>
        /// Total XP accumulated across all ranks.
        /// </summary>
        public static int TotalXP => Internal?.TotalXP ?? 0;

        /// <summary>
        /// XP required to reach the next tier threshold.
        /// </summary>
        public static float XPToNextTier => Internal?.XPToNextTier ?? 0f;

        /// <summary>
        /// The player's current rank and tier combined.
        /// </summary>
        public static FullRank CurrentRank =>
            Internal != null ? FullRank.FromNative(Internal.GetFullRank()) : new FullRank(Rank.StreetRat, 1);

        /// <summary>
        /// Adds XP to the player. Only works when invoked from the host/server.
        /// </summary>
        /// <param name="amount">How much XP to award.</param>
        public static void AddXP(int amount)
        {
            Internal?.AddXP(amount);
        }

        /// <summary>
        /// Gets the XP required to complete a tier for the specified rank.
        /// </summary>
        public static int GetXPForTier(Rank rank)
        {
            return Internal != null ? Internal.GetXPForTier((S1Levelling.ERank)rank) : 0;
        }

        /// <summary>
        /// Converts a target XP value to the associated rank and tier.
        /// </summary>
        public static FullRank GetFullRankForXP(int totalXp)
        {
            return Internal != null
                ? FullRank.FromNative(Internal.GetFullRank(totalXp))
                : new FullRank(Rank.StreetRat, 1);
        }

        /// <summary>
        /// Gets the total XP required to reach a specific rank/tier combination.
        /// </summary>
        public static int GetTotalXPForRank(FullRank rank)
        {
            return Internal != null ? Internal.GetTotalXPForRank(rank.ToNative()) : 0;
        }

        /// <summary>
        /// Returns the order limit multiplier used by customer systems for the specified rank.
        /// </summary>
        public static float GetOrderLimitMultiplier(FullRank rank)
        {
            return S1Levelling.LevelManager.GetOrderLimitMultiplier(rank.ToNative());
        }

        /// <summary>
        /// Registers an unlockable item for the specified rank.
        /// </summary>
        public static void AddUnlockable(Unlockable unlockable)
        {
            if (unlockable == null)
                throw new ArgumentNullException(nameof(unlockable));

            Internal?.AddUnlockable(unlockable.Native);
        }

        /// <summary>
        /// Enumerates unlockables for the provided rank.
        /// </summary>
        public static IEnumerable<Unlockable> GetUnlockables(FullRank rank)
        {
            var manager = Internal;
            if (manager == null)
                yield break;

            if (!manager.Unlockables.TryGetValue(rank.ToNative(), out var unlockables) || unlockables == null)
                yield break;

            for (int i = 0; i < unlockables.Count; i++)
            {
                yield return new Unlockable(unlockables[i]);
            }
        }

        private static S1Levelling.LevelManager? Internal
        {
            get
            {
                var manager = S1Levelling.LevelManager.Instance;
                if (manager != null)
                {
                    TryHookEvents(manager);
                }

                return manager;
            }
        }

        private static void TryHookEvents(S1Levelling.LevelManager manager)
        {
            if (ReferenceEquals(_hookedManager, manager))
                return;

#if IL2CPPMELON
            _rankChangedIl2Cpp ??= DelegateSupport.ConvertDelegate<Il2CppSystem.Action<S1Levelling.FullRank, S1Levelling.FullRank>>(
                new Action<S1Levelling.FullRank, S1Levelling.FullRank>(HandleRankChanged));
            _rankUpIl2Cpp ??= DelegateSupport.ConvertDelegate<Il2CppSystem.Action<S1Levelling.FullRank, S1Levelling.FullRank>>(
                new Action<S1Levelling.FullRank, S1Levelling.FullRank>(HandleRankUp));

            manager.onRankChanged = Il2CppSystem.Delegate.Combine(manager.onRankChanged, _rankChangedIl2Cpp)
                .Cast<Il2CppSystem.Action<S1Levelling.FullRank, S1Levelling.FullRank>>();
            manager.onRankUp = Il2CppSystem.Delegate.Combine(manager.onRankUp, _rankUpIl2Cpp)
                .Cast<Il2CppSystem.Action<S1Levelling.FullRank, S1Levelling.FullRank>>();
#else
            manager.onRankChanged += HandleRankChanged;
            manager.onRankUp += HandleRankUp;
#endif

            _hookedManager = manager;
        }

        private static void HandleRankChanged(S1Levelling.FullRank before, S1Levelling.FullRank after)
        {
            OnXPChanged?.Invoke(FullRank.FromNative(before), FullRank.FromNative(after));
        }

        private static void HandleRankUp(S1Levelling.FullRank before, S1Levelling.FullRank after)
        {
            OnRankUp?.Invoke(FullRank.FromNative(before), FullRank.FromNative(after));
        }
    }
}
