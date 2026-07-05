# S1Toolkit — Mod Writing Context (paste this whole file into the model prompt)

You are writing a MelonLoader mod for "Schedule I" (Unity Il2Cpp, .NET 6).

## Hard rules (violating any = wrong code)

1. Game namespaces start with `Il2Cpp`: `using Il2CppScheduleOne.PlayerScripts;` (never plain `ScheduleOne.*`)
2. Cast only with `obj.TryCast<T>()` — never `as`, `is`, or `(T)obj`
3. No custom MonoBehaviours — inherit `MelonMod`, use its overrides
4. Coroutines only via `MelonCoroutines.Start(MyRoutine())` — never `StartCoroutine`
5. Harmony: `[HarmonyPrefix]`/`[HarmonyPostfix]` only — never Transpiler
6. Config via `MelonPreferences` — no custom JSON/XML files
7. File paths under `MelonEnvironment.UserDataDirectory` — never `Application.dataPath`
8. Unity null check: `if (obj != null)` — never `is null`
9. No threads, no `async`/`await`, no `Task.Delay` — use `S1.Every`/`S1.RunAfter`
10. Do NOT use `S1Toolkit.Modules` (class-based layer) — use only static `Api.*` and `S1.*` calls below

## Template (fill in the `>>>` markers, change nothing else)

```csharp
using MelonLoader;
using UnityEngine;
using S1Toolkit;
using S1Toolkit.Api;

[assembly: MelonInfo(typeof(MyNamespace.MyMod), ">>> ModName", "1.0.0", ">>> Author")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace MyNamespace
{
    public class MyMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Mod loaded.");
            // >>> One-time setup, e.g.:
            // S1.OnKey(KeyCode.F5, () => Api.Money.Add(1000));
            // S1.Every(60f, () => Api.Player.SetEnergy(100));
        }
    }
}
```

## API (most used calls)

```csharp
// Money & player
Api.Money.Add(5000);                       // add cash
float cash = Api.Money.GetBalance();
Api.Player.SetHealth(100);                 // 0-100
Api.Player.SetEnergy(100);                 // 0-100
Api.Player.Teleport(x, y, z);
bool alive = Api.Player.IsAlive();

// Inventory & items (IDs: see table below)
Api.Inventory.Add("ogkush", 5);
Api.Inventory.Remove("cocaine", 3);
int n = Api.Inventory.GetQuantity("ogkush");
bool has = Api.Inventory.HasItem("jar");
bool ok = Api.Item.Exists("meth");         // check before using an ID

// Time
Api.Time.SkipToMorning();                  // 6:00 AM
Api.Time.SetTime(480);                     // minutes since midnight (host only)
Api.Time.OnDayPass += () => { /* each new day */ };

// NPCs (keys: see table below)
Api.NPC.SetRelationship("ming", 5f);       // 0=hostile, 5=loyal
Api.NPC.Teleport("ming", x, y, z);
Api.NPC.OnNPCDeath += id => { };

// Police
Api.Police.ClearWarrant();
bool wanted = Api.Police.IsWanted();

// UI & feedback
Api.UI.Notify("MyMod", "Done!");

// Save & config
Api.Save.SaveData("my_mod", "{\"x\":1}");  // JSON per savegame
string json = Api.Save.LoadData("my_mod");
bool on = Api.Config.Get("MyMod", "enabled", true);

// Helpers (S1Toolkit namespace)
S1.OnKey(KeyCode.F5, () => { });           // hotkey, no OnUpdate needed
S1.Every(10f, () => { });                  // repeating timer (seconds)
S1.RunAfter(3f, () => { });                // one-shot delay
S1.SafeRun("name", () => { });             // error-guarded block for patches
```

## Valid IDs (anything else will fail — verify with Api.Item.Exists)

| Kind | IDs |
|---|---|
| Products | `ogkush` `sour_diesel` `green_crack` `grandaddy_purple` `cocaine` `meth` `shrooms` `liquid_meth` |
| Packaging | `baggie` `jar` `brick` |
| Seeds | `weed_seed` `coca_seed` `shroom_spores` |
| Additives | `fertilizer_basic` `fertilizer_advanced` `growth_booster` |
| Tools/misc | `trimmers` `watering_can` `skateboard` `cash` |
| Weapons | `baseball_bat` `pistol` `revolver` `shotgun` `rifle` `ak47` `m1911` |
| Clothing | `cap` `beanie` `sunglasses` `shirt` `jacket` `pants` `shoes` |
| NPC keys | `uncle_nelson` `billy` `dan` `jen` `ming` `oscar` `sam` `thomas_benzies` `salvador` `fungal_phil` (+ `dealer_*`) |
| Properties | `bungalow` `manor` `motel_room` `rv` `sweatshop` |
| Vehicles | `shitbox` |

## Fix loop

After writing code, run: `pwsh Tools/check.ps1 -Path <mod folder> -FirstErrorOnly`
It prints exactly ONE line like `ERROR File.cs:12 <problem>. Fix: <what to do>`.
Apply that one fix, run again, repeat until it prints `OK - no findings`. Then: `dotnet build`.
