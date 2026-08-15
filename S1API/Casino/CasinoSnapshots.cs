using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Entities;

namespace S1API.Casino
{
    /// <summary>
    /// Represents the native minimum and maximum bet accepted by a casino table.
    /// </summary>
    public readonly struct CasinoBetLimits
    {
        /// <summary>
        /// Creates an immutable bet-limit snapshot.
        /// </summary>
        /// <param name="minimum">The minimum bet accepted by the table.</param>
        /// <param name="maximum">The maximum bet accepted by the table.</param>
        public CasinoBetLimits(float minimum, float maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>Gets the minimum accepted bet.</summary>
        public float Minimum { get; }

        /// <summary>Gets the maximum accepted bet.</summary>
        public float Maximum { get; }
    }

    /// <summary>
    /// Card suits used by the native casino games.
    /// </summary>
    public enum CasinoCardSuit
    {
        /// <summary>Spades.</summary>
        Spades = 0,
        /// <summary>Hearts.</summary>
        Hearts = 1,
        /// <summary>Diamonds.</summary>
        Diamonds = 2,
        /// <summary>Clubs.</summary>
        Clubs = 3
    }

    /// <summary>
    /// Card values used by the native casino games.
    /// </summary>
    public enum CasinoCardValue
    {
        /// <summary>No card value is assigned.</summary>
        Blank = 0,
        /// <summary>Ace.</summary>
        Ace = 1,
        /// <summary>Two.</summary>
        Two = 2,
        /// <summary>Three.</summary>
        Three = 3,
        /// <summary>Four.</summary>
        Four = 4,
        /// <summary>Five.</summary>
        Five = 5,
        /// <summary>Six.</summary>
        Six = 6,
        /// <summary>Seven.</summary>
        Seven = 7,
        /// <summary>Eight.</summary>
        Eight = 8,
        /// <summary>Nine.</summary>
        Nine = 9,
        /// <summary>Ten.</summary>
        Ten = 10,
        /// <summary>Jack.</summary>
        Jack = 11,
        /// <summary>Queen.</summary>
        Queen = 12,
        /// <summary>King.</summary>
        King = 13
    }

    /// <summary>
    /// Immutable public representation of a casino playing card.
    /// </summary>
    public readonly struct CasinoCardSnapshot
    {
        /// <summary>
        /// Creates an immutable card snapshot.
        /// </summary>
        /// <param name="id">The scene-local native card identifier.</param>
        /// <param name="suit">The card suit.</param>
        /// <param name="value">The card value.</param>
        /// <param name="isFaceUp">Whether the card is face up for the current client.</param>
        public CasinoCardSnapshot(string id, CasinoCardSuit suit, CasinoCardValue value, bool isFaceUp)
        {
            Id = id ?? string.Empty;
            Suit = suit;
            Value = value;
            IsFaceUp = isFaceUp;
        }

        /// <summary>Gets the scene-local native card identifier.</summary>
        public string Id { get; }

        /// <summary>Gets the card suit.</summary>
        public CasinoCardSuit Suit { get; }

        /// <summary>Gets the card value.</summary>
        public CasinoCardValue Value { get; }

        /// <summary>Gets whether the card is face up for the current client.</summary>
        public bool IsFaceUp { get; }
    }

    /// <summary>
    /// Immutable player state captured from a casino table.
    /// </summary>
    public sealed class CasinoPlayerSnapshot
    {
        internal CasinoPlayerSnapshot(Player? player, string name, int seatIndex, int score, bool isReady)
        {
            Player = player;
            Name = name;
            SeatIndex = seatIndex;
            Score = score;
            IsReady = isReady;
        }

        /// <summary>
        /// Gets the S1API player wrapper when that player has completed S1API initialization.
        /// </summary>
        public Player? Player { get; }

        /// <summary>Gets the current native player name.</summary>
        public string Name { get; }

        /// <summary>Gets the zero-based table seat index.</summary>
        public int SeatIndex { get; }

        /// <summary>Gets the synchronized score stored by the table.</summary>
        public int Score { get; }

        /// <summary>Gets whether the player has marked themselves ready.</summary>
        public bool IsReady { get; }
    }

    /// <summary>
    /// Stages in a native blackjack round.
    /// </summary>
    public enum BlackjackStage
    {
        /// <summary>The table is accepting players.</summary>
        WaitingForPlayers = 0,
        /// <summary>Initial cards are being dealt.</summary>
        Dealing = 1,
        /// <summary>A player is taking their turn.</summary>
        PlayerTurn = 2,
        /// <summary>The dealer is taking their turn.</summary>
        DealerTurn = 3,
        /// <summary>The round is resolving payouts.</summary>
        Ending = 4
    }

    /// <summary>
    /// Native blackjack payout classifications.
    /// </summary>
    public enum BlackjackPayout
    {
        /// <summary>No payout.</summary>
        None = 0,
        /// <summary>Natural blackjack.</summary>
        Blackjack = 1,
        /// <summary>Standard win.</summary>
        Win = 2,
        /// <summary>Push; the original bet is returned.</summary>
        Push = 3
    }

    /// <summary>
    /// Stages in a native Ride the Bus round.
    /// </summary>
    public enum RideTheBusStage
    {
        /// <summary>The table is accepting players.</summary>
        WaitingForPlayers = 0,
        /// <summary>The player predicts red or black.</summary>
        RedOrBlack = 1,
        /// <summary>The player predicts higher or lower.</summary>
        HigherOrLower = 2,
        /// <summary>The player predicts inside or outside.</summary>
        InsideOrOutside = 3,
        /// <summary>The player predicts the suit.</summary>
        Suit = 4
    }

    /// <summary>
    /// Symbols displayed by a native slot machine.
    /// </summary>
    public enum SlotSymbol
    {
        /// <summary>Cherry.</summary>
        Cherry = 0,
        /// <summary>Lemon.</summary>
        Lemon = 1,
        /// <summary>Grape.</summary>
        Grape = 2,
        /// <summary>Watermelon.</summary>
        Watermelon = 3,
        /// <summary>Bell.</summary>
        Bell = 4,
        /// <summary>Seven.</summary>
        Seven = 5
    }

    /// <summary>
    /// Native slot-machine outcome classifications.
    /// </summary>
    public enum SlotOutcome
    {
        /// <summary>Three sevens.</summary>
        Jackpot = 0,
        /// <summary>Three bells.</summary>
        BigWin = 1,
        /// <summary>Three matching fruit symbols.</summary>
        SmallWin = 2,
        /// <summary>Three fruit symbols.</summary>
        MiniWin = 3,
        /// <summary>No winning combination.</summary>
        NoWin = 4
    }

    /// <summary>
    /// Immutable state for a slot spin as observed by the current peer.
    /// </summary>
    public sealed class SlotSpinSnapshot
    {
        internal SlotSpinSnapshot(
            int bet,
            IReadOnlyList<SlotSymbol> symbols,
            bool wasStartedByLocalPlayer,
            SlotOutcome? outcome,
            int? winAmount)
        {
            Bet = bet;
            Symbols = symbols;
            WasStartedByLocalPlayer = wasStartedByLocalPlayer;
            Outcome = outcome;
            WinAmount = winAmount;
        }

        /// <summary>Gets the bet used for this spin.</summary>
        public int Bet { get; }

        /// <summary>Gets the immutable ordered reel symbols.</summary>
        public IReadOnlyList<SlotSymbol> Symbols { get; }

        /// <summary>Gets whether the native spinner connection belongs to this client.</summary>
        public bool WasStartedByLocalPlayer { get; }

        /// <summary>Gets the outcome after completion, or <c>null</c> while spinning.</summary>
        public SlotOutcome? Outcome { get; }

        /// <summary>Gets the win amount after completion, or <c>null</c> while spinning.</summary>
        public int? WinAmount { get; }

        internal SlotSpinSnapshot Complete(SlotOutcome outcome, int winAmount) =>
            new SlotSpinSnapshot(Bet, Symbols, WasStartedByLocalPlayer, outcome, winAmount);

        internal static IReadOnlyList<SlotSymbol> Freeze(IList<SlotSymbol> symbols)
        {
            if (symbols.Count == 0)
                return new ReadOnlyCollection<SlotSymbol>(Array.Empty<SlotSymbol>());

            var copy = new SlotSymbol[symbols.Count];
            symbols.CopyTo(copy, 0);
            return new ReadOnlyCollection<SlotSymbol>(copy);
        }
    }
}
