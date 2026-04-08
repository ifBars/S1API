#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
#endif

using S1API.Logging;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Rendering
{
    /// <summary>
    /// Factory for creating runtime avatar layer variants by cloning an existing layer and replacing its texture.
    /// </summary>
    public static class AvatarLayerFactory
    {
        private static readonly Log Logger = new Log("AvatarLayerFactory");

        /// <summary>
        /// Clones an avatar layer asset and replaces its primary texture.
        /// </summary>
        /// <param name="sourceResourcePath">The Resources path to the source avatar layer.</param>
        /// <param name="newName">The display name to assign to the cloned layer.</param>
        /// <param name="replacementTexture">The replacement texture to assign to the cloned layer.</param>
        /// <param name="targetResourcePath">Optional target resource path for the cloned layer.</param>
        /// <returns>The cloned avatar layer object, or <see langword="null"/> if cloning failed.</returns>
        public static Object? CloneAvatarLayerWithTexture(
            string sourceResourcePath,
            string newName,
            Texture2D replacementTexture,
            string? targetResourcePath = null)
        {
            if (string.IsNullOrWhiteSpace(sourceResourcePath))
            {
                Logger.Error("Cannot clone avatar layer: sourceResourcePath is null or empty");
                return null;
            }

            if (replacementTexture == null)
            {
                Logger.Error($"Cannot clone avatar layer '{sourceResourcePath}': replacementTexture is null");
                return null;
            }

            try
            {
                Object? sourceLayer = Resources.Load(sourceResourcePath);
                if (sourceLayer == null)
                {
                    Logger.Error($"Failed to load source avatar layer at path: {sourceResourcePath}");
                    return null;
                }

                Object clonedLayer = Object.Instantiate(sourceLayer);
                Object.DontDestroyOnLoad(clonedLayer);
                clonedLayer.hideFlags = HideFlags.DontUnloadUnusedAsset;

                object typedLayer = clonedLayer;
                Type avatarLayerType = typeof(S1AvatarFramework.AvatarLayer);
                if (!avatarLayerType.IsAssignableFrom(clonedLayer.GetType()))
                {
                    object? wrappedLayer = CreateTypedWrapper(clonedLayer, avatarLayerType);
                    if (wrappedLayer == null)
                    {
                        Logger.Error($"Failed to create typed avatar layer wrapper for '{sourceResourcePath}'");
                        return null;
                    }

                    typedLayer = wrappedLayer;
                }

                string assetPath = targetResourcePath ?? sourceResourcePath;
                if (!TrySetMember(avatarLayerType, typedLayer, "Name", newName) ||
                    !TrySetMember(avatarLayerType, typedLayer, "AssetPath", assetPath) ||
                    !TrySetMember(avatarLayerType, typedLayer, "Texture", replacementTexture))
                {
                    Logger.Error($"Failed to configure cloned avatar layer '{sourceResourcePath}'");
                    return null;
                }

                return (typedLayer as Object) ?? clonedLayer;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to clone avatar layer '{sourceResourcePath}': {ex.Message}");
                Logger.Error(ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Registers a cloned avatar layer so it can be resolved through <see cref="Resources.Load(string)"/>.
        /// </summary>
        /// <param name="resourcePath">The Resources path to register the avatar layer at.</param>
        /// <param name="avatarLayer">The cloned avatar layer object to register.</param>
        /// <returns><see langword="true"/> if the avatar layer was registered successfully; otherwise <see langword="false"/>.</returns>
        public static bool RegisterAvatarLayer(string resourcePath, Object avatarLayer)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                Logger.Error("Cannot register avatar layer: resourcePath is null or empty");
                return false;
            }

            if (avatarLayer == null)
            {
                Logger.Error($"Cannot register avatar layer at '{resourcePath}': avatarLayer is null");
                return false;
            }

            bool assetRegistered = RuntimeResourceRegistry.RegisterAsset(resourcePath, avatarLayer);
            if (!assetRegistered)
            {
                return false;
            }

            RuntimeResourceRegistry.RegisterAssetForType(resourcePath, avatarLayer, typeof(S1AvatarFramework.AvatarLayer));
            return true;
        }

        /// <summary>
        /// Creates and registers a cloned avatar layer with a replacement texture.
        /// </summary>
        /// <param name="sourceResourcePath">The Resources path to the source avatar layer.</param>
        /// <param name="targetResourcePath">The Resources path where the cloned layer will be registered.</param>
        /// <param name="newName">The display name to assign to the cloned layer.</param>
        /// <param name="replacementTexture">The replacement texture to assign to the cloned layer.</param>
        /// <returns><see langword="true"/> if the avatar layer was created and registered successfully; otherwise <see langword="false"/>.</returns>
        public static bool CreateAndRegisterAvatarLayer(
            string sourceResourcePath,
            string targetResourcePath,
            string newName,
            Texture2D replacementTexture)
        {
            Object? clonedLayer = CloneAvatarLayerWithTexture(
                sourceResourcePath,
                newName,
                replacementTexture,
                targetResourcePath);

            return clonedLayer != null && RegisterAvatarLayer(targetResourcePath, clonedLayer);
        }

        private static object? CreateTypedWrapper(Object sourceLayer, Type targetType)
        {
            Type? currentType = sourceLayer.GetType();
            PropertyInfo? pointerProperty = null;

            while (currentType != null && pointerProperty == null)
            {
                pointerProperty = currentType.GetProperty(
                    "Pointer",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                currentType = currentType.BaseType;
            }

            if (pointerProperty == null)
            {
                return null;
            }

            object? pointer = pointerProperty.GetValue(sourceLayer);
            ConstructorInfo? constructor = targetType.GetConstructor(new[] { typeof(IntPtr) });
            if (constructor == null || pointer == null)
            {
                return null;
            }

            return constructor.Invoke(new[] { pointer });
        }

        private static bool TrySetMember(Type targetType, object target, string memberName, object value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo? property = targetType.GetProperty(memberName, flags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            FieldInfo? field = targetType.GetField(memberName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            return false;
        }
    }
}
