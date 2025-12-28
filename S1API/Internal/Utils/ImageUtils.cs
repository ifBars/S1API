using S1API.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// A utility class to assist with loading images into the game.
    /// Useful for icons such as on phone apps, custom NPCs, quests, etc.
    /// </summary>
    public static class ImageUtils
    {
        private static readonly Log _loggerInstance = new Log("ImageUtils");

        /// <summary>
        /// Loads an image from the specified file path and converts it into a Sprite object.
        /// </summary>
        /// <param name="fileName">The name of the file (with path) containing the image to load.</param>
        /// <returns>
        /// A Sprite object representing the loaded image, or null if the image could not be loaded or the file does not exist.
        /// </returns>
        public static Sprite? LoadImage(string fileName)
        {
            string fullPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty, fileName);
            if (!File.Exists(fullPath))
            {
                _loggerInstance.Error($"❌ Icon file not found: {fullPath}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(fullPath);
                return LoadImageRaw(data);
            }
            catch (System.Exception ex)
            {
                _loggerInstance.Error("❌ Failed to load sprite: " + ex);
            }

            return null;
        }
        
        /// <summary>
        /// Loads an image from a byte array and converts it into a Sprite object.
        /// </summary>
        /// <param name="data">The byte array containing the image data to load.</param>
        /// <returns>
        /// A Sprite object representing the loaded image, or null if the image could not be loaded.
        /// </returns>
        public static Sprite? LoadImageRaw(byte[] data)
        {
            try
            {
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(data))
                {
                    return TextureToSprite(tex);
                }
            }
            catch (System.Exception ex)
            {
                _loggerInstance.Error("❌ Failed to load sprite: " + ex);
            }
            return null;
        }

        /// <summary>
        /// Converts a Texture2D to a Sprite.
        /// </summary>
        /// <param name="texture">The texture to convert.</param>
        /// <param name="pixelsPerUnit">The pixels per unit for the sprite. Defaults to 100f.</param>
        /// <returns>A Sprite object, or null if texture is null.</returns>
        public static Sprite? TextureToSprite(Texture2D? texture, float pixelsPerUnit = 100f)
        {
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        /// <summary>
        /// Loads an image from an embedded resource stream and converts it into a Sprite object.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The fully qualified name of the embedded resource (e.g., "Namespace.Assets.Icon.png").</param>
        /// <param name="pixelsPerUnit">The pixels per unit for the sprite. Defaults to 100f.</param>
        /// <param name="filterMode">The filter mode for the texture. Defaults to FilterMode.Bilinear.</param>
        /// <returns>
        /// A Sprite object representing the loaded image, or null if the resource could not be found or loaded.
        /// </returns>
        public static Sprite? LoadImageFromResource(Assembly assembly, string resourceName, float pixelsPerUnit = 100f, FilterMode filterMode = FilterMode.Bilinear)
        {
            if (assembly == null)
            {
                _loggerInstance.Error($"❌ Assembly is null when loading resource: {resourceName}");
                return null;
            }

            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        _loggerInstance.Error($"❌ Resource not found: {resourceName}");
                        return null;
                    }

                    byte[] imageData = new byte[stream.Length];
                    stream.Read(imageData, 0, imageData.Length);

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (UnityEngine.ImageConversion.LoadImage(texture, imageData, markNonReadable: false))
                    {
                        texture.filterMode = filterMode;
                        return Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            pixelsPerUnit
                        );
                    }
                }
            }
            catch (System.Exception ex)
            {
                _loggerInstance.Error($"❌ Failed to load sprite from resource '{resourceName}': {ex}");
            }

            return null;
        }
    }
}
