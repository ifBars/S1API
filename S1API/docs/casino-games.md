# Casino game state

S1API exposes read-only managed wrappers for the casino's blackjack table, Ride the Bus table, and slot machines. The wrappers provide discovery, synchronized state snapshots, and round or spin lifecycle events without exposing native casino controllers or RPC methods.

This API observes the game. It does not create custom casino games, replace payouts, submit bets or answers, change readiness, or invoke client/server RPCs.

## Discover games

Casino objects belong to the gameplay scene. Query the registry after the game has loaded, such as from `GameLifecycle.OnLoadComplete`:

```csharp
using S1API.Casino;
using S1API.Lifecycle;

GameLifecycle.OnLoadComplete += () =>
{
    IReadOnlyList<BlackjackGame> blackjack = CasinoGameRegistry.GetBlackjackGames();
    IReadOnlyList<RideTheBusGame> rideTheBus = CasinoGameRegistry.GetRideTheBusGames();
    IReadOnlyList<SlotMachine> slots = CasinoGameRegistry.GetSlotMachines();
};
```

`GetTables()` returns blackjack and Ride the Bus through their shared `CasinoGameTable` base. Slot machines are separate because they do not have seated-player or table-bet state.

Use `CasinoGameRegistry.FindNearestSlotMachine(position, maxDistance)` when you need the nearest slot machine without exposing the game's native casino type:

```csharp
SlotMachine? nearest = CasinoGameRegistry.FindNearestSlotMachine(transform.position, 10f);
if (nearest != null)
    MelonLogger.Msg($"Nearest slot machine: {nearest.Name} at {nearest.Position}");
```

Registry results are read-only snapshots of the active casino objects at the time of discovery. Their wrapper instances represent live scene objects, so wrapper properties continue to reflect current controller state. Calling a discovery method again reflects the active objects in the current scene, while wrappers for the same live scene object retain their identity within that scene.

## Shared table state

Every `CasinoGameTable` exposes:

- `Name` and `Position`
- `IsOpen`, which reports whether this table's interface is open for the local player
- `IsWaitingForPlayers`
- `LocalBet`
- `BetLimits`
- `Players`, an immutable snapshot containing managed `Player` wrappers when available, seat indices, synchronized scores, and ready state

Local-player properties describe the current client. Stage, player, card, score, and spin data reflect the native state received by that peer.

## Blackjack

```csharp
BlackjackGame table = CasinoGameRegistry.GetBlackjackGames()[0];

BlackjackStage stage = table.Stage;
CasinoBetLimits limits = table.BetLimits;
IReadOnlyList<CasinoCardSnapshot> dealerHand = table.DealerHand;

foreach (CasinoPlayerSnapshot player in table.Players)
{
    IReadOnlyList<CasinoCardSnapshot> hand = table.GetPlayerHand(player.SeatIndex);
    MelonLogger.Msg($"{player.Name}: table score {player.Score}, cards {hand.Count}");
}
```

Blackjack also exposes the dealer score, local score, local blackjack/bust flags, ready-player count, and whether the local player belongs to the active round.

Subscribe globally when you want to observe every table:

```csharp
CasinoGameRegistry.BlackjackStageChanged += (table, previous, current) =>
    MelonLogger.Msg($"{table.Name}: {previous} -> {current}");

CasinoGameRegistry.BlackjackRoundStarted += table =>
    MelonLogger.Msg($"Round started at {table.Name}");

CasinoGameRegistry.BlackjackRoundEnded += table =>
    MelonLogger.Msg($"Round ended at {table.Name}");
```

The same events are available on an individual `BlackjackGame` wrapper.

## Ride the Bus

`RideTheBusGame` exposes its stage, question-active flag, remaining answer time, local bet multiplier, multiplied local bet, ready and answered player counts, active-round membership, and immutable card snapshots.

```csharp
RideTheBusGame table = CasinoGameRegistry.GetRideTheBusGames()[0];

CasinoGameRegistry.RideTheBusStageChanged += (game, previous, current) =>
    MelonLogger.Msg($"Ride the Bus: {previous} -> {current}");

CasinoGameRegistry.RideTheBusRoundStarted += game =>
    MelonLogger.Msg("Ride the Bus round started");

CasinoGameRegistry.RideTheBusRoundEnded += game =>
    MelonLogger.Msg("Ride the Bus round ended");
```

The same events are available on an individual `RideTheBusGame` wrapper.

## Slot machines

Slot wrappers expose their position, current bet, available native bets, and spinning state. Spin events include the bet, ordered reel symbols, whether the native spinner belongs to the current client, and the eventual outcome and win amount.

```csharp
CasinoGameRegistry.SlotSpinStarted += (machine, spin) =>
    MelonLogger.Msg($"{machine.Name} started a ${spin.Bet} spin");

CasinoGameRegistry.SlotSpinCompleted += (machine, spin) =>
    MelonLogger.Msg($"{machine.Name}: {spin.Outcome}, won ${spin.WinAmount}");
```

The same events are available on an individual `SlotMachine` wrapper. S1API's existing NPC slot-machine helper also publishes through these lifecycle events.

## Event lifetime

Static registry subscriptions belong to your mod and remain subscribed across scene changes. Unsubscribe them when your mod no longer needs them. S1API clears scene-object wrapper and active-spin state before scene transitions, so discovery never returns cached objects from a previous gameplay scene.

Subscriptions attached directly to a `BlackjackGame`, `RideTheBusGame`, or `SlotMachine` wrapper last only for that wrapper's scene. Query and subscribe to new wrappers after each gameplay scene load, or use the static `CasinoGameRegistry` events when the subscription should remain active across scene changes.
