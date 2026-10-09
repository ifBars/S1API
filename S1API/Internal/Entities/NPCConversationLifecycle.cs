#if IL2CPPMELON
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1NPCs = Il2CppScheduleOne.NPCs;
using ConversationRegistry = Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppScheduleOne.Messaging.MSGConversation>;
#else
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Messaging = ScheduleOne.Messaging;
using S1NPCs = ScheduleOne.NPCs;
using ConversationRegistry = System.Collections.Generic.Dictionary<string, ScheduleOne.Messaging.MSGConversation>;
#endif
using System;
using System.Collections;
using S1API.Internal.Utils;

namespace S1API.Internal.Entities
{
    internal static class NPCConversationLifecycle
    {
        internal static IEnumerator RebindWhenSpawned(S1NPCs.NPC npc)
        {
            // Client Start can precede FishNet assigning the final object ID.
            while (npc != null && npc.NetworkObject != null && !npc.NetworkObject.IsSpawned)
                yield return null;

            if (npc == null)
                yield break;

            try
            {
                RebindAfterSpawn(npc);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[NPC] Could not rebind client conversation for '{npc.ID}': {ex.Message}");
            }
        }

        internal static void RebindAfterSpawn(S1NPCs.NPC npc)
        {
            var conversation = npc.MSGConversation;
            if (conversation == null || npc.NetworkObject == null || !npc.NetworkObject.IsSpawned)
                return;

            // Construction can create a conversation before FishNet assigns an object ID.
            // Keep its UI, history and subscriptions, but use the ID native Awake would assign.
            string spawnedId = "messageconversation_" + npc.NetworkObject.ObjectId;

            var manager = S1DevUtilities.NetworkSingleton<S1Messaging.MessagingManager>.Instance;
            if (manager == null)
                return;

#if IL2CPPMELON
            var registry = manager._senderIdConversationMap;
#else
            var registry = ReflectionUtils.TryGetFieldOrProperty(manager, "_senderIdConversationMap")
                as ConversationRegistry;
#endif
            if (registry == null)
                throw new InvalidOperationException("The native messaging registry is unavailable.");

            RebindConversation(conversation, registry, spawnedId);
        }

        internal static void RebindConversation(
            S1Messaging.MSGConversation conversation,
            ConversationRegistry registry,
            string spawnedId)
        {
            string previousId = conversation.ConversationId;
            if (registry.TryGetValue(spawnedId, out var current) && !conversation.Equals(current))
                throw new InvalidOperationException($"A different conversation is already registered for '{spawnedId}'.");

            if (string.Equals(previousId, spawnedId, StringComparison.Ordinal))
            {
                if (current == null)
                    registry[spawnedId] = conversation;
                return;
            }

            if (!ReflectionUtils.TrySetFieldOrProperty(conversation, nameof(conversation.ConversationId), spawnedId))
                throw new InvalidOperationException("Could not update the conversation's native network ID.");

            // Several unspawned contacts can share the native placeholder ID. Never
            // remove another contact's registration while moving this conversation.
            if (registry.TryGetValue(previousId, out var previous) && conversation.Equals(previous))
                registry.Remove(previousId);
            registry[spawnedId] = conversation;
        }
    }
}
