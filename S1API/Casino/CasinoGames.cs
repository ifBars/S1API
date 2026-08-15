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
using S1API.Internal.Utils;
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

        internal CasinoGameTable(S1Casino.CasinoGameController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        internal S1Casino.CasinoGameController Controller { get; }

        /// <summary>Gets the scene object name.</summary>
        public string Name => Controller.gameObject?.name ?? string.Empty;

        /// <summary>Gets the current world position.</summary>
        public Vector3 Position => Controller.transform.position;

        /// <summary>
        /// Gets whether this table's interface is open for the local player.
        /// </summary>
        public bool IsOpen => Controller.IsOpen;

        /// <summary>Gets whether the table is currently accepting ready players.</summary>
        public bool IsWaitingForPlayers => Controller.IsWaitingForPlayers();

        /// <summary>Gets the local player's currently selected bet.</summary>
        public float LocalBet => Controller.LocalPlayerBet;

        /// <summary>Gets the native minimum and maximum bet.</summary>
        public CasinoBetLimits BetLimits
        {
            get
            {
                Controller.GetBetLimits(out float minimum, out float maximum);
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
                S1Casino.CasinoGamePlayers? players = Controller.Players;
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
                    catch
                    {
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

        internal BlackjackGame(S1Casino.BlackjackGameController controller)
            : base(controller)
        {
            Native = controller;
        }

        internal S1Casino.BlackjackGameController Native { get; }

        /// <summary>Raised after the native blackjack stage changes.</summary>
        public event Action<BlackjackStage, BlackjackStage>? StageChanged;

        /// <summary>Raised when the table enters the dealing stage.</summary>
        public event Action? RoundStarted;

        /// <summary>Raised when the table returns to its waiting stage.</summary>
        public event Action? RoundEnded;

        /// <summary>Gets the current round stage.</summary>
        public BlackjackStage Stage => (BlackjackStage)(int)Native.CurrentStage;

        /// <summary>Gets the current dealer score visible to this peer.</summary>
        public int DealerScore => Native.DealerScore;

        /// <summary>Gets the current local-player score.</summary>
        public int LocalPlayerScore => Native.LocalPlayerScore;

        /// <summary>Gets whether the local player has a natural blackjack.</summary>
        public bool IsLocalPlayerBlackjack => Native.IsLocalPlayerBlackjack;

        /// <summary>Gets whether the local player is bust.</summary>
        public bool IsLocalPlayerBust => Native.IsLocalPlayerBust;

        /// <summary>Gets whether the local player belongs to the active round.</summary>
        public bool IsLocalPlayerInRound => Native.IsLocalPlayerInCurrentRound;

        /// <summary>Gets the number of seated players currently marked ready.</summary>
        public int ReadyPlayerCount => Native.GetPlayersReadyCount();

        /// <summary>
        /// Gets an immutable snapshot of a seated player's current hand.
        /// </summary>
        /// <param name="seatIndex">The zero-based seat index.</param>
        public IReadOnlyList<CasinoCardSnapshot> GetPlayerHand(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= Native.Players.PlayerLimit)
                return EmptyCards;

#if IL2CPPMELON
            var cards = Native.GetPlayerCards(seatIndex);
#else
            var method = typeof(S1Casino.BlackjackGameController).GetMethod(
                "GetPlayerCards",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var cards = method?.Invoke(Native, new object[] { seatIndex })
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
                var cards = Native.dealerHand;
#else
                var dealerHandField = typeof(S1Casino.BlackjackGameController).GetField(
                    "dealerHand",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var cards = dealerHandField?.GetValue(Native) as List<S1Casino.PlayingCard>;
#endif
                return FreezeCards(cards);
            }
        }

        internal void NotifyStageChanged(BlackjackStage previous, BlackjackStage current)
        {
            InvokeSafely(StageChanged, previous, current, nameof(StageChanged));
            if (current == BlackjackStage.Dealing && previous == BlackjackStage.WaitingForPlayers)
                InvokeSafely(RoundStarted, nameof(RoundStarted));
            if (current == BlackjackStage.WaitingForPlayers && previous != current)
                InvokeSafely(RoundEnded, nameof(RoundEnded));
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

        private static void InvokeSafely(Action? handlers, string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }

        private static void InvokeSafely(
            Action<BlackjackStage, BlackjackStage>? handlers,
            BlackjackStage previous,
            BlackjackStage current,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<BlackjackStage, BlackjackStage> handler in handlers.GetInvocationList())
            {
                try { handler(previous, current); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }
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
            Native = controller;
        }

        internal S1Casino.RTBGameController Native { get; }

        /// <summary>Raised after the native Ride the Bus stage changes.</summary>
        public event Action<RideTheBusStage, RideTheBusStage>? StageChanged;

        /// <summary>Raised when a new Ride the Bus round begins.</summary>
        public event Action? RoundStarted;

        /// <summary>Raised when the table returns to its waiting stage.</summary>
        public event Action? RoundEnded;

        /// <summary>Gets the current round stage.</summary>
        public RideTheBusStage Stage => (RideTheBusStage)(int)Native.CurrentStage;

        /// <summary>Gets whether a question is currently accepting answers.</summary>
        public bool IsQuestionActive => Native.IsQuestionActive;

        /// <summary>Gets the local player's current bet multiplier.</summary>
        public float LocalBetMultiplier => Native.LocalPlayerBetMultiplier;

        /// <summary>Gets the local player's multiplied bet.</summary>
        public float MultipliedLocalBet => Native.MultipliedLocalPlayerBet;

        /// <summary>Gets the answer time remaining on the current peer.</summary>
        public float RemainingAnswerTime => Native.RemainingAnswerTime;

        /// <summary>Gets whether the local player belongs to the active round.</summary>
        public bool IsLocalPlayerInRound => Native.IsLocalPlayerInCurrentRound;

        /// <summary>Gets the number of seated players currently marked ready.</summary>
        public int ReadyPlayerCount => Native.GetPlayersReadyCount();

        /// <summary>Gets the number of active-round players who submitted an answer.</summary>
        public int AnsweredPlayerCount => Native.GetAnsweredPlayersCount();

        /// <summary>Gets immutable snapshots of cards currently assigned by the table.</summary>
        public IReadOnlyList<CasinoCardSnapshot> Cards
        {
            get
            {
                if (Native.Cards == null || Native.Cards.Length == 0)
                    return EmptyCards;

                var cards = new List<CasinoCardSnapshot>();
                for (int i = 0; i < Native.Cards.Length; i++)
                {
                    S1Casino.PlayingCard? card = Native.Cards[i];
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
            InvokeSafely(StageChanged, previous, current, nameof(StageChanged));
            if (current == RideTheBusStage.RedOrBlack && previous == RideTheBusStage.WaitingForPlayers)
                InvokeSafely(RoundStarted, nameof(RoundStarted));
            if (current == RideTheBusStage.WaitingForPlayers && previous != current)
                InvokeSafely(RoundEnded, nameof(RoundEnded));
        }

        private static void InvokeSafely(Action? handlers, string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }

        private static void InvokeSafely(
            Action<RideTheBusStage, RideTheBusStage>? handlers,
            RideTheBusStage previous,
            RideTheBusStage current,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<RideTheBusStage, RideTheBusStage> handler in handlers.GetInvocationList())
            {
                try { handler(previous, current); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }
    }

    /// <summary>
    /// Read-only managed view of a native slot machine.
    /// </summary>
    public sealed class SlotMachine
    {
        internal SlotMachine(S1Casino.SlotMachine machine)
        {
            Native = machine ?? throw new ArgumentNullException(nameof(machine));
        }

        internal S1Casino.SlotMachine Native { get; }

        /// <summary>Raised when a synchronized spin begins.</summary>
        public event Action<SlotSpinSnapshot>? SpinStarted;

        /// <summary>Raised when a synchronized spin displays its outcome.</summary>
        public event Action<SlotSpinSnapshot>? SpinCompleted;

        /// <summary>Gets the scene object name.</summary>
        public string Name => Native.gameObject?.name ?? string.Empty;

        /// <summary>Gets the current world position.</summary>
        public Vector3 Position => Native.transform.position;

        /// <summary>Gets whether the reels are currently spinning.</summary>
        public bool IsSpinning => Native.IsSpinning;

        /// <summary>Gets the machine's currently selected bet.</summary>
        public int CurrentBet
        {
            get
            {
                object? value = ReflectionUtils.TryGetFieldOrProperty(Native, "currentBetAmount");
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
            InvokeSafely(SpinStarted, snapshot, nameof(SpinStarted));

        internal void NotifySpinCompleted(SlotSpinSnapshot snapshot) =>
            InvokeSafely(SpinCompleted, snapshot, nameof(SpinCompleted));

        private static void InvokeSafely(
            Action<SlotSpinSnapshot>? handlers,
            SlotSpinSnapshot snapshot,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<SlotSpinSnapshot> handler in handlers.GetInvocationList())
            {
                try { handler(snapshot); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }
    }
}
