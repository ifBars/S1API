namespace S1API.Quests.Identifiers
{
    /// <summary>
    /// Identifier for the base-game quest "Getting Started" (Quest_GettingStarted).
    /// Modders can use <see cref="QuestManager.Get{GettingStarted}()"/> to resolve it.
    /// </summary>
    [QuestName("Getting Started")]
    public sealed class GettingStarted : IQuestIdentifier { }
}

