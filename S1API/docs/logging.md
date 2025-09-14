# Logging

S1API ships a small logging helper under `S1API.Logging.Log`.

## Usage

```csharp
using S1API.Logging;

var log = new Log("NotesApp");
log.Msg("Initialized");
log.Warning("This might take a moment...");
log.Error("Something went wrong");
```

Logs appear in the MelonLoader/BepInEx console and log files depending on runtime.

