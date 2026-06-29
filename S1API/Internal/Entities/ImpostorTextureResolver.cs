#if (IL2CPPMELON)
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Entities.Impostors;
using S1API.Logging;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal static class ImpostorTextureResolver
    {
        private const string CharacterSettingsPath = "charactersettings";
        private static readonly Log Logger = new Log("ImpostorTextureResolver");
        private static readonly string[] KnownImpostorNames =
        {
            "Albert",
            "Alison",
            "Andy",
            "Anna",
            "Austin",
            "Benji",
            "BenziesGoon1",
            "BenziesGoon2",
            "BenziesGoon3",
            "BenziesGoon4",
            "Beth",
            "Billy",
            "Botanist",
            "Brad",
            "Carl",
            "Charles",
            "Chemist",
            "Chloe",
            "Chris",
            "Cleaner",
            "Dan",
            "Dean",
            "Dennis",
            "Donna",
            "Doris",
            "Doug",
            "Elizabeth",
            "Eugene",
            "Fiona",
            "Fixer",
            "Frank",
            "Franklin",
            "Genghis",
            "George",
            "Geraldine",
            "Greg",
            "Harold",
            "Herbert",
            "Igor",
            "Jack",
            "Jackie",
            "Jacob",
            "Jane",
            "Javier",
            "Jeff",
            "Jen",
            "Jennifer",
            "Jeremy",
            "Jerry",
            "Jessi",
            "Joe",
            "Joel",
            "Joyce",
            "Karen",
            "Kathy",
            "Keith",
            "Kevin",
            "Kim",
            "Kyle",
            "Leo",
            "Lily",
            "Lisa",
            "LoanShark1",
            "LoanShark2",
            "Louis",
            "Lucy",
            "Ludwig",
            "Mac",
            "Mario",
            "Mayor",
            "Meg",
            "Melissa",
            "Michael",
            "Mick",
            "Molly",
            "Nelson",
            "OfficerBailey",
            "OfficerCooper",
            "OfficerDavis",
            "OfficerGreen",
            "OfficerHoward",
            "OfficerJackson",
            "OfficerLee",
            "OfficerLeo",
            "OfficerLopez",
            "OfficerMurphy",
            "OfficerOakley",
            "OfficerSanchez",
            "Oscar",
            "Packager",
            "Pam",
            "Pearl",
            "Peggy",
            "Peter",
            "Philip",
            "Randy",
            "Ray",
            "Red",
            "Salvador",
            "Sam",
            "Shirley",
            "Shit1",
            "Stan",
            "Steve",
            "Thomas",
            "Trent",
            "Tyler",
            "Walter",
            "Wei",
            "Wong",
            "WW"
        };

        public static IReadOnlyList<AvatarImpostorDefinition> GetAllDefinitions()
        {
            var definitions = new List<AvatarImpostorDefinition>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddDefinitionsFromNpcRegistry(definitions, seen);

            try
            {
                var settings = Resources.LoadAll<S1AvatarFramework.AvatarSettings>(CharacterSettingsPath);
                if (settings != null)
                {
                    foreach (var setting in settings)
                    {
                        TryAddDefinition(definitions, seen, setting);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load avatar impostor catalog: {ex.Message}");
            }

            foreach (string name in KnownImpostorNames)
            {
                if (seen.Contains(name))
                {
                    continue;
                }

                Texture2D? texture = LoadTextureFromCharacterSettings(name);
                if (texture != null && seen.Add(name))
                {
                    definitions.Add(new AvatarImpostorDefinition(
                        name,
                        $"{CharacterSettingsPath}/{name}",
                        texture));
                }
            }

            definitions.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
            return definitions;
        }

        public static AvatarImpostorDefinition? FindDefinition(string? nameOrPath)
        {
            string normalized = NormalizeName(nameOrPath);
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            AvatarImpostorDefinition? found = GetAllDefinitions()
                .FirstOrDefault(definition =>
                    string.Equals(definition.Name, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(definition.ResourcePath, nameOrPath, StringComparison.OrdinalIgnoreCase));

            if (found != null)
            {
                return found;
            }

            Texture2D? texture = LoadTextureFromCharacterSettings(normalized);
            return texture == null
                ? null
                : new AvatarImpostorDefinition(normalized, $"{CharacterSettingsPath}/{normalized}", texture);
        }

        public static bool TryResolve(
            AvatarImpostorSelection? selection,
            string deterministicKey,
            out Texture2D? texture)
        {
            texture = null;
            if (selection == null || selection.Kind == AvatarImpostorSelectionKind.Preserve)
            {
                return false;
            }

            switch (selection.Kind)
            {
                case AvatarImpostorSelectionKind.Texture:
                    texture = selection.Texture;
                    return texture != null;

                case AvatarImpostorSelectionKind.Definition:
                    texture = selection.Definition?.Texture;
                    return texture != null;

                case AvatarImpostorSelectionKind.Name:
                    texture = FindDefinition(selection.Name)?.Texture;
                    if (texture == null)
                    {
                        Logger.Warning($"Could not resolve avatar impostor '{selection.Name}'.");
                    }

                    return texture != null;

                case AvatarImpostorSelectionKind.Random:
                    texture = ResolveRandom(selection, deterministicKey);
                    return texture != null;

                default:
                    return false;
            }
        }

        private static Texture2D? ResolveRandom(AvatarImpostorSelection selection, string deterministicKey)
        {
            var definitions = ResolveRandomPool(selection.Names);
            if (definitions.Count == 0)
            {
                Logger.Warning("Could not resolve any avatar impostors for random selection.");
                return null;
            }

            int seed = selection.Seed ?? CreateStableSeed(deterministicKey);
            int index = new System.Random(seed).Next(definitions.Count);
            return definitions[index].Texture;
        }

        private static List<AvatarImpostorDefinition> ResolveRandomPool(IReadOnlyList<string>? names)
        {
            if (names == null || names.Count == 0)
            {
                return GetAllDefinitions().ToList();
            }

            var definitions = new List<AvatarImpostorDefinition>();
            foreach (string name in names)
            {
                AvatarImpostorDefinition? definition = FindDefinition(name);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
                else
                {
                    Logger.Warning($"Could not resolve avatar impostor '{name}' for random selection.");
                }
            }

            return definitions;
        }

        private static void AddDefinitionsFromNpcRegistry(
            List<AvatarImpostorDefinition> definitions,
            HashSet<string> seen)
        {
            try
            {
                if (!S1NPCs.NPCManager.InstanceExists || S1NPCs.NPCManager.NPCRegistry == null)
                {
                    return;
                }

                foreach (var npc in S1NPCs.NPCManager.NPCRegistry.ToArray())
                {
                    if (npc == null)
                    {
                        continue;
                    }

                    S1AvatarFramework.AvatarSettings? settings =
                        npc.Avatar?.CurrentSettings ??
                        (npc.Avatar != null
                            ? ReflectionUtils.TryGetFieldOrProperty(npc.Avatar, "InitialAvatarSettings") as S1AvatarFramework.AvatarSettings
                            : null);
                    if (settings == null || settings.ImpostorTexture == null)
                    {
                        continue;
                    }

                    string name = NormalizeName(npc.FirstName);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = NormalizeName(settings.name);
                    }

                    if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                    {
                        continue;
                    }

                    definitions.Add(new AvatarImpostorDefinition(
                        name,
                        $"{CharacterSettingsPath}/{name}",
                        settings.ImpostorTexture));
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to scan NPC registry for avatar impostors: {ex.Message}");
            }
        }

        private static void TryAddDefinition(
            List<AvatarImpostorDefinition> definitions,
            HashSet<string> seen,
            S1AvatarFramework.AvatarSettings? setting)
        {
            if (setting == null || setting.ImpostorTexture == null)
            {
                return;
            }

            string name = setting.name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
            {
                return;
            }

            definitions.Add(new AvatarImpostorDefinition(
                name,
                $"{CharacterSettingsPath}/{name}",
                setting.ImpostorTexture));
        }

        private static Texture2D? LoadTextureFromCharacterSettings(string name)
        {
            try
            {
                var settings = Resources.Load<S1AvatarFramework.AvatarSettings>($"{CharacterSettingsPath}/{name}");
                return settings?.ImpostorTexture;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to load avatar impostor '{name}': {ex.Message}");
                return null;
            }
        }

        private static string NormalizeName(string? nameOrPath)
        {
            string value = (nameOrPath ?? string.Empty).Trim();
            if (value.StartsWith($"{CharacterSettingsPath}/", StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring(CharacterSettingsPath.Length + 1);
            }

            return value;
        }

        private static int CreateStableSeed(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in value ?? string.Empty)
                {
                    hash = (hash * 31) + c;
                }

                return hash;
            }
        }
    }
}
