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
using S1API.Items;
using UnityEngine;
using MelonLoader;
using S1API.Entities.Equippables;
using Object = UnityEngine.Object;

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
        get => NPC.S1NPC.Behaviour.CombatBehaviour.DefaultSearchTime;
        set => NPC.S1NPC.Behaviour.CombatBehaviour.DefaultSearchTime = value;
    }

    /// <summary>
    /// Gets or sets the default weapon asset path for the NPC's combat behaviour.
    /// This property allows you to specify the weapon that the NPC will use by default.
    /// <see cref="Weapon"/> for convenience when setting this property.
    /// </summary>
    public string DefaultWeaponAssetPath
    {
        get
        {
            var defaultWeapon = NPC.S1NPC.Behaviour.CombatBehaviour.DefaultWeapon;
            return defaultWeapon?.AssetPath ?? string.Empty;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                NPC.S1NPC.Behaviour.CombatBehaviour.DefaultWeapon = null;
                return;
            }

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

    /// <summary>
    /// Sets the current weapon of the NPC using an S1API Equippable wrapper.
    /// This method extracts the asset path from the equippable's associated AvatarEquippable component.
    /// </summary>
    /// <param name="equippable">The S1API Equippable wrapper containing the weapon to equip.</param>
    /// <remarks>
    /// This method attempts to find an AvatarEquippable component on the equippable's GameObject
    /// and uses its AssetPath. If no AvatarEquippable is found, this method will log an error.
    /// </remarks>
    public void SetCurrentWeapon(Equippable equippable)
    {
        if (equippable == null)
        {
            Debug.LogError("Cannot set weapon: Equippable is null");
            return;
        }

        var assetPath = GetAssetPathFromEquippable(equippable);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Cannot set weapon: Could not extract asset path from Equippable");
            return;
        }

        SetCurrentWeapon(assetPath);
    }

    /// <summary>
    /// Sets the default weapon for the NPC's combat behaviour using an S1API Equippable wrapper.
    /// This method extracts the asset path from the equippable's associated AvatarEquippable component.
    /// </summary>
    /// <param name="equippable">The S1API Equippable wrapper containing the weapon to set as default.</param>
    /// <remarks>
    /// This method attempts to find an AvatarEquippable component on the equippable's GameObject
    /// and uses its AssetPath. If no AvatarEquippable is found, this method will log an error.
    /// </remarks>
    public void SetDefaultWeapon(Equippable equippable)
    {
        if (equippable == null)
        {
            Debug.LogError("Cannot set default weapon: Equippable is null");
            return;
        }

        var assetPath = GetAssetPathFromEquippable(equippable);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Cannot set default weapon: Could not extract asset path from Equippable");
            return;
        }

        DefaultWeaponAssetPath = assetPath;
    }

    /// <summary>
    /// Sets the current weapon of the NPC using an EquippableBuilder-created equippable.
    /// This method extracts the asset path from the builder's AvatarEquippable configuration.
    /// </summary>
    /// <param name="equippableBuilder">The EquippableBuilder instance containing the weapon configuration.</param>
    /// <remarks>
    /// This method requires the EquippableBuilder to have been configured with WithAvatarEquippable()
    /// to provide an asset path. The builder will be built automatically if not already built.
    /// </remarks>
    public void SetCurrentWeapon(EquippableBuilder equippableBuilder)
    {
        if (equippableBuilder == null)
        {
            Debug.LogError("Cannot set weapon: EquippableBuilder is null");
            return;
        }

        // Build the equippable to get the GameObject
        var equippable = equippableBuilder.Build();
        SetCurrentWeapon(equippable);
    }

    /// <summary>
    /// Sets the default weapon for the NPC's combat behaviour using an EquippableBuilder-created equippable.
    /// This method extracts the asset path from the builder's AvatarEquippable configuration.
    /// </summary>
    /// <param name="equippableBuilder">The EquippableBuilder instance containing the weapon configuration.</param>
    /// <remarks>
    /// This method requires the EquippableBuilder to have been configured with WithAvatarEquippable()
    /// to provide an asset path. The builder will be built automatically if not already built.
    /// </remarks>
    public void SetDefaultWeapon(EquippableBuilder equippableBuilder)
    {
        if (equippableBuilder == null)
        {
            Debug.LogError("Cannot set default weapon: EquippableBuilder is null");
            return;
        }

        // Build the equippable to get the GameObject
        var equippable = equippableBuilder.Build();
        SetDefaultWeapon(equippable);
    }

    /// <summary>
    /// INTERNAL: Extracts the asset path from an S1API Equippable wrapper by finding its associated AvatarEquippable component.
    /// </summary>
    /// <param name="equippable">The S1API Equippable wrapper.</param>
    /// <returns>The asset path if found, otherwise null or empty string.</returns>
    private string GetAssetPathFromEquippable(Equippable equippable)
    {
        if (equippable?.S1Equippable == null)
        {
            return null;
        }

        // Try to get the GameObject from the equippable component
        var gameObject = equippable.S1Equippable.gameObject;
        if (gameObject == null)
        {
            return null;
        }

        // Look for AvatarEquippable component on the GameObject or its children
        var avatarEquippable = gameObject.GetComponent<AvatarEquippable>();
        if (avatarEquippable == null)
        {
            avatarEquippable = gameObject.GetComponentInChildren<AvatarEquippable>();
        }

        // If still not found, check if there's a child GameObject with AvatarEquippable
        if (avatarEquippable == null)
        {
            foreach (Transform child in gameObject.transform)
            {
                avatarEquippable = child.GetComponent<AvatarEquippable>();
                if (avatarEquippable != null)
                {
                    break;
                }
            }
        }

        return avatarEquippable?.AssetPath;
    }
}