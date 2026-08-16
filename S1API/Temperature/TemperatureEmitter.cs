#if IL2CPPMELON
using Il2CppInterop.Runtime;
using NativeEmitterChangedAction = Il2CppSystem.Action;
using S1Temperature = Il2CppScheduleOne.Temperature;
#elif MONOMELON
using NativeEmitterChangedAction = System.Action;
using S1Temperature = ScheduleOne.Temperature;
#endif

using System;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Temperature
{
    /// <summary>
    /// Wraps a native temperature emitter attached to a mod-owned game object.
    /// </summary>
    /// <remarks>
    /// Adding this component does not register it with a native grid, persist its configuration, or synchronize it over the network.
    /// </remarks>
    public sealed class TemperatureEmitter
    {
        /// <summary>
        /// INTERNAL: The native temperature emitter component.
        /// </summary>
        internal readonly S1Temperature.TemperatureEmitter S1TemperatureEmitter;

        private readonly ManagedEventRegistrationTracker<NativeEmitterChangedAction> _changedRegistrations =
            new ManagedEventRegistrationTracker<NativeEmitterChangedAction>();

        /// <summary>
        /// INTERNAL: Creates a wrapper around a native temperature emitter component.
        /// </summary>
        /// <param name="temperatureEmitter">The native temperature emitter component.</param>
        internal TemperatureEmitter(S1Temperature.TemperatureEmitter temperatureEmitter)
        {
            S1TemperatureEmitter = temperatureEmitter;
        }

        /// <summary>
        /// Gets the native temperature emitter attached to a game object.
        /// </summary>
        /// <param name="gameObject">The game object to inspect.</param>
        /// <returns>A temperature-emitter wrapper, or <c>null</c> when the game object has no emitter component.</returns>
        public static TemperatureEmitter? FromGameObject(GameObject gameObject)
        {
            S1Temperature.TemperatureEmitter? emitter =
                gameObject.GetComponent<S1Temperature.TemperatureEmitter>();
            return emitter == null ? null : new TemperatureEmitter(emitter);
        }

        /// <summary>
        /// Gets the first native temperature emitter on a game object, or adds one when none exists.
        /// </summary>
        /// <param name="gameObject">The game object that owns the emitter component.</param>
        /// <returns>A wrapper around the existing or newly added emitter.</returns>
        /// <remarks>
        /// When a game object has multiple native emitter components, this method returns the first component selected by Unity.
        /// </remarks>
        public static TemperatureEmitter GetOrAddComponent(GameObject gameObject)
        {
            S1Temperature.TemperatureEmitter? emitter =
                gameObject.GetComponent<S1Temperature.TemperatureEmitter>();
            return new TemperatureEmitter(
                emitter ?? gameObject.AddComponent<S1Temperature.TemperatureEmitter>());
        }

        /// <summary>
        /// Gets the emitter temperature in the game's native temperature scale.
        /// </summary>
        public float Temperature =>
            S1TemperatureEmitter.Temperature;

        /// <summary>
        /// Gets the emitter range in world units.
        /// </summary>
        public float Range =>
            S1TemperatureEmitter.Range;

        /// <summary>
        /// Gets the emitter position in world space.
        /// </summary>
        public Vector3 EmissionPoint =>
            S1TemperatureEmitter.EmissionPoint;

        /// <summary>
        /// Occurs when the native emitter reports a change.
        /// </summary>
        public event Action OnChanged
        {
            add
            {
                if (value == null)
                    return;

                NativeEmitterChangedAction nativeHandler = CreateNativeChangedHandler(value);
                Subscribe(nativeHandler);
                _changedRegistrations.Add(value, nativeHandler);
            }
            remove
            {
                if (value == null || !_changedRegistrations.TryTakeLast(value, out NativeEmitterChangedAction nativeHandler))
                    return;

                try
                {
                    Unsubscribe(nativeHandler);
                }
                catch
                {
                    _changedRegistrations.Add(value, nativeHandler);
                    throw;
                }
            }
        }

        /// <summary>
        /// Updates the emitter position in world space.
        /// </summary>
        /// <param name="position">The new emitter position in world space.</param>
        public void SetPosition(Vector3 position) =>
            S1TemperatureEmitter.SetPosition(position);

        /// <summary>
        /// Updates the emitter temperature.
        /// </summary>
        /// <param name="temperature">The new temperature in the game's native temperature scale.</param>
        public void SetTemperature(float temperature) =>
            S1TemperatureEmitter.SetTemperature(temperature);

        /// <summary>
        /// Updates the emitter range in world units.
        /// </summary>
        /// <param name="range">The new range in world units.</param>
        public void SetRange(float range) =>
            S1TemperatureEmitter.SetRange(range);

        /// <summary>
        /// Informs native listeners that the emitter changed.
        /// </summary>
        public void NotifyChanged() =>
            S1TemperatureEmitter.NotifyChanged();

        private static NativeEmitterChangedAction CreateNativeChangedHandler(Action handler)
        {
#if IL2CPPMELON
            return DelegateSupport.ConvertDelegate<NativeEmitterChangedAction>(handler)
                ?? throw new InvalidOperationException("Could not create the native temperature-emitter delegate.");
#else
            return handler;
#endif
        }

        private void Subscribe(NativeEmitterChangedAction handler)
        {
#if IL2CPPMELON
            S1TemperatureEmitter.OnEmitterChanged = S1TemperatureEmitter.OnEmitterChanged == null
                ? handler
                : Il2CppSystem.Delegate.Combine(S1TemperatureEmitter.OnEmitterChanged, handler)
                    .Cast<NativeEmitterChangedAction>();
#else
            S1TemperatureEmitter.OnEmitterChanged += handler;
#endif
        }

        private void Unsubscribe(NativeEmitterChangedAction handler)
        {
#if IL2CPPMELON
            Il2CppSystem.Delegate? remaining = Il2CppSystem.Delegate.Remove(
                S1TemperatureEmitter.OnEmitterChanged,
                handler);
            S1TemperatureEmitter.OnEmitterChanged = remaining?.Cast<NativeEmitterChangedAction>();
#else
            S1TemperatureEmitter.OnEmitterChanged -= handler;
#endif
        }
    }
}
