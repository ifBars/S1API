namespace S1API.Quests.Identifiers
{
    /// <summary>
    /// Identifier for the base-game quest "Handlers" (Quest_Packagers).
    /// Modders can use <see cref="QuestManager.Get{Packagers}()"/> to resolve it.
    /// </summary>
    [QuestName("Handlers")]
    public sealed class Packagers : IQuestIdentifier { }
}

