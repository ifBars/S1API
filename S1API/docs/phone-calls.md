# Phone Calls

S1API wraps the game's PhoneCall system to make defining and queuing calls straightforward.

## Concepts

- `PhoneCallDefinition`: abstract base for building a call
- `CallerDefinition`: sets the caller name/icon or from an `NPC`
- `CallStageEntry`: individual stage with text and triggers
- `SystemTriggerType`: when a trigger executes (Start/Done)

## Define a call

```csharp
using S1API.PhoneCalls;
using S1API.Entities;
using UnityEngine;

public class TutorialCall : PhoneCallDefinition
{
    public TutorialCall() : base("Guide Bot") { }

    public void Build()
    {
        var stage = AddStage("Welcome to S1API!");
        stage.AddSystemTrigger(S1API.PhoneCalls.Constants.SystemTriggerType.StartTrigger);
    }
}
```

## Queue the call

```csharp
var call = new TutorialCall();
call.Build();
S1API.PhoneCalls.CallManager.QueueCall(call);
```

S1API queues calls safely and feeds them to the game when it's free. S1API also patches the game's call system to route calls through S1API's queue.

Utilities:

- `CallManager.PendingCount`: how many calls are queued in S1API (not the game's active call)
- `CallManager.ClearPendingQueue()`: clear all pending S1API calls (does not affect current in-game call)

## From an NPC

```csharp
public class NpcCall : PhoneCallDefinition
{
    public NpcCall(NPC npc) : base(npc) { }
}
```

