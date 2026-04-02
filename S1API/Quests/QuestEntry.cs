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
        private const string FixedPoiAnchorName = "S1API.FixedPoiAnchor";

        /// <summary>
        /// INTERNAL: The stored reference to the quest entry in-game.
        /// </summary>
        internal readonly S1Quests.QuestEntry S1QuestEntry;
        private Transform? _fixedPoiAnchor;
        private object? _ensurePoiPresentationCoroutine;

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
                Transform? poILocation = S1QuestEntry.PoILocation;
                if (poILocation != null)
                    return poILocation.position;
                return Vector3.zero;
            }
            set
            {
                Transform poiAnchor = GetOrCreateFixedPoiAnchor();
                poiAnchor.position = value;
                BindPoiLocation(poiAnchor, autoUpdatePoILocation: false);
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

            BindPoiLocation(npcTransform, autoUpdatePoILocation: true);
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
        /// Clears any active POI from this quest entry and hides the compass marker.
        /// </summary>
        public void ClearPOI()
        {
            StopPendingPoiPresentation();

            S1QuestEntry.PoILocation = null;
            S1QuestEntry.AutoCreatePoI = false;
            S1QuestEntry.AutoUpdatePoILocation = false;

            if (S1QuestEntry.PoI != null)
            {
                S1QuestEntry.DestroyPoI();
            }

            S1QuestEntry.UpdateCompassElement();
        }

        /// <summary>
        /// Binds a quest entry to a POI transform and schedules late POI/compass setup.
        /// </summary>
        /// <param name="poiLocation">The transform to use as the POI source.</param>
        /// <param name="autoUpdatePoILocation">Whether the game should follow the POI transform automatically.</param>
        private void BindPoiLocation(Transform poiLocation, bool autoUpdatePoILocation)
        {
            S1QuestEntry.PoILocation = poiLocation;
            S1QuestEntry.AutoCreatePoI = true;
            S1QuestEntry.AutoUpdatePoILocation = autoUpdatePoILocation;
            SyncPoiInstancePosition();
            SchedulePoiPresentation();
        }

        private Transform GetOrCreateFixedPoiAnchor()
        {
            if (_fixedPoiAnchor != null)
            {
                return _fixedPoiAnchor;
            }

            Transform? existingAnchor = S1QuestEntry.transform.Find(FixedPoiAnchorName);
            if (existingAnchor != null)
            {
                _fixedPoiAnchor = existingAnchor;
                return _fixedPoiAnchor;
            }

            GameObject anchorObject = new GameObject(FixedPoiAnchorName);
            anchorObject.transform.SetParent(S1QuestEntry.transform, false);
            _fixedPoiAnchor = anchorObject.transform;
            return _fixedPoiAnchor;
        }

        private void SchedulePoiPresentation()
        {
            StopPendingPoiPresentation();
            _ensurePoiPresentationCoroutine = MelonCoroutines.Start(EnsurePoiPresentation());
        }

        private void StopPendingPoiPresentation()
        {
            if (_ensurePoiPresentationCoroutine == null)
                return;

            MelonCoroutines.Stop(_ensurePoiPresentationCoroutine);
            _ensurePoiPresentationCoroutine = null;
        }

        /// <summary>
        /// Ensures POI and compass presentation is created even when the POI target is assigned after Start().
        /// </summary>
        private IEnumerator EnsurePoiPresentation()
        {
            yield return null;

            try
            {
                if (S1QuestEntry.PoILocation == null)
                    yield break;

                if (S1QuestEntry.AutoCreatePoI && S1QuestEntry.PoI == null)
                {
                    S1QuestEntry.CreatePoI();
                }

                if (ReflectionUtils.TryGetFieldOrProperty(S1QuestEntry, "compassElement") == null)
                {
                    S1QuestEntry.CreateCompassElement();
                }

                SyncPoiInstancePosition();
                S1QuestEntry.UpdateCompassElement();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[S1API] Failed to update POI presentation for quest entry: {ex.Message}");
            }
            finally
            {
                _ensurePoiPresentationCoroutine = null;
            }
        }

        private void SyncPoiInstancePosition()
        {
            if (S1QuestEntry.PoI == null || S1QuestEntry.PoILocation == null)
                return;

            S1QuestEntry.PoI.transform.position = S1QuestEntry.PoILocation.position;
            S1QuestEntry.PoI.UpdatePosition();
        }
    }
}
