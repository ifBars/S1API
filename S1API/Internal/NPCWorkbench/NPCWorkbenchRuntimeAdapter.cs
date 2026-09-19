#if IL2CPPMELON
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Entities;
using S1API.Internal.Entities;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Internal.NPCWorkbench
{
    internal sealed class NPCWorkbenchSourceOption
    {
        internal NPCWorkbenchSourceOption(string id, string displayName, bool isCustom)
        {
            Id = id;
            DisplayName = displayName;
            IsCustom = isCustom;
        }

        internal string Id { get; }
        internal string DisplayName { get; }
        internal bool IsCustom { get; }
    }

    internal static class NPCWorkbenchRuntimeAdapter
    {
        internal static IReadOnlyList<NPCWorkbenchSourceOption> GetSources(bool custom)
        {
            if (!custom)
                return GetNativeSources();

            return NPC.All
                .Where(npc => npc != null && npc.IsCustomNPC)
                .OrderBy(npc => npc.ID, StringComparer.OrdinalIgnoreCase)
                .Select(npc => new NPCWorkbenchSourceOption(
                    npc.ID,
                    $"{npc.FullName} ({npc.ID})",
                    isCustom: true))
                .ToArray();
        }

        internal static NPCWorkbenchDraft Import(string id, bool custom)
        {
            return custom ? ImportCustom(id) : ImportNative(id);
        }

        private static NPCWorkbenchDraft ImportCustom(string id)
        {
            var npc = NPC.All.FirstOrDefault(candidate =>
                candidate != null &&
                candidate.IsCustomNPC &&
                string.Equals(candidate.ID, id, StringComparison.OrdinalIgnoreCase));
            if (npc == null)
                throw new InvalidOperationException($"S1API NPC '{id}' is not available in the current scene.");

            var draft = new NPCWorkbenchDraft
            {
                SourceKind = NPCWorkbenchSourceKind.S1API,
                SourceId = npc.ID,
                SourceDisplayName = $"{npc.FullName} ({npc.ID})"
            };

            using (var settings = new AvatarSettingsScope(npc.Appearance.CreateSettingsSnapshot()))
                CopyAppearance(settings.Value, draft.Appearance);

            var identity = npc.S1NPC.gameObject.GetComponent<NPCPrefabIdentity>() ??
                           npc.S1NPC.gameObject.GetComponentInChildren<NPCPrefabIdentity>(true);
            var impostor = identity?.AppearanceImpostorSelection;
            if (impostor?.Kind == AvatarImpostorSelectionKind.Name)
                draft.Appearance.ImpostorId = impostor.Name;
            else if (impostor?.Kind == AvatarImpostorSelectionKind.Definition)
                draft.Appearance.ImpostorId = impostor.Definition?.Name;

            return draft;
        }

        private static NPCWorkbenchDraft ImportNative(string id)
        {
            var npc = S1NPCs.NPCManager.NPCRegistry.ToArray().FirstOrDefault(candidate =>
                candidate != null &&
                string.Equals(NPCDataAccess.GetId(candidate), id, StringComparison.OrdinalIgnoreCase) &&
                !IsCustom(candidate));
            if (npc == null)
                throw new InvalidOperationException($"Native NPC '{id}' is not available in the current scene.");

            var avatar = npc.Avatar ?? npc.gameObject.GetComponentInChildren<S1AvatarFramework.Avatar>(true);
            S1AvatarFramework.AvatarSettings? sourceSettings = null;
            if (sourceSettings == null && avatar != null)
            {
                sourceSettings = ReflectionUtils.TryGetFieldOrProperty(
                    avatar,
                    "InitialAvatarSettings") as S1AvatarFramework.AvatarSettings;
            }
            if (sourceSettings == null)
                throw new InvalidOperationException($"Native NPC '{id}' has no available appearance settings.");

            var draft = new NPCWorkbenchDraft
            {
                SourceKind = NPCWorkbenchSourceKind.Native,
                SourceId = id,
                SourceDisplayName = $"{GetDisplayName(npc)} ({id})"
            };

            using (var settings = new AvatarSettingsScope(ScriptableObject.Instantiate(sourceSettings)))
                CopyAppearance(settings.Value, draft.Appearance);

            return draft;
        }

        private static IReadOnlyList<NPCWorkbenchSourceOption> GetNativeSources()
        {
            return S1NPCs.NPCManager.NPCRegistry.ToArray()
                .Where(npc =>
                    npc != null &&
                    !IsCustom(npc) &&
                    !string.IsNullOrWhiteSpace(NPCDataAccess.GetId(npc)))
                .GroupBy(NPCDataAccess.GetId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(NPCDataAccess.GetId, StringComparer.OrdinalIgnoreCase)
                .Select(npc =>
                {
                    var id = NPCDataAccess.GetId(npc);
                    return new NPCWorkbenchSourceOption(
                        id,
                        $"{GetDisplayName(npc)} ({id})",
                        isCustom: false);
                })
                .ToArray();
        }

        private static bool IsCustom(S1NPCs.NPC npc)
        {
            if (npc.gameObject.GetComponent<NPCPrefabIdentity>() != null)
                return true;

            return NPC.All.Any(wrapper =>
                wrapper != null &&
                wrapper.IsCustomNPC &&
                wrapper.S1NPC == npc);
        }

        private static string GetDisplayName(S1NPCs.NPC npc)
        {
            var firstName = NPCDataAccess.GetFirstName(npc);
            var lastName = NPCDataAccess.GetLastName(npc);
            var fullName = string.Join(
                " ",
                new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
            return string.IsNullOrWhiteSpace(fullName)
                ? NPCDataAccess.GetId(npc)
                : fullName;
        }

        internal static S1AvatarFramework.AvatarSettings CreateSettings(
            NPCWorkbenchDraft draft,
            S1AvatarFramework.AvatarSettings? template)
        {
            var settings = template != null
                ? ScriptableObject.Instantiate(template)
                : ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
            var appearance = draft.Appearance;

            settings.Gender = appearance.Gender;
            settings.Height = appearance.Height;
            settings.Weight = appearance.Weight;
            settings.SkinColor = ToColor(appearance.SkinColor);
            settings.LeftEyeLidColor = ToColor(appearance.LeftEyeLidColor);
            settings.RightEyeLidColor = ToColor(appearance.RightEyeLidColor);
            settings.EyeBallTint = ToColor(appearance.EyeBallTint);
            settings.EyeballMaterialIdentifier = appearance.EyeballMaterialIdentifier;
            settings.PupilDilation = appearance.PupilDilation;
            settings.EyebrowScale = appearance.EyebrowScale;
            settings.EyebrowThickness = appearance.EyebrowThickness;
            settings.EyebrowRestingHeight = appearance.EyebrowRestingHeight;
            settings.EyebrowRestingAngle = appearance.EyebrowRestingAngle;
            settings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = appearance.LeftEye.TopLidOpen,
                bottomLidOpen = appearance.LeftEye.BottomLidOpen
            };
            settings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = appearance.RightEye.TopLidOpen,
                bottomLidOpen = appearance.RightEye.BottomLidOpen
            };
            settings.HairPath = appearance.HairPath;
            settings.HairColor = ToColor(appearance.HairColor);

            settings.FaceLayerSettings.Clear();
            foreach (var layer in appearance.FaceLayers)
            {
                settings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                {
                    layerPath = layer.Path,
                    layerTint = ToColor(layer.Color)
                });
            }

            settings.BodyLayerSettings.Clear();
            foreach (var layer in appearance.BodyLayers)
            {
                settings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                {
                    layerPath = layer.Path,
                    layerTint = ToColor(layer.Color)
                });
            }

            settings.AccessorySettings.Clear();
            foreach (var layer in appearance.Accessories)
            {
                settings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
                {
                    path = layer.Path,
                    color = ToColor(layer.Color)
                });
            }

            return settings;
        }

        private static void CopyAppearance(
            S1AvatarFramework.AvatarSettings settings,
            NPCWorkbenchAppearance appearance)
        {
            appearance.Gender = settings.Gender;
            appearance.Height = settings.Height;
            appearance.Weight = settings.Weight;
            appearance.SkinColor = FromColor(settings.SkinColor);
            appearance.LeftEyeLidColor = FromColor(settings.LeftEyeLidColor);
            appearance.RightEyeLidColor = FromColor(settings.RightEyeLidColor);
            appearance.EyeBallTint = FromColor(settings.EyeBallTint);
            appearance.EyeballMaterialIdentifier = settings.EyeballMaterialIdentifier ?? string.Empty;
            appearance.PupilDilation = settings.PupilDilation;
            appearance.EyebrowScale = settings.EyebrowScale;
            appearance.EyebrowThickness = settings.EyebrowThickness;
            appearance.EyebrowRestingHeight = settings.EyebrowRestingHeight;
            appearance.EyebrowRestingAngle = settings.EyebrowRestingAngle;
            appearance.LeftEye = new NPCWorkbenchEyeSettings(
                settings.LeftEyeRestingState.topLidOpen,
                settings.LeftEyeRestingState.bottomLidOpen);
            appearance.RightEye = new NPCWorkbenchEyeSettings(
                settings.RightEyeRestingState.topLidOpen,
                settings.RightEyeRestingState.bottomLidOpen);
            appearance.HairPath = settings.HairPath ?? string.Empty;
            appearance.HairColor = FromColor(settings.HairColor);

            appearance.FaceLayers.Clear();
            foreach (var layer in settings.FaceLayerSettings)
                appearance.FaceLayers.Add(new NPCWorkbenchLayer
                {
                    Path = layer.layerPath ?? string.Empty,
                    Color = FromColor(layer.layerTint)
                });

            appearance.BodyLayers.Clear();
            foreach (var layer in settings.BodyLayerSettings)
                appearance.BodyLayers.Add(new NPCWorkbenchLayer
                {
                    Path = layer.layerPath ?? string.Empty,
                    Color = FromColor(layer.layerTint)
                });

            appearance.Accessories.Clear();
            foreach (var layer in settings.AccessorySettings)
                appearance.Accessories.Add(new NPCWorkbenchLayer
                {
                    Path = layer.path ?? string.Empty,
                    Color = FromColor(layer.color)
                });
        }

        private static Color ToColor(NPCWorkbenchColor value) =>
            new Color32(value.Red, value.Green, value.Blue, value.Alpha);

        private static NPCWorkbenchColor FromColor(Color value)
        {
            var color = (Color32)value;
            return new NPCWorkbenchColor(color.r, color.g, color.b, color.a);
        }

        private sealed class AvatarSettingsScope : IDisposable
        {
            internal AvatarSettingsScope(S1AvatarFramework.AvatarSettings value)
            {
                Value = value;
            }

            internal S1AvatarFramework.AvatarSettings Value { get; }

            public void Dispose()
            {
                if (Value != null)
                    UnityEngine.Object.Destroy(Value);
            }
        }
    }
}
