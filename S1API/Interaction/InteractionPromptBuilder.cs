using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Interaction
{
    /// <summary>
    /// Builds a native Schedule One interaction prompt for a mod-owned GameObject.
    /// </summary>
    /// <remarks>
    /// The target must contain a collider on a layer included by the game's interaction
    /// search mask. The builder does not create colliders or change object layers.
    /// </remarks>
    public sealed class InteractionPromptBuilder
    {
        private readonly GameObject _target;
        private readonly List<Action> _hoveredCallbacks = new List<Action>();
        private readonly List<Action> _interactionStartedCallbacks = new List<Action>();
        private readonly List<Action> _interactionEndedCallbacks = new List<Action>();

        private string? _message;
        private InteractionPromptInput _input = InteractionPromptInput.Interact;
        private InteractionPromptState _state = InteractionPromptState.Default;
        private float _range = InteractionPromptContract.NativeMaxInteractionRange;
        private int _priority;
        private bool _limitAngle;
        private float _angleLimit = 90f;
        private Transform? _displayPoint;
        private Collider? _displayCollider;
        private InteractionPrompt? _builtPrompt;

        internal GameObject Target => _target;
        internal string Message => _message!;
        internal InteractionPromptInput Input => _input;
        internal InteractionPromptState State => _state;
        internal float Range => _range;
        internal int Priority => _priority;
        internal bool LimitAngle => _limitAngle;
        internal float AngleLimit => _angleLimit;
        internal Transform? DisplayPoint => _displayPoint;
        internal Collider? DisplayCollider => _displayCollider;
        internal IReadOnlyList<Action> HoveredCallbacks => _hoveredCallbacks;
        internal IReadOnlyList<Action> InteractionStartedCallbacks => _interactionStartedCallbacks;
        internal IReadOnlyList<Action> InteractionEndedCallbacks => _interactionEndedCallbacks;

        internal InteractionPromptBuilder(GameObject target)
        {
            if (ReferenceEquals(target, null) || target == null)
                throw new ArgumentNullException(nameof(target));

            _target = target;
        }

        /// <summary>
        /// Creates a builder for a mod-owned GameObject.
        /// </summary>
        /// <param name="target">The GameObject whose collider should be interactable.</param>
        /// <returns>A new interaction prompt builder.</returns>
        public static InteractionPromptBuilder Create(GameObject target) =>
            new InteractionPromptBuilder(target);

        /// <summary>Sets the text rendered by the native interaction prompt.</summary>
        /// <param name="message">Non-empty player-facing prompt text.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithMessage(string message)
        {
            EnsureMutable();
            _message = InteractionPromptContract.NormalizeMessage(message);
            return this;
        }

        /// <summary>Sets the native input action used to begin the interaction.</summary>
        /// <param name="input">The interaction input action.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithInput(InteractionPromptInput input)
        {
            EnsureMutable();
            InteractionPromptContract.ValidateEnum(input, nameof(input));
            _input = input;
            return this;
        }

        /// <summary>Sets the native prompt state.</summary>
        /// <param name="state">The visual and interaction state.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithState(InteractionPromptState state)
        {
            EnsureMutable();
            InteractionPromptContract.ValidateEnum(state, nameof(state));
            _state = state;
            return this;
        }

        /// <summary>
        /// Sets the maximum distance at which the prompt can be selected.
        /// </summary>
        /// <param name="range">A finite distance between zero and four metres.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithRange(float range)
        {
            EnsureMutable();
            _range = InteractionPromptContract.NormalizeRange(range);
            return this;
        }

        /// <summary>Sets the priority used when native prompts overlap.</summary>
        /// <param name="priority">A higher value takes precedence.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithPriority(int priority)
        {
            EnsureMutable();
            _priority = priority;
            return this;
        }

        /// <summary>
        /// Limits selection to the specified horizontal angle around the target's forward direction.
        /// </summary>
        /// <param name="angleLimit">A finite angle greater than zero and at most 180 degrees.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithAngleLimit(float angleLimit)
        {
            EnsureMutable();
            _angleLimit = InteractionPromptContract.NormalizeAngleLimit(angleLimit);
            _limitAngle = true;
            return this;
        }

        /// <summary>Clears any angle restriction.</summary>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithoutAngleLimit()
        {
            EnsureMutable();
            _limitAngle = false;
            return this;
        }

        /// <summary>
        /// Uses a transform as the native prompt display anchor.
        /// </summary>
        /// <param name="displayPoint">The transform whose position should be used.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithDisplayLocation(Transform displayPoint)
        {
            EnsureMutable();
            if (ReferenceEquals(displayPoint, null) || displayPoint == null)
                throw new ArgumentNullException(nameof(displayPoint));

            _displayPoint = displayPoint;
            _displayCollider = null;
            return this;
        }

        /// <summary>
        /// Uses a collider's closest point to the player as the native prompt display anchor.
        /// </summary>
        /// <param name="displayCollider">The collider used for display positioning.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder WithDisplayLocation(Collider displayCollider)
        {
            EnsureMutable();
            if (ReferenceEquals(displayCollider, null) || displayCollider == null)
                throw new ArgumentNullException(nameof(displayCollider));

            _displayCollider = displayCollider;
            _displayPoint = null;
            return this;
        }

        /// <summary>Registers a callback invoked while this prompt is selected.</summary>
        /// <param name="callback">The callback invoked by the native hover lifecycle.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder OnHovered(Action callback)
        {
            EnsureMutable();
            AddCallback(_hoveredCallbacks, callback, nameof(callback));
            return this;
        }

        /// <summary>Registers a callback invoked when the native interaction starts.</summary>
        /// <param name="callback">The callback invoked by the native interaction lifecycle.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder OnInteractionStarted(Action callback)
        {
            EnsureMutable();
            AddCallback(_interactionStartedCallbacks, callback, nameof(callback));
            return this;
        }

        /// <summary>Registers a callback invoked when the native interaction ends.</summary>
        /// <param name="callback">The callback invoked by the native interaction lifecycle.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public InteractionPromptBuilder OnInteractionEnded(Action callback)
        {
            EnsureMutable();
            AddCallback(_interactionEndedCallbacks, callback, nameof(callback));
            return this;
        }

        /// <summary>
        /// Attaches the native interaction component and returns a managed runtime handle.
        /// </summary>
        /// <returns>The managed interaction prompt handle.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the message is missing, the target already has an interaction component,
        /// or no usable collider exists beneath the target.
        /// </exception>
        public InteractionPrompt Build()
        {
            if (_builtPrompt != null)
                return _builtPrompt;
            if (_message == null)
                throw new InvalidOperationException("WithMessage must be called before Build().");

            _builtPrompt = new InteractionPrompt(this);
            return _builtPrompt;
        }

        private void EnsureMutable()
        {
            if (_builtPrompt != null)
                throw new InvalidOperationException("This interaction prompt builder has already been built.");
        }

        private static void AddCallback(List<Action> callbacks, Action callback, string parameterName)
        {
            if (callback == null)
                throw new ArgumentNullException(parameterName);
            if (!callbacks.Contains(callback))
                callbacks.Add(callback);
        }
    }
}
