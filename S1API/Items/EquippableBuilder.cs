#if (IL2CPPMELON)
using S1Equipping = Il2CppScheduleOne.Equipping;
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Equipping = ScheduleOne.Equipping;
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

using System;
using S1API.Internal.Items;
using S1API.Internal.Utils;
using S1API.Items;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items
{
    /// <summary>
    /// Builder for creating equippable components that can be attached to items.
    /// Use this to create custom equippable behavior for items.
    /// </summary>
    public sealed class EquippableBuilder
    {
        private GameObject _gameObject;
        private S1Equipping.Equippable _equippable;
        private bool _canInteract = true;
        private bool _canPickup = true;
        
        // Viewmodel-specific configuration
        private Vector3? _viewmodelPosition;
        private Vector3? _viewmodelRotation;
        private Vector3? _viewmodelScale;
        private string _avatarEquippableAssetPath;
        private S1AvatarEquipping.AvatarEquippable.EHand _avatarHand = S1AvatarEquipping.AvatarEquippable.EHand.Right;
        private string _avatarAnimationTrigger;
        private readonly System.Collections.Generic.List<Action<ItemInstance>> _useCallbacks = new System.Collections.Generic.List<Action<ItemInstance>>();

        /// <summary>
        /// Creates an equippable GameObject with the specified equippable component type.
        /// </summary>
        /// <typeparam name="T">The type of equippable component to create. Must inherit from the game's Equippable class.</typeparam>
        /// <param name="name">Optional name for the GameObject. If not provided, uses the type name.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder CreateEquippable<T>(string name = null) where T : S1Equipping.Equippable
        {
            string gameObjectName = string.IsNullOrEmpty(name) ? $"Equippable_{typeof(T).Name}" : name;
            _gameObject = new GameObject(gameObjectName);
            _equippable = _gameObject.AddComponent<T>();
            return this;
        }

        /// <summary>
        /// Creates a basic equippable GameObject with the default Equippable component.
        /// </summary>
        /// <param name="name">Optional name for the GameObject.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder CreateBasicEquippable(string name = null)
        {
            string gameObjectName = string.IsNullOrEmpty(name) ? "Equippable_Basic" : name;
            _gameObject = new GameObject(gameObjectName);
            _equippable = _gameObject.AddComponent<S1Equipping.Equippable>();
            return this;
        }

        /// <summary>
        /// Creates a viewmodel equippable GameObject with Equippable_Viewmodel component.
        /// This allows the item to be held in first-person with a 3D model.
        /// </summary>
        /// <param name="name">Optional name for the GameObject.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder CreateViewmodelEquippable(string name = null)
        {
            string gameObjectName = string.IsNullOrEmpty(name) ? "Equippable_Viewmodel" : name;
            _gameObject = new GameObject(gameObjectName);
            
            // Always create regular Equippable_Viewmodel - callback will be handled in Build() if needed
            _equippable = _gameObject.AddComponent<S1Equipping.Equippable_Viewmodel>();
            
            return this;
        }

        /// <summary>
        /// Configures interaction and pickup capabilities when this item is equipped.
        /// </summary>
        /// <param name="canInteract">Whether the player can interact with objects when this is equipped.</param>
        /// <param name="canPickup">Whether the player can pick up items when this is equipped.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder WithInteraction(bool canInteract, bool canPickup)
        {
            _canInteract = canInteract;
            _canPickup = canPickup;
            return this;
        }

        /// <summary>
        /// Configures the first-person viewmodel transform settings.
        /// Only applies to viewmodel equippables created with <see cref="CreateViewmodelEquippable"/>.
        /// </summary>
        /// <param name="position">Local position offset for the viewmodel.</param>
        /// <param name="rotation">Local euler angles for the viewmodel.</param>
        /// <param name="scale">Local scale for the viewmodel (default: Vector3.one).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder WithViewmodelTransform(Vector3 position, Vector3 rotation, Vector3? scale = null)
        {
            _viewmodelPosition = position;
            _viewmodelRotation = rotation;
            _viewmodelScale = scale ?? Vector3.one;
            return this;
        }

        /// <summary>
        /// Configures the third-person avatar equippable animation.
        /// Only applies to viewmodel equippables created with <see cref="CreateViewmodelEquippable"/>.
        /// </summary>
        /// <param name="assetPath">Resources path to the AvatarEquippable prefab (e.g., "Equippables/MyItem").</param>
        /// <param name="hand">Which hand holds the item in third-person (Left or Right).</param>
        /// <param name="animationTrigger">Animation trigger/bool name for third-person animation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder WithAvatarEquippable(string assetPath, AvatarHand hand = AvatarHand.Right, string animationTrigger = "RightArm_Hold_ClosedHand")
        {
            _avatarEquippableAssetPath = assetPath;
            _avatarHand = hand == AvatarHand.Left 
                ? S1AvatarEquipping.AvatarEquippable.EHand.Left 
                : S1AvatarEquipping.AvatarEquippable.EHand.Right;
            _avatarAnimationTrigger = animationTrigger;
            return this;
        }

        /// <summary>
        /// Registers a callback to be invoked when the player uses this item (clicks while holding it).
        /// Only applies to viewmodel equippables created with <see cref="CreateViewmodelEquippable"/>.
        /// The callback receives the ItemInstance being used.
        /// </summary>
        /// <param name="callback">Callback to invoke when the item is used.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public EquippableBuilder WithUseCallback(Action<ItemInstance> callback)
        {
            if (callback != null && !_useCallbacks.Contains(callback))
            {
                _useCallbacks.Add(callback);
            }
            return this;
        }

        /// <summary>
        /// Builds and finalizes the equippable component.
        /// Configures the GameObject to be persistent and inactive (prefab-like state).
        /// </summary>
        /// <returns>A wrapper around the created equippable component.</returns>
        public Equippable Build()
        {
            if (_equippable == null)
            {
                throw new System.InvalidOperationException("Cannot build equippable: No equippable component created. Call CreateEquippable<T>(), CreateBasicEquippable(), or CreateViewmodelEquippable() first.");
            }

            // Apply basic configuration
            _equippable.CanInteractWhenEquipped = _canInteract;
            _equippable.CanPickUpWhenEquipped = _canPickup;

            // Apply viewmodel-specific configuration if this is a viewmodel equippable
            if (_equippable is S1Equipping.Equippable_Viewmodel viewmodelEquippable)
            {
                // If we have use callbacks, replace with the callback component
                if (_useCallbacks != null && _useCallbacks.Count > 0)
                {
                    // Store the viewmodel settings
                    var position = _viewmodelPosition ?? viewmodelEquippable.localPosition;
                    var rotation = _viewmodelRotation ?? viewmodelEquippable.localEulerAngles;
                    var scale = _viewmodelScale ?? viewmodelEquippable.localScale;
                    var avatarEquippable = viewmodelEquippable.AvatarEquippable;
                    
                    // Remove the old component
                    Object.DestroyImmediate(viewmodelEquippable);
                    
                    // Add the callback component - RegisterTypeInIl2Cpp handles Il2Cpp registration
                    var callbackComponent = _gameObject.AddComponent<EquippableUseCallback>();
                    
                    // Transfer all callbacks to the component
                    foreach (var callback in _useCallbacks)
                    {
                        callbackComponent.AddCallback(callback);
                    }
                    
                    callbackComponent.localPosition = position;
                    callbackComponent.localEulerAngles = rotation;
                    callbackComponent.localScale = scale;
                    callbackComponent.AvatarEquippable = avatarEquippable;
                    
                    _equippable = callbackComponent;
                    viewmodelEquippable = callbackComponent;
                }
                
                if (_viewmodelPosition.HasValue)
                {
                    viewmodelEquippable.localPosition = _viewmodelPosition.Value;
                }
                if (_viewmodelRotation.HasValue)
                {
                    viewmodelEquippable.localEulerAngles = _viewmodelRotation.Value;
                }
                if (_viewmodelScale.HasValue)
                {
                    viewmodelEquippable.localScale = _viewmodelScale.Value;
                }

                // Configure AvatarEquippable if provided
                if (!string.IsNullOrEmpty(_avatarEquippableAssetPath))
                {
                    // Create a child GameObject for the AvatarEquippable
                    var avatarEquippableGO = new GameObject("AvatarEquippable");
                    avatarEquippableGO.transform.SetParent(_gameObject.transform);
                    
                    var avatarEquippable = avatarEquippableGO.AddComponent<S1AvatarEquipping.AvatarEquippable>();
                    avatarEquippable.AssetPath = _avatarEquippableAssetPath;
                    avatarEquippable.Hand = _avatarHand;
                    avatarEquippable.AnimationTrigger = _avatarAnimationTrigger;
                    
                    // Create an alignment point if it doesn't exist
                    if (avatarEquippable.AlignmentPoint == null)
                    {
                        var alignmentPoint = new GameObject("AlignmentPoint");
                        alignmentPoint.transform.SetParent(avatarEquippableGO.transform);
                        alignmentPoint.transform.localPosition = Vector3.zero;
                        alignmentPoint.transform.localRotation = Quaternion.identity;
                        avatarEquippable.AlignmentPoint = alignmentPoint.transform;
                    }
                    
                    viewmodelEquippable.AvatarEquippable = avatarEquippable;
                }
            }

            ApplyInteractionSettings(_equippable);

            // Make it persistent across scene loads
            Object.DontDestroyOnLoad(_gameObject);

            // Set inactive (prefab-like state)
            _gameObject.SetActive(false);

            return new Equippable(_equippable);
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game equippable component.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal S1Equipping.Equippable BuildInternal()
        {
            if (_equippable == null)
            {
                throw new System.InvalidOperationException("Cannot build equippable: No equippable component created. Call CreateEquippable<T>() or CreateBasicEquippable() first.");
            }

            ApplyInteractionSettings(_equippable);

            // Make it persistent across scene loads
            Object.DontDestroyOnLoad(_gameObject);

            // Set inactive (prefab-like state)
            _gameObject.SetActive(false);

            return _equippable;
        }

        /// <summary>
        /// INTERNAL: Applies interaction and pickup settings across Mono/Il2Cpp builds.
        /// </summary>
        private void ApplyInteractionSettings(S1Equipping.Equippable equippable)
        {
            if (equippable == null)
                return;

            // Direct assignment for Mono / most Il2Cpp fields
            try { equippable.CanInteractWhenEquipped = _canInteract; } catch { }
            try { equippable.CanPickUpWhenEquipped = _canPickup; } catch { }

            // Fallbacks for builds where fields become properties or have different casing
            ReflectionUtils.TrySetFieldOrProperty(equippable, "CanInteractWhenEquipped", _canInteract);
            ReflectionUtils.TrySetFieldOrProperty(equippable, "canInteractWhenEquipped", _canInteract);
            ReflectionUtils.TrySetFieldOrProperty(equippable, "CanPickUpWhenEquipped", _canPickup);
            ReflectionUtils.TrySetFieldOrProperty(equippable, "canPickUpWhenEquipped", _canPickup);
        }
    }

    /// <summary>
    /// Represents which hand holds an equippable item in third-person view.
    /// </summary>
    public enum AvatarHand
    {
        /// <summary>
        /// Left hand holds the item.
        /// </summary>
        Left,

        /// <summary>
        /// Right hand holds the item (default).
        /// </summary>
        Right
    }
}

