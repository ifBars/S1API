using System.Collections.Generic;
using S1API.Entities.Impostors;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal enum AvatarImpostorSelectionKind
    {
        Preserve,
        Definition,
        Name,
        Texture,
        Random
    }

    internal sealed class AvatarImpostorSelection
    {
        private AvatarImpostorSelection(
            AvatarImpostorSelectionKind kind,
            AvatarImpostorDefinition? definition = null,
            string? name = null,
            Texture2D? texture = null,
            IReadOnlyList<string>? names = null,
            int? seed = null)
        {
            Kind = kind;
            Definition = definition;
            Name = name;
            Texture = texture;
            Names = names;
            Seed = seed;
        }

        public AvatarImpostorSelectionKind Kind { get; }

        public AvatarImpostorDefinition? Definition { get; }

        public string? Name { get; }

        public Texture2D? Texture { get; }

        public IReadOnlyList<string>? Names { get; }

        public int? Seed { get; }

        public static AvatarImpostorSelection Preserve()
        {
            return new AvatarImpostorSelection(AvatarImpostorSelectionKind.Preserve);
        }

        public static AvatarImpostorSelection FromDefinition(AvatarImpostorDefinition definition)
        {
            return new AvatarImpostorSelection(AvatarImpostorSelectionKind.Definition, definition: definition);
        }

        public static AvatarImpostorSelection FromName(string name)
        {
            return new AvatarImpostorSelection(AvatarImpostorSelectionKind.Name, name: name);
        }

        public static AvatarImpostorSelection FromTexture(Texture2D texture)
        {
            return new AvatarImpostorSelection(AvatarImpostorSelectionKind.Texture, texture: texture);
        }

        public static AvatarImpostorSelection Random(IReadOnlyList<string>? names, int? seed)
        {
            return new AvatarImpostorSelection(AvatarImpostorSelectionKind.Random, names: names, seed: seed);
        }
    }
}
