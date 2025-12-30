using System.Reflection;
using UnityEngine;

namespace S1API.Utils
{
    /// <summary>
    /// A utility class to assist with loading images into the game.
    /// Useful for icons such as on phone apps, custom NPCs, quests, etc.
    /// This class is intended for public use by mod developers.
    /// </summary>
    public static class ImageUtils
    {
        /// <summary>
        /// Loads an image from the specified file path and converts it into a Sprite object.
        /// </summary>
        /// <param name="fileName">The name of the file (with path) containing the image to load.</param>
        /// <returns>
        /// A Sprite object representing the loaded image, or null if the image could not be loaded or the file does not exist.
        /// </returns>
        public static Sprite? LoadImage(string fileName) =>
            Internal.Utils.ImageUtils.LoadImage(fileName);

        /// <summary>
        /// Loads an image from a byte array and converts it into a Sprite object.
        /// </summary>
        /// <param name="data">The byte array containing the image data to load.</param>
        /// <returns>
        /// A Sprite object representing the loaded image, or null if the image could not be loaded.
        /// </returns>
        public static Sprite? LoadImageRaw(byte[] data) =>
            Internal.Utils.ImageUtils.LoadImageRaw(data);

        /// <summary>
        /// Converts a Texture2D to a Sprite.
        /// </summary>
        /// <param name="texture">The texture to convert.</param>
        /// <param name="pixelsPerUnit">The pixels per unit for the sprite. Defaults to 100f.</param>
        /// <returns>A Sprite object, or null if texture is null.</returns>
        public static Sprite? TextureToSprite(Texture2D? texture, float pixelsPerUnit = 100f) =>
            Internal.Utils.ImageUtils.TextureToSprite(texture, pixelsPerUnit);

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
        public static Sprite? LoadImageFromResource(Assembly assembly, string resourceName, float pixelsPerUnit = 100f, FilterMode filterMode = FilterMode.Bilinear) =>
            Internal.Utils.ImageUtils.LoadImageFromResource(assembly, resourceName, pixelsPerUnit, filterMode);
    }
}

