#if (IL2CPPMELON)
using S1Combat = Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppFishNet.Object;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Combat = ScheduleOne.Combat;
using ScheduleOne.AvatarFramework.Equipping;
using FishNet.Object;
#endif
using System.Reflection;
using S1API.Entities.Interfaces;
using UnityEngine;
using MelonLoader;
using S1API.Entities.Equippables;

namespace S1API.Entities.Behaviour;

/// <summary>
/// Represents the combat behaviour of an NPC, allowing configuration of combat-related settings and actions.
/// </summary>
public class CombatBehaviour
{
    /// <summary>
    /// INTERNAL: NPC reference
    /// </summary>
    internal readonly NPC NPC;

    /// <summary>
    /// INTERNAL: Constructor used for assigning the NPC instance.
    /// </summary>
    /// <param name="npc">NPC instance.</param>
    internal CombatBehaviour(NPC npc)
    {
        NPC = npc;
    }

    /// <summary>
    /// Gets or sets the range in units at which the NPC will give up pursuing a target.
    /// </summary>
    public float GiveUpRange
    {
        get => NPC.S1NPC.Behaviour.CombatBehaviour.GiveUpRange;
        set => NPC.S1NPC.Behaviour.CombatBehaviour.GiveUpRange = value;
    }

    /// <summary>
    /// Gets or sets the time in seconds the NPC will continue to pursue a target before giving up.
    /// </summary>
    public float GiveUpTime
    {
        get => NPC.S1NPC.Behaviour.CombatBehaviour.GiveUpTime;
        set => NPC.S1NPC.Behaviour.CombatBehaviour.GiveUpTime = value;
    }

    /// <summary>
    /// Gets or sets the default weapon asset path for the NPC's combat behaviour.
    /// This property allows you to specify the weapon that the NPC will use by default.
    /// <see cref="Weapon"/> for convenience when setting this property.
    /// </summary>
    public string DefaultWeaponAssetPath
    {
        get => NPC.S1NPC.Behaviour.CombatBehaviour.DefaultWeapon.AssetPath;
        set
        {
            var go = Resources.Load(value) as GameObject;
            if (go == null)
            {
                Debug.LogError("Could not find weapon at path: " + value);
                return;
            }

            var equippable = Object.Instantiate(go).GetComponent<AvatarEquippable>();
            if (equippable == null)
            {
                Debug.LogError("Could not get AvatarEquippable from weapon at path: " + value);
                return;
            }

            NPC.S1NPC.Behaviour.CombatBehaviour.DefaultWeapon = equippable as AvatarWeapon;
        }
    }

    /// <summary>
    /// Sets the specified target as the NPC's combat target and enables attacking behavior.
    /// </summary>
    /// <param name="target">The target entity to be attacked by the NPC.</param>
    public void SetAndAttackTarget(IEntity target)
    {
        var targetNob = target.gameObject.GetComponent<NetworkObject>();
        NPC.S1NPC.Behaviour.CombatBehaviour.SetTargetAndEnable_Server(targetNob);
    }

    /// <summary>
    /// Sets the current weapon of the NPC by specifying the weapon's resource path.
    /// </summary>
    /// <param name="weaponPath">The resource path of the weapon to equip (e.g., "Avatar/Equippables/M1911"). For convenience, <see cref="Weapon"/> can be used.</param>
    public void SetCurrentWeapon(string weaponPath)
    {
#if (!IL2CPPMELON)
        var setWeaponMethod =
            typeof(S1Combat.CombatBehaviour).GetMethod("SetWeapon", BindingFlags.Instance | BindingFlags.NonPublic);
        setWeaponMethod?.Invoke(NPC.S1NPC.Behaviour.CombatBehaviour, new object[] { weaponPath });
#else
        NPC.S1NPC.Behaviour.CombatBehaviour.SetWeapon(weaponPath);
#endif
    }
}