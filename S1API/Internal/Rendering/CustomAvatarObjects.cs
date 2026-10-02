#if IL2CPPMELON
using S1Avatar = Il2CppScheduleOne.Avatar;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1CoreAvatar = Il2CppScheduleOne.Core.Avatar;
#elif MONOMELON
using S1Avatar = ScheduleOne.Avatar;
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1CoreAvatar = ScheduleOne.Core.Avatar;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Rendering
{
    /// <summary>
    /// INTERNAL: Builds and registers custom <c>AvatarObject</c> prefabs for runtime-created accessories.
    /// </summary>
    /// <remarks>
    /// From 0.4.7 the game dresses an avatar in <c>AvatarObject</c> prefabs, not in the legacy <c>Accessory</c>
    /// prefabs. A legacy accessory only points at its <c>AvatarObject</c> through
    /// <c>Accessory.AvatarObjectEquivalent</c>. A clothing item stores that <c>AvatarObject</c>
    /// (<c>ClothingDefinition.ClothingAvatarObject</c>), serializes it by <c>Id</c>, and the avatar later finds
    /// the prefab again with <c>AvatarObjectLibrary.TryGetAvatarObjectById</c>. A custom accessory therefore needs
    /// an <c>AvatarObject</c> of its own, with an id nothing else uses, registered in that library.
    /// </remarks>
    internal static class CustomAvatarObjects
    {
        private static readonly Log Logger = new Log("CustomAvatarObjects");

        private const string HolderName = "S1API_CustomAvatarObjects";

        private static GameObject? _holder;

        /// <summary>
        /// The id a custom accessory's <c>AvatarObject</c> is registered under: the resource path the mod registers it
        /// at, which is stable across sessions (saves refer to it), or a path built from the source and new name when
        /// the clone is made without a target path.
        /// </summary>
        internal static string DeriveId(string? targetResourcePath, string sourceResourcePath, string newName)
        {
            if (!string.IsNullOrWhiteSpace(targetResourcePath))
                return targetResourcePath!;
            return sourceResourcePath.TrimEnd('/') + "/" + newName;
        }

        /// <summary>
        /// Clones an <c>AvatarObject</c> prefab into one with its own name and id, kept under an inactive holder so
        /// that, like a prefab asset, it neither renders nor runs <c>Awake</c> until the game instantiates it.
        /// </summary>
        internal static S1CoreAvatar.AvatarObject? Clone(S1CoreAvatar.AvatarObject source, string name, string id)
        {
            try
            {
                var clone = Object.Instantiate(source, EnsureHolder().transform, false);
                clone.gameObject.name = name;

                if (!ReflectionUtils.TrySetFieldOrProperty(clone, "_id", id)
                    || !ReflectionUtils.TrySetFieldOrProperty(clone, "_name", name))
                {
                    Logger.Error($"Could not set the id or name on the AvatarObject cloned for '{id}'; "
                                 + "the game's AvatarObject members have changed.");
                    Object.Destroy(clone.gameObject);
                    return null;
                }

                return clone;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to clone AvatarObject for '{id}': {ex.Message}");
                Logger.Error(ex.StackTrace ?? ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Applies an optional primary colour default to the cloned avatar object's serialized properties.
        /// </summary>
        internal static void ApplyColorTint(S1CoreAvatar.AvatarObject avatarObject, Color? colorTint)
        {
            if (colorTint.HasValue)
                avatarObject.PropertyCollection?.GetFirstColorProperty()?.SetValue(colorTint.Value);
        }

        /// <summary>
        /// Makes the avatar-object library return <paramref name="avatarObject"/> for its id, which is how the game
        /// finds the prefab when it applies a serialized avatar object.
        /// </summary>
        internal static bool RegisterInLibrary(S1CoreAvatar.AvatarObject avatarObject)
        {
            try
            {
                string id = avatarObject.Id;
                if (string.IsNullOrEmpty(id))
                {
                    Logger.Error("Cannot register an AvatarObject with no id in the library.");
                    return false;
                }

#if IL2CPPMELON
                var library = S1Avatar.AvatarObjectLibrary._avatarObjects;
#else
                var library = ReflectionUtils.TryGetStaticFieldOrProperty(
                    typeof(S1Avatar.AvatarObjectLibrary),
                    "_avatarObjects") as Dictionary<string, S1CoreAvatar.AvatarObject>;
#endif
                if (library == null)
                {
                    Logger.Error($"AvatarObjectLibrary has no dictionary to register '{id}' in.");
                    return false;
                }

                library[id] = avatarObject;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register an AvatarObject in the library: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Names the game members this relies on that are missing. A test calls it, so a game update that renames one
        /// fails the build instead of leaving a custom accessory that quietly does not show.
        /// </summary>
        internal static IReadOnlyList<string> FindMissingGameMembers()
        {
            var missing = new List<string>();
            RequireMember(missing, typeof(S1CoreAvatar.AvatarObject), "_id");
            RequireMember(missing, typeof(S1CoreAvatar.AvatarObject), "_name");
            RequireMember(missing, typeof(S1CoreAvatar.AvatarObject), "Id");
            RequireMember(missing, typeof(S1Avatar.AvatarObjectLibrary), "_avatarObjects");
            RequireMember(missing, typeof(S1AvatarFramework.Accessory), "AvatarObjectEquivalent");
            return missing;
        }

        private static void RequireMember(List<string> missing, Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            if (type.GetField(name, flags) == null && type.GetProperty(name, flags) == null)
                missing.Add(type.Name + "." + name);
        }

        private static GameObject EnsureHolder()
        {
            if (_holder != null)
                return _holder;

            _holder = new GameObject(HolderName);
            _holder.SetActive(false);
            Object.DontDestroyOnLoad(_holder);
            return _holder;
        }
    }
}
