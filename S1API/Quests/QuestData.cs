namespace S1API.Quests
{
    /// <summary>
    /// Generic data applied for all custom quests.
    /// </summary>
    public class QuestData
    {
        /// <summary>
        /// TODO
        /// </summary>
        public readonly string ClassName;
        
        /// <summary>
        /// TODO
        /// </summary>
        public QuestData(string className) => 
            ClassName = className;
    }
}