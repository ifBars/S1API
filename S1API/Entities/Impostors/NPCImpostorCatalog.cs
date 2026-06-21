using System;
using System.Collections.Generic;
using S1API.Internal.Entities;

namespace S1API.Entities.Impostors
{
    /// <summary>
    /// Provides access to avatar impostors discovered from the game's runtime character settings resources.
    /// </summary>
    public static class NPCImpostorCatalog
    {
        /// <summary>
        /// Gets all currently discoverable game-owned NPC impostors.
        /// </summary>
        /// <returns>Definitions for character settings that contain an impostor texture.</returns>
        public static IReadOnlyList<AvatarImpostorDefinition> GetAll()
        {
            return ImpostorTextureResolver.GetAllDefinitions();
        }

        /// <summary>
        /// Finds an impostor by character settings name or resource path.
        /// </summary>
        /// <param name="name">Name such as <c>Kyle</c>, or path such as <c>charactersettings/Kyle</c>.</param>
        /// <param name="impostor">The matched impostor definition, if found.</param>
        /// <returns><c>true</c> when a matching impostor was found.</returns>
        public static bool TryFind(string name, out AvatarImpostorDefinition? impostor)
        {
            impostor = ImpostorTextureResolver.FindDefinition(name);
            return impostor != null;
        }

        /// <summary>
        /// Finds an impostor by character settings name or resource path.
        /// </summary>
        /// <param name="name">Name such as <c>Kyle</c>, or path such as <c>charactersettings/Kyle</c>.</param>
        /// <returns>The matched impostor definition, or <c>null</c> if no match was found.</returns>
        public static AvatarImpostorDefinition? Find(string name)
        {
            return ImpostorTextureResolver.FindDefinition(name);
        }

        /// <summary>
        /// Selects a deterministic random impostor from all currently discoverable impostors.
        /// </summary>
        /// <param name="seed">Seed used to choose the impostor.</param>
        /// <returns>A discovered impostor definition, or <c>null</c> when none are available.</returns>
        public static AvatarImpostorDefinition? GetRandom(int seed)
        {
            IReadOnlyList<AvatarImpostorDefinition> all = GetAll();
            if (all.Count == 0)
            {
                return null;
            }

            int index = new Random(seed).Next(all.Count);
            return all[index];
        }
    }
}
