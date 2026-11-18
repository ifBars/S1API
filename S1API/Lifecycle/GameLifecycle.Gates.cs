using System;
using S1API.Internal.Lifecycle;

namespace S1API.Lifecycle
{
    public static partial class GameLifecycle
    {
        /// <summary>
        /// Registers a check that can block a save. Before every save S1API calls <paramref name="reason"/>; when it
        /// returns a non-empty text, the save does not run, a warning with owner and reason is logged and
        /// <see cref="OnSaveBlocked"/> fires. Return <c>null</c> to allow the save.
        /// </summary>
        /// <param name="owner">Name shown in the log and passed to <see cref="OnSaveBlocked"/>, usually the mod name.</param>
        /// <param name="reason">Returns why the save must not run, or <c>null</c>.</param>
        /// <returns>A handle that removes the veto when disposed.</returns>
        /// <remarks>
        /// Covers <c>SaveManager.Save()</c> and <c>SaveManager.Save(string)</c>. A blocked save also skips the modded
        /// data S1API writes (saveables, quests, custom products), so the save on disk stays as it was. A veto that
        /// throws is logged once and skipped. No notice is shown to the player; use <see cref="OnSaveBlocked"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var veto = GameLifecycle.AddSaveVeto("MyMod", () => _worldBroken ? "the world change failed" : null);
        /// </code>
        /// </example>
        public static IDisposable AddSaveVeto(string owner, Func<string?> reason) =>
            SaveLoadGates.AddSaveVeto(owner, reason);

        /// <summary>
        /// Fired with owner and reason when a save veto blocks a save.
        /// </summary>
        public static event Action<string, string>? OnSaveBlocked
        {
            add => SaveLoadGates.SaveBlocked += value;
            remove => SaveLoadGates.SaveBlocked -= value;
        }

        /// <summary>
        /// Registers a gate that the game start waits for. While <paramref name="ready"/> returns false,
        /// <c>LoadManager.StartGame</c> and <c>LoadManager.LoadAsClient</c> are deferred and started again with the
        /// same arguments once every gate is open.
        /// </summary>
        /// <param name="owner">Name shown in the log, usually the mod name.</param>
        /// <param name="ready">Returns true once the mod is ready for the load. Checked every frame while a load waits.</param>
        /// <param name="timeoutSeconds">After this many seconds of waiting the gate is logged as an error and the load continues without it.</param>
        /// <returns>A handle that removes the gate when disposed. A deferred load continues if this was the last closed gate.</returns>
        /// <remarks>
        /// A gate that throws is logged once and treated as open. Patches of other mods on these two methods also run
        /// for the deferred call; they can check <c>__runOriginal</c> to see whether the load really starts.
        /// </remarks>
        /// <example>
        /// <code>
        /// var gate = GameLifecycle.AddLoadGate("MyMod", () => !_captureRunning, 60f);
        /// </code>
        /// </example>
        public static IDisposable AddLoadGate(string owner, Func<bool> ready, float timeoutSeconds = 30f) =>
            SaveLoadGates.AddLoadGate(owner, ready, timeoutSeconds);
    }
}
