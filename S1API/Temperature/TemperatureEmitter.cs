#if IL2CPPMELON
using Il2CppInterop.Runtime;
using NativeEmitterChangedAction = Il2CppSystem.Action;
using S1Temperature = Il2CppScheduleOne.Temperature;
#elif MONOMELON
using NativeEmitterChangedAction = System.Action;
using S1Temperature = ScheduleOne.Temperature;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Temperature;
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

        /// <summary>
        /// INTERNAL: Creates a wrapper around a native temperature emitter component.
        /// </summary>
        /// <param name="temperatureEmitter">The native temperature emitter component.</param>
        internal TemperatureEmitter(S1Temperature.TemperatureEmitter temperatureEmitter)
        {
            S1TemperatureEmitter = temperatureEmitter;
        }

        /// <summary>
        /// Gets the default ambient temperature, in degrees Celsius.
        /// </summary>
        public const float DefaultAmbientTemperature = TemperatureValidation.DefaultAmbientTemperature;

        /// <summary>
        /// Gets the minimum emitter temperature, in degrees Celsius.
        /// </summary>
        public const float MinTemperature = TemperatureValidation.MinTemperature;

        /// <summary>
        /// Gets the maximum emitter temperature, in degrees Celsius.
        /// </summary>
        public const float MaxTemperature = TemperatureValidation.MaxTemperature;

        /// <summary>
        /// Gets the default emitter range, in world units.
        /// </summary>
        public const float DefaultRange = TemperatureValidation.DefaultRange;

        /// <summary>
        /// Gets the minimum emitter range, in world units.
        /// </summary>
        public const float MinRange = TemperatureValidation.MinRange;

        /// <summary>
        /// Gets the maximum emitter range, in world units.
        /// </summary>
        public const float MaxRange = TemperatureValidation.MaxRange;

        /// <summary>
        /// Gets the native temperature emitter attached to a game object.
        /// </summary>
        /// <param name="gameObject">The game object to inspect.</param>
        /// <returns>A temperature-emitter wrapper, or <c>null</c> when the game object has no emitter component.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameObject"/> is <c>null</c> or destroyed.</exception>
        public static TemperatureEmitter? FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            PruneDestroyedRegistrationStates();
            S1Temperature.TemperatureEmitter? emitter =
                gameObject.GetComponent<S1Temperature.TemperatureEmitter>();
            return emitter == null ? null : new TemperatureEmitter(emitter);
        }

        /// <summary>
        /// Gets the first native temperature emitter on a game object, or adds one when none exists.
        /// </summary>
        /// <param name="gameObject">The game object that owns the emitter component.</param>
        /// <returns>A wrapper around the existing or newly added emitter.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameObject"/> is <c>null</c> or destroyed.</exception>
        /// <remarks>
        /// When a game object has multiple native emitter components, this method returns the first component selected by Unity.
        /// </remarks>
        public static TemperatureEmitter GetOrAddComponent(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            PruneDestroyedRegistrationStates();
            S1Temperature.TemperatureEmitter? emitter =
                gameObject.GetComponent<S1Temperature.TemperatureEmitter>();
            return new TemperatureEmitter(
                emitter ?? gameObject.AddComponent<S1Temperature.TemperatureEmitter>());
        }

        /// <summary>
        /// Gets the emitter temperature in degrees Celsius.
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
        /// Creates an immutable managed snapshot of the emitter's current values.
        /// </summary>
        /// <returns>A snapshot suitable for <see cref="TemperatureAlgorithm.GetTemperatureAtPoint"/>.</returns>
        public TemperatureEmitterInfo ToInfo() =>
            new TemperatureEmitterInfo(Temperature, Range, EmissionPoint);

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
                GetChangedRegistrationState().Registrations.Add(value, nativeHandler);
            }
            remove
            {
                if (value == null || !TryTakeChangedRegistration(value, out ChangedRegistrationState state,
                        out NativeEmitterChangedAction nativeHandler))
                    return;

                try
                {
                    Unsubscribe(nativeHandler);
                }
                catch
                {
                    state.Registrations.Add(value, nativeHandler);
                    throw;
                }

                if (state.Registrations.IsEmpty)
                    ChangedRegistrations.Remove(S1TemperatureEmitter.GetInstanceID());
            }
        }

        /// <summary>
        /// Updates the emitter position in world space.
        /// </summary>
        /// <param name="position">The new emitter position in world space. Every component must be finite.</param>
        /// <exception cref="ArgumentOutOfRangeException">A position component is not finite.</exception>
        public void SetPosition(Vector3 position)
        {
            TemperatureValidation.EnsureFinite(position, nameof(position));
            S1TemperatureEmitter.SetPosition(position);
        }

        /// <summary>
        /// Updates the emitter temperature.
        /// </summary>
        /// <param name="temperature">
        /// The new temperature in degrees Celsius. Finite values are clamped to
        /// <see cref="MinTemperature"/> through <see cref="MaxTemperature"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="temperature"/> is not finite.</exception>
        public void SetTemperature(float temperature)
        {
            float clampedTemperature = TemperatureValidation.ClampTemperature(temperature, nameof(temperature));
            S1TemperatureEmitter.SetTemperature(clampedTemperature);
        }

        /// <summary>
        /// Updates the emitter range in world units.
        /// </summary>
        /// <param name="range">
        /// The new range in world units. Finite values are clamped to <see cref="MinRange"/> through
        /// <see cref="MaxRange"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="range"/> is not finite.</exception>
        public void SetRange(float range)
        {
            float clampedRange = TemperatureValidation.ClampRange(range, nameof(range));
            S1TemperatureEmitter.SetRange(clampedRange);
        }

        /// <summary>
        /// Informs native listeners that the emitter changed.
        /// </summary>
        public void NotifyChanged() =>
            S1TemperatureEmitter.NotifyChanged();

        private static readonly Dictionary<int, ChangedRegistrationState> ChangedRegistrations =
            new Dictionary<int, ChangedRegistrationState>();

        private static NativeEmitterChangedAction CreateNativeChangedHandler(Action handler)
        {
#if IL2CPPMELON
            return DelegateSupport.ConvertDelegate<NativeEmitterChangedAction>(handler)
                ?? throw new InvalidOperationException("Could not create the native temperature-emitter delegate.");
#else
            return handler;
#endif
        }

        private ChangedRegistrationState GetChangedRegistrationState()
        {
            PruneDestroyedRegistrationStates();
            int instanceId = S1TemperatureEmitter.GetInstanceID();
            if (ChangedRegistrations.TryGetValue(instanceId, out ChangedRegistrationState? state))
                return state;

            state = new ChangedRegistrationState(S1TemperatureEmitter);
            ChangedRegistrations.Add(instanceId, state);
            return state;
        }

        private bool TryTakeChangedRegistration(
            Action managedHandler,
            out ChangedRegistrationState state,
            out NativeEmitterChangedAction nativeHandler)
        {
            PruneDestroyedRegistrationStates();
            if (ChangedRegistrations.TryGetValue(
                    S1TemperatureEmitter.GetInstanceID(),
                    out ChangedRegistrationState? registrationState)
                && registrationState.Registrations.TryTakeLast(managedHandler, out nativeHandler))
            {
                state = registrationState;
                return true;
            }

            state = null!;
            nativeHandler = default!;
            return false;
        }

        private static void PruneDestroyedRegistrationStates()
        {
            List<int>? destroyedIds = null;
            foreach (KeyValuePair<int, ChangedRegistrationState> registration in ChangedRegistrations)
            {
                if (registration.Value.S1TemperatureEmitter != null)
                    continue;

                destroyedIds ??= new List<int>();
                destroyedIds.Add(registration.Key);
            }

            if (destroyedIds == null)
                return;

            foreach (int destroyedId in destroyedIds)
                ChangedRegistrations.Remove(destroyedId);
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

        private sealed class ChangedRegistrationState
        {
            internal S1Temperature.TemperatureEmitter S1TemperatureEmitter { get; }

            internal ManagedEventRegistrationTracker<NativeEmitterChangedAction> Registrations { get; } =
                new ManagedEventRegistrationTracker<NativeEmitterChangedAction>();

            internal ChangedRegistrationState(S1Temperature.TemperatureEmitter temperatureEmitter)
            {
                S1TemperatureEmitter = temperatureEmitter;
            }
        }
    }
}
