#if (IL2CPPMELON)
using S1Other = Il2CppScheduleOne.NPCs.Other;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Other = ScheduleOne.NPCs.Other;
#endif

namespace S1API.Entities.Actions
{
    /// <summary>
    /// Wraps the drinking action for an NPC. Use to equip a drink and play the drinking animation.
    /// </summary>
    /// <remarks>
    /// Requires the drinking component to be configured on the prefab via <see cref="NPCPrefabBuilder.EnsureDrinking"/>.
    /// If not present, <see cref="Begin"/> and <see cref="End"/> are no-ops.
    /// </remarks>
    public sealed class NPCDrinking
    {
        /// <summary>
        /// INTERNAL: NPC reference.
        /// </summary>
        internal readonly NPC NPC;

        /// <summary>
        /// INTERNAL: Constructor used for assigning the NPC instance.
        /// </summary>
        /// <param name="npc">NPC instance.</param>
        internal NPCDrinking(NPC npc)
        {
            NPC = npc;
        }

        private S1Other.DrinkItem GetComponent()
        {
            return NPC?.S1NPC?.GetComponentInChildren<S1Other.DrinkItem>(true);
        }

        /// <summary>
        /// Whether the drinking action is currently active (drink equipped, animation playing).
        /// </summary>
        public bool IsActive
        {
            get
            {
                var comp = GetComponent();
                return comp != null && comp.active;
            }
        }

        /// <summary>
        /// Begins the drinking action: equips the drink and plays the drinking animation.
        /// </summary>
        /// <remarks>
        /// No-op if the drinking component is not on the prefab. Call <see cref="NPCPrefabBuilder.EnsureDrinking"/> during ConfigurePrefab to add it.
        /// </remarks>
        public void Begin()
        {
            var comp = GetComponent();
            if (comp != null)
                comp.Begin();
        }

        /// <summary>
        /// Ends the drinking action: unequips the drink and stops the animation.
        /// </summary>
        public void End()
        {
            var comp = GetComponent();
            if (comp != null)
                comp.End();
        }
    }
}
