#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
using Il2CppInterop.Runtime.Attributes;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// INTERNAL: Stores identity and appearance defaults on the prefab so clients receive
    /// the same configuration on network spawn without relying on RPCs/SyncVars.
    /// On Il2Cpp, stores data in a static registry keyed by prefab name to work around
    /// field serialization issues with RegisterTypeInIl2Cpp components.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    public sealed class NPCPrefabIdentity : MonoBehaviour
    {
        // Public fields for Mono compatibility (auto-serialized there)
        public string Id;
        public string FirstName;
        public string LastName;
        public S1AvatarFramework.AvatarSettings AppearanceDefaults;

        // Static registry to preserve data across network instantiation on Il2Cpp
        private static readonly Dictionary<string, IdentityData> _registry = new Dictionary<string, IdentityData>();
        private bool _applied;

        private struct IdentityData
        {
            public string Id;
            public string FirstName;
            public string LastName;
            public S1AvatarFramework.AvatarSettings AppearanceDefaults;
        }

        private void Awake()
        {
            // On Il2Cpp, restore fields from registry if this is a spawned instance
            #if IL2CPPMELON
            TryRestoreFromRegistry();
            #endif
        }

        private void Start()
        {
            // Best-effort: apply immediately, then retry briefly in case Avatar isn't yet available on clients.
            TryApplyNow();
            if (!_applied)
                MelonCoroutines.Start(DelayedApply());
        }

        /// <summary>
        /// INTERNAL: Called by NPCPrefabBuilder to register identity data for Il2Cpp.
        /// On Mono this is unnecessary as fields auto-serialize, but on Il2Cpp we need
        /// a static registry to survive network instantiation.
        /// </summary>
        internal void RegisterToStaticCache(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return;

            var data = new IdentityData
            {
                Id = this.Id,
                FirstName = this.FirstName,
                LastName = this.LastName,
                AppearanceDefaults = this.AppearanceDefaults
            };

            _registry[prefabName] = data;
        }

        private void TryRestoreFromRegistry()
        {
            // Get prefab name - could be from the instance or from template
            string prefabName = gameObject.name;
            
            // Remove "(Clone)" suffix if present
            if (prefabName.EndsWith("(Clone)"))
                prefabName = prefabName.Substring(0, prefabName.Length - 7);

            if (_registry.TryGetValue(prefabName, out var data))
            {
                this.Id = data.Id;
                this.FirstName = data.FirstName;
                this.LastName = data.LastName;
                this.AppearanceDefaults = data.AppearanceDefaults;
            }
        }
        
#if IL2CPPMELON
        [HideFromIl2Cpp]
#endif
        private IEnumerator DelayedApply()
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



