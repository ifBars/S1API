#if (IL2CPPMELON)
using S1Equipping = Il2CppScheduleOne.Equipping;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1GameInput = Il2CppScheduleOne.GameInput;
using S1DevUtils = Il2CppScheduleOne.DevUtilities;
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Equipping = ScheduleOne.Equipping;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1GameInput = ScheduleOne.GameInput;
using S1DevUtils = ScheduleOne.DevUtilities;
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
#endif

using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.Items;
using UnityEngine;
using UnityEngine.Events;

namespace S1API.Internal.Items
{
    /// <summary>
    /// INTERNAL: Simple MonoBehaviour that extends Equippable_Viewmodel and handles input detection for use callbacks.
    /// Only created when WithUseCallback() is called on the builder.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal class EquippableUseCallback : S1Equipping.Equippable_Viewmodel
    {
        /// <summary>
        /// UnityEvent that fires when the item is used. Uses non-generic UnityEvent for Il2Cpp compatibility.
        /// The ItemInstance is automatically passed to registered callbacks.
        /// </summary>
        public UnityEvent OnUse = new UnityEvent();

        /// <summary>
        /// INTERNAL: List of callbacks to invoke when the item is used.
        /// </summary>
        private readonly List<Action<ItemInstance>> _callbacks = new List<Action<ItemInstance>>();

        /// <summary>
        /// INTERNAL: Adds a callback to be invoked when the item is used.
        /// </summary>
#if !MONOMELON
        [HideFromIl2Cpp]
#endif
        internal void AddCallback(Action<ItemInstance> callback)
        {
            if (callback != null && !_callbacks.Contains(callback))
            {
                _callbacks.Add(callback);
            }
        }

#if IL2CPPMELON
        /// <summary>
        /// IL2CPP constructor required for RegisterTypeInIl2Cpp
        /// </summary>
        public EquippableUseCallback(IntPtr ptr) : base(ptr) { }

        /// <summary>
        /// Mono-side constructor for instantiation from managed code
        /// </summary>
        public EquippableUseCallback() : base(ClassInjector.DerivedConstructorPointer<EquippableUseCallback>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
#endif

#if MONOMELON
        protected override void Update()
#else
        public override void Update()
#endif
        {
            base.Update();

            // Check for primary click input with proper conditions
            if (OnUse != null && 
                S1GameInput.GetButtonDown(S1GameInput.ButtonCode.PrimaryClick) &&
                !S1GameInput.IsTyping &&
                S1DevUtils.PlayerSingleton<S1PlayerScripts.PlayerCamera>.Instance.activeUIElementCount == 0)
            {
                // Invoke the callback with the current item instance wrapped
                if (itemInstance != null)
                {
                    try
                    {
                        var wrappedInstance = new ItemInstance(itemInstance);
                        
                        // Invoke all registered callbacks
                        if (_callbacks != null)
                        {
                            foreach (var callback in _callbacks)
                            {
                                try
                                {
                                    callback?.Invoke(wrappedInstance);
                                }
                                catch (Exception ex)
                                {
                                    MelonLoader.MelonLogger.Error($"[EquippableUseCallback] Exception in callback: {ex.Message}");
                                    MelonLoader.MelonLogger.Error(ex.StackTrace);
                                }
                            }
                        }
                        
                        // Also invoke the UnityEvent (for any non-managed listeners)
                        OnUse?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MelonLoader.MelonLogger.Error($"[EquippableUseCallback] Exception in use callback: {ex.Message}");
                        MelonLoader.MelonLogger.Error(ex.StackTrace);
                    }
                }
            }
        }
    }
}

