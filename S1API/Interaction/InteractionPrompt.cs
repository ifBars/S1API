using System;
using UnityEngine;

namespace S1API.Interaction
{
    /// <summary>
    /// Managed handle for a native Schedule One interaction prompt.
    /// </summary>
    public sealed class InteractionPrompt : IDisposable
    {
        private readonly Internal.Interaction.InteractionPromptRuntime _runtime;
        private bool _removed;

        /// <summary>Creates a builder for a mod-owned GameObject.</summary>
        /// <param name="target">The GameObject whose collider should be interactable.</param>
        /// <returns>A new interaction prompt builder.</returns>
        public static InteractionPromptBuilder CreateBuilder(GameObject target) =>
            InteractionPromptBuilder.Create(target);

        /// <summary>The GameObject containing the native prompt component.</summary>
        public GameObject Target => _runtime.Target;

        /// <summary>The current prompt message.</summary>
        public string Message { get; private set; }

        /// <summary>The current native input action.</summary>
        public InteractionPromptInput Input { get; private set; }

        /// <summary>The current native prompt state.</summary>
        public InteractionPromptState State { get; private set; }

        /// <summary>The current maximum interaction range.</summary>
        public float Range { get; private set; }

        /// <summary>The current overlap priority.</summary>
        public int Priority { get; private set; }

        /// <summary>Whether the prompt currently applies an angle restriction.</summary>
        public bool IsAngleLimited { get; private set; }

        /// <summary>The current angle restriction in degrees.</summary>
        public float AngleLimit { get; private set; }

        /// <summary>Whether this handle has been removed or its target destroyed.</summary>
        public bool IsRemoved => _removed || !_runtime.IsAttached;

        /// <summary>Invoked while the native interaction manager selects this prompt.</summary>
        public event Action? Hovered;

        /// <summary>Invoked when the native interaction starts.</summary>
        public event Action? InteractionStarted;

        /// <summary>Invoked when the native interaction ends.</summary>
        public event Action? InteractionEnded;

        internal InteractionPrompt(InteractionPromptBuilder builder)
        {
            Message = builder.Message;
            Input = builder.Input;
            State = builder.State;
            Range = builder.Range;
            Priority = builder.Priority;
            IsAngleLimited = builder.LimitAngle;
            AngleLimit = builder.AngleLimit;
            _runtime = new Internal.Interaction.InteractionPromptRuntime(
                this,
                builder.Target,
                builder.Input,
                builder.State,
                builder.Message,
                builder.Range,
                builder.Priority,
                builder.LimitAngle,
                builder.AngleLimit,
                builder.DisplayPoint,
                builder.DisplayCollider);

            foreach (Action callback in builder.HoveredCallbacks)
                Hovered += callback;
            foreach (Action callback in builder.InteractionStartedCallbacks)
                InteractionStarted += callback;
            foreach (Action callback in builder.InteractionEndedCallbacks)
                InteractionEnded += callback;
        }

        /// <summary>Updates the native prompt message.</summary>
        /// <param name="message">Non-empty player-facing prompt text.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetMessage(string message)
        {
            EnsureUsable();
            Message = InteractionPromptContract.NormalizeMessage(message);
            _runtime.SetMessage(Message);
            return this;
        }

        /// <summary>Updates the native input action.</summary>
        /// <param name="input">The interaction input action.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetInput(InteractionPromptInput input)
        {
            EnsureUsable();
            InteractionPromptContract.ValidateEnum(input, nameof(input));
            Input = input;
            _runtime.SetInput(input);
            return this;
        }

        /// <summary>Updates the native prompt state.</summary>
        /// <param name="state">The visual and interaction state.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetState(InteractionPromptState state)
        {
            EnsureUsable();
            InteractionPromptContract.ValidateEnum(state, nameof(state));
            State = state;
            _runtime.SetState(state);
            return this;
        }

        /// <summary>Updates the maximum selection distance.</summary>
        /// <param name="range">A finite distance between zero and four metres.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetRange(float range)
        {
            EnsureUsable();
            Range = InteractionPromptContract.NormalizeRange(range);
            _runtime.SetRange(Range);
            return this;
        }

        /// <summary>Updates the overlap priority.</summary>
        /// <param name="priority">A higher value takes precedence.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetPriority(int priority)
        {
            EnsureUsable();
            Priority = priority;
            _runtime.SetPriority(priority);
            return this;
        }

        /// <summary>Applies a horizontal angle restriction.</summary>
        /// <param name="angleLimit">A finite angle greater than zero and at most 180 degrees.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetAngleLimit(float angleLimit)
        {
            EnsureUsable();
            AngleLimit = InteractionPromptContract.NormalizeAngleLimit(angleLimit);
            IsAngleLimited = true;
            _runtime.SetAngleLimit(AngleLimit);
            return this;
        }

        /// <summary>Clears the horizontal angle restriction.</summary>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt WithoutAngleLimit()
        {
            EnsureUsable();
            IsAngleLimited = false;
            _runtime.ClearAngleLimit();
            return this;
        }

        /// <summary>Updates the prompt display anchor to a transform.</summary>
        /// <param name="displayPoint">The transform whose position should be used.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetDisplayLocation(Transform displayPoint)
        {
            EnsureUsable();
            if (ReferenceEquals(displayPoint, null) || displayPoint == null)
                throw new ArgumentNullException(nameof(displayPoint));
            _runtime.SetDisplayLocation(displayPoint);
            return this;
        }

        /// <summary>Updates the prompt display anchor to a collider.</summary>
        /// <param name="displayCollider">The collider used for display positioning.</param>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt SetDisplayLocation(Collider displayCollider)
        {
            EnsureUsable();
            if (ReferenceEquals(displayCollider, null) || displayCollider == null)
                throw new ArgumentNullException(nameof(displayCollider));
            _runtime.SetDisplayLocation(displayCollider);
            return this;
        }

        /// <summary>Resets prompt positioning to the target transform.</summary>
        /// <returns>This prompt for fluent chaining.</returns>
        public InteractionPrompt ClearDisplayLocation()
        {
            EnsureUsable();
            _runtime.ClearDisplayLocation();
            return this;
        }

        /// <summary>Removes the owned native prompt component.</summary>
        /// <returns><see langword="true"/> when this call performed the removal.</returns>
        public bool Remove()
        {
            if (IsRemoved)
                return false;

            _removed = true;
            _runtime.Dispose();
            return true;
        }

        /// <inheritdoc />
        public void Dispose() => Remove();

        internal void RaiseHovered() => Hovered?.Invoke();
        internal void RaiseInteractionStarted() => InteractionStarted?.Invoke();
        internal void RaiseInteractionEnded() => InteractionEnded?.Invoke();

        private void EnsureUsable()
        {
            if (IsRemoved)
                throw new ObjectDisposedException(nameof(InteractionPrompt));
        }
    }
}
