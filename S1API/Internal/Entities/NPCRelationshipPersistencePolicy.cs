using System;

namespace S1API.Internal.Entities
{
    internal static class NPCRelationshipPersistencePolicy
    {
        internal static bool IsValidSavedDelta(float relationDelta) =>
            !float.IsNaN(relationDelta) && !float.IsInfinity(relationDelta);

        internal static bool ShouldApplyDefaults(bool relationshipLoadedFromSave) =>
            !relationshipLoadedFromSave;

        /// <summary>
        /// Native <c>NPCRelationData.Init</c> starts an NPC at
        /// <c>Relationship.DefaultRelationshipValue * 5</c>, so a configured delta in [0, 5]
        /// is stored on the NPC data as delta / 5.
        /// </summary>
        internal static float ToNativeDefaultRelationshipValue(float relationDelta) =>
            Math.Max(0f, Math.Min(5f, relationDelta)) / 5f;
    }
}
