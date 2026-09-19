#if MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#elif IL2CPPMELON
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Entities;
using S1API.Internal.Patches;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class AvatarAccessoryDiagnostics
    {
        private const int NativeAccessorySlotCount = 9;
        private static readonly Log Logger = new Log("AvatarAccessoryDiagnostics");
        private static readonly HashSet<string> WarnedInvalidResources =
            new HashSet<string>(StringComparer.Ordinal);

        internal static void Validate(
            S1AvatarFramework.Avatar avatar,
            S1AvatarFramework.AvatarSettings settings)
        {
            if (avatar == null || !NPCPatches.IsS1ApiCustomNpcComponent(avatar))
                return;

            OwnerContext owner = ResolveOwner(avatar);
            try
            {
                ValidateCore(settings, owner);
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"[S1API][AvatarAccessoryValidation] Could not validate accessory settings for " +
                    $"S1API NPC {owner.Description}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void ValidateCore(
            S1AvatarFramework.AvatarSettings settings,
            OwnerContext owner)
        {
            if (settings?.AccessorySettings == null)
                return;

            int count = Math.Min(settings.AccessorySettings.Count, NativeAccessorySlotCount);
            for (int index = 0; index < count; index++)
            {
                var accessorySetting = settings.AccessorySettings[index];
                string? path = accessorySetting?.path;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                // Runtime-registered accessories have already passed AccessoryFactory's
                // registration contract. In particular, an IL2CPP wrapper can compare null
                // after registration even while the native Resources patch can still serve it.
                if (RuntimeResourceRegistry.IsRegistered(path))
                    continue;

                UnityEngine.Object? resource = Resources.Load(path);
                if (resource == null)
                {
                    WarnInvalidAccessoryOnce(
                        owner,
                        index,
                        path,
                        "Resources.Load returned null at runtime");
                    continue;
                }

                if (!CrossType.Is(resource, out GameObject accessoryObject) || accessoryObject == null)
                {
                    WarnInvalidAccessoryOnce(
                        owner,
                        index,
                        path,
                        "Resources.Load resolved an asset that is not a GameObject at runtime");
                    continue;
                }

                if (accessoryObject.GetComponent<S1AvatarFramework.Accessory>() == null)
                {
                    WarnInvalidAccessoryOnce(
                        owner,
                        index,
                        path,
                        $"the loaded GameObject '{accessoryObject.name}' has no AvatarFramework.Accessory component");
                }
            }
        }

        private static void WarnInvalidAccessoryOnce(
            OwnerContext owner,
            int index,
            string path,
            string reason)
        {
            string warningKey = owner.StableKey + "|" + index + "|" + path + "|" + reason;
            lock (WarnedInvalidResources)
            {
                if (!WarnedInvalidResources.Add(warningKey))
                    return;
            }

            Logger.Warning(
                $"[S1API][AvatarAccessoryValidation] S1API NPC {owner.Description} has an invalid accessory " +
                $"at index {index}: path='{path}'; {reason}. Correct or remove the accessory path in " +
                "WithAppearanceDefaults/AddAccessory, or register the custom accessory with " +
                "AccessoryFactory before the appearance is applied. S1API left the settings unchanged; " +
                "the native ApplyAccessorySettings call may throw.");
        }

        private static OwnerContext ResolveOwner(S1AvatarFramework.Avatar avatar)
        {
            NPCPrefabIdentity? identity = null;
            try { identity = FindIdentity(avatar); }
            catch { }

            string? prefabName = null;
            try
            {
                prefabName = identity?.PrefabName
                             ?? identity?.gameObject?.name
                             ?? FindS1ApiRootName(avatar);
            }
            catch
            {
            }

            string? id = identity?.Id;
            string? name = JoinName(identity?.FirstName, identity?.LastName);
            if ((string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                && !string.IsNullOrWhiteSpace(prefabName))
            {
                try
                {
                    string registryPrefabName =
                        NormalizeGameObjectName(prefabName) ?? prefabName;
                    if (NPCPrefabIdentity.TryGetIdentityFromRegistry(
                            registryPrefabName,
                            out string? registryId,
                            out string? firstName,
                            out string? lastName,
                            out _))
                    {
                        id = string.IsNullOrWhiteSpace(id) ? registryId : id;
                        name = string.IsNullOrWhiteSpace(name) ? JoinName(firstName, lastName) : name;
                    }
                }
                catch
                {
                }
            }

            string gameObjectName = NormalizeGameObjectName(prefabName)
                                    ?? avatar.gameObject?.name
                                    ?? avatar.name
                                    ?? "<unknown-avatar>";
            if (string.IsNullOrWhiteSpace(name))
                name = !string.IsNullOrWhiteSpace(id) ? id : gameObjectName;

            string description = FormatOwnerDescription(name, id, gameObjectName);
            string stableKey = SelectStableOwnerKey(id, prefabName, description);
            return new OwnerContext(description, stableKey);
        }

        private static NPCPrefabIdentity? FindIdentity(Component component)
        {
            for (Transform? current = component.transform; current != null; current = current.parent)
            {
                NPCPrefabIdentity? identity = current.gameObject?.GetComponent<NPCPrefabIdentity>();
                if (identity != null)
                    return identity;
            }

            return component.GetComponentInParent<NPCPrefabIdentity>(true);
        }

        private static string? FindS1ApiRootName(Component component)
        {
            for (Transform? current = component.transform; current != null; current = current.parent)
            {
                string? name = current.gameObject?.name;
                if (!string.IsNullOrWhiteSpace(name)
                    && name.StartsWith("S1API_", StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            return null;
        }

        private static string? JoinName(string? firstName, string? lastName)
        {
            string name = $"{firstName} {lastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private static string? NormalizeGameObjectName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            const string cloneSuffix = "(Clone)";
            return name.EndsWith(cloneSuffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - cloneSuffix.Length)
                : name;
        }

        internal static string FormatOwnerDescription(
            string? name,
            string? id,
            string? gameObjectName)
        {
            string safeName = string.IsNullOrWhiteSpace(name) ? "<unknown>" : name;
            string safeId = string.IsNullOrWhiteSpace(id) ? "<unknown-id>" : id;
            string safeGameObjectName = NormalizeGameObjectName(gameObjectName) ?? "<unknown-object>";
            return $"'{safeName}' (ID='{safeId}', GameObject='{safeGameObjectName}')";
        }

        internal static string SelectStableOwnerKey(
            string? id,
            string? prefabName,
            string fallbackDescription)
        {
            if (!string.IsNullOrWhiteSpace(id))
                return "id:" + id;

            string? normalizedPrefabName = NormalizeGameObjectName(prefabName);
            return !string.IsNullOrWhiteSpace(normalizedPrefabName)
                ? "prefab:" + normalizedPrefabName
                : "description:" + fallbackDescription;
        }

        private readonly struct OwnerContext
        {
            internal OwnerContext(string description, string stableKey)
            {
                Description = description;
                StableKey = stableKey;
            }

            internal string Description { get; }
            internal string StableKey { get; }
        }
    }
}
