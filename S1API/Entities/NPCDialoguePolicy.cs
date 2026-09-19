using System;

namespace S1API.Entities
{
    internal static class NPCDialoguePolicy
    {
        internal static bool MatchesChoiceContainer(string? candidateName, string? requestedName)
        {
            return !string.IsNullOrEmpty(candidateName)
                && !string.IsNullOrEmpty(requestedName)
                && string.Equals(candidateName, requestedName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
