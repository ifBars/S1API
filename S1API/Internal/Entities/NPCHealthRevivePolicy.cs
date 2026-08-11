namespace S1API.Internal.Entities
{
    /// <summary>
    /// INTERNAL: Selects the safe revive path for custom NPCs during FishNet initialization.
    /// </summary>
    internal static class NPCHealthRevivePolicy
    {
        internal static bool ShouldUsePreSpawnFallback(
            bool isInMainScene,
            bool isCustomNpc,
            bool isSpawned) =>
            isInMainScene && isCustomNpc && !isSpawned;

        internal static bool ShouldSuppressSpawnedClientRevive(
            bool isSpawned,
            bool isServer) =>
            isSpawned && !isServer;
    }
}
