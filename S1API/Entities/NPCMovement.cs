#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
#endif

using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Modder-facing movement wrapper for an <see cref="NPC"/>.
    /// Provides navigation, warping, facing, and reachability helpers.
    /// </summary>
    public class NPCMovement
    {
        #region Internal Members

        /// <summary>
        /// INTERNAL: Reference to the NPC on API side.
        /// </summary>
        private readonly NPC NPC;

        /// <summary>
        /// INTERNAL: Constructor used for assigning the NPC instance.
        /// </summary>
        /// <param name="npc">API-side NPC wrapper.</param>
        internal NPCMovement(NPC npc)
        {
            NPC = npc;
            NPC.S1NPC.Movement.enabled = true;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Current foot position of the NPC in world space.
        /// </summary>
        public Vector3 FootPosition =>
            NPC.S1NPC.Movement != null ? NPC.S1NPC.Movement.FootPosition : NPC.gameObject.transform.position;

        /// <summary>
        /// Current destination being navigated to, if any.
        /// </summary>
        public Vector3 CurrentDestination =>
            NPC.S1NPC.Movement != null ? NPC.S1NPC.Movement.CurrentDestination : NPC.gameObject.transform.position;

        /// <summary>
        /// Whether the NPC is currently moving along a path.
        /// </summary>
        public bool IsMoving =>
            NPC.S1NPC.Movement != null && NPC.S1NPC.Movement.IsMoving;

        /// <summary>
        /// Sets a new pathfinding destination in world space.
        /// </summary>
        /// <param name="position">Target destination.</param>
        public void SetDestination(Vector3 position)
        {
            if (NPC.S1NPC.Movement != null)
                NPC.S1NPC.Movement.SetDestination(position);
        }

        /// <summary>
        /// Instantly moves the NPC to the given world position.
        /// </summary>
        /// <param name="position">Target position.</param>
        public void Warp(Vector3 position)
        {
            if (NPC.S1NPC.Movement != null)
                NPC.S1NPC.Movement.Warp(position);
            else
                NPC.gameObject.transform.position = position;
        }

        /// <summary>
        /// Stops any active movement/pathing immediately.
        /// </summary>
        public void Stop()
        {
            if (NPC.S1NPC.Movement != null)
                NPC.S1NPC.Movement.Stop();
        }

        /// <summary>
        /// Returns whether the NPC can path to a position.
        /// </summary>
        /// <param name="position">Target position.</param>
        public bool CanGetTo(Vector3 position) =>
            NPC.S1NPC.Movement != null && NPC.S1NPC.Movement.CanGetTo(position);

        /// <summary>
        /// Returns whether the NPC can path to a position within a threshold.
        /// </summary>
        /// <param name="position">Target position.</param>
        /// <param name="within">Acceptable distance to target.</param>
        public bool CanGetTo(Vector3 position, float within) =>
            NPC.S1NPC.Movement != null && NPC.S1NPC.Movement.CanGetTo(position, within);

        /// <summary>
        /// Rotates to face a world-space direction.
        /// </summary>
        /// <param name="forward">Forward vector to face.</param>
        public void FaceDirection(Vector3 forward)
        {
            if (NPC.S1NPC.Movement != null)
                NPC.S1NPC.Movement.FaceDirection(forward);
        }

        /// <summary>
        /// Rotates to face a world-space point.
        /// </summary>
        /// <param name="position">World position to face.</param>
        public void FacePoint(Vector3 position)
        {
            if (NPC.S1NPC.Movement != null)
                NPC.S1NPC.Movement.FacePoint(position);
        }

        #endregion

        #region Speed Control

        /// <summary>
        /// Gets or sets the overall speed multiplier that affects all movement.
        /// </summary>
        public float SpeedMultiplier
        {
            get => SpeedController?.SpeedMultiplier ?? 1f;
            set
            {
                if (SpeedController != null)
                    SpeedController.SpeedMultiplier = value;
            }
        }

        /// <summary>
        /// Gets the default walking speed of the NPC.
        /// </summary>
        public float DefaultWalkSpeed =>
            SpeedController?.DefaultWalkSpeed ?? 0.08f;

        /// <summary>
        /// Adds or updates a speed control with the specified parameters.
        /// Higher priority values override lower ones.
        /// </summary>
        /// <param name="id">Unique identifier for this speed control.</param>
        /// <param name="priority">Priority level (higher values take precedence).</param>
        /// <param name="speed">Movement speed multiplier.</param>
        public void AddSpeedControl(string id, int priority, float speed)
        {
            if (SpeedController == null) return;
            var control = new S1NPCs.NPCSpeedController.SpeedControl(id, priority, speed);
            SpeedController.AddSpeedControl(control);
        }

        /// <summary>
        /// Removes a speed control by its ID.
        /// </summary>
        /// <param name="id">The ID of the speed control to remove.</param>
        public void RemoveSpeedControl(string id)
        {
            SpeedController?.RemoveSpeedControl(id);
        }

        /// <summary>
        /// Checks if a speed control with the given ID exists.
        /// </summary>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if the speed control exists, false otherwise.</returns>
        public bool DoesSpeedControlExist(string id)
        {
            return SpeedController?.DoesSpeedControlExist(id) ?? false;
        }

        /// <summary>
        /// INTERNAL: Gets the speed controller component from the NPC.
        /// </summary>
        private S1NPCs.NPCSpeedController? SpeedController =>
            NPC.S1NPC.Movement?.SpeedController;

        #endregion
    }
}