namespace S1API.Quests.Identifiers
{
    /// <summary>
    /// Identifier for the base-game quest "Money Management" (Quest).
    /// Modders can use <see cref="QuestManager.Get{MoneyManagement}()"/> to resolve it.
    /// </summary>
    [QuestName("Money Management")]
    public sealed class MoneyManagement : IQuestIdentifier { }
}

