#if IL2CPPMELON
using S1Interaction = Il2CppScheduleOne.Interaction;
#elif MONOMELON
using S1Interaction = ScheduleOne.Interaction;
#endif

using System;
using S1API.Utils;
using UnityEngine;

namespace S1API.Internal.Interaction
{
    internal sealed class InteractionPromptRuntime : IDisposable
    {
        private readonly global::S1API.Interaction.InteractionPrompt _owner;
        private readonly S1Interaction.InteractableObject _interactable;
        private readonly Action _hoveredHandler;
        private readonly Action _interactionStartedHandler;
        private readonly Action _interactionEndedHandler;
        private bool _disposed;

        internal GameObject Target { get; }

        internal bool IsAttached =>
            !_disposed && _interactable != null && Target != null;

        internal InteractionPromptRuntime(
            global::S1API.Interaction.InteractionPrompt owner,
            GameObject target,
            global::S1API.Interaction.InteractionPromptInput input,
            global::S1API.Interaction.InteractionPromptState state,
            string message,
            float range,
            int priority,
            bool limitAngle,
            float angleLimit,
            Transform? displayPoint,
            Collider? displayCollider)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            global::S1API.Interaction.InteractionPromptContract.ValidateTarget(target);
            if (target.GetComponent<S1Interaction.InteractableObject>() != null)
            {
                throw new InvalidOperationException(
                    $"GameObject '{target.name}' already contains an InteractableObject component.");
            }

            Target = target;
            _owner = owner;
            S1Interaction.InteractableObject? interactable = null;
            try
            {
                interactable = target.AddComponent<S1Interaction.InteractableObject>();
                _interactable = interactable;
                _hoveredHandler = _owner.RaiseHovered;
                _interactionStartedHandler = _owner.RaiseInteractionStarted;
                _interactionEndedHandler = _owner.RaiseInteractionEnded;
                ApplyInput(input);
                ApplyState(state);
                _interactable.SetMessage(message);
                _interactable.MaxInteractionRange = range;
                _interactable.Priority = priority;
                _interactable.LimitInteractionAngle = limitAngle;
                _interactable.AngleLimit = angleLimit;
                ApplyDisplayLocation(displayPoint, displayCollider);
                ValidateColliderComposition(interactable);

                EventHelper.AddListener(_hoveredHandler, _interactable.onHovered);
                EventHelper.AddListener(_interactionStartedHandler, _interactable.onInteractStart);
                EventHelper.AddListener(_interactionEndedHandler, _interactable.onInteractEnd);
            }
            catch
            {
                if (interactable != null)
                    UnityEngine.Object.Destroy(interactable);
                throw;
            }
        }

        internal void SetMessage(string message) => _interactable.SetMessage(message);

        internal void SetInput(global::S1API.Interaction.InteractionPromptInput input) => ApplyInput(input);

        internal void SetState(global::S1API.Interaction.InteractionPromptState state) => ApplyState(state);

        internal void SetRange(float range) => _interactable.MaxInteractionRange = range;

        internal void SetPriority(int priority) => _interactable.Priority = priority;

        internal void SetAngleLimit(float angleLimit)
        {
            _interactable.AngleLimit = angleLimit;
            _interactable.LimitInteractionAngle = true;
        }

        internal void ClearAngleLimit() => _interactable.LimitInteractionAngle = false;

        internal void SetDisplayLocation(Transform displayPoint) =>
            ApplyDisplayLocation(displayPoint, null);

        internal void SetDisplayLocation(Collider displayCollider) =>
            ApplyDisplayLocation(null, displayCollider);

        internal void ClearDisplayLocation() =>
            ApplyDisplayLocation(null, null);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_interactable != null)
            {
                EventHelper.RemoveListener(_hoveredHandler, _interactable.onHovered);
                EventHelper.RemoveListener(_interactionStartedHandler, _interactable.onInteractStart);
                EventHelper.RemoveListener(_interactionEndedHandler, _interactable.onInteractEnd);
                UnityEngine.Object.Destroy(_interactable);
            }
        }

        private void ApplyInput(global::S1API.Interaction.InteractionPromptInput input)
        {
            S1Interaction.InteractableObject.EInteractionType nativeInput =
                input == global::S1API.Interaction.InteractionPromptInput.PrimaryClick
                    ? S1Interaction.InteractableObject.EInteractionType.LeftMouse_Click
                    : S1Interaction.InteractableObject.EInteractionType.Key_Press;
            _interactable.SetInteractionType(nativeInput);
        }

        private void ApplyState(global::S1API.Interaction.InteractionPromptState state)
        {
            S1Interaction.InteractableObject.EInteractableState nativeState =
                (S1Interaction.InteractableObject.EInteractableState)(int)state;
            _interactable.SetInteractableState(nativeState);
        }

        private void ApplyDisplayLocation(Transform? displayPoint, Collider? displayCollider)
        {
            if (displayCollider != null)
            {
                if (!global::S1API.Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(
                        _interactable,
                        "displayLocationCollider",
                        displayCollider))
                {
                    throw new InvalidOperationException(
                        "The native interaction component does not expose displayLocationCollider.");
                }

                _interactable.displayLocationPoint = null;
                return;
            }

            if (!global::S1API.Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(
                    _interactable,
                    "displayLocationCollider",
                    null))
            {
                throw new InvalidOperationException(
                    "The native interaction component does not expose displayLocationCollider.");
            }

            _interactable.displayLocationPoint = displayPoint;
        }

        private void ValidateColliderComposition(S1Interaction.InteractableObject interactable)
        {
            Collider[] colliders = Target.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && collider.GetComponentInParent<S1Interaction.InteractableObject>() == interactable)
                    return;
            }

            throw new InvalidOperationException(
                $"GameObject '{Target.name}' and its children do not contain a collider usable by the interaction manager.");
        }
    }
}
