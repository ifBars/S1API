#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using NativeAction = Il2CppSystem.Action;
using S1Messaging = Il2CppScheduleOne.Messaging;
#elif MONOMELON
using NativeAction = System.Action;
using S1Messaging = ScheduleOne.Messaging;
#endif

using System;
using S1API.Internal.Abstraction;
using S1API.Logging;
using S1API.Messaging;

namespace S1API.Entities
{
    /// <summary>
    /// Provides conversation state, events, and messaging helpers for an NPC.
    /// </summary>
    public sealed class NPCMessaging
    {
        private static readonly Log Logger = new Log("NPCMessaging");

        private readonly NPC _npc;
        private Action? _conversationOpenedHandlers;
        private S1Messaging.MSGConversation? _subscribedConversation;
        private NativeAction? _nativeConversationOpenedDispatcher;

        internal NPCMessaging(NPC npc)
        {
            _npc = npc;
        }

        /// <summary>
        /// Gets whether the NPC's conversation is currently marked as read.
        /// </summary>
        /// <remarks>A conversation that has not been created yet is treated as read.</remarks>
        public bool IsRead =>
            _npc.S1NPC?.MSGConversation?.Read ?? true;

        /// <summary>
        /// Gets whether the NPC's conversation is currently marked as unread.
        /// </summary>
        /// <remarks>This reflects the game's conversation-level state, not per-message read receipts.</remarks>
        public bool HasUnreadMessages =>
            !IsRead;

        /// <summary>
        /// Gets whether the NPC's conversation is currently open in the phone's Messages app.
        /// </summary>
        public bool IsOpen =>
            _npc.S1NPC?.MSGConversation?.IsOpen ?? false;

        /// <summary>
        /// Occurs when the player opens the NPC's conversation in the phone's Messages app.
        /// </summary>
        /// <remarks>The event is raised again each time the conversation is closed and reopened.</remarks>
        public event Action OnConversationOpened
        {
            add
            {
                if (value == null)
                    return;

                _conversationOpenedHandlers += value;
                EnsureConversationHook();
            }
            remove
            {
                if (value == null)
                    return;

                _conversationOpenedHandlers -= value;
                if (_conversationOpenedHandlers == null)
                    RemoveConversationHook();
            }
        }

        /// <summary>
        /// Sends a text message from this NPC to the players.
        /// </summary>
        /// <param name="message">The message to display. Unity rich text is allowed.</param>
        /// <param name="responses">Optional responses to display after the message.</param>
        /// <param name="responseDelay">The delay before the player can select a response.</param>
        /// <param name="network">Whether the message should propagate to all players.</param>
        public void SendTextMessage(
            string message,
            Response[]? responses = null,
            float responseDelay = 1f,
            bool network = true) =>
            _npc.SendTextMessage(message, responses, responseDelay, network);

        internal void EnsureConversationHook()
        {
            if (_conversationOpenedHandlers == null)
                return;

            S1Messaging.MSGConversation? conversation = _npc.S1NPC?.MSGConversation;
            if (conversation == null || conversation == _subscribedConversation)
                return;

            RemoveConversationHook();
            NativeAction dispatcher = GetOrCreateNativeDispatcher();

#if (IL2CPPMELON)
            conversation.onConversationOpened = conversation.onConversationOpened == null
                ? dispatcher
                : Il2CppSystem.Delegate.Combine(conversation.onConversationOpened, dispatcher).Cast<NativeAction>();
#else
            conversation.onConversationOpened = (NativeAction?)Delegate.Combine(
                conversation.onConversationOpened,
                dispatcher);
#endif
            _subscribedConversation = conversation;
        }

        internal void Cleanup()
        {
            RemoveConversationHook();
            _conversationOpenedHandlers = null;
            _nativeConversationOpenedDispatcher = null;
        }

        private NativeAction GetOrCreateNativeDispatcher()
        {
            if (_nativeConversationOpenedDispatcher != null)
                return _nativeConversationOpenedDispatcher;

#if (IL2CPPMELON)
            _nativeConversationOpenedDispatcher = DelegateSupport.ConvertDelegate<NativeAction>(
                new Action(DispatchConversationOpened))
                ?? throw new InvalidOperationException("Could not create the native conversation-opened dispatcher.");
#else
            _nativeConversationOpenedDispatcher = DispatchConversationOpened;
#endif
            return _nativeConversationOpenedDispatcher;
        }

        private void RemoveConversationHook()
        {
            if (_subscribedConversation == null || _nativeConversationOpenedDispatcher == null)
            {
                _subscribedConversation = null;
                return;
            }

#if (IL2CPPMELON)
            Il2CppSystem.Delegate? remaining = Il2CppSystem.Delegate.Remove(
                _subscribedConversation.onConversationOpened,
                _nativeConversationOpenedDispatcher);
            _subscribedConversation.onConversationOpened = remaining?.Cast<NativeAction>();
#else
            _subscribedConversation.onConversationOpened = (NativeAction?)Delegate.Remove(
                _subscribedConversation.onConversationOpened,
                _nativeConversationOpenedDispatcher);
#endif
            _subscribedConversation = null;
        }

        private void DispatchConversationOpened()
        {
            Action? handlers = _conversationOpenedHandlers;
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"An NPCMessaging.OnConversationOpened subscriber failed: {ex.Message}");
                }
            }
        }
    }
}
