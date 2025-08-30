using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace S1API.Internal.Abstraction
{
    /// <summary>
    /// This static class provides us an easy wrapper for subscribing and unsubscribing unity actions.
    /// </summary>
    public static class EventHelper
    {
        /// <summary>
        /// INTERNAL: Tracking for subscribed actions.
        /// </summary>
        internal static readonly Dictionary<Action, Delegate> SubscribedActions = new Dictionary<Action, Delegate>();

        /// <summary>
        /// INTERNAL: Tracking for subscribed generic actions.
        /// Maps Action<T> to UnityAction<T> instances for safe remove on IL2CPP.
        /// </summary>
        private static readonly Dictionary<Delegate, Delegate> SubscribedGenericActions = new Dictionary<Delegate, Delegate>();

        /// <summary>
        /// Adds a listener to the event, as well as the subscription list.
        /// </summary>
        /// <param name="listener">The action / method you want to subscribe.</param>
        /// <param name="unityEvent">The event you want to subscribe to.</param>
        public static void AddListener(Action listener, UnityEvent unityEvent)
        {
            if (SubscribedActions.ContainsKey(listener))
                return;

#if (IL2CPPMELON || IL2CPPBEPINEX)
            // On IL2CPP prefer System.Action to avoid UnityAction .ctor issues
            System.Action wrapped = new System.Action(listener);
            unityEvent.AddListener(wrapped);
#else
            UnityAction wrapped = new UnityAction(listener);
            unityEvent.AddListener(wrapped);
#endif
            SubscribedActions.Add(listener, wrapped);
        }

        /// <summary>
        /// Adds an EventTrigger entry in a cross-compatible manner.
        /// Use this from Mono mods so IL2CPP handles the actual Entry construction.
        /// </summary>
        /// <param name="trigger">Target EventTrigger component.</param>
        /// <param name="eventType">The EventTriggerType to subscribe to.</param>
        /// <param name="listener">Callback invoked when the event fires.</param>
        public static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, Action listener)
        {
            if (trigger == null || listener == null)
                return;

            AddEventTrigger(trigger, eventType, (_)=> listener());
        }

        /// <summary>
        /// Adds an EventTrigger entry with access to BaseEventData.
        /// </summary>
        /// <param name="trigger">Target EventTrigger component.</param>
        /// <param name="eventType">The EventTriggerType to subscribe to.</param>
        /// <param name="listener">Callback invoked with BaseEventData when the event fires.</param>
        public static void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> listener)
        {
            if (trigger == null || listener == null)
                return;

            var entry = new EventTrigger.Entry { eventID = eventType };
            AddListener(listener, entry.callback);
            trigger.triggers.Add(entry);
        }

        /// <summary>
        /// Removes a listener to the event, as well as the subscription list.
        /// </summary>
        /// <param name="listener">The action / method you want to unsubscribe.</param>
        /// <param name="unityEvent">The event you want to unsubscribe from.</param>
        public static void RemoveListener(Action listener, UnityEvent unityEvent)
        {
            SubscribedActions.TryGetValue(listener, out Delegate? wrappedAction);
            SubscribedActions.Remove(listener);
            if (wrappedAction == null)
                return;
#if (IL2CPPMELON || IL2CPPBEPINEX)
            if (wrappedAction is System.Action sys)
                unityEvent.RemoveListener(sys);
#else
            if (wrappedAction is UnityAction ua)
                unityEvent.RemoveListener(ua);
#endif
        }

        /// <summary>
        /// Adds a listener for UnityEvent<T> in an IL2CPP-safe manner.
        /// </summary>
        public static void AddListener<T>(Action<T> listener, UnityEvent<T> unityEvent)
        {
            if (listener == null || unityEvent == null)
                return;

            if (SubscribedGenericActions.ContainsKey(listener))
                return;

#if (IL2CPPMELON || IL2CPPBEPINEX)
            // Use System.Action<T> wrapper for IL2CPP
            System.Action<T> wrapped = new System.Action<T>(listener);
            unityEvent.AddListener(wrapped);
#else
            UnityAction<T> wrapped = new UnityAction<T>(listener);
            unityEvent.AddListener(wrapped);
#endif
            SubscribedGenericActions.Add(listener, wrapped);
        }

        /// <summary>
        /// Removes a listener for UnityEvent<T> added via AddListener<T>.
        /// </summary>
        public static void RemoveListener<T>(Action<T> listener, UnityEvent<T> unityEvent)
        {
            if (listener == null || unityEvent == null)
                return;

            if (!SubscribedGenericActions.TryGetValue(listener, out Delegate wrapped))
                return;

#if (IL2CPPMELON || IL2CPPBEPINEX)
            if (wrapped is System.Action<T> sys)
                unityEvent.RemoveListener(sys);
#else
            if (wrapped is UnityAction<T> ua)
                unityEvent.RemoveListener(ua);
#endif
            SubscribedGenericActions.Remove(listener);
        }
    }
}