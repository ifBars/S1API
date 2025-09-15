namespace S1API.Quests
{
    /// <summary>
    /// Generic data applied for all custom quests.
    /// </summary>
    public class QuestData
    {
        /// <summary>
        /// Fully qualified quest class name used by the game
        /// to instantiate or resolve the custom quest.
        /// </summary>
        public readonly string ClassName;
        
        /// <summary>
        /// Creates a new quest data descriptor.
        /// </summary>
        /// <param name="className">Fully qualified quest type name (e.g.,
        /// "MyMod.Quests.IntroQuest").</param>
        public QuestData(string className) => 
            ClassName = className;
    }
}
