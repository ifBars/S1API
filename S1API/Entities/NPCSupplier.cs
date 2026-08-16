#if IL2CPPMELON
using Il2CppInterop.Runtime;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Messaging = Il2CppScheduleOne.Messaging;
using NativeAction = Il2CppSystem.Action;
#elif MONOMELON
using S1Economy = ScheduleOne.Economy;
using S1Messaging = ScheduleOne.Messaging;
using NativeAction = System.Action;
#endif

using System;
using System.Collections.Generic;
using S1API.Deliveries;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Shops;
using S1API.Storages;

namespace S1API.Entities
{
    /// <summary>
    /// Represents the current native state of a supplier NPC.
    /// </summary>
    public enum SupplierStatus
    {
        /// <summary>The supplier is not preparing a drop or attending a meeting.</summary>
        Idle,

        /// <summary>The supplier is preparing a dead-drop order.</summary>
        PreparingDeadDrop,

        /// <summary>The supplier is attending a meeting with the player.</summary>
        Meeting
    }

    /// <summary>
    /// Provides safe runtime access to native supplier behavior for an <see cref="NPC"/>.
    /// </summary>
    public sealed class NPCSupplier
    {
        private static readonly Log Logger = new Log("NPCSupplier");
        private readonly NPC npc;
        private Action? deadDropReadyHandlers;
        private S1Economy.Supplier? subscribedSupplier;
        private NativeAction? nativeDeadDropReadyDispatcher;

        internal NPCSupplier(NPC npc)
        {
            this.npc = npc ?? throw new ArgumentNullException(nameof(npc));
        }

        internal S1Economy.Supplier? Component
        {
            get
            {
                if (CrossType.Is(npc.S1NPC, out S1Economy.Supplier supplier))
                    return supplier;

                return npc.gameObject != null
                    ? npc.gameObject.GetComponent<S1Economy.Supplier>()
                    : null;
            }
        }

        /// <summary>
        /// Gets whether the wrapped NPC currently has a native supplier root.
        /// </summary>
        public bool IsSupplier => Component != null;

        /// <summary>
        /// Gets the supplier's current activity state.
        /// </summary>
        public SupplierStatus Status => Component == null
            ? SupplierStatus.Idle
            : (SupplierStatus)(int)Component.Status;

        /// <summary>
        /// Gets whether delivery orders have been unlocked for this supplier.
        /// </summary>
        public bool DeliveriesEnabled => Component?.DeliveriesEnabled ?? false;

        /// <summary>
        /// Gets the player's current debt to this supplier.
        /// </summary>
        public float Debt => Component?.Debt ?? 0f;

        /// <summary>
        /// Gets the number of in-game minutes until the active dead drop is ready, or -1 when none is pending.
        /// </summary>
        public int MinutesUntilDeadDropReady => Component?.MinsUntilDeaddropReady ?? -1;

        /// <summary>
        /// Gets the relationship-scaled dead-drop spending limit.
        /// </summary>
        public float DeadDropLimit => Component?.GetDeadDropLimit() ?? 0f;

        /// <summary>
        /// Gets the supplier's wrapped shop, or <see langword="null"/> until its runtime infrastructure is ready.
        /// </summary>
        public Shop? Shop
        {
            get
            {
                S1Economy.Supplier? supplier = Component;
                return supplier?.Shop == null ? null : new Shop(supplier.Shop);
            }
        }

        /// <summary>
        /// Gets the supplier's wrapped stash storage, or <see langword="null"/> until it is ready.
        /// </summary>
        public StorageInstance? Stash
        {
            get
            {
                S1Economy.Supplier? supplier = Component;
                return supplier?.Stash?.Storage == null
                    ? null
                    : new StorageInstance(supplier.Stash.Storage);
            }
        }

        /// <summary>
        /// Gets a read-only collection snapshot of live delivery wrappers for this supplier.
        /// </summary>
        public IReadOnlyList<Delivery> ActiveDeliveries
        {
            get
            {
                Shop? shop = Shop;
                return shop == null
                    ? Array.Empty<Delivery>()
                    : DeliveryRegistry.GetForShop(shop);
            }
        }

        /// <summary>
        /// Ensures supplier-specific messaging state is initialized for the wrapped NPC.
        /// </summary>
        /// <remarks>
        /// The native supplier root must be declared by overriding <see cref="NPC.IsSupplier"/>.
        /// This method does not replace an already spawned root component.
        /// </remarks>
        internal void EnsureSupplier()
        {
            if (Component == null)
            {
                Logger.Warning($"Supplier root not present for NPC '{npc.ID}'. Override NPC.IsSupplier to return true.");
                return;
            }

            // Native suppliers keep a conversation object for persistence and
            // networking, but do not add it to Messages until SupplierUnlocked.
            npc.SetConversationCategory(
                S1Messaging.EConversationCategory.Supplier,
                ensureUi: false);
            npc.EnsureMessageConversationReady(resetDefaults: false);
            EnsureDeadDropReadyDispatcher();
        }

        /// <summary>
        /// Unlocks this supplier through the native networked supplier flow.
        /// </summary>
        /// <remarks>
        /// This is a client-safe request; the native supplier RPC performs the authoritative change.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when the wrapped NPC is not a supplier.</exception>
        public void Unlock()
        {
            S1Economy.Supplier? supplier = Component;
            if (supplier == null)
                throw new InvalidOperationException("The wrapped NPC is not a supplier.");

            supplier.SendUnlocked();
        }

        /// <summary>
        /// Ends the supplier's active meeting, if one is in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the wrapped NPC is not a supplier or the caller is not the server/host.
        /// </exception>
        public void EndMeeting()
        {
            S1Economy.Supplier? supplier = Component;
            if (supplier == null)
                throw new InvalidOperationException("The wrapped NPC is not a supplier.");
            if (!supplier.IsServerInitialized)
            {
                throw new InvalidOperationException(
                    "Supplier meetings can only be ended by the server or host.");
            }

            if (supplier.Status == S1Economy.Supplier.ESupplierStatus.Meeting)
                supplier.EndMeeting();
        }

        /// <summary>
        /// Raised when the supplier's dead-drop contents become ready.
        /// </summary>
        public event Action OnDeadDropReady
        {
            add
            {
                if (value == null)
                    return;

                deadDropReadyHandlers += value;
                EnsureDeadDropReadyDispatcher();
            }
            remove
            {
                deadDropReadyHandlers -= value;
                if (deadDropReadyHandlers == null && subscribedSupplier != null)
                {
                    if (nativeDeadDropReadyDispatcher != null)
                        subscribedSupplier.OnDeaddropReady -= nativeDeadDropReadyDispatcher;
                    subscribedSupplier = null;
                }
            }
        }

        private void EnsureDeadDropReadyDispatcher()
        {
            S1Economy.Supplier? supplier = Component;
            if (supplier == null || supplier == subscribedSupplier)
                return;

            NativeAction dispatcher = GetOrCreateNativeDeadDropReadyDispatcher();
            if (subscribedSupplier != null)
                subscribedSupplier.OnDeaddropReady -= dispatcher;

            subscribedSupplier = supplier;
            subscribedSupplier.OnDeaddropReady += dispatcher;
        }

        private NativeAction GetOrCreateNativeDeadDropReadyDispatcher()
        {
            if (nativeDeadDropReadyDispatcher != null)
                return nativeDeadDropReadyDispatcher;

#if IL2CPPMELON
            NativeAction dispatcher = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(
                new Action(DispatchDeadDropReady))
                ?? throw new InvalidOperationException("Could not create the native dead-drop event dispatcher.");
#else
            NativeAction dispatcher = DispatchDeadDropReady;
#endif
            nativeDeadDropReadyDispatcher = dispatcher;
            return dispatcher;
        }

        private void DispatchDeadDropReady()
        {
            Action? handlers = deadDropReadyHandlers;
            if (handlers == null)
                return;

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Logger.Warning($"An NPCSupplier.OnDeadDropReady subscriber failed: {ex.Message}");
                }
            }
        }
    }
}
