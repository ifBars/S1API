using System;

namespace S1API.Graffiti
{
    /// <summary>
    /// Provides events for graffiti-related gameplay actions.
    /// </summary>
    public static class GraffitiEvents
    {
        /// <summary>
        /// Fired when a player completes a graffiti piece and receives XP/rewards.
        /// This event is triggered when the player closes the graffiti UI after painting.
        /// </summary>
        public static event Action<SpraySurface>? GraffitiCompleted;

        /// <summary>
        /// INTERNAL: Called by GraffitiPatches when Reward() is invoked.
        /// Fires the GraffitiCompleted event.
        /// </summary>
        /// <param name="spraySurface">The spray surface that was completed.</param>
        internal static void OnGraffitiRewarded(SpraySurface spraySurface)
        {
            try
            {
                GraffitiCompleted?.Invoke(spraySurface);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"[S1API.GraffitiEvents] Error in OnGraffitiRewarded: {ex.Message}");
                MelonLoader.MelonLogger.Error($"[S1API.GraffitiEvents] StackTrace: {ex.StackTrace}");
            }
        }
    }
}

