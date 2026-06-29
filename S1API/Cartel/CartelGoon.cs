#if (IL2CPPMELON)
using S1Cartel = Il2CppScheduleOne.Cartel;
using S1Player = Il2CppScheduleOne.PlayerScripts;
using S1Combat = Il2CppScheduleOne.Combat;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Cartel = ScheduleOne.Cartel;
using S1Player = ScheduleOne.PlayerScripts;
using S1Combat = ScheduleOne.Combat;
#endif

using S1API.Entities;
using S1API.Entities.Interfaces;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Cartel
{
    /// <summary>
    /// Represents a cartel goon (enemy NPC) that can be spawned and controlled.
    /// </summary>
    public class CartelGoon
    {
        /// <summary>
        /// INTERNAL: Reference to the game's CartelGoon instance.
        /// </summary>
        internal readonly S1Cartel.CartelGoon S1Goon;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game CartelGoon instance.
        /// </summary>
        internal CartelGoon(S1Cartel.CartelGoon goon)
        {
            S1Goon = goon;
        }

        /// <summary>
        /// Whether this goon is still conscious (alive and not knocked out).
        /// </summary>
        public bool IsConscious => S1Goon.IsConscious;

        /// <summary>
        /// Whether this goon is currently spawned in the world.
        /// </summary>
        public bool IsSpawned => S1Goon.IsSpawned;

        /// <summary>
        /// The current world position of this goon.
        /// </summary>
        public Vector3 Position => S1Goon.transform.position;

        /// <summary>
        /// The GameObject associated with this goon.
        /// </summary>
        public GameObject GameObject => S1Goon.gameObject;

        /// <summary>
        /// Teleports this goon to a specific world position.
        /// </summary>
        /// <param name="position">The target position.</param>
        public void WarpTo(Vector3 position)
        {
            S1Goon.Movement?.Warp(position);
        }

        /// <summary>
        /// Makes this goon attack the local player.
        /// </summary>
        public void AttackPlayer()
        {
            var player = S1Player.Player.Local;
            if (player == null) return;

            var targetable = player.GetComponent<S1Combat.ICombatTargetable>();
            if (targetable != null)
            {
                S1Goon.AttackEntity(targetable);
            }
        }

        /// <summary>
        /// Makes this goon attack a specific entity.
        /// </summary>
        /// <param name="target">The target entity to attack.</param>
        public void Attack(IEntity target)
        {
            if (target?.gameObject == null) return;

            // Get the ICombatTargetable component for attacking
            var targetable = target.gameObject.GetComponent<S1Combat.ICombatTargetable>();
            if (targetable != null)
            {
                S1Goon.AttackEntity(targetable);
            }
        }

        /// <summary>
        /// Despawns this goon, removing them from the world.
        /// </summary>
        public void Despawn()
        {
            S1Goon.Despawn();
        }

        /// <summary>
        /// Sets or clears the default weapon for this goon.
        /// Pass null to make them use fists.
        /// </summary>
        /// <param name="weaponAssetPath">The weapon asset path, or null for fists.</param>
        public void SetDefaultWeapon(string? weaponAssetPath)
        {
            var combatBehaviour = S1Goon.Behaviour?.CombatBehaviour;
            if (combatBehaviour == null) return;

            if (string.IsNullOrEmpty(weaponAssetPath))
            {
                SetCombatDefaultWeapon(combatBehaviour, null);
            }
            else
            {
                var go = Resources.Load(weaponAssetPath) as GameObject;
                if (go != null)
                {
#if (IL2CPPMELON)
                    var equippable = UnityEngine.Object.Instantiate(go).GetComponent<Il2CppScheduleOne.AvatarFramework.Equipping.AvatarWeapon>();
#else
                    var equippable = UnityEngine.Object.Instantiate(go).GetComponent<ScheduleOne.AvatarFramework.Equipping.AvatarWeapon>();
#endif
                    SetCombatDefaultWeapon(combatBehaviour, equippable);
                }
            }
        }

        private static void SetCombatDefaultWeapon(object combatBehaviour, object? weapon)
        {
            var method = combatBehaviour.GetType().GetMethod("SetDefaultWeapon", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(combatBehaviour, new[] { weapon });
                return;
            }

            if (!ReflectionUtils.TrySetFieldOrProperty(combatBehaviour, "DefaultWeapon", weapon))
            {
                ReflectionUtils.TrySetFieldOrProperty(combatBehaviour, "_defaultWeapon", weapon);
            }
        }
    }
}
