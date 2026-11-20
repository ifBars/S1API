using System;
using S1API.Internal.Entities;

namespace S1API.Entities
{
    public abstract partial class NPC
    {
        private Action<NpcImpact>? _impactHandlers;

        /// <summary>
        /// Called after this NPC processed a hit, on every peer that runs the hit.
        /// </summary>
        /// <remarks>
        /// Raised only for hits the game acts on: not for an NPC that ignores combat impacts and not for a hit
        /// it already processed. A handler that throws is logged and the other handlers still run.
        /// </remarks>
        public event Action<NpcImpact> OnImpact
        {
            add => _impactHandlers += value;
            remove => _impactHandlers -= value;
        }

        /// <summary>
        /// Called for every NPC just before the game initializes it, before the first frame it can be seen.
        /// </summary>
        /// <remarks>
        /// Change <see cref="NpcInitializingArgs.Mugshot"/> to replace the mugshot. The NPC has no S1API wrapper yet at
        /// this point, so the arguments carry the id. A handler that throws is logged and the other handlers still run.
        /// </remarks>
        public static event Action<NpcInitializingArgs>? OnInitializing;

        internal void RaiseImpact(NpcImpact impact)
        {
            NpcAttackEvents.Info($"{ID} impact {impact.Type} damage {impact.Damage}.");
            NpcAttackEvents.Invoke(_impactHandlers, $"OnImpact of {ID}", h => ((Action<NpcImpact>)h)(impact));
        }

        internal static void RaiseInitializing(NpcInitializingArgs args)
        {
            NpcAttackEvents.Info($"{args.Id} initializing.");
            NpcAttackEvents.Invoke(OnInitializing, $"OnInitializing of {args.Id}", h => ((Action<NpcInitializingArgs>)h)(args));
        }
    }
}
