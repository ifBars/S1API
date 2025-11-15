using System;
using System.Linq;
using System.Reflection;
using S1API.Quests.Identifiers;

#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
#endif

namespace S1API.Quests
{
    /// <summary>
    /// Provided management of quests across the game.
    /// </summary>
    public static class QuestManager
    {
        /// <summary>
        /// INTERNAL: Tracking of all custom quests.
        /// </summary>
        internal static readonly System.Collections.Generic.List<Quest> Quests = new System.Collections.Generic.List<Quest>();

        /// <summary>
        /// Creates a new quest for the player to complete from your custom quest class.
        /// </summary>
        /// <param name="guid">The unique identifier for this quest. By default, assigned a random GUID.</param>
        /// <typeparam name="T">Your custom quest class that derived from <see cref="Quest"/>.</typeparam>
        /// <returns>The instance of your quest.</returns>
        public static Quest CreateQuest<T>(string? guid = null) where T : Quest =>
            CreateQuest(typeof(T), guid);

        /// <summary>
        /// Creates a new quest for the player to complete from your custom quest class.
        /// </summary>
        /// <param name="questType">Your custom quest class that derived from <see cref="Quest"/>.</param>
        /// <param name="guid">The unique identifier for this quest. By default, assigned a random GUID.</param>
        /// <returns></returns>
        public static Quest CreateQuest(Type questType, string? guid = null)
        {
            Quest? quest = (Quest)Activator.CreateInstance(questType)!;
            if (quest == null)
                throw new Exception($"Unable to create quest instance of {questType.FullName}!");

            Quests.Add(quest);
            return quest;
        }

        /// <summary>
        /// Returns a <see cref="Quest"/> instance by the Quest GUID
        /// </summary>
        /// <param name="guid">The unique identifier to use for searching this quest</param>
        /// <returns>The quest instance</returns>
        public static Quest? GetQuestByGuid(string guid)
        {
            return Quests.FirstOrDefault(x => x.S1Quest.StaticGUID == guid);
        }

        /// <summary>
        /// Returns a <see cref="Quest"/> instance by the Quest Name.
        /// Searches custom mod quests only. For base game quests, use <see cref="GetBaseGameQuestByName"/>.
        /// </summary>
        /// <param name="questName">The quest title to use for searching this quest</param>
        /// <returns>The quest instance, or null if not found</returns>
        public static Quest? GetQuestByName(string questName)
        {
            return Quests.FirstOrDefault(x => x.S1Quest.Title == questName);
        }

        /// <summary>
        /// Returns a base game quest by its title.
        /// Base game quests cannot be wrapped as S1API.Quest instances, so this returns the game's Quest object directly.
        /// Use typed identifiers (<see cref="Get{T}"/>) for type-safe access to base game quests.
        /// </summary>
        /// <param name="questTitle">The quest title to search for</param>
        /// <returns>The base game quest, or null if not found</returns>
        internal static object? GetBaseGameQuestByName(string questTitle)
        {
            try
            {
#if (MONOMELON || MONOBEPINEX)
                var gameQuests = S1Quests.Quest.Quests;
                if (gameQuests != null)
                {
                    foreach (var gameQuest in gameQuests)
                    {
                        if (gameQuest != null && gameQuest.Title == questTitle)
                        {
                            return gameQuest;
                        }
                    }
                }
#elif (IL2CPPMELON || IL2CPPBEPINEX)
                var gameQuests = S1Quests.Quest.Quests;
                if (gameQuests != null)
                {
                    for (int i = 0; i < gameQuests.Count; i++)
                    {
                        var gameQuest = gameQuests[i];
                        if (gameQuest != null && gameQuest.Title == questTitle)
                        {
                            return gameQuest;
                        }
                    }
                }
#endif
            }
            catch (System.Exception ex)
            {
                MelonLoader.MelonLogger.Error($"[QuestManager] Exception while searching for base game quest '{questTitle}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Returns a <see cref="QuestWrapper"/> instance using a typed identifier.
        /// Declare an identifier class annotated with [Identifiers.QuestName("...")].
        /// Works for both custom mod quests and base game quests.
        /// </summary>
        /// <typeparam name="T">A quest identifier type implementing <see cref="IQuestIdentifier"/>.</typeparam>
        /// <returns>The quest wrapper instance, or null if not found.</returns>
        public static QuestWrapper? Get<T>() where T : IQuestIdentifier
        {
            var t = typeof(T);
            string? name = TryGetNameFromIdentifier(t);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            // First check custom mod quests
            var customQuest = GetQuestByName(name);
            if (customQuest != null)
            {
                return new QuestWrapper(customQuest);
            }

            // Then check base game quests
            var baseGameQuest = GetBaseGameQuestByName(name);
            if (baseGameQuest != null)
            {
                return new QuestWrapper((S1Quests.Quest)baseGameQuest);
            }

            return null;
        }

        private static string TryGetNameFromIdentifier(Type t)
        {
            try
            {
                var attr = t.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().FullName == typeof(QuestNameAttribute).FullName);
                if (attr != null)
                {
                    var prop = attr.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    return prop?.GetValue(attr) as string;
                }
            }
            catch { }
            return null;
        }
    }
}
