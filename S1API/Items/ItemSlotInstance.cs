#if (IL2CPPMELON)
using Il2CppInterop.Runtime;
using NativeSlotAction = Il2CppSystem.Action;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX)
using NativeSlotAction = System.Action;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Product = ScheduleOne.Product;
#endif

using System;
using S1API.Internal.Utils;
using S1API.Money;
using S1API.Products;

namespace S1API.Items
{
    /// <summary>
    /// Represents an item slot within the game.
    /// These are present within storage, the hot bar, etc.
    /// </summary>
    /// <remarks>
    /// Lock state and events reflect native state delivered to the current peer. They do not grant
    /// authority or guarantee network callback ordering. Events belong to this wrapper: retain it
    /// to unsubscribe before discarding it, and subscribe again when a save reload replaces the slot.
    /// </remarks>
    public class ItemSlotInstance
    {
        /// <summary>
        /// INTERNAL: The reference to the item slot in the game.
        /// </summary>
        internal readonly S1ItemFramework.ItemSlot S1ItemSlot;

        private ManagedEventRegistrationTracker<NativeSlotAction>? _lockedHandlers;
        private ManagedEventRegistrationTracker<NativeSlotAction>? _unlockedHandlers;
        private ManagedEventRegistrationTracker<NativeSlotAction>? _itemInstanceChangedHandlers;
        private ManagedEventRegistrationTracker<NativeSlotAction>? _itemDataChangedHandlers;
        private ManagedEventRegistrationTracker<NativeSlotAction>? _filterChangedHandlers;

        /// <summary>
        /// Creates an item slot instance from the in game slot.
        /// </summary>
        /// <param name="itemSlot"></param>
        internal ItemSlotInstance(S1ItemFramework.ItemSlot itemSlot) =>
            S1ItemSlot = itemSlot;

        /// <summary>
        /// The quantity of item in this slot.
        /// </summary>
        public int Quantity =>
            S1ItemSlot.Quantity;

        /// <summary>
        /// Gets whether the native slot has an active lock on the current peer.
        /// </summary>
        public bool IsLocked =>
            S1ItemSlot.IsLocked;

        /// <summary>
        /// Gets the native slot's independent flag that prevents adding items.
        /// </summary>
        public bool IsAddLocked =>
            S1ItemSlot.IsAddLocked;

        /// <summary>
        /// Gets the native slot's independent flag that prevents removing items.
        /// </summary>
        public bool IsRemovalLocked =>
            S1ItemSlot.IsRemovalLocked;

        /// <summary>
        /// Gets a managed snapshot of the active lock reason, or <c>null</c> when no lock is active.
        /// </summary>
        /// <remarks>
        /// The snapshot contains no native owner and does not change when the slot's lock changes.
        /// </remarks>
        public ItemSlotLockInfo? ActiveLock
        {
            get
            {
                S1ItemFramework.ItemSlotLock? activeLock = S1ItemSlot.ActiveLock;
                return activeLock == null ? null : new ItemSlotLockInfo(activeLock.LockReason);
            }
        }

        /// <summary>
        /// Occurs when the native slot reports that a lock was applied.
        /// </summary>
        /// <remarks>
        /// Independent changes to <see cref="IsAddLocked"/> or <see cref="IsRemovalLocked"/> do not raise this event.
        /// </remarks>
        public event Action Locked
        {
            add => S1ItemSlot.onLocked = AddHandler(S1ItemSlot.onLocked, value, ref _lockedHandlers);
            remove => S1ItemSlot.onLocked = RemoveHandler(S1ItemSlot.onLocked, value, _lockedHandlers);
        }

        /// <summary>
        /// Occurs when the native slot reports that its active lock was removed.
        /// </summary>
        public event Action Unlocked
        {
            add => S1ItemSlot.onUnlocked = AddHandler(S1ItemSlot.onUnlocked, value, ref _unlockedHandlers);
            remove => S1ItemSlot.onUnlocked = RemoveHandler(S1ItemSlot.onUnlocked, value, _unlockedHandlers);
        }

        /// <summary>
        /// Occurs when the native slot reports an item-instance change.
        /// </summary>
        public event Action ItemInstanceChanged
        {
            add => S1ItemSlot.onItemInstanceChanged = AddHandler(S1ItemSlot.onItemInstanceChanged, value, ref _itemInstanceChangedHandlers);
            remove => S1ItemSlot.onItemInstanceChanged = RemoveHandler(S1ItemSlot.onItemInstanceChanged, value, _itemInstanceChangedHandlers);
        }

        /// <summary>
        /// Occurs when the native slot reports a change to its item data.
        /// </summary>
        public event Action ItemDataChanged
        {
            add => S1ItemSlot.onItemDataChanged = AddHandler(S1ItemSlot.onItemDataChanged, value, ref _itemDataChangedHandlers);
            remove => S1ItemSlot.onItemDataChanged = RemoveHandler(S1ItemSlot.onItemDataChanged, value, _itemDataChangedHandlers);
        }

        /// <summary>
        /// Occurs when the native slot reports a player-filter change.
        /// </summary>
        public event Action FilterChanged
        {
            add => S1ItemSlot.onFilterChange = AddHandler(S1ItemSlot.onFilterChange, value, ref _filterChangedHandlers);
            remove => S1ItemSlot.onFilterChange = RemoveHandler(S1ItemSlot.onFilterChange, value, _filterChangedHandlers);
        }

        /// <summary>
        /// The item instance the slot contains.
        /// </summary>
        public ItemInstance? ItemInstance
        {
            get
            {
                if (CrossType.Is(S1ItemSlot.ItemInstance,
                        out S1Product.ProductItemInstance productItemInstance))
                    return new ProductInstance(productItemInstance);

                if (CrossType.Is(S1ItemSlot.ItemInstance,
                        out S1ItemFramework.CashInstance cashInstance))
                    return new CashInstance(cashInstance);

                if (CrossType.Is(S1ItemSlot.ItemInstance,
                        out S1ItemFramework.ItemInstance itemInstance))
                    return new ItemInstance(itemInstance);

                return null;
            }
        }

        /// <summary>
        /// Adds a quantity to the item in this slot.
        /// NOTE: Negative numbers are supported and allowed.
        /// </summary>
        /// <param name="amount"></param>
        public void AddQuantity(int amount) =>
            S1ItemSlot.ChangeQuantity(amount);

        private static NativeSlotAction? AddHandler(
            NativeSlotAction? current,
            Action? handler,
            ref ManagedEventRegistrationTracker<NativeSlotAction>? registrations)
        {
            if (handler == null)
                return current;

            // A distinct adapter keeps removal from touching an identical native listener.
            Action adapter = () => handler();
#if IL2CPPMELON
            NativeSlotAction nativeHandler = DelegateSupport.ConvertDelegate<NativeSlotAction>(adapter)
                ?? throw new InvalidOperationException("Could not create the native item-slot delegate.");
            NativeSlotAction combined = current == null
                ? nativeHandler
                : Il2CppSystem.Delegate.Combine(current, nativeHandler).Cast<NativeSlotAction>();
#else
            NativeSlotAction? combined = current + adapter;
            NativeSlotAction nativeHandler = adapter;
#endif
            (registrations ??= new ManagedEventRegistrationTracker<NativeSlotAction>()).Add(handler, nativeHandler);
            return combined;
        }

        private static NativeSlotAction? RemoveHandler(
            NativeSlotAction? current,
            Action? handler,
            ManagedEventRegistrationTracker<NativeSlotAction>? registrations)
        {
            if (handler == null || registrations == null
                || !registrations.TryTakeLast(handler, out NativeSlotAction nativeHandler))
                return current;

#if IL2CPPMELON
            return Il2CppSystem.Delegate.Remove(current, nativeHandler)?.Cast<NativeSlotAction>();
#else
            return current - nativeHandler;
#endif
        }
    }
}
