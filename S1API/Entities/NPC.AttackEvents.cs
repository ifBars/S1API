#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif (MONOMELON)
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using S1API.Internal.Entities;

namespace S1API.Entities
{
    public abstract partial class NPC
    {
        private Action<Player?>? _attackedByPlayerHandlers;
        private Action? _knockedOutHandlers;

        /// <summary>
        /// Called when the game notes that a player attacked this NPC, on every peer that runs the hit.
        /// The player is null when S1API has no wrapper for the native player.
        /// </summary>
        /// <remarks>A handler that throws is logged and the other handlers still run.</remarks>
        public event Action<Player?> OnAttackedByPlayer
        {
            add => _attackedByPlayerHandlers += value;
            remove => _attackedByPlayerHandlers -= value;
        }

        /// <summary>
        /// Called when this NPC is knocked out. Not called for an invincible NPC, which the game does not knock out.
        /// </summary>
        /// <remarks>A handler that throws is logged and the other handlers still run.</remarks>
        public event Action OnKnockedOut
        {
            add => _knockedOutHandlers += value;
            remove => _knockedOutHandlers -= value;
        }

        /// <summary>
        /// The player who attacked this NPC last, or null when no player attacked it since it spawned.
        /// </summary>
        public Player? LastAttacker { get; private set; }

        /// <summary>
        /// Called when a player attacks any NPC, with the NPC and the player (null without a wrapper).
        /// </summary>
        /// <remarks>For mods that watch every NPC. A handler that throws is logged and the other handlers still run.</remarks>
        public static event Action<NPC, Player?>? OnAnyAttackedByPlayer;

        /// <summary>
        /// Called when any NPC is knocked out.
        /// </summary>
        /// <remarks>For mods that watch every NPC. A handler that throws is logged and the other handlers still run.</remarks>
        public static event Action<NPC>? OnAnyKnockedOut;

        internal void RaiseAttackedByPlayer(S1PlayerScripts.Player? nativePlayer)
        {
            var player = ReferenceEquals(nativePlayer, null) ? null : ResolvePlayer(nativePlayer);
            LastAttacker = player;
            NpcAttackEvents.Info($"{ID} attacked by {(player != null ? player.Name : "a player without a wrapper")}.");
            NpcAttackEvents.Invoke(_attackedByPlayerHandlers, $"OnAttackedByPlayer of {ID}", h => ((Action<Player?>)h)(player));
            NpcAttackEvents.Invoke(OnAnyAttackedByPlayer, "OnAnyAttackedByPlayer", h => ((Action<NPC, Player?>)h)(this, player));
        }

        internal void RaiseKnockedOut()
        {
            NpcAttackEvents.Info($"{ID} knocked out.");
            NpcAttackEvents.Invoke(_knockedOutHandlers, $"OnKnockedOut of {ID}", h => ((Action)h)());
            NpcAttackEvents.Invoke(OnAnyKnockedOut, "OnAnyKnockedOut", h => ((Action<NPC>)h)(this));
        }
    }
}
