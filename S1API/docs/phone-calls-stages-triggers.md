# Phone Calls: Stages & Triggers

This page covers the non-obvious parts of the phone call API in `S1API/PhoneCalls/`:

- multi-stage calls
- stage start/done triggers
- variable/quest setters via `SystemTriggerEntry`

For the short "queue a call" intro, see `S1API/docs/phone-calls.md`.

## Multi-stage calls

`PhoneCallDefinition.AddStage(text)` appends a stage to the underlying in-game `PhoneCallData`.

```csharp
using S1API.PhoneCalls;

public sealed class MultiStageCall : PhoneCallDefinition
{
    public MultiStageCall() : base("Guide Bot")
    {
    }

    public void Build()
    {
        AddStage("Hey. First message.");
        AddStage("Second message.");
        AddStage("Third message.");
    }
}
```

Then queue it:

```csharp
var call = new MultiStageCall();
call.Build();
CallManager.QueueCall(call);
```

## Stage triggers (Start vs Done)

Each `CallStageEntry` can have system triggers attached:

- `SystemTriggerType.StartTrigger`: runs before stage text is displayed
- `SystemTriggerType.DoneTrigger`: runs after the player finishes the stage

```csharp
using S1API.PhoneCalls;
using S1API.PhoneCalls.Constants;

var stage = AddStage("This stage sets a variable when it starts.");
var trig = stage.AddSystemTrigger(SystemTriggerType.StartTrigger);
trig.AddVariableSetter(EvaluationType.PassOnTrue, "my_variable", "1");
```

## SystemTriggerEntry

`S1API.Conditions.SystemTriggerEntry` wraps the game's trigger object and exposes:

- `AddVariableSetter(...)`
- `AddQuestSetter(...)`
- `OnEvaluateTrue` / `OnEvaluateFalse` events
- `Trigger()` (manual evaluation)

### Variable setters

```csharp
using S1API.PhoneCalls.Constants;

trig.AddVariableSetter(EvaluationType.PassOnTrue, "tutorial_seen", "true");
trig.OnEvaluateTrue += () => MelonLoader.MelonLogger.Msg("Trigger passed");
```

### Quest setters

If you want to start/advance quests from calls, you can attach quest setters (see also `S1API/docs/quests-system.md`).

```csharp
using S1API.PhoneCalls.Constants;
using S1API.Quests;
using S1API.Quests.Constants;

// Example: set quest state when conditions evaluate true.
trig.AddQuestSetter(
    EvaluationType.PassOnTrue,
    questData,
    questAction: QuestAction.Start
);
```

## Common gotchas

- Calls with 0 stages are skipped by `CallManager` to avoid crashing the call UI.
- `CallManager.PendingCount` is S1API's queue only; it does not include an active in-game call.

## See Also

- `S1API/docs/phone-calls.md`
- <xref:S1API.PhoneCalls>
- <xref:S1API.Conditions.SystemTriggerEntry>
