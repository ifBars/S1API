---
title: "Game Time System"
description: "Reference for the game time system — TimeManager, GameDateTime, EDay, and time-based events"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Game Time System

## TimeManager (`Il2CppScheduleOne.GameTime.TimeManager`)
`NetworkSingleton<TimeManager>`, ISaveable

### Time Format
24-hour integer format (HHMM): `0` = midnight, `1200` = noon, `2359` = 11:59 PM

### Key Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `TickDuration` | 0.5s | Real-time tick interval |
| `EndOfDay` | 400 | Time frozen at (night cycle) |
| `WakeTime` | 700 | Time skipped to after sleep |
| `DefaultCycleDuration` | 24 min | Real minutes for a full game day |
| `MinuteDuration` | 1 min | Real seconds per game minute (`CycleDuration / 24`) |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `CurrentTime` | `int` (get) | Current 24-hour time (HHMM) |
| `CurrentDay` | `EDay` (get) | Current day of week |
| `DayIndex` | `int` (get) | `ElapsedDays % 7` |
| `ElapsedDays` | `int` (get/set) | Total days elapsed |
| `IsNight` | `bool` (get) | True if 1800-0600 |
| `IsEndOfDay` | `bool` (get) | True if `CurrentTime == 400` |
| `NormalizedTimeOfDay` | `float` (get) | 0.0-1.0 normalized time |
| `TimeSpeedMultiplier` | `float` (get) | Default 1.0, server-only set |
| `IsSleepInProgress` | `bool` (get) | True during sleep transition |
| `Playtime` | `float` (get/set) | Total real-time seconds played |
| `DailyMinSum` | `int` (get) | Minutes since midnight today |

### Events
| Event | Args | Description |
|-------|------|-------------|
| `onMinutePass` | `Action` | Each game minute (capped during EndOfDay) |
| `onUncappedMinutePass` | `Action` | Each game minute (always fires) |
| `onTick` | `Action` | Every ~0.45s real-time |
| `onTimeChanged` | `Action` | Any time change |
| `onTimeSkip` | `Action<int>` | Time skip occurred (args: minutes skipped) |
| `onTimeSet` | `Action` | Time was explicitly set |
| `onHourPass` | `Action` | Each game hour |
| `onDayPass` | `Action` | Each new day (midnight) |
| `onWeekPass` | `Action` | Each Monday |
| `onSleepStart` | `Action` | Sleep transition begins |
| `onSleepEnd` | `Action` | Sleep transition ends |
| `onUpdate` | `Action` | Every frame (unscaled delta) |
| `onFixedUpdate` | `Action` | Every fixed update |

### Methods
| Method | Description |
|--------|-------------|
| `GetDateTime()` | Returns `GameDateTime` struct |
| `IsCurrentTimeWithinRange(int min, int max)` | Time range check (wraps midnight) |
| `IsCurrentDateWithinRange(GameDateTime start, GameDateTime end)` | Date range check |
| `SetTimeAndSync(int time)` | Host-only, sets time + syncs clients |
| `SetTimeSpeedMultiplier(float mult)` | Server-only, set time speed |
| `SetCycleDuration(float minutes)` | Server-only, set real minutes per day |
| `GetTotalMinSum()` | `ElapsedDays * 1440 + DailyMinSum` |
| `StartSleep()` | RPC, initiates sleep sequence |

### Static Utility Methods
| Method | Description |
|--------|-------------|
| `IsGivenTimeWithinRange(int time, int min, int max)` | Static time range check |
| `IsValid24HourTime(string input)` | Regex validation |
| `Get12HourTime(float time, bool appendDesignator)` | Convert to 12-hour format |
| `Get24HourTimeFromMinSum(int minSum)` | Minutes → HHMM |
| `GetMinSumFrom24HourTime(int time)` | HHMM → minutes |
| `GetMinutesToDisplayTime(int minutes)` | `"Xh Ym"` format |
| `AddMinutesTo24HourTime(int time, int minsToAdd)` | Add minutes to HHMM |

### Sleep Sequence
1. All players must be ready to sleep (checked per frame)
2. Host calls `StartSleep()` → `RpcLogic___StartSleep` → fires `onSleepStart`
3. Host sleep done flag set false
4. Each player must call `SetHostSleepDone(true)` (usually via bed UI)
5. When all done: `SkipForwardToTime(700)` → fires `onTimeSkip`, `onHourPass`, `onDayPass`
6. Sleep ends → `onSleepEnd` → auto-save

### Day-Night Cycle
- 0600 = sunrise, 1800 = sunset
- Time frozen at 0400-0600 (EndOfDay)
- `IsNight` = 1800-0600
- `CurrentDay` wraps at midnight based on `ElapsedDays % 7`
- Cycle duration default = 24 real minutes

### Usage Pattern
```csharp
// Subscribe to minute events
NetworkSingleton<TimeManager>.Instance.onMinutePass += OnMinPass;
NetworkSingleton<TimeManager>.Instance.onTimeSkip += OnTimeSkipped;

// Check time of day
bool isNight = NetworkSingleton<TimeManager>.Instance.IsNight;
int currentTime = NetworkSingleton<TimeManager>.Instance.CurrentTime;

// Get structured date
GameDateTime dateTime = NetworkSingleton<TimeManager>.Instance.GetDateTime();
int totalMinSum = dateTime.GetMinSum();

// Server-only time manipulation
if (InstanceFinder.IsServer)
{
    Singleton<TimeManager>.Instance.SetTimeSpeedMultiplier(2f);
    Singleton<TimeManager>.Instance.SetTimeAndSync(1200);
}
```

---

## GameDateTime (`Il2CppScheduleOne.GameTime.GameDateTime`)
Serializable struct for absolute date/time.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `elapsedDays` | `int` | Total elapsed days |
| `time` | `int` | Current time in HHMM |

### Constructors
| Constructor | Description |
|-------------|-------------|
| `GameDateTime(int elapsedDays, int time)` | From days + time |
| `GameDateTime(int minSum)` | From total minutes |
| `GameDateTime(GameDateTimeData data)` | From save data |

### Methods
| Method | Description |
|--------|-------------|
| `GetMinSum()` | `elapsedDays * 1440 + minSumFromTime` |
| `AddMins(int mins)` | Returns new GameDateTime + minutes |
| `GetCopy()` | Returns copy |

### Operators
`+`, `-`, `>`, `>=`, `<`, `<=` (all compare via `GetMinSum()`)

---

## EDay Enum
| Value | Index |
|-------|-------|
| Monday | 0 |
| Tuesday | 1 |
| Wednesday | 2 |
| Thursday | 3 |
| Friday | 4 |
| Saturday | 5 |
| Sunday | 6 |

## TimedCallback
Utility for delayed game-time callbacks.

### Constructor
```csharp
new TimedCallback(Action callback, int durationMinutes, bool tickAtEndOfDay = true, bool tickOnTimeSkip = true)
```

### Methods
| Method | Description |
|--------|-------------|
| `Cancel()` | Cancel and cleanup |
| `Reset()` | Reset to initial duration |

### Usage
```csharp
TimedCallback callback = new TimedCallback(() =>
{
    MelonLogger.Msg("2 game hours passed!");
}, 120); // 120 minutes = 2 hours
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Time.GetCurrentHour()` | Get current in-game hour |
| `Api.Time.SetTime()` | Set game time |
| `Api.Time.SkipToMorning()` | Skip to morning |
| `Api.Time.SkipToNight()` | Skip to night |
| `Api.Time.OnMinutePass` | Event: minute passed |
| `Api.Time.OnDayPass` | Event: day passed |
