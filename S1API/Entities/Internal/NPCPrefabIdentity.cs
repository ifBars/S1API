#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
#endif

using UnityEngine;
using MelonLoader;

namespace S1API.Entities.Internal
{
    /// <summary>
    /// INTERNAL: Stores identity and appearance defaults on the prefab so clients receive
    /// the same configuration on network spawn without relying on RPCs/SyncVars.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    public sealed class NPCPrefabIdentity : MonoBehaviour
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public S1AvatarFramework.AvatarSettings AppearanceDefaults;

        private bool _applied;

        private void Start()
        {
            // Best-effort: apply immediately, then retry briefly in case Avatar isn't yet available on clients.
            TryApplyNow();
            if (!_applied)
                MelonCoroutines.Start(DelayedApply());
        }

        private System.Collections.IEnumerator DelayedApply()
        {
            float start = Time.realtimeSinceStartup;
            float timeout = 3f;
            while (!_applied && (Time.realtimeSinceStartup - start) < timeout)
            {
                TryApplyNow();
                if (_applied)
                    yield break;
                yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// Apply stored defaults to a base-game NPC instance.
        /// Safe to call on both server and clients.
        /// </summary>
        public void ApplyTo(S1NPCs.NPC npc)
        {
            if (npc == null)
                return;

            try
            {
                if (!string.IsNullOrEmpty(FirstName))
                    npc.FirstName = FirstName;
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(LastName))
                    npc.LastName = LastName;
            }
            catch { }
            try
            {
                if (!string.IsNullOrEmpty(Id))
                    npc.ID = Id;
            }
            catch { }

            try
            {
                var avatar = npc.Avatar ?? npc.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                if (avatar != null && AppearanceDefaults != null)
                {
                    avatar.LoadAvatarSettings(AppearanceDefaults);
                }
            }
            catch { }
        }

        private void TryApplyNow()
        {
            try
            {
                var npc = GetComponent<S1NPCs.NPC>();
                if (npc == null)
                    return;
                ApplyTo(npc);
                // Consider applied once avatar exists or when only identity fields are requested
                var avatar = npc.Avatar ?? npc.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
                _applied = (AppearanceDefaults == null) || (avatar != null);
            }
            catch { }
        }
    }
}


