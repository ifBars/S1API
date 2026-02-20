#if (IL2CPPMELON)
using S1Other = Il2CppScheduleOne.NPCs.Other;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Other = ScheduleOne.NPCs.Other;
#endif

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
            if (comp != null)
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
            if (comp != null)
            {
                comp.End();
                SetActive(false);
            }
        }
    }
}
