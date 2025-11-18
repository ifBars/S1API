#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;

namespace S1API.Quests
{
    /// <summary>
    /// Wrapper for accessing quests that can handle both custom mod quests and base game quests.
    /// Provides unified access to quest events and properties.
    /// </summary>
    public sealed class QuestWrapper
    {
        private readonly Quest? _modQuest;
        private readonly S1Quests.Quest? _baseGameQuest;
        private readonly bool _isBaseGameQuest;
        private readonly Dictionary<Action, Action<S1Quests.EQuestState>> _failWrapperActions = new Dictionary<Action, Action<S1Quests.EQuestState>>();

        internal QuestWrapper(Quest modQuest)
        {
            _modQuest = modQuest;
            _baseGameQuest = null;
            _isBaseGameQuest = false;
        }

        internal QuestWrapper(S1Quests.Quest baseGameQuest)
        {
            _modQuest = null;
            _baseGameQuest = baseGameQuest;
            _isBaseGameQuest = true;
        }

        /// <summary>
        /// The quest title.
        /// </summary>
        public string Title => _isBaseGameQuest ? _baseGameQuest!.Title : _modQuest!.S1Quest.Title;

        /// <summary>
        /// An action called once a quest has been completed.
        /// </summary>
        public event Action OnComplete
        {
            add
            {
                if (_isBaseGameQuest)
                {
                    EventHelper.AddListener(value, _baseGameQuest!.onComplete);
                }
                else
                {
                    _modQuest!.OnComplete += value;
                }
            }
            remove
            {
                if (_isBaseGameQuest)
                {
                    EventHelper.RemoveListener(value, _baseGameQuest!.onComplete);
                }
                else
                {
                    _modQuest!.OnComplete -= value;
                }
            }
        }

        /// <summary>
        /// An action called once a quest has been failed.
        /// </summary>
        public event Action OnFail
        {
            add
            {
                if (_isBaseGameQuest)
                {
                    // Create a wrapper action that checks if state is Failed
                    Action<S1Quests.EQuestState> wrapper = (state) =>
                    {
                        if (state == S1Quests.EQuestState.Failed)
                        {
                            value?.Invoke();
                        }
                    };
                    
                    // Store the mapping so we can remove it later
                    _failWrapperActions[value] = wrapper;
                    
                    // Subscribe to onQuestEnd
                    EventHelper.AddListener(wrapper, _baseGameQuest!.onQuestEnd);
                }
                else
                {
                    _modQuest!.OnFail += value;
                }
            }
            remove
            {
                if (_isBaseGameQuest)
                {
                    // Retrieve and remove the wrapper action
                    if (_failWrapperActions.TryGetValue(value, out var wrapper))
                    {
                        EventHelper.RemoveListener(wrapper, _baseGameQuest!.onQuestEnd);
                        _failWrapperActions.Remove(value);
                    }
                }
                else
                {
                    _modQuest!.OnFail -= value;
                }
            }
        }

        /// <summary>
        /// Gets the quest entries for this quest.
        /// </summary>
        public System.Collections.Generic.List<QuestEntry> QuestEntries
        {
            get
            {
                if (_isBaseGameQuest)
                {
                    var entries = new System.Collections.Generic.List<QuestEntry>();
                    if (_baseGameQuest!.Entries != null)
                    {
                        foreach (var entry in _baseGameQuest.Entries)
                        {
                            entries.Add(new QuestEntry(entry));
                        }
                    }
                    return entries;
                }
                else
                {
                    return _modQuest!.QuestEntries;
                }
            }
        }
    }
}

