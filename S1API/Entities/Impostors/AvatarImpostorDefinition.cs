using UnityEngine;

namespace S1API.Entities.Impostors
{
    /// <summary>
    /// Describes an avatar impostor texture discovered from the game's character settings resources.
    /// </summary>
    /// <remarks>
    /// The texture is owned by the game runtime. S1API exposes the name and resource path so mods can choose
    /// an existing impostor without bundling or redistributing base-game assets.
    /// </remarks>
    public sealed class AvatarImpostorDefinition
    {
        internal AvatarImpostorDefinition(string name, string resourcePath, Texture2D texture)
        {
            Name = name;
            ResourcePath = resourcePath;
            Texture = texture;
        }

        /// <summary>
        /// Gets the character settings resource name, such as <c>Kyle</c> or <c>Mick</c>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the Unity Resources path for the character settings asset.
        /// </summary>
        public string ResourcePath { get; }

        internal Texture2D Texture { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} ({ResourcePath})";
        }
    }
}
