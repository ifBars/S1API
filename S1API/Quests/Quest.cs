#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
using S1Dev = Il2CppScheduleOne.DevUtilities;
using S1Map = Il2CppScheduleOne.Map;
using S1Data = Il2CppScheduleOne.Persistence.Datas;
using S1Contacts = Il2CppScheduleOne.UI.Phone.ContactsApp;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
using S1Dev = ScheduleOne.DevUtilities;
using S1Map = ScheduleOne.Map;
using S1Data = ScheduleOne.Persistence.Datas;
using S1Contacts = ScheduleOne.UI.Phone.ContactsApp;
#endif

#if (MONOMELON || MONOBEPINEX)
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
#elif (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppSystem.Collections;
using Il2CppSystem.Collections.Generic;
#endif

using System;
using System.Collections;
using System.IO;
using MelonLoader;
using S1API.Entities;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;
using S1API.Quests.Constants;
using S1API.Saveables;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace S1API.Quests
{
    /// <summary>
    /// An abstract class intended to be derived from for creating custom quests in the game.
    /// </summary>
    public abstract class Quest : Saveable
    {
        /// <summary>
        /// The title of the quest to display for the player.
        /// </summary>
        protected abstract string Title { get; }

        /// <summary>
        /// The description provided to the player.
        /// </summary>
        protected abstract string Description { get; }

        /// <summary>
        /// Whether to automatically begin the quest once instanced.
        /// NOTE: If this is false, you must manually `.Begin()` this quest.
        /// </summary>
        protected virtual bool AutoBegin => true;

        /// <summary>
        /// The current quest state for this quest
        /// </summary>
        protected QuestState QuestState => (QuestState)S1Quest.State;

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// A list of all quest entries added to this quest.
        /// </summary>
        protected readonly System.Collections.Generic.List<QuestEntry> QuestEntries = new System.Collections.Generic.List<QuestEntry>();

        [SaveableField("QuestData")]
        private readonly QuestData _questData;

        internal string? SaveFolder => S1Quest.SaveFolderName;
        /// <summary>
        /// Optional icon sprite to display for the quest.
        /// Override to use a custom icon loaded at runtime (e.g., from a file).
        /// protected override Sprite? QuestIcon => ImageUtils.LoadImage("icon.png");
        /// </summary>
        protected virtual Sprite? QuestIcon => null;

        internal readonly S1Quests.Quest S1Quest;
        private readonly GameObject _gameObject;

        /// <summary>
        /// INTERNAL: Public constructor used for instancing the quest.
        /// </summary>
        public Quest()
        {
            _questData = new QuestData(GetType().Name);

            _gameObject = new GameObject("Quest");
            S1Quest = _gameObject.AddComponent<S1Quests.Quest>();
            S1Quest.StaticGUID = string.Empty;
#if (MONOMELON || MONOBEPINEX)
            FieldInfo titleField = AccessTools.Field(typeof(S1Quests.Quest), "title");
            titleField.SetValue(S1Quest, Title);
#elif (IL2CPPMELON || IL2CPPBEPINEX)
            S1Quest.title = Title;
#endif
            S1Quest.onActiveState = new UnityEvent();
            S1Quest.onComplete = new UnityEvent();
            S1Quest.onInitialComplete = new UnityEvent();
            S1Quest.onQuestBegin = new UnityEvent();
            S1Quest.onQuestEnd = new UnityEvent<S1Quests.EQuestState>();
            S1Quest.onTrackChange = new UnityEvent<bool>();
            S1Quest.TrackOnBegin = true;
            S1Quest.AutoCompleteOnAllEntriesComplete = true;
#if (MONOMELON || MONOBEPINEX)
            FieldInfo autoInitField = AccessTools.Field(typeof(S1Quests.Quest), "autoInitialize");
            autoInitField.SetValue(S1Quest, false);
#elif (IL2CPPMELON || IL2CPPBEPINEX)
            S1Quest.autoInitialize = false;
#endif

            // Setup quest icon prefab
            GameObject iconPrefabObject = new GameObject("IconPrefab",
                CrossType.Of<RectTransform>(),
                CrossType.Of<CanvasRenderer>(),
                CrossType.Of<Image>()
            );
            iconPrefabObject.transform.SetParent(_gameObject.transform);
            Image iconImage = iconPrefabObject.GetComponent<Image>();
            iconImage.sprite = QuestIcon ?? S1Dev.PlayerSingleton<S1Contacts.ContactsApp>.Instance.AppIcon;
            S1Quest.IconPrefab = iconPrefabObject.GetComponent<RectTransform>();

            // Setup UI for POI prefab
            var uiPrefabObject = new GameObject("PoIUIPrefab",
                CrossType.Of<RectTransform>(),
                CrossType.Of<CanvasRenderer>(),
                CrossType.Of<EventTrigger>(),
                CrossType.Of<Button>()
            );
            uiPrefabObject.transform.SetParent(_gameObject.transform);

            var labelObject = new GameObject("MainLabel",
                CrossType.Of<RectTransform>(),
                CrossType.Of<CanvasRenderer>(),
                CrossType.Of<Text>()
            );
            labelObject.transform.SetParent(uiPrefabObject.transform);

            var iconContainerObject = new GameObject("IconContainer",
                CrossType.Of<RectTransform>(),
                CrossType.Of<CanvasRenderer>(),
                CrossType.Of<Image>()
            );
            iconContainerObject.transform.SetParent(uiPrefabObject.transform);
            Image poiIconImage = iconContainerObject.GetComponent<Image>();
            poiIconImage.sprite = QuestIcon ?? S1Dev.PlayerSingleton<S1Contacts.ContactsApp>.Instance.AppIcon;
            RectTransform iconRectTransform = poiIconImage.GetComponent<RectTransform>();
            iconRectTransform.sizeDelta = new Vector2(20, 20);

            // Setup POI prefab
            GameObject poiPrefabObject = new GameObject("POIPrefab");
            poiPrefabObject.SetActive(false);
            poiPrefabObject.transform.SetParent(_gameObject.transform);
            S1Map.POI poi = poiPrefabObject.AddComponent<S1Map.POI>();
            poi.DefaultMainText = "Did it work?";
#if (MONOMELON || MONOBEPINEX)
            FieldInfo uiPrefabField = AccessTools.Field(typeof(S1Map.POI), "UIPrefab");
            uiPrefabField.SetValue(poi, uiPrefabObject);
#elif (IL2CPPMELON || IL2CPPBEPINEX)
            poi.UIPrefab = uiPrefabObject;
#endif
            S1Quest.PoIPrefab = poiPrefabObject;

            S1Quest.onQuestEnd.AddListener((UnityAction<S1Quests.EQuestState>)OnQuestEnded);
        }

        /// <summary>
        /// INTERNAL: Delayed initialization of the quest.
        /// This allows the base game to get things setup beforehand.
        /// </summary>
        internal override void CreateInternal()
        {
            base.CreateInternal();

            // Initialize the quest
            S1Quest.InitializeQuest(Title, Description, Array.Empty<S1Data.QuestEntryData>(), S1Quest?.StaticGUID);

            if (AutoBegin)
                S1Quest?.Begin();
        }

        internal override void SaveInternal(string folderPath, ref List<string> extraSaveables)
        {
            string questDataPath = Path.Combine(folderPath, S1Quest.SaveFolderName);
            if (!Directory.Exists(questDataPath))
                Directory.CreateDirectory(questDataPath);

            base.SaveInternal(questDataPath, ref extraSaveables);
        }

        /// <summary>
        /// INTERNAL: Called when the quest ends
        /// </summary>
        /// <param name="questState">The state it ended in.</param>
        internal void OnQuestEnded(S1Quests.EQuestState questState)
        {
            // Cleanup our quest in the API manager as well as game quests list
            S1Quests.Quest.Quests.Remove(S1Quest);
            QuestManager.Quests.Remove(this);
        }

        /// <summary>
        /// Adds a new quest entry to the quest.
        /// </summary>
        /// <param name="title">The title for the quest entry.</param>
        /// <param name="poiPosition">A position for the point-of-interest, if applicable.</param>
        /// <returns>A reference to the quest entry</returns>
        protected QuestEntry AddEntry(string title, Vector3? poiPosition = null)
        {
            var questEntryObject = new GameObject($"QuestEntry");
            questEntryObject.transform.SetParent(_gameObject?.transform);

            S1Quests.QuestEntry s1QuestEntry = questEntryObject.AddComponent<S1Quests.QuestEntry>();
            
            // Set PoILocation based on whether a location is provided
            // If no location, set to null to prevent POI and compass element creation
            if (poiPosition == null)
            {
                // Set PoILocation to null to prevent compass waypoint creation
                s1QuestEntry.PoILocation = null;
                // Set AutoCreatePoI to false to prevent POI marker creation
                s1QuestEntry.AutoCreatePoI = false;
            }
            else
            {
                // Set PoILocation to the transform when a location is provided
                s1QuestEntry.PoILocation = questEntryObject.transform;
                // Enable AutoCreatePoI to allow POI marker creation
                s1QuestEntry.AutoCreatePoI = true;
            }
            
            S1Quest.Entries.Add(s1QuestEntry);

            QuestEntry questEntry = new QuestEntry(s1QuestEntry)
            {
                Title = title
            };
            
            // Only set POIPosition if a location was provided
            if (poiPosition != null)
            {
                questEntry.POIPosition = poiPosition.Value;
            }
            
            QuestEntries.Add(questEntry);

            return questEntry;
        }

        /// <summary>
        /// Adds a new quest entry to the quest with an NPC as the POI location.
        /// The POI marker will automatically update when the NPC moves.
        /// </summary>
        /// <param name="title">The title for the quest entry.</param>
        /// <param name="npc">The NPC to use as the POI location.</param>
        /// <returns>A reference to the quest entry</returns>
        protected QuestEntry AddEntry(string title, NPC npc)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            var questEntryObject = new GameObject($"QuestEntry");
            questEntryObject.transform.SetParent(_gameObject?.transform);
            // Ensure the GameObject is active so Start() will run
            questEntryObject.SetActive(true);

            S1Quests.QuestEntry s1QuestEntry = questEntryObject.AddComponent<S1Quests.QuestEntry>();
            
            // Set PoILocation to the NPC's transform so the POI follows the NPC
            // This must be set BEFORE adding to Entries list so Start() can create the POI
            Transform npcTransform = npc.Transform;
            if (npcTransform == null)
            {
                // Fallback: set to null if NPC transform is not available
                s1QuestEntry.PoILocation = null;
                s1QuestEntry.AutoCreatePoI = false;
                s1QuestEntry.AutoUpdatePoILocation = false;
            }
            else
            {
                s1QuestEntry.PoILocation = npcTransform;
                // Enable AutoCreatePoI to allow POI marker creation (defaults to true, but ensure it's set)
                s1QuestEntry.AutoCreatePoI = true;
                // Enable AutoUpdatePoILocation so the POI follows the NPC when it moves
                s1QuestEntry.AutoUpdatePoILocation = true;
            }
            
            S1Quest.Entries.Add(s1QuestEntry);

            QuestEntry questEntry = new QuestEntry(s1QuestEntry)
            {
                Title = title
            };
            
            QuestEntries.Add(questEntry);

            // Use a coroutine to ensure POI creation happens after Start() has run
            // This handles cases where Start() hasn't executed yet or has already completed
            if (s1QuestEntry.PoILocation != null && s1QuestEntry.AutoCreatePoI)
            {
                MelonCoroutines.Start(EnsurePOICreation(s1QuestEntry));
            }

            return questEntry;
        }

        /// <summary>
        /// An action called once a quest has been completed.
        /// </summary>
        public event Action OnComplete
        {
            add => EventHelper.AddListener(value, S1Quest.onComplete);
            remove => EventHelper.RemoveListener(value, S1Quest.onComplete);
        }

        /// <summary>
        /// Starts the quest for the save file.
        /// </summary>
        public void Begin() => S1Quest?.Begin();

        /// <summary>
        /// Cancels the quest for the save file.
        /// </summary>
        public void Cancel() => S1Quest?.Cancel();

        /// <summary>
        /// Expires the quest for the save file.
        /// </summary>
        public void Expire() => S1Quest?.Expire();

        /// <summary>
        /// Fails the quest for the save file.
        /// </summary>
        public void Fail() => S1Quest?.Fail();

        /// <summary>
        /// Completes the quest for the save file.
        /// </summary>
        public void Complete() => S1Quest?.Complete();

        /// <summary>
        /// Ends the quest for the save file.
        /// NOTE: This is done upon completion of the entries by default.
        /// </summary>
        public void End() => S1Quest?.End();

        /// <summary>
        /// INTERNAL: Coroutine to ensure POI creation happens after Start() has executed.
        /// Waits one frame to allow Unity's Start() method to run, which will automatically
        /// call CreatePoI() if AutoCreatePoI is true and PoI is null.
        /// If Start() has already run, we call CreatePoI() directly since it's a public method.
        /// </summary>
        /// <param name="questEntry">The quest entry to create POI for.</param>
        private static System.Collections.IEnumerator EnsurePOICreation(S1Quests.QuestEntry questEntry)
        {
            // Wait one frame to allow Start() to execute if it hasn't run yet
            yield return null;

            // If Start() hasn't created the POI yet (e.g., Start() already ran before we set PoILocation),
            // call CreatePoI() directly since it's a public method
            // CreatePoI() checks for PoI == null, PoILocation != null, and ParentQuest != null internally
            if (questEntry.PoILocation != null && questEntry.AutoCreatePoI)
            {
                try
                {
                    questEntry.CreatePoI();
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging, but don't fail completely
                    // The POI might be created by Start() on the next frame if timing is off
                    UnityEngine.Debug.LogWarning($"[S1API] Failed to create POI for quest entry: {ex.Message}");
                }
            }
        }
    }
}
