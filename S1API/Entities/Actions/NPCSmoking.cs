#if (IL2CPPMELON)
using S1CoreEquipping = Il2CppScheduleOne.Core.Equipping.Framework;
using S1Other = Il2CppScheduleOne.NPCs.Other;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1CoreEquipping = ScheduleOne.Core.Equipping.Framework;
using S1Other = ScheduleOne.NPCs.Other;
#endif
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Entities.Actions
{
    /// <summary>
    /// Wraps the smoking action for an NPC. Use to start or stop the smoking animation and cigarette visual.
    /// </summary>
    /// <remarks>
    /// Requires the smoking behaviour to be configured on the prefab via <see cref="NPCPrefabBuilder.EnsureSmokeBreak"/>.
    /// If not present, <see cref="Begin"/> and <see cref="End"/> are no-ops.
    /// </remarks>
    public sealed class NPCSmoking
    {
        /// <summary>
        /// INTERNAL: NPC reference.
        /// </summary>
        internal readonly NPC NPC;

        private bool _isActive;

        /// <summary>
        /// INTERNAL: Constructor used for assigning the NPC instance.
        /// </summary>
        /// <param name="npc">NPC instance.</param>
        internal NPCSmoking(NPC npc)
        {
            NPC = npc;
        }

        private S1Other.SmokeCigarette GetComponent()
        {
            return NPC?.S1NPC?.GetComponentInChildren<S1Other.SmokeCigarette>(true);
        }

        private bool EnsureConfigured(S1Other.SmokeCigarette comp)
        {
            if (NPC?.S1NPC == null)
                return false;

#if (IL2CPPMELON)
            comp._npc = NPC.S1NPC;
            if (comp._cigarette == null)
                comp._cigarette = ResolveCigaretteData();

            return comp._npc != null && comp._cigarette != null;
#else
            if (ReflectionUtils.TryGetFieldOrProperty(comp, "_npc") == null)
                ReflectionUtils.TrySetFieldOrProperty(comp, "_npc", NPC.S1NPC);

            if (ReflectionUtils.TryGetFieldOrProperty(comp, "_cigarette") == null)
            {
                var cigaretteData = ResolveCigaretteData();
                if (cigaretteData != null)
                    ReflectionUtils.TrySetFieldOrProperty(comp, "_cigarette", cigaretteData);
            }

            return ReflectionUtils.TryGetFieldOrProperty(comp, "_npc") != null
                && ReflectionUtils.TryGetFieldOrProperty(comp, "_cigarette") != null;
#endif
        }

        private static S1CoreEquipping.EquippableData ResolveCigaretteData()
        {
            return Resources.Load<S1CoreEquipping.EquippableData>("equippables/cigarette/Cigarette");
        }

        /// <summary>
        /// Whether the smoking action is currently active (cigarette visible, animation playing).
        /// </summary>
        public bool IsActive => _isActive;

        private void SetActive(bool value) => _isActive = value;

        /// <summary>
        /// Begins the smoking action: shows the cigarette and plays the smoking animation.
        /// </summary>
        /// <remarks>
        /// No-op if the smoking component is not on the prefab. Call <see cref="NPCPrefabBuilder.EnsureSmokeBreak"/> during ConfigurePrefab to add it.
        /// </remarks>
        public void Begin()
        {
            var comp = GetComponent();
            if (comp != null && EnsureConfigured(comp))
            {
                comp.Begin();
                SetActive(true);
            }
        }

        /// <summary>
        /// Ends the smoking action: removes the cigarette and stops the animation.
        /// </summary>
        public void End()
        {
            var comp = GetComponent();
            if (comp != null && _isActive)
            {
                comp.End();
                SetActive(false);
            }
        }
    }
}
