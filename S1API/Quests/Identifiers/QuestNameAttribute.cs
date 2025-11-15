using System;

namespace S1API.Quests.Identifiers
{
    /// <summary>
    /// Annotate quest identifier types with the display title of the quest.
    /// Usage: [QuestName("Finishing the Job")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class QuestNameAttribute : Attribute
    {
        public string Name { get; }
        public QuestNameAttribute(string name)
        {
            Name = name;
        }
    }
}

