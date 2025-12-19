#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
#endif

using System;
using System.Collections;
using MelonLoader;
using S1API.Entities;
using S1API.Graffiti;
using S1API.Internal.Abstraction;
using S1API.Internal.Utils;
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
                else
                {
                    // Backward compatibility: If PoILocation is null but we're setting a position,
                    // create the transform and enable POI creation (for older mods that set POIPosition after AddEntry)
                    Transform entryTransform = S1QuestEntry.transform;
                    S1QuestEntry.PoILocation = entryTransform;
                    entryTransform.position = value;
                    
                    // Enable AutoCreatePoI to allow POI marker creation
                    S1QuestEntry.AutoCreatePoI = true;
                }
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

        /// <summary>
        /// Sets the POI location to follow an NPC by type.
        /// The POI marker will automatically update when the NPC moves.
        /// </summary>
        /// <typeparam name="T">The NPC type to follow.</typeparam>
        /// <returns>True if the NPC was found and POI location was set, false otherwise.</returns>
        public bool SetPOIToNPC<T>() where T : NPC
        {
            NPC? npc = NPC.Get<T>();
            if (npc == null)
                return false;
            
            return SetPOIToNPC(npc);
        }

        /// <summary>
        /// Sets the POI location to follow an NPC instance.
        /// The POI marker will automatically update when the NPC moves.
        /// If the POI hasn't been created yet, it will be created automatically.
        /// </summary>
        /// <param name="npc">The NPC instance to follow.</param>
        /// <returns>True if the NPC was valid and POI location was set, false otherwise.</returns>
        public bool SetPOIToNPC(NPC npc)
        {
            if (npc == null || npc.gameObject == null)
                return false;

            Transform npcTransform = npc.Transform;
            if (npcTransform == null)
                return false;

            // Set PoILocation, AutoCreatePoI, and AutoUpdatePoILocation to enable POI creation and NPC following
            S1QuestEntry.PoILocation = npcTransform;
            S1QuestEntry.AutoCreatePoI = true;
            // Enable AutoUpdatePoILocation so the POI follows the NPC when it moves
            S1QuestEntry.AutoUpdatePoILocation = true;
            
            // Use a coroutine to ensure POI creation happens after Start() has executed
            // This handles cases where SetPOIToNPC is called after Start() has already run
            // or when AddEntry was called without a location first
            MelonCoroutines.Start(EnsurePOICreationForNPC(S1QuestEntry));
            
            return true;
        }

        /// <summary>
        /// Sets the POI location to a spray surface's position.
        /// </summary>
        /// <param name="spraySurface">The spray surface to set POI to.</param>
        /// <returns>True if the spray surface was valid and POI location was set, false otherwise.</returns>
        public bool SetPOIToSpraySurface(SpraySurface spraySurface)
        {
            if (spraySurface == null)
                return false;
            
            POIPosition = spraySurface.Position;
            return true;
        }

        /// <summary>
        /// INTERNAL: Coroutine to ensure POI creation happens after Start() has executed.
        /// Waits one frame to allow Unity's Start() method to run, which will automatically
        /// call CreatePoI() if AutoCreatePoI is true and PoI is null.
        /// If Start() has already run, we call CreatePoI() directly since it's a public method.
        /// </summary>
        /// <param name="questEntry">The quest entry to create POI for.</param>
        private static System.Collections.IEnumerator EnsurePOICreationForNPC(S1Quests.QuestEntry questEntry)
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
