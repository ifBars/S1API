#if IL2CPPMELON
using S1Casino = Il2CppScheduleOne.Casino;
#elif MONOMELON
using S1Casino = ScheduleOne.Casino;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Lifecycle;
using S1API.Logging;
using UnityEngine;

namespace S1API.Casino
{
    /// <summary>
    /// Discovers native casino games and publishes their synchronized lifecycle transitions.
    /// </summary>
    /// <remarks>
    /// This first-version API is intentionally read-only. It does not expose native RPCs, payout
    /// replacement, game authoring, or methods that mutate bets and round state.
    /// </remarks>
    public static class CasinoGameRegistry
    {
        private static readonly Log Logger = new Log("CasinoGameRegistry");
        private static readonly Dictionary<int, BlackjackGame> BlackjackGames = new Dictionary<int, BlackjackGame>();
        private static readonly Dictionary<int, RideTheBusGame> RideTheBusGames = new Dictionary<int, RideTheBusGame>();
        private static readonly Dictionary<int, SlotMachine> SlotMachines = new Dictionary<int, SlotMachine>();
        private static readonly Dictionary<int, SlotSpinSnapshot> ActiveSpins = new Dictionary<int, SlotSpinSnapshot>();
        private static bool _lifecycleHooked;

        /// <summary>Raised after a blackjack table changes stage.</summary>
        public static event Action<BlackjackGame, BlackjackStage, BlackjackStage>? BlackjackStageChanged;

        /// <summary>Raised when a blackjack table enters the dealing stage.</summary>
        public static event Action<BlackjackGame>? BlackjackRoundStarted;

        /// <summary>Raised when a blackjack table returns to the waiting stage.</summary>
        public static event Action<BlackjackGame>? BlackjackRoundEnded;

        /// <summary>Raised after the Ride the Bus table changes stage.</summary>
        public static event Action<RideTheBusGame, RideTheBusStage, RideTheBusStage>? RideTheBusStageChanged;

        /// <summary>Raised when a Ride the Bus table begins its first question.</summary>
        public static event Action<RideTheBusGame>? RideTheBusRoundStarted;

        /// <summary>Raised when a Ride the Bus table returns to the waiting stage.</summary>
        public static event Action<RideTheBusGame>? RideTheBusRoundEnded;

        /// <summary>Raised when a slot machine begins a synchronized spin.</summary>
        public static event Action<SlotMachine, SlotSpinSnapshot>? SlotSpinStarted;

        /// <summary>Raised when a slot machine displays a synchronized spin outcome.</summary>
        public static event Action<SlotMachine, SlotSpinSnapshot>? SlotSpinCompleted;

        /// <summary>Gets immutable wrappers for all active blackjack tables.</summary>
        public static IReadOnlyList<BlackjackGame> GetBlackjackGames()
        {
            EnsureLifecycleHook();
            var nativeGames = UnityEngine.Object.FindObjectsOfType<S1Casino.BlackjackGameController>();
            var games = new List<BlackjackGame>(nativeGames.Length);
            for (int i = 0; i < nativeGames.Length; i++)
            {
                if (nativeGames[i] != null)
                    games.Add(Wrap(nativeGames[i]));
            }
            return new ReadOnlyCollection<BlackjackGame>(games);
        }

        /// <summary>Gets immutable wrappers for all active Ride the Bus tables.</summary>
        public static IReadOnlyList<RideTheBusGame> GetRideTheBusGames()
        {
            EnsureLifecycleHook();
            var nativeGames = UnityEngine.Object.FindObjectsOfType<S1Casino.RTBGameController>();
            var games = new List<RideTheBusGame>(nativeGames.Length);
            for (int i = 0; i < nativeGames.Length; i++)
            {
                if (nativeGames[i] != null)
                    games.Add(Wrap(nativeGames[i]));
            }
            return new ReadOnlyCollection<RideTheBusGame>(games);
        }

        /// <summary>Gets immutable wrappers for all active native slot machines.</summary>
        public static IReadOnlyList<SlotMachine> GetSlotMachines()
        {
            EnsureLifecycleHook();
            var nativeMachines = UnityEngine.Object.FindObjectsOfType<S1Casino.SlotMachine>();
            var machines = new List<SlotMachine>(nativeMachines.Length);
            for (int i = 0; i < nativeMachines.Length; i++)
            {
                if (nativeMachines[i] != null)
                    machines.Add(Wrap(nativeMachines[i]));
            }
            return new ReadOnlyCollection<SlotMachine>(machines);
        }

        /// <summary>Finds the active slot machine nearest to a world position.</summary>
        /// <param name="position">The world position to search from.</param>
        /// <param name="maxDistance">The maximum search distance.</param>
        /// <returns>The nearest managed slot-machine wrapper, or <see langword="null"/> when none is in range.</returns>
        public static SlotMachine? FindNearestSlotMachine(Vector3 position, float maxDistance)
        {
            S1Casino.SlotMachine? native = SlotMachineHelper.FindNearestNativeSlotMachine(position, maxDistance);
            return native == null ? null : Wrap(native);
        }

        /// <summary>Gets immutable wrappers for all active blackjack and Ride the Bus tables.</summary>
        public static IReadOnlyList<CasinoGameTable> GetTables()
        {
            var tables = new List<CasinoGameTable>();
            tables.AddRange(GetBlackjackGames());
            tables.AddRange(GetRideTheBusGames());
            return new ReadOnlyCollection<CasinoGameTable>(tables);
        }

        internal static BlackjackGame Wrap(S1Casino.BlackjackGameController native)
        {
            EnsureLifecycleHook();
            int key = native.GetInstanceID();
            if (!BlackjackGames.TryGetValue(key, out BlackjackGame? game))
            {
                game = new BlackjackGame(native);
                BlackjackGames[key] = game;
            }
            return game;
        }

        internal static RideTheBusGame Wrap(S1Casino.RTBGameController native)
        {
            EnsureLifecycleHook();
            int key = native.GetInstanceID();
            if (!RideTheBusGames.TryGetValue(key, out RideTheBusGame? game))
            {
                game = new RideTheBusGame(native);
                RideTheBusGames[key] = game;
            }
            return game;
        }

        internal static SlotMachine Wrap(S1Casino.SlotMachine native)
        {
            EnsureLifecycleHook();
            int key = native.GetInstanceID();
            if (!SlotMachines.TryGetValue(key, out SlotMachine? machine))
            {
                machine = new SlotMachine(native);
                SlotMachines[key] = machine;
            }
            return machine;
        }

        internal static void NotifyBlackjackStageChanged(
            S1Casino.BlackjackGameController native,
            BlackjackStage previous,
            BlackjackStage current)
        {
            if (previous == current)
                return;

            BlackjackGame game = Wrap(native);
            game.NotifyStageChanged(previous, current);
            InvokeSafely(BlackjackStageChanged, game, previous, current, nameof(BlackjackStageChanged));

            if (current == BlackjackStage.Dealing && previous == BlackjackStage.WaitingForPlayers)
                InvokeSafely(BlackjackRoundStarted, game, nameof(BlackjackRoundStarted));
            if (current == BlackjackStage.WaitingForPlayers)
                InvokeSafely(BlackjackRoundEnded, game, nameof(BlackjackRoundEnded));
        }

        internal static void NotifyRideTheBusStageChanged(
            S1Casino.RTBGameController native,
            RideTheBusStage previous,
            RideTheBusStage current)
        {
            if (previous == current)
                return;

            RideTheBusGame game = Wrap(native);
            game.NotifyStageChanged(previous, current);
            InvokeSafely(RideTheBusStageChanged, game, previous, current, nameof(RideTheBusStageChanged));

            if (current == RideTheBusStage.RedOrBlack && previous == RideTheBusStage.WaitingForPlayers)
                InvokeSafely(RideTheBusRoundStarted, game, nameof(RideTheBusRoundStarted));
            if (current == RideTheBusStage.WaitingForPlayers)
                InvokeSafely(RideTheBusRoundEnded, game, nameof(RideTheBusRoundEnded));
        }

        internal static void NotifySlotSpinStarted(
            S1Casino.SlotMachine native,
            SlotSpinSnapshot snapshot)
        {
            int key = native.GetInstanceID();
            ActiveSpins[key] = snapshot;

            SlotMachine machine = Wrap(native);
            machine.NotifySpinStarted(snapshot);
            InvokeSafely(SlotSpinStarted, machine, snapshot, nameof(SlotSpinStarted));
        }

        internal static void NotifySlotSpinCompleted(
            S1Casino.SlotMachine native,
            SlotOutcome outcome,
            int winAmount)
        {
            int key = native.GetInstanceID();
            if (!ActiveSpins.TryGetValue(key, out SlotSpinSnapshot? started))
                return;

            ActiveSpins.Remove(key);
            SlotSpinSnapshot completed = started.Complete(outcome, winAmount);
            SlotMachine machine = Wrap(native);
            machine.NotifySpinCompleted(completed);
            InvokeSafely(SlotSpinCompleted, machine, completed, nameof(SlotSpinCompleted));
        }

        internal static void LogSubscriberFailure(string eventName, Exception exception) =>
            Logger.Warning($"A {eventName} subscriber failed: {exception.Message}");

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;

            GameLifecycle.OnPreSceneChange += ClearSceneState;
            _lifecycleHooked = true;
        }

        private static void ClearSceneState()
        {
            BlackjackGames.Clear();
            RideTheBusGames.Clear();
            SlotMachines.Clear();
            ActiveSpins.Clear();
        }

        private static void InvokeSafely<T>(Action<T>? handlers, T value, string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try { handler(value); }
                catch (Exception ex) { LogSubscriberFailure(eventName, ex); }
            }
        }

        private static void InvokeSafely<T1, T2>(
            Action<T1, T2>? handlers,
            T1 value1,
            T2 value2,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T1, T2> handler in handlers.GetInvocationList())
            {
                try { handler(value1, value2); }
                catch (Exception ex) { LogSubscriberFailure(eventName, ex); }
            }
        }

        private static void InvokeSafely<T1, T2, T3>(
            Action<T1, T2, T3>? handlers,
            T1 value1,
            T2 value2,
            T3 value3,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T1, T2, T3> handler in handlers.GetInvocationList())
            {
                try { handler(value1, value2, value3); }
                catch (Exception ex) { LogSubscriberFailure(eventName, ex); }
            }
        }
    }
}
