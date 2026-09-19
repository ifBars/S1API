namespace S1API.Internal.Entities
{
    internal static class NPCRelationshipPersistencePolicy
    {
        internal static bool IsValidSavedDelta(float relationDelta) =>
            !float.IsNaN(relationDelta) && !float.IsInfinity(relationDelta);

        internal static bool ShouldApplyDefaults(bool relationshipLoadedFromSave) =>
            !relationshipLoadedFromSave;
    }
}
