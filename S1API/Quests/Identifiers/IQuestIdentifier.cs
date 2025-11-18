namespace S1API.Quests.Identifiers
{
    /// <summary>
    /// Marker interface for quest identifier types used with QuestManager.Get<T>().
    /// Implement empty classes like 'public sealed class DefeatCartel : IQuestIdentifier {}'
    /// and optionally annotate with [QuestName("Finishing the Job")].
    /// </summary>
    public interface IQuestIdentifier { }
}

