#if IL2CPPMELON
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1AvatarTools = Il2CppScheduleOne.Avatar.Tools;
using S1Avatar = Il2CppScheduleOne.Avatar;
using S1CoreAvatar = Il2CppScheduleOne.Core.Avatar;
using S1CharacterCreator = Il2CppScheduleOne.CharacterCreator.CharacterCreator;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1AvatarTools = ScheduleOne.Avatar.Tools;
using S1Avatar = ScheduleOne.Avatar;
using S1CoreAvatar = ScheduleOne.Core.Avatar;
using S1CharacterCreator = ScheduleOne.CharacterCreator.CharacterCreator;
#endif

using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Compatibility
{
    internal static class AvatarCompatibility
    {
        private static readonly object MugshotLock = new object();
        private static bool _mugshotInUse;
        private static S1AvatarTools.MugshotGenerator? _fallbackMugshotGenerator;
        private static object? _captureCoroutine;
        internal static int RenderingEpoch { get; private set; }

        internal static void ResetRendering()
        {
            RenderingEpoch++;
            if (_captureCoroutine != null)
            {
                MelonLoader.MelonCoroutines.Stop(_captureCoroutine);
                _captureCoroutine = null;
            }
            if (_fallbackMugshotGenerator != null)
                Object.Destroy(_fallbackMugshotGenerator.gameObject);
            _fallbackMugshotGenerator = null;
            ReleaseMugshotGenerator();
        }

        internal static void StartPortraitCapture(
            S1AvatarTools.MugshotGenerator generator,
            S1CoreAvatar.NakedAppearance appearance,
            S1CoreAvatar.Outfit outfit,
            Action<Texture2D> callback)
        {
            _captureCoroutine = MelonLoader.MelonCoroutines.Start(
                CapturePortrait(generator, appearance, outfit, callback));
        }

        internal static S1AvatarTools.MugshotGenerator? FindMugshotGenerator(
            S1AvatarFramework.Avatar? preferredAvatar = null)
        {
            var active = Object.FindObjectOfType<S1AvatarTools.MugshotGenerator>();
            if (active != null)
                return active;

            foreach (var candidate in Resources.FindObjectsOfTypeAll<S1AvatarTools.MugshotGenerator>())
            {
                if (candidate != null && candidate.gameObject.scene.IsValid())
                    return candidate;
            }

            if (_fallbackMugshotGenerator != null)
                return _fallbackMugshotGenerator;

            foreach (var creator in Resources.FindObjectsOfTypeAll<S1CharacterCreator>())
            {
                var creatorAvatar = Utils.ReflectionUtils.TryGetFieldOrProperty(creator, "_avatar")
                    as S1AvatarFramework.Avatar;
                var sourceCamera = Utils.ReflectionUtils.TryGetFieldOrProperty(creator, "_cameraPosition")
                    as Transform;
                if (creatorAvatar == null || sourceCamera == null)
                    continue;

                var sourceAvatar = preferredAvatar ?? FindRuntimeAvatar() ?? creatorAvatar;

                var root = new GameObject("S1API Avatar Rendering Rig")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                Object.DontDestroyOnLoad(root);

                var cameraForward = sourceCamera.forward;
                var horizontalForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;
                if (horizontalForward.sqrMagnitude < 0.01f)
                    horizontalForward = Vector3.forward;
                root.transform.position =
                    sourceCamera.position + horizontalForward * 1.4f - Vector3.up * 1.55f;
                root.transform.rotation = Quaternion.LookRotation(-horizontalForward, Vector3.up);

                var avatarObject = Object.Instantiate(sourceAvatar.gameObject);
                avatarObject.name = "Avatar";
                avatarObject.hideFlags = HideFlags.HideAndDontSave;
                avatarObject.transform.SetParent(root.transform, false);
                avatarObject.transform.localPosition = Vector3.zero;
                avatarObject.transform.localRotation = Quaternion.identity;
                avatarObject.SetActive(true);

                var cameraObject = new GameObject("Camera Position")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                cameraObject.transform.SetParent(root.transform, false);
                cameraObject.transform.position = sourceCamera.position;
                cameraObject.transform.rotation = Quaternion.LookRotation(
                    root.transform.position + Vector3.up * 1.45f - sourceCamera.position,
                    Vector3.up);

                AddPreviewLight(
                    root.transform,
                    "Key Light",
                    Quaternion.Euler(28f, 205f, 0f),
                    0.65f,
                    new Color(1f, 0.9f, 0.82f));
                AddPreviewLight(
                    root.transform,
                    "Fill Light",
                    Quaternion.Euler(20f, 25f, 0f),
                    0.35f,
                    new Color(0.7f, 0.82f, 1f));
                AddCameraFillLight(root.transform, cameraObject.transform);

                var generator = root.AddComponent<S1AvatarTools.MugshotGenerator>();
                var avatar = avatarObject.GetComponent<S1AvatarFramework.Avatar>();
                if (avatar != null)
                    PrepareDetachedAvatar(avatar);
                if (avatar == null ||
                    !Utils.ReflectionUtils.TrySetFieldOrProperty(generator, "_avatar", avatar) ||
                    !Utils.ReflectionUtils.TrySetFieldOrProperty(
                        generator,
                        "_cameraPosition",
                        cameraObject.transform))
                {
                    Object.Destroy(root);
                    return null;
                }

                _fallbackMugshotGenerator = generator;
                return generator;
            }

            return null;
        }

        private static S1AvatarFramework.Avatar? FindRuntimeAvatar()
        {
            foreach (var avatar in Object.FindObjectsOfType<S1AvatarFramework.Avatar>())
            {
                if (avatar != null && avatar.gameObject.activeInHierarchy)
                    return avatar;
            }

            return null;
        }

        internal static void PrepareDetachedAvatar(S1AvatarFramework.Avatar avatar)
        {
            // Serialized mesh blend weights survive cloning; the native value caches do not.
            Utils.ReflectionUtils.TrySetFieldOrProperty(avatar.Appearance, "_appliedGender", -1f);
            Utils.ReflectionUtils.TrySetFieldOrProperty(avatar.Appearance, "_appliedWeight", -1f);
            foreach (var loader in avatar.GetComponentsInChildren<S1AvatarTools.OutfitLoader>(true))
                loader.enabled = false;
            foreach (var loader in avatar.GetComponentsInChildren<S1AvatarTools.NakedAppearanceLoader>(true))
                loader.enabled = false;
            // Runtime clones must not follow the player or switch to a distance impostor.
            avatar.CancelInvoke("UpdateAnimationActive");
            avatar.SetVisible(true);
            if (Utils.ReflectionUtils.TryGetFieldOrProperty(avatar, "_impostor") is Component impostor)
                impostor.gameObject.SetActive(false);

            if (avatar.LookController != null)
            {
                avatar.LookController.CancelInvoke();
                avatar.LookController.enabled = false;
                if (Utils.ReflectionUtils.TryGetFieldOrProperty(avatar.LookController, "Aim") is Behaviour aim)
                    aim.enabled = false;
            }
            if (avatar.Eyes != null)
                Utils.ReflectionUtils.TrySetFieldOrProperty(avatar.Eyes, "blinkingEnabled", false);
            if (avatar.EmotionManager != null)
            {
                avatar.EmotionManager.CancelInvoke();
                avatar.EmotionManager.enabled = false;
            }

            // Unity clones the hierarchy but not AvatarAppearance's private runtime lists.
            // Remove copied objects so subsequent appearance applications own every instance.
            foreach (var attachment in avatar.GetComponentsInChildren<S1CoreAvatar.AvatarAttachment>(true))
            {
                attachment.gameObject.SetActive(false);
                Object.Destroy(attachment.gameObject);
            }
            if (Utils.ReflectionUtils.TryGetFieldOrProperty(avatar.Appearance, "_avatarObjectsContainer")
                is Transform objects)
            {
                for (var index = objects.childCount - 1; index >= 0; index--)
                {
                    var child = objects.GetChild(index).gameObject;
                    child.SetActive(false);
                    Object.Destroy(child);
                }
            }

            foreach (var animator in avatar.GetComponentsInChildren<Animator>(true))
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
            }
            foreach (var lod in avatar.GetComponentsInChildren<LODGroup>(true))
                lod.ForceLOD(0);
            foreach (var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
        }

        private static void AddPreviewLight(
            Transform parent,
            string name,
            Quaternion rotation,
            float intensity,
            Color color)
        {
            var lightObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = 20
            };
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = rotation;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.cullingMask = 1 << 20;
            light.shadows = LightShadows.None;
        }

        private static void AddCameraFillLight(Transform parent, Transform cameraPosition)
        {
            var lightObject = new GameObject("Camera Fill Light")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = 20
            };
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = cameraPosition.position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 0.25f;
            light.color = new Color(1f, 0.95f, 0.9f);
            light.cullingMask = 1 << 20;
            light.shadows = LightShadows.None;
        }

        internal static bool TryAcquireMugshotGenerator(S1AvatarTools.MugshotGenerator generator)
        {
            lock (MugshotLock)
            {
                if (_mugshotInUse ||
                    Utils.ReflectionUtils.TryGetFieldOrProperty(generator, "_mugshotRoutine") != null)
                {
                    return false;
                }

                _mugshotInUse = true;
                return true;
            }
        }

        private static IEnumerator CapturePortrait(
            S1AvatarTools.MugshotGenerator generator,
            S1CoreAvatar.NakedAppearance appearance,
            S1CoreAvatar.Outfit outfit,
            Action<Texture2D> callback)
        {
            var avatar = Utils.ReflectionUtils.TryGetFieldOrProperty(generator, "_avatar")
                as S1AvatarFramework.Avatar;
            if (avatar == null)
                yield break;

            var cameraObject = new GameObject("S1API Portrait Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            var delivered = false;
            try
            {
                var normalized = appearance.Clone();
                normalized.Height = 1.875f;
                avatar.Appearance.ApplyNakedAppearance(normalized);
                avatar.Appearance.ApplyOutfit(outfit);
                avatar.SetVisible(true);
                foreach (var transform in avatar.GetComponentsInChildren<Transform>(true))
                    transform.gameObject.layer = 20;
                foreach (var lod in avatar.GetComponentsInChildren<LODGroup>(true))
                    lod.ForceLOD(0);
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << 20;
                camera.orthographic = true;
                camera.orthographicSize = 0.34f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 5f;
                var focus = avatar.transform.position + Vector3.up * 1.7f;
                camera.transform.position = focus + avatar.transform.forward * 2f;
                camera.transform.LookAt(focus);
                target.Create();
                camera.targetTexture = target;
                // Let both animation and deferred attachment destruction settle before capture.
                yield return null;
                yield return new WaitForEndOfFrame();
                camera.Render();
                var previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                    texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    texture.Apply();
                    delivered = true;
                    callback(texture);
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }
            finally
            {
                _captureCoroutine = null;
                camera.targetTexture = null;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(cameraObject);
                if (!delivered)
                    callback(null!);
            }
        }

        internal static void ReleaseMugshotGenerator()
        {
            lock (MugshotLock)
                _mugshotInUse = false;
        }

        internal static Texture2D ResizePortrait(Texture2D source, int size)
        {
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(
                    source,
                    target,
                    Vector2.one,
                    Vector2.zero);
                RenderTexture.active = target;
                var portrait = new Texture2D(size, size, TextureFormat.RGBA32, false);
                portrait.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                portrait.Apply();
                return portrait;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        internal static void ApplyLegacySettings(
            S1AvatarFramework.Avatar avatar,
            S1AvatarFramework.AvatarSettings settings)
        {
            if (avatar?.Appearance == null || settings == null)
                return;

            CreateRenderInputs(settings, out var nakedAppearance, out var outfit);
            avatar.Appearance.ApplyNakedAppearance(nakedAppearance);
            avatar.Appearance.ApplyOutfit(outfit);
            Object.Destroy(outfit);
        }

        internal static S1AvatarFramework.AvatarSettings? CaptureLegacySettings(S1AvatarFramework.Avatar avatar)
        {
            var appearance = avatar.Appearance?.AppliedNakedAppearance;
            if (appearance == null)
                return null;

            var settings = ScriptableObject.CreateInstance<S1AvatarFramework.AvatarSettings>();
            settings.Gender = appearance.Gender;
            settings.Height = appearance.Height / 1.875f;
            settings.Weight = appearance.Weight;
            settings.SkinColor = appearance.SkinColor;
            settings.HairColor = appearance.HairColor;
            settings.EyeBallTint = appearance.LeftEyeSettings.EyeballColor;
            settings.PupilDilation = appearance.LeftEyeSettings.PupilDilation;
            settings.LeftEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = appearance.LeftEyelidSettings.RestingState.TopLidOpenness,
                bottomLidOpen = appearance.LeftEyelidSettings.RestingState.BottomLidOpenness
            };
            settings.RightEyeRestingState = new S1AvatarFramework.Eye.EyeLidConfiguration
            {
                topLidOpen = appearance.RightEyelidSettings.RestingState.TopLidOpenness,
                bottomLidOpen = appearance.RightEyelidSettings.RestingState.BottomLidOpenness
            };
            settings.EyebrowScale = appearance.LeftEyebrowSettings.Scale;
            settings.EyebrowThickness = appearance.LeftEyebrowSettings.Thickness;
            settings.EyebrowRestingHeight = appearance.LeftEyebrowSettings.RestingHeight;
            settings.EyebrowRestingAngle = appearance.LeftEyebrowSettings.RestingAngle;
            foreach (var obj in avatar.GetComponentsInChildren<S1CoreAvatar.AvatarObject>(true))
            {
                var serialized = obj.Serialize();
                var color = serialized.Colors != null && serialized.Colors.Length > 0
                    ? serialized.Colors[0].Value : Color.white;
#if IL2CPPMELON
                var isHair = obj.TryCast<S1CoreAvatar.HairAvatarObject>() != null;
                var isFace = obj.TryCast<S1CoreAvatar.FaceAvatarObject>() != null;
#else
                var isHair = obj is S1CoreAvatar.HairAvatarObject;
                var isFace = obj is S1CoreAvatar.FaceAvatarObject;
#endif
                if (isHair)
                    settings.HairPath = obj.Id;
                else if (obj.PrimaryType == S1CoreAvatar.AvatarObject.EType.Worn)
                    settings.AccessorySettings.Add(new S1AvatarFramework.AvatarSettings.AccessorySetting
                    { path = obj.Id, color = color });
                else if (isFace)
                    settings.FaceLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                    { layerPath = obj.Id, layerTint = color });
                else
                    settings.BodyLayerSettings.Add(new S1AvatarFramework.AvatarSettings.LayerSetting
                    { layerPath = obj.Id, layerTint = color });
            }
            return settings;
        }

        internal static void CreateRenderInputs(
            S1AvatarFramework.AvatarSettings settings,
            out S1CoreAvatar.NakedAppearance nakedAppearance,
            out S1CoreAvatar.Outfit outfit)
        {
            nakedAppearance = settings.EquivalentNakedAppearance?.Appearance != null
                ? settings.EquivalentNakedAppearance.Appearance.Clone()
                : CreateNakedAppearance(settings);
            outfit = settings.EquivalentOutfit != null
                ? Object.Instantiate(settings.EquivalentOutfit)
                : CreateOutfit(settings);
        }

        internal static bool TryResolveAvatarObject(
            string resourcePath,
            Color color,
            out S1CoreAvatar.SerializedAvatarObject serialized)
        {
            serialized = S1CoreAvatar.SerializedAvatarObject.Null;
            if (string.IsNullOrWhiteSpace(resourcePath))
                return false;

            var prefab = Resources.Load<GameObject>(resourcePath);
            var accessory = prefab != null ? prefab.GetComponent<S1AvatarFramework.Accessory>() : null;
            S1CoreAvatar.AvatarObject? avatarObject = accessory != null
                ? accessory.AvatarObjectEquivalent : null;
            if (avatarObject == null)
            {
                var layer = Resources.Load<S1AvatarFramework.AvatarLayer>(resourcePath);
                if (layer != null)
                    avatarObject = layer.AvatarObjectEquivalent;
            }

            if (avatarObject == null &&
                !S1Avatar.AvatarObjectLibrary.TryGetAvatarObjectById(resourcePath, out avatarObject))
            {
                var finalSegment = resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
                S1Avatar.AvatarObjectLibrary.TryGetAvatarObjectById(finalSegment, out avatarObject);
            }

            if (avatarObject == null)
                return false;

            serialized = avatarObject.SerializeWithPrimaryColor(color);
            return true;
        }

        private static S1CoreAvatar.NakedAppearance CreateNakedAppearance(
            S1AvatarFramework.AvatarSettings settings)
        {
            var legacy = ScriptableObject.CreateInstance<S1AvatarFramework.Customization.BasicAvatarSettings>();
            legacy.Gender = settings.Gender < 0.35f ? 0 : 1;
            legacy.Weight = settings.Weight;
            legacy.SkinColor = settings.SkinColor;
            legacy.HairStyle = settings.HairPath ?? string.Empty;
            legacy.HairColor = settings.HairColor;
            legacy.EyeballColor = settings.EyeBallTint;
            legacy.PupilDilation = settings.PupilDilation;
            legacy.UpperEyeLidRestingPosition = settings.UpperEyelidRestingPosition;
            legacy.LowerEyeLidRestingPosition = settings.LowerEyelidRestingPosition;
            legacy.EyebrowScale = settings.EyebrowScale;
            legacy.EyebrowThickness = settings.EyebrowThickness;
            legacy.EyebrowRestingHeight = settings.EyebrowRestingHeight;
            legacy.EyebrowRestingAngle = settings.EyebrowRestingAngle;

            var appearance = S1AvatarTools.BasicAvatarSettingsConverter.ConvertToNakedAppearance(
                legacy,
                includeUnderwear: true,
                includeNipples: true,
                includeEyeShadow: true);
            Object.Destroy(legacy);

            appearance.Gender = settings.Gender;
            appearance.Height = settings.Height > 0f ? settings.Height * 1.875f : 1.875f;

            if (TryResolveAvatarObject(settings.HairPath ?? string.Empty, settings.HairColor, out var hair))
                appearance.AddAvatarObject(hair);

            if (settings.FaceLayerSettings != null)
            {
                foreach (var layer in settings.FaceLayerSettings)
                {
                    if (TryResolveAvatarObject(layer.layerPath, layer.layerTint, out var avatarObject))
                        appearance.AddAvatarObject(avatarObject);
                }
            }

            if (settings.BodyLayerSettings != null)
            {
                foreach (var layer in settings.BodyLayerSettings)
                {
                    if (TryResolveAvatarObject(layer.layerPath, layer.layerTint, out var avatarObject))
                        appearance.AddAvatarObject(avatarObject);
                }
            }

            return appearance;
        }

        private static S1CoreAvatar.Outfit CreateOutfit(S1AvatarFramework.AvatarSettings settings)
        {
            var serialized = new List<S1CoreAvatar.SerializedAvatarObject>();
            if (settings.AccessorySettings != null)
            {
                foreach (var accessory in settings.AccessorySettings)
                {
                    if (TryResolveAvatarObject(accessory.path, accessory.color, out var avatarObject))
                        serialized.Add(avatarObject);
                }
            }

            var outfit = ScriptableObject.CreateInstance<S1CoreAvatar.Outfit>();
#if IL2CPPMELON
            var objects = new Il2CppReferenceArray<S1CoreAvatar.SerializedAvatarObject>(serialized.Count);
            for (var index = 0; index < serialized.Count; index++)
                objects[index] = serialized[index];
            outfit.AvatarObjects = objects;
#else
            outfit.AvatarObjects = serialized.ToArray();
#endif
            return outfit;
        }
    }
}
