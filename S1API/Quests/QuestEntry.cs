#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
#endif

using System;
using S1API.Internal.Abstraction;
using S1API.Quests.Constants;
using UnityEngine;

namespace S1API.Quests
{
    /// <summary>
    /// Represents a quest entry on a quest. These are the individual `tasks` on a quest.
    /// </summary>
    public class QuestEntry
    {
        /// <summary>
        /// INTERNAL: The stored reference to the quest entry in-game.
        /// </summary>
        internal readonly S1Quests.QuestEntry S1QuestEntry;

        /// <summary>
        /// INTERNAL: Creates a quest entry from an in-game quest entry instance.
        /// </summary>
        /// <param name="questEntry"></param>
        internal QuestEntry(S1Quests.QuestEntry questEntry) =>
            S1QuestEntry = questEntry;
        /// <summary>
        /// The current state of this quest entry.
        /// </summary>
        public QuestState State => (QuestState)(int)S1QuestEntry.State;

        /// <summary>
        /// An action called once a quest has been completed.
        /// </summary>
        public event Action OnComplete
        {
            add => EventHelper.AddListener(value, S1QuestEntry.onComplete);
            remove => EventHelper.RemoveListener(value, S1QuestEntry.onComplete);
        }

        /// <summary>
        /// The title displayed for the quest entry.
        /// </summary>
        public string Title
        {
            get => S1QuestEntry.Title;
            set => S1QuestEntry.SetEntryTitle(value);
        }

        /// <summary>
        /// The point-of-interest world position.
        /// Returns Vector3.zero if no location is set.
        /// Setting a position will create/update the PoILocation transform if it doesn't exist.
        /// </summary>
        public Vector3 POIPosition
        {
            get
            {
                object? poILocation = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(S1QuestEntry, "PoILocation");
                if (poILocation is Transform transform && transform != null)
                    return transform.position;
                return Vector3.zero;
            }
            set
            {
                object? poILocation = Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(S1QuestEntry, "PoILocation");
                if (poILocation is Transform transform && transform != null)
                {
                    transform.position = value;
                }
                // If PoILocation is null, we can't set the position (entry has no location)
            }
        }

        /// <summary>
        /// Marks the quest entry as started and transitions its
        /// <see cref="State"/> to the in-progress state in-game.
        /// </summary>
        public void Begin() => S1QuestEntry.Begin();

        /// <summary>
        /// Marks the quest entry as completed.
        /// </summary>
        public void Complete() => S1QuestEntry.Complete();

        /// <summary>
        /// Manually sets the state of the quest entry.
        /// </summary>
        /// <param name="questState">The state you want the entry to be.</param>
        public void SetState(QuestState questState) =>
            S1QuestEntry.SetState((S1Quests.EQuestState)questState);
    }
}
