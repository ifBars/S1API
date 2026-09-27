#if IL2CPPMELON
using S1Avatar = Il2CppScheduleOne.AvatarFramework.Avatar;
using S1NPC = Il2CppScheduleOne.NPCs.NPC;
using S1NPCData = Il2CppScheduleOne.NPCs.Framework.NPCData;
using S1VOEmitter = Il2CppScheduleOne.VoiceOver.VOEmitter;
#else
using S1Avatar = ScheduleOne.AvatarFramework.Avatar;
using S1NPC = ScheduleOne.NPCs.NPC;
using S1NPCData = ScheduleOne.NPCs.Framework.NPCData;
using S1VOEmitter = ScheduleOne.VoiceOver.VOEmitter;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal sealed class NPCNativeAwakeScope
    {
        private readonly S1NPC _npc;
        private readonly List<GameObject> _hiddenEmitterAncestors = new List<GameObject>();
        private S1NPCData? _originalData;
        private bool _createConversationOnStart;

        internal NPCNativeAwakeScope(S1NPC npc) =>
            _npc = npc;

        internal void Prepare()
        {
            // Native private helpers may be inlined in IL2CPP. Prepare their inputs at
            // Awake itself, then restore them even if native initialization throws.
            var avatar = _npc.GetComponentInChildren<S1Avatar>(true);
            var emitter = avatar != null ? avatar.GetComponentInChildren<S1VOEmitter>(true) : null;
            if (emitter != null)
            {
                for (Transform current = emitter.transform; current != null && current != avatar!.transform; current = current.parent)
                {
                    if (!current.gameObject.activeSelf)
                        _hiddenEmitterAncestors.Add(current.gameObject);
                }
                foreach (GameObject ancestor in _hiddenEmitterAncestors)
                    ancestor.SetActive(true);
            }

            if (_npc.MSGConversation != null)
            {
                _originalData = NPCDataAccess.GetDataObject(_npc)?.GetOriginalData();
                if (_originalData != null)
                {
                    _createConversationOnStart = _originalData.Messaging.CreateConversationOnStart;
                    _originalData.Messaging.CreateConversationOnStart = false;
                }
            }
        }

        internal void Restore()
        {
            if (_originalData != null)
            {
                _originalData.Messaging.CreateConversationOnStart = _createConversationOnStart;
                if (_npc != null && _npc.NPCData != null)
                    _npc.NPCData.Messaging.CreateConversationOnStart = _createConversationOnStart;
            }

            foreach (GameObject ancestor in _hiddenEmitterAncestors)
            {
                if (ancestor != null)
                    ancestor.SetActive(false);
            }
        }
    }
}
