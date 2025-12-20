#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1Health = Il2CppScheduleOne.PlayerScripts.Health;
#else
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1Health = ScheduleOne.PlayerScripts.Health;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Entities.Interfaces;
using S1API.Internal.Abstraction;
using S1API.Law;
using UnityEngine;
using S1API.Vehicles;
using S1API.Property;
using S1API.Map;

namespace S1API.Entities
{
    /// <summary>
    /// Represents a player within the game.
    /// </summary>
    public class Player : IEntity, IHealth
    {
        /// <summary>
        /// Health when the player is invincible.
        /// Invincibility isn't baked in the base game.
        /// Hence, why we're doing it this way :).
        /// </summary>
        private const float InvincibleHealth = 1000000000f;

        /// <summary>
        /// The standard MAX_HEALTH of a player.
        /// </summary>
        private const float MortalHealth = 100f;

        /// <summary>
        /// All players currently in the game.
        /// </summary>
        public static readonly List<Player> All = new List<Player>();

        /// <summary>
        /// Fired when any player finishes client startup.
        /// </summary>
        public static event Action<Player>? PlayerSpawned;

        /// <summary>
        /// Fired when the local (client-owned) player finishes client startup.
        /// </summary>
        public static event Action<Player>? LocalPlayerSpawned;

        /// <summary>
        /// Fired when a player is destroyed/removed.
        /// </summary>
        public static event Action<Player>? PlayerDespawned;

        /// <summary>
        /// INTERNAL: Tracking of the S1 instance of the player.
        /// </summary>
        internal S1PlayerScripts.Player S1Player;

        /// <summary>
        /// INTERNAL: Constructor to create a new player from an S1 instance.
        /// </summary>
        /// <param name="player"></param>
        internal Player(S1PlayerScripts.Player player)
        {
            S1Player = player;
            All.Add(this);
        }

        /// <summary>
        /// The current client player (player executing your code).
        /// </summary>
        public static Player Local =>
            All.FirstOrDefault(player => player.IsLocal)!;

        /// <summary>
        /// Whether this player is the client player or a networked player.
        /// </summary>
        public bool IsLocal =>
            S1Player.IsLocalPlayer;

        /// <summary>
        /// The name of the player.
        /// For single player, this appears to always return `Player`.
        /// </summary>
        public string Name =>
            S1Player.PlayerName;

        /// <summary>
        /// INTERNAL: The game object associated with this player.
        /// </summary>
        GameObject IEntity.gameObject =>
            S1Player.gameObject;

        /// <summary>
        /// The world position of the player.
        /// </summary>
        public Vector3 Position
        {
            get => ((IEntity)this).gameObject.transform.position;
            set => ((IEntity)this).gameObject.transform.position = value;
        }

        /// <summary>
        /// The transform of the player.
        /// Please do not set the properties of the Transform.
        /// </summary>
        public Transform Transform =>
            ((IEntity)this).gameObject.transform;

        /// <summary>
        /// The scale of the player.
        /// </summary>
        public float Scale
        {
            get => S1Player.Scale;
            set => S1Player.SetScale(value);
        }

        /// <summary>
        /// The player's crime data, including wanted level, crimes committed, and pursuit state.
        /// </summary>
        public PlayerCrimeData CrimeData =>
            new PlayerCrimeData(S1Player.CrimeData);

        /// <summary>
        /// The current health of the player.
        /// </summary>
        public float CurrentHealth =>
            S1Player.Health.CurrentHealth;

        /// <summary>
        /// The maximum health of the player.
        /// Note: In the base game this is a constant (100). Changing it at runtime is unsupported.
        /// Setting this will clamp the player's current health to the specified value.
        /// </summary>
        public float MaxHealth
        {
            get => MortalHealth;
            set => S1Player.Health.SetHealth(value);
        }

        /// <summary>
        /// Whether the player is dead or not.
        /// </summary>
        public bool IsDead =>
            !S1Player.Health.IsAlive;

        /// <summary>
        /// Whether the player is invincible or not.
        /// </summary>
        public bool IsInvincible
        {
            get => InvinciblePlayers.Contains(S1Player);
            set
            {
                if (value)
                    InvinciblePlayers.Add(S1Player);
                else
                    InvinciblePlayers.Remove(S1Player);
            }
        }

        /// <summary>
        /// The Last vehicle this player has driven
        /// </summary>
        public LandVehicle? LastDrivenVehicle {
            get {
                if (S1Player.LastDrivenVehicle == null)
                    return null;
                foreach (var v in VehicleRegistry.GetAll()) {
                    if (v.S1LandVehicle == S1Player.LastDrivenVehicle) {
                        return v;
                    }
                }
                return null;
            }
        }
        /// <summary>
        /// If player is currently in a vehicle
        /// </summary>
        public bool IsInVehicle => S1Player.IsInVehicle;

        /// <summary>
        /// Time since player has exited a vehicle
        /// </summary>
        public float TimeSinceVehicleExit => S1Player.TimeSinceVehicleExit;

        /// <summary>
        /// Is player crouching
        /// </summary>
        public bool Crouched => S1Player.Crouched;

        /// <summary>
        /// If player is ready for sleep
        /// </summary>
        public bool IsReadyToSleep => S1Player.IsReadyToSleep;

        /// <summary>
        /// If player is currently using a skateboard
        /// </summary>
        public bool IsSkating => S1Player.IsSkating;

        /// <summary>
        /// If player is currently sleeping
        /// </summary>
        public bool IsSleeping => S1Player.IsSleeping;

        /// <summary>
        /// Is Player currently ragdolled
        /// </summary>
        public bool IsRagdolled => S1Player.IsRagdolled;

        /// <summary>
        /// If player is currently under arrest
        /// </summary>
        public bool IsArrested => S1Player.IsArrested;

        /// <summary>
        /// Is player currently under tased effect
        /// </summary>
        public bool IsTased => S1Player.IsTased;

        /// <summary>
        /// If player is unconscious
        /// </summary>
        public bool IsUnconscious => S1Player.IsUnconscious;

        /// <summary>
        /// Property this player is currently in
        /// </summary>
        public BaseProperty? CurrentProperty {
            get {
                if(S1Player.CurrentProperty == null)
                    return null;
                foreach (var p in PropertyManager.GetAllProperties()) {
                    if (p.InnerProperty == S1Player.CurrentProperty)
                        return p;
                }
                return null;
            }
        }

        /// <summary>
        /// Last Property this player visted
        /// (May only be owned properties)
        /// </summary>
        public BaseProperty? LastVisitedProperty {
            get {
                var s1Prop = S1Player.LastVisitedProperty;
                if(s1Prop == null)
                    return null;
                foreach (var p in PropertyManager.GetAllProperties()) {
                    if (p.InnerProperty == s1Prop)
                        return p;
                }
                return null;
            }
        }
        /// <summary>
        /// Region this player is currently in
        /// </summary>
        public Region CurrentRegion => (Region)S1Player.CurrentRegion;

        /// <summary>
        /// Revives the player.
        /// </summary>
        public void Revive() =>
            S1Player.Health.Revive(Position, Quaternion.identity);

        /// <summary>
        /// Deals damage to the player.
        /// </summary>
        /// <param name="amount">The amount of damage to deal.</param>
        public void Damage(int amount)
        {
            if (amount <= 0)
                return;

            S1Player.Health.TakeDamage(amount);
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        /// <param name="amount">The amount of healing to apply to the player.</param>
        public void Heal(int amount)
        {
            if (amount <= 0)
                return;

            S1Player.Health.SetHealth(CurrentHealth + amount);
        }

        /// <summary>
        /// Kills the player.
        /// </summary>
        public void Kill() =>
            S1Player.Health.SetHealth(0f);

        /// <summary>
        /// Called when the player dies.
        /// </summary>
        public event Action OnDeath
        {
            add => EventHelper.AddListener(value, S1Player.Health.onDie);
            remove => EventHelper.RemoveListener(value, S1Player.Health.onDie);
        }

        private static readonly HashSet<S1PlayerScripts.Player> InvinciblePlayers = new HashSet<S1PlayerScripts.Player>();

        internal static bool IsPlayerInvincible(S1PlayerScripts.Player player) => InvinciblePlayers.Contains(player);

        internal static void RaisePlayerSpawned(Player player)
        {
            PlayerSpawned?.Invoke(player);
        }

        internal static void RaiseLocalPlayerSpawned(Player player)
        {
            LocalPlayerSpawned?.Invoke(player);
        }

        internal static void RaisePlayerDespawned(Player player)
        {
            PlayerDespawned?.Invoke(player);
        }
    }
}
