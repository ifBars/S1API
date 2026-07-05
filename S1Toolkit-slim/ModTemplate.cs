// ============================================================
// ModTemplate.cs – base skeleton for Schedule I MelonLoader mods
//
// // >>> MARKER comments mark the spots a weak LLM should fill in.
// Everything else is hard-wired and compiles as-is.
// ============================================================

using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using S1Toolkit;

[assembly: MelonInfo(typeof(ModTemplate.MyMod), ">>> ModName", "1.0.0", ">>> Author")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonColor(255, 180, 40, 40)]

namespace ModTemplate
{
    public class MyMod : MelonMod
    {
        public static MyMod Instance { get; private set; }

        // ── Config ────────────────────────────────────
        private const string CatName = ">>> CategoryName";
        private static MelonPreferences_Category _category;

        // >>> Declare config entries here:
        // public static MelonPreferences_Entry<float> MyMultiplier;

        public override void OnInitializeMelon()
        {
            Instance = this;

            // ── Config ──
            _category = MelonPreferences.CreateCategory(CatName, ">>> Display Name");
            _category.LoadFromFile();

            // >>> Initialize config entries:
            // MyMultiplier = _category.CreateEntry("MyMultiplier", 1.5f, "Multiplier", "Description");

            _category.SaveToFile(false);

            // ── Safety log ──
            MelonLogger.Msg($"[{CatName}] Initialized.");
        }

        public override void OnLateInitializeMelon()
        {
            // Harmony patches declared via attributes are registered automatically here
        }

        public override void OnUpdate()
        {
            // >>> Hotkey example (commented out):
            // if (Input.GetKeyDown(KeyCode.F5))
            //     MelonLogger.Msg("F5 pressed!");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // >>> Scene-specific logic:
            // if (sceneName == "Main") { ... }
        }
    }

    // ════════════════════════════════════════════════════════
    // Example patch (Postfix)
    // Enable by removing the comment markers.
    //
    // >>> LLM: copy this pattern for your own patches.
    // ════════════════════════════════════════════════════════

    // [HarmonyPatch(typeof(Il2CppScheduleOne.PlayerScripts.Health.PlayerHealth), nameof(Il2CppScheduleOne.PlayerScripts.Health.PlayerHealth.TakeDamage))]
    // public static class PlayerHealth_TakeDamage_Patch
    // {
    //     [HarmonyPostfix]
    //     private static void Postfix(Il2CppScheduleOne.PlayerScripts.Health.PlayerHealth __instance,
    //         float damage, ref bool __result)
    //     {
    //         S1.SafeRun("TakeDamage", () =>
    //         {
    //             // >>> Your code here:
    //             // MelonLogger.Msg($"Damage taken: {damage}");
    //         });
    //     }
    // }
}
