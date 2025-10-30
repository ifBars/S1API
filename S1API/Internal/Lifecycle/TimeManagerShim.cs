#if (IL2CPPMELON)
using S1GameTime = Il2CppScheduleOne.GameTime;
using Il2CppInterop.Runtime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1GameTime = ScheduleOne.GameTime;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Internal.Lifecycle
{
    /// <summary>
    /// INTERNAL: Shim class to hold TimeManager delegates before the real TimeManager instance is available.
    /// </summary>
    internal class TimeManagerShim
    {
        private static TimeManagerShim _instance;
        internal static TimeManagerShim Instance => _instance ??= new TimeManagerShim();

        internal Action onSleepStart = delegate { };
        internal Action onHourPass = delegate { };

#if IL2CPPMELON
        private Il2CppSystem.Action il2cppOnSleepStart;
        private Il2CppSystem.Action il2cppOnHourPass;
        private readonly List<Il2CppSystem.Action> _addedSleepStart = new();
        private readonly List<Il2CppSystem.Action> _addedHourPass = new();
#endif

        private TimeManagerShim() { }

        internal void AddDelegatesToReal()
        {
            try
            {
                var real = S1GameTime.TimeManager.Instance;
                if (real == null)
                {
                    Debug.LogWarning(
                        "TimeManagerShim: Real TimeManager instance not available yet; cannot migrate delegates.");
                    return;
                }

#if IL2CPPMELON
                il2cppOnSleepStart ??= DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(onSleepStart);
                il2cppOnHourPass ??= DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(onHourPass);

                if (!_addedSleepStart.Contains(il2cppOnSleepStart))
                {
                    real.onSleepStart = Il2CppSystem.Delegate.Combine(real.onSleepStart, il2cppOnSleepStart)
                        .Cast<Il2CppSystem.Action>();
                    _addedSleepStart.Add(il2cppOnSleepStart);
                }

                if (!_addedHourPass.Contains(il2cppOnHourPass))
                {
                    real.onHourPass = Il2CppSystem.Delegate.Combine(real.onHourPass, il2cppOnHourPass)
                        .Cast<Il2CppSystem.Action>();
                    _addedHourPass.Add(il2cppOnHourPass);
                }
#else
            real.onSleepStart = (Action)Delegate.Combine(real.onSleepStart, onSleepStart);
            real.onHourPass = (Action)Delegate.Combine(real.onHourPass, onHourPass);
#endif
            }
            catch
            {
            }
        }

        internal void DeleteDelegatesFromReal()
        {
            try
            {
                var real = S1GameTime.TimeManager.Instance;
                if (real == null)
                {
                    Debug.LogWarning(
                        "TimeManagerShim: Real TimeManager instance no longer available; cannot delete delegates.");
                    return;
                }

#if IL2CPPMELON
                foreach (var d in _addedSleepStart)
                    real.onSleepStart = RemoveAll(real.onSleepStart, d);
                _addedSleepStart.Clear();

                foreach (var d in _addedHourPass)
                    real.onHourPass = RemoveAll(real.onHourPass, d);
                _addedHourPass.Clear();
#else
            real.onSleepStart = (Action)Delegate.RemoveAll(real.onSleepStart, onSleepStart);
            real.onHourPass = (Action)Delegate.RemoveAll(real.onHourPass, onHourPass);
#endif
            }
            catch
            {
            }
        }

#if IL2CPPMELON
        private static Il2CppSystem.Action RemoveAll(Il2CppSystem.Action original, Il2CppSystem.Action toRemove)
        {
            if (original == null) return null;

            var list = original.GetInvocationList();
            Il2CppSystem.Action result = null;

            foreach (var d in list)
            {
                if (!d.Equals(toRemove))
                    result = result == null
                        ? d.Cast<Il2CppSystem.Action>()
                        : Il2CppSystem.Delegate.Combine(result, d).Cast<Il2CppSystem.Action>();
            }

            return result;
        }
#endif
    }
}