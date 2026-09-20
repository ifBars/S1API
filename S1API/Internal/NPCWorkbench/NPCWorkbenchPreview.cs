#if IL2CPPMELON
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1AvatarTools = Il2CppScheduleOne.Avatar.Tools;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1AvatarTools = ScheduleOne.Avatar.Tools;
#endif

using System;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.NPCWorkbench
{
    internal sealed class NPCWorkbenchPreview : IDisposable
    {
        private static readonly Log Logger = new Log("NPCWorkbenchPreview");
        private const int TextureWidth = 640;
        private const int TextureHeight = 800;
        private readonly GameObject _root;
        private readonly S1AvatarFramework.Avatar _avatar;
        private readonly Camera _camera;
        private readonly RenderTexture _texture;
        private readonly int _renderLayer;
        private S1AvatarFramework.AvatarSettings? _template;
        private S1AvatarFramework.AvatarSettings? _appliedSettings;
        private NPCWorkbenchDraft? _pendingDraft;
        private int _settleFrames;
        private int _rendererRefreshFrames;
        private float _yaw;
        private float _pitch;
        private float _distance = 3.8f;
        private bool _disposed;

        private NPCWorkbenchPreview(
            GameObject root,
            S1AvatarFramework.Avatar avatar,
            Camera camera,
            RenderTexture texture,
            int renderLayer)
        {
            _root = root;
            _avatar = avatar;
            _camera = camera;
            _texture = texture;
            _renderLayer = renderLayer;
            _template = null;
            UpdateCamera();
        }

        internal Texture Texture => _texture;

        internal static bool TryCreate(out NPCWorkbenchPreview? preview, out string failure)
        {
            preview = null;
            failure = string.Empty;
            var generator = Compatibility.AvatarCompatibility.FindMugshotGenerator();
            var source = generator != null
                ? Utils.ReflectionUtils.TryGetFieldOrProperty(generator, "_avatar") as S1AvatarFramework.Avatar
                : null;
            if (source == null)
            {
                failure = "The native avatar preview rig is not ready. Load a save, then reopen the workbench.";
                return false;
            }

            GameObject? root = null;
            RenderTexture? texture = null;
            try
            {
                var layer = LayerMask.NameToLayer("IconGeneration");
                if (layer < 0)
                    layer = 30;

                root = new GameObject("S1API NPC Workbench Preview");
                root.transform.position = new Vector3(0f, -1000f, 0f);

                var avatarObject = Object.Instantiate(source.gameObject, root.transform, false);
                avatarObject.name = "Detached Avatar";
                avatarObject.transform.localPosition = Vector3.zero;
                avatarObject.transform.localRotation = Quaternion.identity;
                avatarObject.SetActive(true);
                SetLayerRecursively(avatarObject, layer);

                var avatar = avatarObject.GetComponent<S1AvatarFramework.Avatar>();
                if (avatar == null)
                    throw new InvalidOperationException("The cloned native preview rig has no Avatar component.");

                Compatibility.AvatarCompatibility.PrepareDetachedAvatar(avatar);

                avatar.SetVisible(true);
                foreach (var renderer in avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    renderer.updateWhenOffscreen = true;

                texture = new RenderTexture(TextureWidth, TextureHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "S1API NPC Workbench Preview",
                    antiAliasing = 2
                };
                texture.Create();

                var cameraObject = new GameObject("Preview Camera");
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
                camera.cullingMask = 1 << layer;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 20f;
                camera.fieldOfView = 34f;
                camera.targetTexture = texture;

                var keyObject = new GameObject("Key Light");
                keyObject.transform.SetParent(root.transform, false);
                keyObject.transform.localPosition = new Vector3(-2f, 3f, 3f);
                keyObject.transform.LookAt(root.transform.position + Vector3.up);
                var key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.25f;
                key.color = new Color(1f, 0.88f, 0.76f);
                key.cullingMask = 1 << layer;
                key.shadows = LightShadows.None;

                var fillObject = new GameObject("Fill Light");
                fillObject.transform.SetParent(root.transform, false);
                fillObject.transform.localEulerAngles = new Vector3(30f, 210f, 0f);
                var fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.65f;
                fill.color = new Color(0.55f, 0.72f, 1f);
                fill.cullingMask = 1 << layer;
                fill.shadows = LightShadows.None;

                preview = new NPCWorkbenchPreview(root, avatar, camera, texture, layer);
                return true;
            }
            catch (Exception ex)
            {
                failure = $"Could not create the detached avatar preview: {ex.Message}";
                Logger.Error(failure);
                if (texture != null)
                {
                    texture.Release();
                    Object.Destroy(texture);
                }
                if (root != null)
                    Object.Destroy(root);
                return false;
            }
        }

        internal void ScheduleApply(NPCWorkbenchDraft draft)
        {
            _pendingDraft = draft.Clone();
            _settleFrames = 1;
        }

        internal void Tick()
        {
            if (_disposed)
                return;

            if (_pendingDraft != null)
            {
                if (_settleFrames-- > 0)
                    return;

                var draft = _pendingDraft;
                _pendingDraft = null;
                Apply(draft);
            }

            if (_rendererRefreshFrames-- > 0)
                RefreshRenderers();
        }

        internal void Orbit(float horizontal, float vertical)
        {
            _yaw += horizontal;
            _pitch = Mathf.Clamp(_pitch - vertical, -25f, 45f);
            UpdateCamera();
        }

        internal void Zoom(float delta)
        {
            _distance = Mathf.Clamp(_distance - delta, 1.45f, 4.5f);
            UpdateCamera();
        }

        internal void SetPose(int pose)
        {
            if (_avatar.Animation == null)
                return;

            _avatar.Animation.SetCrouched(pose == 2);
            _avatar.SetAnimationBool("Sitting", pose == 1);
            _avatar.Animation.SetMotion(Vector3.zero, pose == 2);
        }

        internal void ResetView()
        {
            _yaw = 0f;
            _pitch = 0f;
            _distance = 3.8f;
            SetPose(0);
            UpdateCamera();
        }

        private void Apply(NPCWorkbenchDraft draft)
        {
            var settings = NPCWorkbenchRuntimeAdapter.CreateSettings(draft, _template);
            try
            {
                Compatibility.AvatarCompatibility.ApplyLegacySettings(_avatar, settings);
                _avatar.SetVisible(true);
                RefreshRenderers();
                _rendererRefreshFrames = 2;

                if (_appliedSettings != null)
                    Object.Destroy(_appliedSettings);
                _appliedSettings = settings;
            }
            catch
            {
                Object.Destroy(settings);
                throw;
            }
        }

        private void RefreshRenderers()
        {
            SetLayerRecursively(_avatar.gameObject, _renderLayer);
            foreach (var lod in _avatar.gameObject.GetComponentsInChildren<LODGroup>(true))
                lod.ForceLOD(0);
            foreach (var renderer in _avatar.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
        }

        private void UpdateCamera()
        {
            var target = _root.transform.position + new Vector3(0f, 1.05f, 0f);
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.position = target + rotation * new Vector3(0f, 0f, _distance);
            _camera.transform.LookAt(target);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            for (var index = 0; index < root.transform.childCount; index++)
                SetLayerRecursively(root.transform.GetChild(index).gameObject, layer);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _pendingDraft = null;
            if (_camera != null)
                _camera.targetTexture = null;
            if (_texture != null)
            {
                _texture.Release();
                Object.Destroy(_texture);
            }
            if (_template != null)
                Object.Destroy(_template);
            if (_appliedSettings != null)
                Object.Destroy(_appliedSettings);
            if (_root != null)
                Object.Destroy(_root);
        }
    }
}
