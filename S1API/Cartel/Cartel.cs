#if (IL2CPPMELON)
using Il2Cpp;
using S1Cartel = Il2CppScheduleOne.Cartel;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using Il2CppInterop.Runtime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Cartel = ScheduleOne.Cartel;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Internal.Utils;

namespace S1API.Cartel
{
    /// <summary>
    /// Provides access to Cartel status and state information.
    /// Wraps the game's Cartel singleton to provide a modder-friendly API.
    /// </summary>
    public sealed class Cartel
    {
        private static Cartel? _cachedInstance;
        private static S1Cartel.Cartel? _lastS1Cartel;
#if IL2CPPMELON
        private static readonly Dictionary<Action<CartelStatus, CartelStatus>, Delegate> _eventDelegates = new Dictionary<Action<CartelStatus, CartelStatus>, Delegate>();
#endif

        /// <summary>
        /// INTERNAL: Reference to the game Cartel instance.
        /// </summary>
        internal readonly S1Cartel.Cartel S1Cartel;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game Cartel instance.
        /// </summary>
        /// <param name="cartel">The game Cartel instance to wrap.</param>
        internal Cartel(S1Cartel.Cartel cartel)
        {
            S1Cartel = cartel;
        }

        /// <summary>
        /// Gets the current Cartel instance, or null if not available.
        /// </summary>
        public static Cartel? Instance
        {
            get
            {
                var s1Cartel = S1DevUtilities.NetworkSingleton<S1Cartel.Cartel>.Instance;
                if (s1Cartel == null)
                {
                    _cachedInstance = null;
                    _lastS1Cartel = null;
                    return null;
                }

                // Return cached instance if the underlying game instance hasn't changed
                if (_cachedInstance != null && _lastS1Cartel == s1Cartel)
                {
                    return _cachedInstance;
                }

                _cachedInstance = new Cartel(s1Cartel);
                _lastS1Cartel = s1Cartel;
                return _cachedInstance;
            }
        }

        /// <summary>
        /// The current status of the Cartel.
        /// </summary>
        public CartelStatus Status =>
            ConvertStatus(S1Cartel.Status);

        /// <summary>
        /// The number of hours since the Cartel status last changed.
        /// </summary>
        public int HoursSinceStatusChange =>
            S1Cartel.HoursSinceStatusChange;

        /// <summary>
        /// Event fired when the Cartel status changes.
        /// Provides the old status and new status as parameters.
        /// </summary>
        public event Action<CartelStatus, CartelStatus> OnStatusChange
        {
            add
            {
                if (value == null)
                    return;

#if IL2CPPMELON
                // For IL2CPP, use reflection to get the event's add_ method
                var eventInfo = typeof(S1Cartel.Cartel).GetEvent("OnStatusChange", BindingFlags.Public | BindingFlags.Instance);
                if (eventInfo == null)
                    return;

                // Create a wrapper delegate that matches the game's event signature
                var ecartelStatusType = typeof(ECartelStatus);
                var handlerType = typeof(Action<,>).MakeGenericType(ecartelStatusType, ecartelStatusType);
                
                // Create a wrapper instance to hold the value
                var wrapper = new EventWrapper(value);
                var wrapperMethod = typeof(EventWrapper).GetMethod(nameof(EventWrapper.Handle), BindingFlags.Public | BindingFlags.Instance);
                var managedDelegate = Delegate.CreateDelegate(handlerType, wrapper, wrapperMethod);

                // Convert to Il2Cpp delegate
                var il2cppActionType = typeof(Il2CppSystem.Action<,>).MakeGenericType(ecartelStatusType, ecartelStatusType);
                var convertMethod = typeof(DelegateSupport).GetMethod("ConvertDelegate", BindingFlags.Public | BindingFlags.Static);
                var genericConvert = convertMethod.MakeGenericMethod(il2cppActionType);
                var il2cppDelegate = genericConvert.Invoke(null, new object[] { managedDelegate });

                // Use the add_ method to subscribe
                var addMethod = eventInfo.GetAddMethod();
                addMethod?.Invoke(S1Cartel, new object[] { il2cppDelegate });

                // Track for removal
                _eventDelegates[value] = il2cppDelegate as Delegate;
#else
                // For Mono, use standard += operator
                S1Cartel.OnStatusChange += (oldStatus, newStatus) =>
                {
                    value?.Invoke(ConvertStatus(oldStatus), ConvertStatus(newStatus));
                };
#endif
            }
            remove
            {
                if (value == null)
                    return;

#if IL2CPPMELON
                // Try to remove the tracked delegate
                if (_eventDelegates.TryGetValue(value, out var il2cppDelegate))
                {
                    var eventInfo = typeof(S1Cartel.Cartel).GetEvent("OnStatusChange", BindingFlags.Public | BindingFlags.Instance);
                    if (eventInfo != null)
                    {
                        var removeMethod = eventInfo.GetRemoveMethod();
                        removeMethod?.Invoke(S1Cartel, new object[] { il2cppDelegate });
                    }
                    
                    _eventDelegates.Remove(value);
                }
#else
                // Note: Event removal is not fully supported due to delegate wrapping
                // Modders should be cautious when removing handlers
#endif
            }
        }

#if IL2CPPMELON
        /// <summary>
        /// INTERNAL: Wrapper class to hold the event handler for IL2CPP delegate conversion.
        /// </summary>
        private class EventWrapper
        {
            private readonly Action<CartelStatus, CartelStatus> _handler;

            public EventWrapper(Action<CartelStatus, CartelStatus> handler)
            {
                _handler = handler;
            }

            public void Handle(ECartelStatus oldStatus, ECartelStatus newStatus)
            {
                _handler?.Invoke(ConvertStatus(oldStatus), ConvertStatus(newStatus));
            }

            private static CartelStatus ConvertStatus(object status)
            {
                if (status == null)
                    return CartelStatus.Unknown;

                int statusValue = (int)status;
                return statusValue switch
                {
                    0 => CartelStatus.Unknown,
                    1 => CartelStatus.Truced,
                    2 => CartelStatus.Hostile,
                    3 => CartelStatus.Defeated,
                    _ => CartelStatus.Unknown
                };
            }
        }
#endif

        /// <summary>
        /// Converts the game's ECartelStatus enum to S1API's CartelStatus enum.
        /// </summary>
        private static CartelStatus ConvertStatus(object status)
        {
            if (status == null)
                return CartelStatus.Unknown;

            // Handle both enum types (Mono/Il2Cpp)
            int statusValue = (int)status;
            return statusValue switch
            {
                0 => CartelStatus.Unknown,
                1 => CartelStatus.Truced,
                2 => CartelStatus.Hostile,
                3 => CartelStatus.Defeated,
                _ => CartelStatus.Unknown
            };
        }
    }
}

