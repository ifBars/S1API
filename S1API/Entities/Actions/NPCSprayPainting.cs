#if (IL2CPPMELON)
using S1Other = Il2CppScheduleOne.NPCs.Other;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Other = ScheduleOne.NPCs.Other;
#endif

using UnityEngine;

namespace S1API.Entities.Actions
{
    /// <summary>
    /// Wraps the spray painting action for an NPC. Use to equip the spray can, toggle spray effects, and control the animation.
    /// </summary>
    /// <remarks>
    /// Requires the spray paint component to be configured on the prefab via <see cref="NPCPrefabBuilder.EnsureGraffiti"/>.
    /// If not present, <see cref="Begin"/>, <see cref="End"/>, and <see cref="SetEffect"/> are no-ops.
    /// </remarks>
    public sealed class NPCSprayPainting
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
        internal NPCSprayPainting(NPC npc)
        {
            NPC = npc;
        }

        private S1Other.SprayPaint GetComponent()
        {
            return NPC?.S1NPC?.GetComponentInChildren<S1Other.SprayPaint>(true);
        }

        /// <summary>
        /// Whether the spray painting action is currently active (spray can equipped, animation playing).
        /// </summary>
        public bool IsActive => _isActive;

        private void SetActive(bool value) => _isActive = value;

        /// <summary>
        /// Begins the spray painting action: equips the spray can and plays the UseSprayCan animation.
        /// </summary>
        /// <remarks>
        /// No-op if the spray paint component is not on the prefab. Call <see cref="NPCPrefabBuilder.EnsureGraffiti"/> during ConfigurePrefab to add it.
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
        /// Ends the spray painting action: unequips the spray can and stops the animation.
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

        /// <summary>
        /// Sets the spray effect (particle and sound) on or off.
        /// </summary>
        /// <param name="enabled">Whether the spray effect should be visible and audible.</param>
        /// <param name="color">Color for the spray particles when enabling. Ignored when disabling.</param>
        public void SetEffect(bool enabled, Color color = default)
        {
            var comp = GetComponent();
            if (comp != null)
                comp.SetEffect(enabled, color);
        }
    }
}
