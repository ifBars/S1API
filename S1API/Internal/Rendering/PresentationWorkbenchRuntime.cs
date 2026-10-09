#if IL2CPPMELON
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Logging;
using S1API.Rendering;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Rendering
{
    internal static class PresentationWorkbenchRuntime
    {
        private const string ActiveUiName = "S1API.PresentationWorkbench";
        private const float IconDebounceSeconds = 0.12f;
        private static readonly Log Logger = new Log("PresentationWorkbench");
        private static Session? _session;

        internal static bool IsOpen => _session != null;

        internal static string? ActiveDefinitionId => _session?.Definition.Id;

        internal static bool Open(string id, string? targetKind = null)
        {
            Close();
            PresentationWorkbenchDefinition? definition = null;
            try
            {
                if (!PresentationWorkbenchResolver.TryResolve(
                        id,
                        targetKind,
                        out definition,
                        out string failure) ||
                    definition == null)
                {
                    Logger.Warning(failure);
                    return false;
                }

                if (!TryGetGameplayState(
                        out S1PlayerScripts.Player? player,
                        out S1PlayerScripts.PlayerInventory? inventory,
                        out S1PlayerScripts.PlayerCamera? playerCamera,
                        out S1PlayerScripts.PlayerMovement? movement))
                {
                    Logger.Warning(
                        "The presentation workbench requires a spawned local player in " +
                        "a render-ready Main or Tutorial scene.");
                    return false;
                }

                var session = new Session(
                    definition,
                    inventory!,
                    playerCamera!,
                    movement!);
                _session = session;
                session.Open();
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Could not open presentation workbench " +
                    $"'{definition?.Id ?? id}': " +
                    exception);
                Close();
                return false;
            }
        }

        internal static void Tick()
        {
            try
            {
                _session?.Tick();
            }
            catch (Exception exception)
            {
                Logger.Error($"Presentation workbench update failed: {exception}");
                Close();
            }
        }

        internal static void Close()
        {
            Session? session = _session;
            _session = null;
            if (session == null)
                return;

            try
            {
                session.Dispose();
            }
            catch (Exception exception)
            {
                Logger.Error(
                    $"Presentation workbench cleanup failed: {exception}");
            }
        }

        private static bool TryGetGameplayState(
            out S1PlayerScripts.Player? player,
            out S1PlayerScripts.PlayerInventory? inventory,
            out S1PlayerScripts.PlayerCamera? playerCamera,
            out S1PlayerScripts.PlayerMovement? movement)
        {
            player = S1PlayerScripts.Player.Local;
            inventory =
                S1DevUtilities.PlayerSingleton<
                    S1PlayerScripts.PlayerInventory>.InstanceExists
                    ? S1DevUtilities.PlayerSingleton<
                        S1PlayerScripts.PlayerInventory>.Instance
                    : null;
            playerCamera =
                S1DevUtilities.PlayerSingleton<
                    S1PlayerScripts.PlayerCamera>.InstanceExists
                    ? S1DevUtilities.PlayerSingleton<
                        S1PlayerScripts.PlayerCamera>.Instance
                    : null;
            movement =
                S1DevUtilities.PlayerSingleton<
                    S1PlayerScripts.PlayerMovement>.InstanceExists
                    ? S1DevUtilities.PlayerSingleton<
                        S1PlayerScripts.PlayerMovement>.Instance
                    : null;
            return player != null &&
                   inventory != null &&
                   playerCamera != null &&
                   movement != null &&
                   inventory.EquipContainer != null;
        }

        private sealed class Session : IDisposable
        {
            private readonly S1PlayerScripts.PlayerInventory _inventory;
            private readonly S1PlayerScripts.PlayerCamera _playerCamera;
            private readonly S1PlayerScripts.PlayerMovement _movement;
            private readonly bool _previousCanLook;
            private readonly bool _previousCanMove;
            private readonly bool _previousHotbarEnabled;
            private readonly bool _previousEquippingEnabled;
            private readonly CursorLockMode _previousCursorLockMode;
            private readonly bool _previousCursorVisible;
            private readonly GameObject? _previousEquippable;
            private readonly bool _previousEquippableActive;
            private readonly PreviewState? _firstPerson;
            private readonly PreviewState? _avatar;
            private readonly IconState? _icon;
            private readonly PresentationWorkbenchView _view;
            private PresentationWorkbenchMode _mode;
            private GameObject? _previewVisual;
            private GameObject? _avatarPreviewRoot;
            private GameObject? _avatarAnchor;
            private GameObject? _avatarStage;
            private S1AvatarFramework.Avatar? _avatarRig;
            private S1AvatarFramework.AvatarSettings? _avatarSettings;
            private Camera? _avatarCamera;
            private RenderTexture? _avatarTexture;
            private Vector3 _avatarLastMousePosition;
            private float _avatarYaw;
            private float _avatarPitch;
            private float _avatarDistance = 2.8f;
            private int _avatarSettleFrames;
            private int _avatarRendererRefreshFrames;
            private bool _avatarAppearanceApplied;
            private bool _avatarAnimationApplied;
            private bool _avatarHasMousePosition;
            private Texture2D? _iconTexture;
            private Sprite? _iconSprite;
            private float _iconRefreshAt = -1f;
            private int _iconCaptureRevision;
            private bool _iconCaptureRunning;
            private bool _disposed;

            internal Session(
                PresentationWorkbenchDefinition definition,
                S1PlayerScripts.PlayerInventory inventory,
                S1PlayerScripts.PlayerCamera playerCamera,
                S1PlayerScripts.PlayerMovement movement)
            {
                Definition = definition;
                _inventory = inventory;
                _playerCamera = playerCamera;
                _movement = movement;
                _previousCanLook = playerCamera.CanLook;
                _previousCanMove = movement.CanMove;
                _previousHotbarEnabled = inventory.HotbarEnabled;
                _previousEquippingEnabled = inventory.EquippingEnabled;
                _previousCursorLockMode = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                _previousEquippable = inventory.Equippable?.gameObject;
                _previousEquippableActive =
                    _previousEquippable != null &&
                    _previousEquippable.activeSelf;

                _firstPerson = CreateState(definition.FirstPerson);
                _avatar = CreateState(definition.Avatar);
                _icon = CreateIconState(definition.Icon);
                _mode = GetFirstMode(definition);
                _view = new PresentationWorkbenchView(
                    definition,
                    SelectMode,
                    UpdateTransform,
                    ToggleFit,
                    UpdateCameraFill,
                    Reset,
                    Copy,
                    PresentationWorkbenchRuntime.Close);
            }

            internal PresentationWorkbenchDefinition Definition { get; }

            internal void Open()
            {
                _playerCamera.AddActiveUIElement(ActiveUiName);
                _playerCamera.SetCanLook(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _movement.CanMove = false;
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "HotbarEnabled",
                    false);
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "EquippingEnabled",
                    false);
                _previousEquippable?.SetActive(false);
                SelectMode(_mode);
            }

            internal void Tick()
            {
                if (_mode == PresentationWorkbenchMode.Icon &&
                    _iconRefreshAt >= 0f &&
                    Time.unscaledTime >= _iconRefreshAt &&
                    !_iconCaptureRunning)
                {
                    _iconRefreshAt = -1f;
                    _iconCaptureRunning = true;
                    MelonCoroutines.Start(
                        CaptureIconAfterRender(_iconCaptureRevision));
                }

                if (_mode == PresentationWorkbenchMode.Avatar &&
                    _avatarCamera != null)
                {
                    TickAvatarStage();
                    UpdateAvatarCameraInput();
                    UpdateAvatarCamera();
                    _avatarCamera.Render();
                }
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                DestroyPreview();
                DestroyIconTexture();
                _view.Dispose();
                if (_previousEquippable != null)
                    _previousEquippable.SetActive(_previousEquippableActive);
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "HotbarEnabled",
                    _previousHotbarEnabled);
                ReflectionUtils.TrySetFieldOrProperty(
                    _inventory,
                    "EquippingEnabled",
                    _previousEquippingEnabled);
                _movement.CanMove = _previousCanMove;
                _playerCamera.RemoveActiveUIElement(ActiveUiName);
                _playerCamera.SetCanLook(_previousCanLook);
                Cursor.lockState = _previousCursorLockMode;
                Cursor.visible = _previousCursorVisible;
            }

            private void SelectMode(PresentationWorkbenchMode mode)
            {
                if (!Supports(mode))
                    return;

                DestroyPreview();
                _mode = mode;
                switch (mode)
                {
                    case PresentationWorkbenchMode.FirstPerson:
                        CreateFirstPersonPreview();
                        break;
                    case PresentationWorkbenchMode.Avatar:
                        CreateAvatarPreview();
                        break;
                    case PresentationWorkbenchMode.Icon:
                        ScheduleIconCapture(immediate: true);
                        break;
                }

                PreviewState state = GetState(mode);
                _view.SetMode(
                    mode,
                    state.Current,
                    _icon?.FitToCamera ?? false,
                    _icon?.CameraFill ?? 0.72f);
            }

            private void CreateFirstPersonPreview()
            {
                PreviewState state = _firstPerson!;
                _previewVisual = CloneSource(state.Provider, "FirstPerson");
                _previewVisual.transform.SetParent(
                    _inventory.EquipContainer,
                    false);
                state.Current.ApplyTo(_previewVisual.transform);
                SetLayerRecursively(_previewVisual, "Viewmodel");
                DisablePhysics(_previewVisual);
                _previewVisual.SetActive(true);
                _view.SetPreview(null);
            }

            private void CreateAvatarPreview()
            {
                PreviewState state = _avatar!;
                PresentationWorkbenchDefinition.AvatarPreviewContext context =
                    Definition.Avatar!;
                Transform handContainer;
                Transform alignmentPoint;
                object avatar =
                    CreateAvatarStage();
                ResolveAvatarHand(
                    avatar,
                    context.Hand,
                    out handContainer,
                    out alignmentPoint);

                _avatarPreviewRoot =
                    CloneSource(context.PreviewRootProvider, "Avatar");
                _previewVisual =
                    context.EditableVisualResolver(_avatarPreviewRoot) ??
                    throw new InvalidOperationException(
                        "The avatar preview prefab has no editable visual.");

                if (context.AlignAvatarEquippable)
                {
                    AlignAvatarEquippable(
                        _avatarPreviewRoot,
                        handContainer,
                        alignmentPoint);
                }
                else
                {
                    _avatarAnchor =
                        new GameObject("S1API Avatar Preview Anchor");
                    _avatarAnchor.transform.SetParent(handContainer, false);
                    _avatarAnchor.transform.SetPositionAndRotation(
                        alignmentPoint.position,
                        alignmentPoint.rotation);
                    _avatarPreviewRoot.transform.SetParent(
                        _avatarAnchor.transform,
                        false);
                }

                state.Current.ApplyTo(_previewVisual.transform);
                SetLayerRecursively(
                    _avatarPreviewRoot,
                    "IconGeneration");
                DisablePhysics(_avatarPreviewRoot);
                _avatarPreviewRoot.SetActive(true);
                CreateAvatarCamera();
            }

            private static void AlignAvatarEquippable(
                GameObject root,
                Transform handContainer,
                Transform handAlignmentPoint)
            {
                S1AvatarEquipping.AvatarEquippable? equippable =
                    root.GetComponentInChildren<
                        S1AvatarEquipping.AvatarEquippable>(true);
                Transform? modelAlignmentPoint =
                    equippable?.AlignmentPoint;
                if (modelAlignmentPoint == null)
                {
                    throw new InvalidOperationException(
                        "The avatar equippable preview has no alignment point.");
                }

                root.transform.SetParent(handContainer);
                root.transform.rotation =
                    handAlignmentPoint.rotation *
                    (Quaternion.Inverse(modelAlignmentPoint.rotation) *
                     root.transform.rotation);
                root.transform.position =
                    handAlignmentPoint.position +
                    (root.transform.position - modelAlignmentPoint.position);
            }

            private object CreateAvatarStage()
            {
#if false
                var generator = S1AvatarFramework.MugshotGenerator.Instance;
                var source = generator != null ? generator.MugshotRig : null;
                if (source == null)
                {
                    throw new InvalidOperationException(
                        "The native avatar preview rig is not ready.");
                }

                int layer = LayerMask.NameToLayer("IconGeneration");
                if (layer < 0)
                    layer = 30;

                _avatarStage = new GameObject(
                    "S1API Presentation Workbench Avatar Stage");
                _avatarStage.transform.position =
                    new Vector3(0f, -1000f, 0f);

                GameObject avatarObject =
                    Object.Instantiate(
                        source.gameObject,
                        _avatarStage.transform,
                        false);
                avatarObject.name = "Detached Avatar";
                avatarObject.SetActive(true);
                S1DevUtilities.LayerUtility.SetLayerRecursively(
                    avatarObject,
                    layer);

                var avatar =
                    avatarObject.GetComponent<S1AvatarFramework.Avatar>() ??
                    throw new InvalidOperationException(
                        "The native avatar preview rig has no Avatar component.");
                _avatarRig = avatar;
                S1AvatarFramework.AvatarSettings? defaultSettings =
                    generator?.DefaultSettings;
                _avatarSettings =
                    source.CurrentSettings != null
                        ? Object.Instantiate(source.CurrentSettings)
                        : defaultSettings != null
                            ? Object.Instantiate(defaultSettings)
                            : CreateFallbackAvatarSettings();
                _avatarSettleFrames = 1;
                avatar.SetVisible(true);
                if (avatar.Animation != null)
                    avatar.Animation.AllowCulling = false;
                RefreshAvatarRenderers();

                return avatar;
#else
                throw new InvalidOperationException(
                    "Avatar presentation previews are unavailable with the Schedule I 0.4.7 avatar pipeline.");
#endif
            }

            private void TickAvatarStage()
            {
                if (_avatarRig == null)
                    return;

                if (!_avatarAppearanceApplied)
                {
                    if (_avatarSettleFrames-- > 0)
                        return;

                    Compatibility.AvatarCompatibility.ApplyLegacySettings(_avatarRig, _avatarSettings!);
                    _avatarRig.SetVisible(true);
                    _avatarRendererRefreshFrames = 2;
                    _avatarAppearanceApplied = true;
                    _avatarSettleFrames = 2;
                    RefreshAvatarRenderers();
                    return;
                }

                if (!_avatarAnimationApplied)
                {
                    if (_avatarSettleFrames-- > 0)
                    {
                        RefreshAvatarRenderers();
                        return;
                    }

                    ApplyAvatarAnimation();
                }

                if (_avatarRendererRefreshFrames-- > 0)
                    RefreshAvatarRenderers();
            }

            private static S1AvatarFramework.AvatarSettings
                CreateFallbackAvatarSettings()
            {
                var settings =
                    ScriptableObject.CreateInstance<
                        S1AvatarFramework.AvatarSettings>();
                settings.SkinColor = new Color32(150, 120, 95, 255);
                settings.Height = 1f;
                settings.Gender = 0.5f;
                settings.Weight = 0.5f;
                settings.EyeBallTint = Color.white;
                settings.PupilDilation = 1f;
                settings.HairPath = string.Empty;
                settings.HairColor = Color.black;
                settings.LeftEyeRestingState =
                    new S1AvatarFramework.Eye.EyeLidConfiguration
                    {
                        topLidOpen = 0.5f,
                        bottomLidOpen = 0.5f,
                    };
                settings.RightEyeRestingState =
                    new S1AvatarFramework.Eye.EyeLidConfiguration
                    {
                        topLidOpen = 0.5f,
                        bottomLidOpen = 0.5f,
                    };
                settings.LeftEyeLidColor =
                    new Color32(150, 120, 95, 255);
                settings.RightEyeLidColor =
                    new Color32(150, 120, 95, 255);
                return settings;
            }

            private void ApplyAvatarAnimation()
            {
                if (_avatarRig == null || _avatarAnimationApplied)
                    return;

                _avatarAnimationApplied = true;
                PresentationWorkbenchDefinition.AvatarPreviewContext context =
                    Definition.Avatar!;
                if (string.IsNullOrWhiteSpace(context.AnimationTrigger))
                    return;

                if (context.AnimationUsesBool)
                {
                    _avatarRig.SetAnimationBool(
                        context.AnimationTrigger,
                        true);
                }
                else
                {
                    _avatarRig.SetAnimationTrigger(
                        context.AnimationTrigger);
                }
            }

            private void RefreshAvatarRenderers()
            {
                if (_avatarRig == null)
                    return;

                SetLayerRecursively(
                    _avatarRig.gameObject,
                    "IconGeneration");
                var renderers =
                    _avatarRig.gameObject.GetComponentsInChildren<
                        SkinnedMeshRenderer>(true);
                for (int index = 0; index < renderers.Length; index++)
                    renderers[index].updateWhenOffscreen = true;
            }

            private void CreateAvatarCamera()
            {
                if (_avatarStage == null)
                    throw new InvalidOperationException(
                        "The avatar preview stage is unavailable.");

                int layer = LayerMask.NameToLayer("IconGeneration");
                if (layer < 0)
                    layer = 30;

                var cameraRoot =
                    new GameObject("S1API Presentation Workbench Camera");
                cameraRoot.transform.SetParent(
                    _avatarStage.transform,
                    false);
                _avatarCamera = cameraRoot.AddComponent<Camera>();
                _avatarCamera.enabled = true;
                _avatarCamera.clearFlags = CameraClearFlags.SolidColor;
                _avatarCamera.backgroundColor =
                    new Color(0.025f, 0.03f, 0.04f, 1f);
                _avatarCamera.fieldOfView = 34f;
                _avatarCamera.nearClipPlane = 0.05f;
                _avatarCamera.farClipPlane = 20f;
                _avatarCamera.cullingMask = 1 << layer;
                _avatarTexture =
                    new RenderTexture(640, 800, 24, RenderTextureFormat.ARGB32)
                    {
                        name = "S1API Presentation Workbench Avatar",
                        antiAliasing = 2,
                };
                _avatarTexture.Create();
                _avatarCamera.targetTexture = _avatarTexture;
                CreateAvatarKeyLight(layer);
                CreateAvatarLight(
                    "Fill Light",
                    new Vector3(30f, 210f, 0f),
                    0.65f,
                    new Color(0.55f, 0.72f, 1f),
                    layer);
                UpdateAvatarCamera();
                _view.SetPreview(_avatarTexture);
            }

            private void CreateAvatarKeyLight(int layer)
            {
                var lightObject = new GameObject("Key Light");
                lightObject.transform.SetParent(
                    _avatarStage!.transform,
                    false);
                lightObject.transform.localPosition =
                    new Vector3(-2f, 3f, -3f);
                lightObject.transform.LookAt(
                    _avatarStage.transform.position + Vector3.up);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.88f, 0.76f);
                light.cullingMask = 1 << layer;
            }

            private void UpdateAvatarCameraInput()
            {
                Vector3 mouse = UnityEngine.Input.mousePosition;
                if (!_avatarHasMousePosition)
                {
                    _avatarLastMousePosition = mouse;
                    _avatarHasMousePosition = true;
                    return;
                }

                if (_view.IsPointerOverPreview(mouse))
                {
                    if (UnityEngine.Input.GetMouseButton(0))
                    {
                        Vector3 delta = mouse - _avatarLastMousePosition;
                        _avatarYaw += delta.x * 0.35f;
                        _avatarPitch =
                            Mathf.Clamp(
                                _avatarPitch - delta.y * 0.35f,
                                -25f,
                                45f);
                    }

                    _avatarDistance =
                        Mathf.Clamp(
                            _avatarDistance -
                            UnityEngine.Input.mouseScrollDelta.y * 0.2f,
                            1.45f,
                            4.5f);
                }

                _avatarLastMousePosition = mouse;
            }

            private void UpdateAvatarCamera()
            {
                if (_avatarCamera == null || _avatarStage == null)
                    return;

                Vector3 target =
                    _avatarStage.transform.position +
                    new Vector3(0f, 1.05f, 0f);
                Quaternion rotation =
                    Quaternion.Euler(_avatarPitch, _avatarYaw, 0f);
                _avatarCamera.transform.position =
                    target +
                    rotation * new Vector3(0f, 0f, _avatarDistance);
                _avatarCamera.transform.LookAt(target);
            }

            private void CreateAvatarLight(
                string name,
                Vector3 rotation,
                float intensity,
                Color color,
                int layer)
            {
                var lightObject = new GameObject(name);
                lightObject.transform.SetParent(
                    _avatarStage!.transform,
                    false);
                lightObject.transform.localEulerAngles = rotation;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = intensity;
                light.color = color;
                light.cullingMask = 1 << layer;
            }

            private void ResolveAvatarHand(
                object avatar,
                AvatarHand hand,
                out Transform handContainer,
                out Transform alignmentPoint)
            {
                object animation =
                    ReflectionUtils.TryGetFieldOrProperty(avatar, "Animation") ??
                    throw new InvalidOperationException(
                        "The local player avatar animation rig is unavailable.");
                string handName =
                    hand == AvatarHand.Left ? "LeftHand" : "RightHand";
                handContainer =
                    ReflectionUtils.TryGetFieldOrProperty(
                        animation,
                        handName + "Container") as Transform ??
                    throw new InvalidOperationException(
                        $"{handName}Container is unavailable.");
                alignmentPoint =
                    ReflectionUtils.TryGetFieldOrProperty(
                        animation,
                        handName + "AlignmentPoint") as Transform ??
                    throw new InvalidOperationException(
                        $"{handName}AlignmentPoint is unavailable.");
            }

            private void UpdateTransform(
                Vector3 position,
                Vector3 rotation,
                Vector3 scale)
            {
                if (!IsFinite(position) ||
                    !IsFinite(rotation) ||
                    !IsFinite(scale))
                {
                    _view.SetStatus("Transform values must be finite.");
                    return;
                }

                var value =
                    new PresentationWorkbenchTransform(
                        position,
                        rotation,
                        scale);
                PreviewState state = GetState(_mode);
                state.Current = value;
                if (_previewVisual != null)
                    value.ApplyTo(_previewVisual.transform);
                if (_mode == PresentationWorkbenchMode.Icon)
                    ScheduleIconCapture(immediate: false);
                _view.SetStatus("Preview updated.");
            }

            private void ToggleFit()
            {
                if (_mode != PresentationWorkbenchMode.Icon || _icon == null)
                    return;

                _icon.FitToCamera = !_icon.FitToCamera;
                _view.SetMode(
                    _mode,
                    _icon.Current,
                    _icon.FitToCamera,
                    _icon.CameraFill);
                ScheduleIconCapture(immediate: false);
            }

            private void UpdateCameraFill(float value)
            {
                if (_mode != PresentationWorkbenchMode.Icon || _icon == null)
                    return;

                if (value <= 0f || value > 2f)
                {
                    _view.SetStatus(
                        "Camera fill must be greater than zero and at most 2.");
                    return;
                }

                _icon.CameraFill = value;
                ScheduleIconCapture(immediate: false);
                _view.SetStatus("Icon recapture scheduled.");
            }

            private void Reset()
            {
                PreviewState state = GetState(_mode);
                state.Current = state.Initial;
                if (_icon != null &&
                    _mode == PresentationWorkbenchMode.Icon)
                {
                    _icon.FitToCamera = _icon.InitialFitToCamera;
                    _icon.CameraFill = _icon.InitialCameraFill;
                    ScheduleIconCapture(immediate: false);
                }
                else if (_previewVisual != null)
                {
                    state.Current.ApplyTo(_previewVisual.transform);
                }

                if (_mode == PresentationWorkbenchMode.Avatar)
                {
                    _avatarYaw = 0f;
                    _avatarPitch = 0f;
                    _avatarDistance = 2.8f;
                    UpdateAvatarCamera();
                }

                _view.SetMode(
                    _mode,
                    state.Current,
                    _icon?.FitToCamera ?? false,
                    _icon?.CameraFill ?? 0.72f);
                _view.SetStatus("Initial values restored.");
            }

            private void Copy()
            {
                PreviewState state = GetState(_mode);
                string text =
                    _mode == PresentationWorkbenchMode.Icon && _icon != null
                        ? PresentationWorkbenchExporter.FormatIcon(
                            state.Current.LocalEulerAngles,
                            state.Current.LocalScale,
                            _icon.FitToCamera,
                            _icon.CameraFill,
                            _icon.Size)
                        : PresentationWorkbenchExporter.FormatTransform(
                            state.ExportKind,
                            state.Current.LocalPosition,
                            state.Current.LocalEulerAngles,
                            state.Current.LocalScale);
                GUIUtility.systemCopyBuffer = text;
                _view.SetStatus("Copied C# values to the clipboard.");
            }

            private void ScheduleIconCapture(bool immediate)
            {
                _iconCaptureRevision++;
                _iconRefreshAt =
                    Time.unscaledTime +
                    (immediate ? 0f : IconDebounceSeconds);
                _view.SetStatus(
                    immediate
                        ? "Preparing icon preview..."
                        : "Icon recapture scheduled.");
            }

            private IEnumerator CaptureIconAfterRender(int revision)
            {
                try
                {
                    const int maxAttempts = 8;
                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                    {
                        yield return null;
                        yield return new WaitForEndOfFrame();
                        if (_disposed ||
                            _mode != PresentationWorkbenchMode.Icon ||
                            revision != _iconCaptureRevision)
                        {
                            yield break;
                        }

                        if (TryCaptureIcon())
                        {
                            _view.SetStatus(
                                $"Icon preview captured after {attempt} attempt(s).");
                            yield break;
                        }
                    }

                    _view.SetStatus(
                        "Icon capture produced no visible pixels after 8 attempts.");
                }
                finally
                {
                    _iconCaptureRunning = false;
                    if (!_disposed &&
                        _mode == PresentationWorkbenchMode.Icon &&
                        revision != _iconCaptureRevision)
                    {
                        _iconRefreshAt = Time.unscaledTime;
                    }
                }
            }

            private bool TryCaptureIcon()
            {
                IconState state = _icon!;
                GameObject visual = CloneSource(state.Provider, "Icon");
                try
                {
                    state.Current.ApplyTo(visual.transform);
                    Sprite? icon =
                        IconFactory.GenerateIconSprite(
                            visual.transform,
                            state.Size,
                            bakeSkinnedMeshes: true,
                            state.FitToCamera,
                            state.CameraFill);
                    if (icon == null || icon.texture == null)
                    {
                        if (icon != null)
                            Object.Destroy(icon);
                        return false;
                    }

                    DestroyIconTexture();
                    _iconSprite = icon;
                    _iconTexture = icon.texture;
                    _view.SetPreview(_iconTexture);
                    return true;
                }
                finally
                {
                    Object.Destroy(visual);
                }
            }

            private void DestroyPreview()
            {
                if (_avatarPreviewRoot != null)
                    Object.Destroy(_avatarPreviewRoot);
                else if (_previewVisual != null)
                    Object.Destroy(_previewVisual);
                if (_avatarAnchor != null)
                    Object.Destroy(_avatarAnchor);
                if (_avatarCamera != null)
                {
                    _avatarCamera.targetTexture = null;
                    Object.Destroy(_avatarCamera.gameObject);
                }
                if (_avatarTexture != null)
                {
                    _avatarTexture.Release();
                    Object.Destroy(_avatarTexture);
                }
                if (_avatarStage != null)
                    Object.Destroy(_avatarStage);
                if (_avatarSettings != null)
                    Object.Destroy(_avatarSettings);

                _previewVisual = null;
                _avatarPreviewRoot = null;
                _avatarAnchor = null;
                _avatarStage = null;
                _avatarRig = null;
                _avatarSettings = null;
                _avatarCamera = null;
                _avatarTexture = null;
                _avatarSettleFrames = 0;
                _avatarRendererRefreshFrames = 0;
                _avatarAppearanceApplied = false;
                _avatarAnimationApplied = false;
                _avatarHasMousePosition = false;
                _view.SetPreview(null);
            }

            private void DestroyIconTexture()
            {
                if (_iconSprite != null)
                    Object.Destroy(_iconSprite);
                if (_iconTexture != null)
                    Object.Destroy(_iconTexture);
                _iconSprite = null;
                _iconTexture = null;
            }

            private bool Supports(PresentationWorkbenchMode mode) =>
                mode == PresentationWorkbenchMode.FirstPerson
                    ? _firstPerson != null
                    : mode == PresentationWorkbenchMode.Avatar
                        ? _avatar != null
                        : _icon != null;

            private PreviewState GetState(PresentationWorkbenchMode mode) =>
                mode == PresentationWorkbenchMode.FirstPerson
                    ? _firstPerson!
                    : mode == PresentationWorkbenchMode.Avatar
                        ? _avatar!
                        : _icon!;

            private static PreviewState? CreateState(
                PresentationWorkbenchDefinition.PreviewContext? context)
            {
                if (context == null)
                    return null;

                return new PreviewState(
                    context.Provider,
                    context.InitialTransform ??
                    CaptureAuthoredTransform(context.Provider),
                    context.ExportKind);
            }

            private static PreviewState? CreateState(
                PresentationWorkbenchDefinition.AvatarPreviewContext? context)
            {
                if (context == null)
                    return null;

                return new PreviewState(
                    context.Provider,
                    context.InitialTransform ??
                    CaptureAuthoredTransform(context.Provider),
                    context.ExportKind);
            }

            private static IconState? CreateIconState(
                PresentationWorkbenchDefinition.IconPreviewContext? context)
            {
                if (context == null)
                    return null;

                return new IconState(
                    context.Provider,
                    context.InitialTransform ??
                    CaptureAuthoredTransform(context.Provider),
                    context.Size,
                    context.FitToCamera,
                    context.CameraFill);
            }

            private static PresentationWorkbenchTransform
                CaptureAuthoredTransform(Func<GameObject?> provider)
            {
                GameObject source = GetSource(provider, "initial");
                return PresentationWorkbenchTransform.From(source.transform);
            }

            private static GameObject CloneSource(
                Func<GameObject?> provider,
                string context)
            {
                GameObject source = GetSource(provider, context);
                GameObject clone = Object.Instantiate(source);
                clone.SetActive(false);
                DisableBehaviours(clone);
                clone.name = $"S1API {context} Preview ({source.name})";
                return clone;
            }

            private static GameObject GetSource(
                Func<GameObject?> provider,
                string context)
            {
                GameObject? source;
                try
                {
                    source = provider();
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"{context} preview source provider threw an exception.",
                        exception);
                }

                return source ??
                       throw new InvalidOperationException(
                           $"{context} preview source provider returned null.");
            }

            private static PresentationWorkbenchMode GetFirstMode(
                PresentationWorkbenchDefinition definition)
            {
                if (definition.SupportsFirstPerson)
                    return PresentationWorkbenchMode.FirstPerson;
                if (definition.SupportsAvatar)
                    return PresentationWorkbenchMode.Avatar;
                return PresentationWorkbenchMode.Icon;
            }

            private static void SetLayerRecursively(
                GameObject root,
                string layerName)
            {
                int layer = LayerMask.NameToLayer(layerName);
                if (layer >= 0)
                {
                    S1DevUtilities.LayerUtility.SetLayerRecursively(root, layer);
                }
            }

            private static void DisablePhysics(GameObject root)
            {
                var colliders =
                    root.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }

                var rigidbodies =
                    root.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    Rigidbody rigidbody = rigidbodies[i];
                    rigidbody.isKinematic = true;
                    rigidbody.detectCollisions = false;
                }
            }

            private static void DisableBehaviours(GameObject root)
            {
                var behaviours =
                    root.GetComponentsInChildren<Behaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                    behaviours[index].enabled = false;
            }

            private static bool IsFinite(Vector3 value) =>
                IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

            private static bool IsFinite(float value) =>
                !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private class PreviewState
        {
            internal PreviewState(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform initial,
                PresentationWorkbenchExportKind exportKind)
            {
                Provider = provider;
                Initial = initial;
                Current = initial;
                ExportKind = exportKind;
            }

            internal Func<GameObject?> Provider { get; }

            internal PresentationWorkbenchTransform Initial { get; }

            internal PresentationWorkbenchTransform Current { get; set; }

            internal PresentationWorkbenchExportKind ExportKind { get; }
        }

        private sealed class IconState : PreviewState
        {
            internal IconState(
                Func<GameObject?> provider,
                PresentationWorkbenchTransform initial,
                int size,
                bool fitToCamera,
                float cameraFill)
                : base(
                    provider,
                    initial,
                    PresentationWorkbenchExportKind.IconFactory)
            {
                Size = size;
                InitialFitToCamera = fitToCamera;
                FitToCamera = fitToCamera;
                InitialCameraFill = cameraFill;
                CameraFill = cameraFill;
            }

            internal int Size { get; }

            internal bool InitialFitToCamera { get; }

            internal bool FitToCamera { get; set; }

            internal float InitialCameraFill { get; }

            internal float CameraFill { get; set; }
        }
    }
}
