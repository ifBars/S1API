#if (IL2CPPMELON)
using S1Other = Il2CppScheduleOne.NPCs.Other;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Other = ScheduleOne.NPCs.Other;
#endif

namespace S1API.Entities.Actions
{
    /// <summary>
    /// Wraps the generic item holding action for an NPC. Use to equip or unequip any equippable item.
    /// </summary>
    /// <remarks>
    /// Requires the item holding component to be configured on the prefab via <see cref="NPCPrefabBuilder.EnsureItemHolding"/>.
    /// The equippable asset path is set during prefab configuration. If the component is not present,
    /// <see cref="Begin"/> and <see cref="End"/> are no-ops.
    /// </remarks>
    public sealed class NPCItemHolding
    {
        /// <summary>
        /// INTERNAL: NPC reference.
        /// </summary>
        internal readonly NPC NPC;

        /// <summary>
        /// INTERNAL: Constructor used for assigning the NPC instance.
        /// </summary>
        /// <param name="npc">NPC instance.</param>
        internal NPCItemHolding(NPC npc)
        {
            NPC = npc;
        }

        private S1Other.HoldItem GetComponent()
        {
            return NPC?.S1NPC?.GetComponentInChildren<S1Other.HoldItem>(true);
        }

        /// <summary>
        /// Whether the item holding action is currently active (item equipped).
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
        /// Begins holding the configured item: equips the item specified in the prefab configuration.
        /// </summary>
        /// <remarks>
        /// No-op if the item holding component is not on the prefab. Call <see cref="NPCPrefabBuilder.EnsureItemHolding"/> during ConfigurePrefab to add it.
        /// </remarks>
        public void Begin()
        {
            var comp = GetComponent();
            if (comp != null)
                comp.Begin();
        }

        /// <summary>
        /// Ends holding the item: unequips and clears the held item.
        /// </summary>
        public void End()
        {
            var comp = GetComponent();
            if (comp != null)
                comp.End();
        }
    }
}
