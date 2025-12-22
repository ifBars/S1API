using S1API.Logging;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace S1API.Rendering
{
    /// <summary>
    /// Utility class for loading and creating textures at runtime.
    /// </summary>
    public static class TextureUtils
    {
        private static readonly Log Logger = new Log("TextureUtils");

        /// <summary>
        /// Loads a texture from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The fully qualified name of the embedded resource.</param>
        /// <param name="filterMode">The filter mode for the texture.</param>
        /// <param name="wrapMode">The wrap mode for the texture.</param>
        /// <returns>The loaded texture, or null if loading failed.</returns>
        public static Texture2D LoadTextureFromResource(
            Assembly assembly,
            string resourceName,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            if (assembly == null)
            {
                Logger.Error($"Assembly is null when loading texture resource: {resourceName}");
                return null;
            }

            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Logger.Error($"Texture resource not found: {resourceName}");
                        return null;
                    }

                    byte[] imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, imageData.Length);

                    return LoadTextureFromBytes(imageData, filterMode, wrapMode);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load texture from resource '{resourceName}': {ex}");
                return null;
            }
        }

        /// <summary>
        /// Loads a texture from a byte array.
        /// </summary>
        /// <param name="imageData">The image data bytes.</param>
        /// <param name="filterMode">The filter mode for the texture.</param>
        /// <param name="wrapMode">The wrap mode for the texture.</param>
        /// <returns>The loaded texture, or null if loading failed.</returns>
        public static Texture2D LoadTextureFromBytes(
            byte[] imageData,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            try
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(texture, imageData, markNonReadable: false))
                {
                    texture.filterMode = filterMode;
                    texture.wrapMode = wrapMode;
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load texture from bytes: {ex}");
            }

            return null;
        }

        /// <summary>
        /// Loads a texture from a file path.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <param name="filterMode">The filter mode for the texture.</param>
        /// <param name="wrapMode">The wrap mode for the texture.</param>
        /// <returns>The loaded texture, or null if loading failed.</returns>
        public static Texture2D LoadTextureFromFile(
            string filePath,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            if (!File.Exists(filePath))
            {
                Logger.Error($"Texture file not found: {filePath}");
                return null;
            }

            try
            {
                byte[] imageData = File.ReadAllBytes(filePath);
                return LoadTextureFromBytes(imageData, filterMode, wrapMode);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load texture from file '{filePath}': {ex}");
                return null;
            }
        }
    }
}

