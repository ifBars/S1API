using System;
using S1API.Casino;

namespace S1API.Internal
{
    internal static class CasinoEventInvoker
    {
        internal static void Invoke(Action? handlers, string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }

        internal static void Invoke<T>(Action<T>? handlers, T value, string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try { handler(value); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }

        internal static void Invoke<T1, T2>(
            Action<T1, T2>? handlers,
            T1 value1,
            T2 value2,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T1, T2> handler in handlers.GetInvocationList())
            {
                try { handler(value1, value2); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }

        internal static void Invoke<T1, T2, T3>(
            Action<T1, T2, T3>? handlers,
            T1 value1,
            T2 value2,
            T3 value3,
            string eventName)
        {
            if (handlers == null)
                return;

            foreach (Action<T1, T2, T3> handler in handlers.GetInvocationList())
            {
                try { handler(value1, value2, value3); }
                catch (Exception ex) { CasinoGameRegistry.LogSubscriberFailure(eventName, ex); }
            }
        }
    }
}
