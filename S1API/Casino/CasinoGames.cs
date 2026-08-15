#if IL2CPPMELON
using S1Casino = Il2CppScheduleOne.Casino;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif MONOMELON
using S1Casino = ScheduleOne.Casino;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using S1API.Entities;
using S1API.Internal;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;

namespace S1API.Casino
{
    /// <summary>
    /// Read-only managed view of a native multiplayer casino table.
    /// </summary>
    public abstract class CasinoGameTable
    {
        private static readonly IReadOnlyList<CasinoPlayerSnapshot> EmptyPlayers =
            new ReadOnlyCollection<CasinoPlayerSnapshot>(Array.Empty<CasinoPlayerSnapshot>());
        private static readonly Log Logger = new Log("CasinoGameTable");

        internal CasinoGameTable(S1Casino.CasinoGameController controller)
        {
            S1Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        internal S1Casino.CasinoGameController S1Controller { get; }

        /// <summary>Gets the scene object name.</summary>
        public string Name => S1Controller.gameObject?.name ?? string.Empty;

        /// <summary>Gets the current world position.</summary>
        public Vector3 Position => S1Controller.transform.position;

        /// <summary>
        /// Gets whether this table's interface is open for the local player.
        /// </summary>
        public bool IsOpen => S1Controller.IsOpen;

        /// <summary>Gets whether the table is currently accepting ready players.</summary>
        public bool IsWaitingForPlayers => S1Controller.IsWaitingForPlayers();

        /// <summary>Gets the local player's currently selected bet.</summary>
        public float LocalBet => S1Controller.LocalPlayerBet;

        /// <summary>Gets the native minimum and maximum bet.</summary>
        public CasinoBetLimits BetLimits
        {
            get
            {
                S1Controller.GetBetLimits(out float minimum, out float maximum);
                return new CasinoBetLimits(minimum, maximum);
            }
        }

        /// <summary>
        /// Gets an immutable snapshot of players currently seated at the table.
        /// </summary>
        public IReadOnlyList<CasinoPlayerSnapshot> Players
        {
            get
            {
                S1Casino.CasinoGamePlayers? players = S1Controller.Players;
                if (players == null)
                    return EmptyPlayers;

                var snapshots = new List<CasinoPlayerSnapshot>();
                for (int seatIndex = 0; seatIndex < players.PlayerLimit; seatIndex++)
                {
                    S1PlayerScripts.Player? nativePlayer = players.GetPlayer(seatIndex);
                    if (nativePlayer == null)
                        continue;

                    bool ready = false;
                    try
                    {
                        S1Casino.CasinoGamePlayerData? data = players.GetPlayerData(nativePlayer);
                        ready = data != null && data.GetData<bool>("Ready");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to read ready state for player '{nativePlayer.PlayerName}': {ex.Message}");
                    }

                    snapshots.Add(new CasinoPlayerSnapshot(
                        ResolvePlayer(nativePlayer),
                        nativePlayer.PlayerName ?? string.Empty,
                        seatIndex,
                        players.GetPlayerScore(nativePlayer),
                        ready));
                }

                return snapshots.Count == 0
                    ? EmptyPlayers
                    : new ReadOnlyCollection<CasinoPlayerSnapshot>(snapshots);
            }
        }

        private static Player? ResolvePlayer(S1PlayerScripts.Player nativePlayer) =>
            Player.All.FirstOrDefault(player => player.S1Player == nativePlayer);
    }

    /// <summary>
    /// Read-only managed view of a native blackjack table.
    /// </summary>
    public sealed class BlackjackGame : CasinoGameTable
    {
        private static readonly IReadOnlyList<CasinoCardSnapshot> EmptyCards =
            new ReadOnlyCollection<CasinoCardSnapshot>(Array.Empty<CasinoCardSnapshot>());
#if MONOMELON
        private const BindingFlags NativeMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly MethodInfo? GetPlayerCardsMethod =
            typeof(S1Casino.BlackjackGameController).GetMethod(
                "GetPlayerCards",
                NativeMemberFlags,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null);
        private static readonly FieldInfo? DealerHandField =
            typeof(S1Casino.BlackjackGameController).GetField("dealerHand", NativeMemberFlags);
#endif

        internal BlackjackGame(S1Casino.BlackjackGameController controller)
            : base(controller)
        {
            S1Native = controller;
        }

        internal S1Casino.BlackjackGameController S1Native { get; }

        /// <summary>Raised after the native blackjack stage changes.</summary>
        public event Action<BlackjackStage, BlackjackStage>? StageChanged;

        /// <summary>Raised when the table enters the dealing stage.</summary>
        public event Action? RoundStarted;

        /// <summary>Raised when the table returns to its waiting stage.</summary>
        public event Action? RoundEnded;

        /// <summary>Gets the current round stage.</summary>
        public BlackjackStage Stage => (BlackjackStage)(int)S1Native.CurrentStage;

        /// <summary>Gets the current dealer score visible to this peer.</summary>
        public int DealerScore => S1Native.DealerScore;

        /// <summary>Gets the current local-player score.</summary>
        public int LocalPlayerScore => S1Native.LocalPlayerScore;

        /// <summary>Gets whether the local player has a natural blackjack.</summary>
        public bool IsLocalPlayerBlackjack => S1Native.IsLocalPlayerBlackjack;

        /// <summary>Gets whether the local player is bust.</summary>
        public bool IsLocalPlayerBust => S1Native.IsLocalPlayerBust;

        /// <summary>Gets whether the local player belongs to the active round.</summary>
        public bool IsLocalPlayerInRound => S1Native.IsLocalPlayerInCurrentRound;

        /// <summary>Gets the number of seated players currently marked ready.</summary>
        public int ReadyPlayerCount => S1Native.GetPlayersReadyCount();

        /// <summary>
        /// Gets an immutable snapshot of a seated player's current hand.
        /// </summary>
        /// <param name="seatIndex">The zero-based seat index.</param>
        public IReadOnlyList<CasinoCardSnapshot> GetPlayerHand(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= S1Native.Players.PlayerLimit)
                return EmptyCards;

#if IL2CPPMELON
            var cards = S1Native.GetPlayerCards(seatIndex);
#else
            var cards = GetPlayerCardsMethod?.Invoke(S1Native, new object[] { seatIndex })
                as List<S1Casino.PlayingCard>;
#endif
            return FreezeCards(cards);
        }

        /// <summary>Gets an immutable snapshot of the dealer's current hand.</summary>
        public IReadOnlyList<CasinoCardSnapshot> DealerHand
        {
            get
            {
#if IL2CPPMELON
                var cards = S1Native.dealerHand;
#else
                var cards = DealerHandField?.GetValue(S1Native) as List<S1Casino.PlayingCard>;
#endif
                return FreezeCards(cards);
            }
        }

        internal void NotifyStageChanged(BlackjackStage previous, BlackjackStage current)
        {
            CasinoEventInvoker.Invoke(StageChanged, previous, current, nameof(StageChanged));
            if (current == BlackjackStage.Dealing && previous == BlackjackStage.WaitingForPlayers)
                CasinoEventInvoker.Invoke(RoundStarted, nameof(RoundStarted));
            if (current == BlackjackStage.WaitingForPlayers && previous != current)
                CasinoEventInvoker.Invoke(RoundEnded, nameof(RoundEnded));
        }

        private static IReadOnlyList<CasinoCardSnapshot> FreezeCards(
#if IL2CPPMELON
            Il2CppSystem.Collections.Generic.List<S1Casino.PlayingCard>? cards)
#else
            List<S1Casino.PlayingCard>? cards)
#endif
        {
            if (cards == null || cards.Count == 0)
                return EmptyCards;

            var snapshots = new List<CasinoCardSnapshot>(cards.Count);
            for (int i = 0; i < cards.Count; i++)
            {
                S1Casino.PlayingCard? card = cards[i];
                if (card != null)
                    snapshots.Add(ToSnapshot(card));
            }

            return snapshots.Count == 0
                ? EmptyCards
                : new ReadOnlyCollection<CasinoCardSnapshot>(snapshots);
        }

        internal static CasinoCardSnapshot ToSnapshot(S1Casino.PlayingCard card) =>
            new CasinoCardSnapshot(
                card.CardID ?? string.Empty,
                (CasinoCardSuit)(int)card.Suit,
                (CasinoCardValue)(int)card.Value,
                card.IsFaceUp);
    }

    /// <summary>
    /// Read-only managed view of the native Ride the Bus table.
    /// </summary>
    public sealed class RideTheBusGame : CasinoGameTable
    {
        private static readonly IReadOnlyList<CasinoCardSnapshot> EmptyCards =
            new ReadOnlyCollection<CasinoCardSnapshot>(Array.Empty<CasinoCardSnapshot>());

        internal RideTheBusGame(S1Casino.RTBGameController controller)
            : base(controller)
        {
            S1Native = controller;
        }

        internal S1Casino.RTBGameController S1Native { get; }

        /// <summary>Raised after the native Ride the Bus stage changes.</summary>
        public event Action<RideTheBusStage, RideTheBusStage>? StageChanged;

        /// <summary>Raised when a new Ride the Bus round begins.</summary>
        public event Action? RoundStarted;

        /// <summary>Raised when the table returns to its waiting stage.</summary>
        public event Action? RoundEnded;

        /// <summary>Gets the current round stage.</summary>
        public RideTheBusStage Stage => (RideTheBusStage)(int)S1Native.CurrentStage;

        /// <summary>Gets whether a question is currently accepting answers.</summary>
        public bool IsQuestionActive => S1Native.IsQuestionActive;

        /// <summary>Gets the local player's current bet multiplier.</summary>
        public float LocalBetMultiplier => S1Native.LocalPlayerBetMultiplier;

        /// <summary>Gets the local player's multiplied bet.</summary>
        public float MultipliedLocalBet => S1Native.MultipliedLocalPlayerBet;

        /// <summary>Gets the answer time remaining on the current peer.</summary>
        public float RemainingAnswerTime => S1Native.RemainingAnswerTime;

        /// <summary>Gets whether the local player belongs to the active round.</summary>
        public bool IsLocalPlayerInRound => S1Native.IsLocalPlayerInCurrentRound;

        /// <summary>Gets the number of seated players currently marked ready.</summary>
        public int ReadyPlayerCount => S1Native.GetPlayersReadyCount();

        /// <summary>Gets the number of active-round players who submitted an answer.</summary>
        public int AnsweredPlayerCount => S1Native.GetAnsweredPlayersCount();

        /// <summary>Gets immutable snapshots of cards currently assigned by the table.</summary>
        public IReadOnlyList<CasinoCardSnapshot> Cards
        {
            get
            {
                var nativeCards = S1Native.Cards;
                if (nativeCards == null || nativeCards.Length == 0)
                    return EmptyCards;

                var cards = new List<CasinoCardSnapshot>();
                for (int i = 0; i < nativeCards.Length; i++)
                {
                    S1Casino.PlayingCard? card = nativeCards[i];
                    if (card != null && (int)card.Value != (int)CasinoCardValue.Blank)
                        cards.Add(BlackjackGame.ToSnapshot(card));
                }

                return cards.Count == 0
                    ? EmptyCards
                    : new ReadOnlyCollection<CasinoCardSnapshot>(cards);
            }
        }

        internal void NotifyStageChanged(RideTheBusStage previous, RideTheBusStage current)
        {
            CasinoEventInvoker.Invoke(StageChanged, previous, current, nameof(StageChanged));
            if (current == RideTheBusStage.RedOrBlack && previous == RideTheBusStage.WaitingForPlayers)
                CasinoEventInvoker.Invoke(RoundStarted, nameof(RoundStarted));
            if (current == RideTheBusStage.WaitingForPlayers && previous != current)
                CasinoEventInvoker.Invoke(RoundEnded, nameof(RoundEnded));
        }
    }

    /// <summary>
    /// Read-only managed view of a native slot machine.
    /// </summary>
    public sealed class SlotMachine
    {
        internal SlotMachine(S1Casino.SlotMachine machine)
        {
            S1Native = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        internal S1Casino.SlotMachine S1Native { get; }

        /// <summary>Raised when a synchronized spin begins.</summary>
        public event Action<SlotSpinSnapshot>? SpinStarted;

        /// <summary>Raised when a synchronized spin displays its outcome.</summary>
        public event Action<SlotSpinSnapshot>? SpinCompleted;

        /// <summary>Gets the scene object name.</summary>
        public string Name => S1Native.gameObject?.name ?? string.Empty;

        /// <summary>Gets the current world position.</summary>
        public Vector3 Position => S1Native.transform.position;

        /// <summary>Gets whether the reels are currently spinning.</summary>
        public bool IsSpinning => S1Native.IsSpinning;

        /// <summary>Gets the machine's currently selected bet.</summary>
        public int CurrentBet
        {
            get
            {
                object? value = ReflectionUtils.TryGetFieldOrProperty(S1Native, "currentBetAmount");
                return value is int bet ? bet : 0;
            }
        }

        /// <summary>Gets the immutable native bet choices.</summary>
        public IReadOnlyList<int> AvailableBets
        {
            get
            {
                var nativeAmounts = S1Casino.SlotMachine.BetAmounts;
                var amounts = new int[nativeAmounts.Length];
                for (int i = 0; i < nativeAmounts.Length; i++)
                    amounts[i] = nativeAmounts[i];
                return new ReadOnlyCollection<int>(amounts);
            }
        }

        internal void NotifySpinStarted(SlotSpinSnapshot snapshot) =>
            CasinoEventInvoker.Invoke(SpinStarted, snapshot, nameof(SpinStarted));

        internal void NotifySpinCompleted(SlotSpinSnapshot snapshot) =>
            CasinoEventInvoker.Invoke(SpinCompleted, snapshot, nameof(SpinCompleted));
    }
}
