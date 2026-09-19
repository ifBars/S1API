#if IL2CPPMELON
using S1AvatarEquipping = Il2CppScheduleOne.AvatarFramework.Equipping;
using S1Effects = Il2CppScheduleOne.Effects;
using S1Equipping = Il2CppScheduleOne.Equipping;
using S1Product = Il2CppScheduleOne.Product;
using S1Station = Il2CppScheduleOne.StationFramework;
using S1Storage = Il2CppScheduleOne.Storage;
using EffectList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Effects.Effect>;
#elif MONOMELON
using S1AvatarEquipping = ScheduleOne.AvatarFramework.Equipping;
using S1Effects = ScheduleOne.Effects;
using S1Equipping = ScheduleOne.Equipping;
using S1Product = ScheduleOne.Product;
using S1Station = ScheduleOne.StationFramework;
using S1Storage = ScheduleOne.Storage;
using EffectList = System.Collections.Generic.List<ScheduleOne.Effects.Effect>;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Products;
using S1API.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Clones native presentation scaffolds and replaces only their visual payload.
    /// </summary>
    internal sealed class CustomProductPresentationRuntime :
        ICustomProductPresentationRuntime
    {
        internal static readonly CustomProductPresentationRuntime Instance =
            new CustomProductPresentationRuntime();
        private static GameObject? _cacheRoot;
        private static readonly object GeneratedIconQueueGate = new object();
        private static readonly Queue<GeneratedIconRequest> GeneratedIconQueue =
            new Queue<GeneratedIconRequest>();
        private static bool _isProcessingGeneratedIcons;
        private const string DiscoveryVisualRootName =
            "S1API_CustomProductDiscoveryVisual";

        private sealed class GeneratedIconRequest
        {
            internal GeneratedIconRequest(
                CustomProductDefinitionRegistration product,
                ProductPresentationProfileRegistration registration,
                CustomProductPresentationState state)
            {
                Product = product;
                Registration = registration;
                State = state;
            }

            internal CustomProductDefinitionRegistration Product { get; }

            internal ProductPresentationProfileRegistration Registration { get; }

            internal CustomProductPresentationState State { get; }
        }

        internal enum GeneratedIconAttemptResult
        {
            Success,
            Retry,
            Failure
        }

        private CustomProductPresentationRuntime()
        {
        }

        public void Apply(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfileRegistration? profile)
        {
            if (product.Metadata == null)
                return;

            if (profile == null)
            {
                if (product.PresentationState != null)
                    RestoreBaseline(product);
                return;
            }

            CustomProductPresentationState state =
                product.PresentationState ??=
                    new CustomProductPresentationState(
                        product.Definition,
                        product.Metadata);
            if (ReferenceEquals(state.AppliedRegistration, profile) &&
                GeneratedObjectsAreAlive(state.GeneratedObjects))
            {
                QueuePendingGeneratedIcon(product, profile, state);
                return;
            }

            BuildAndCommit(product, profile);
        }

        internal static void ApplyDiscoveryVisual(
            S1Product.MultiTypeVisualsSetter setter,
            S1Product.ProductDefinition product,
            EffectList properties)
        {
            if (setter == null)
                return;

            RemoveDiscoveryVisual(setter.transform);
            if (product == null ||
                properties == null ||
                !CustomProductDefinitionRegistry.TryGetMetadata(
                    product,
                    out CustomProductDefinitionMetadata? metadata) ||
                metadata == null ||
                !ProductPresentationProfileRegistry.TryResolve(
                    product.ID,
                    metadata.ProductKind.Id,
                    out ProductPresentationProfileRegistration? registration) ||
                registration == null)
            {
                return;
            }

            ProductPresentationProfile profile = registration.Profile;
            if (!profile.TryGetVisualProvider(
                    ProductPresentationContext.FunctionalProduct,
                    out Func<GameObject?>? provider) ||
                provider == null)
            {
                return;
            }

            GameObject? source;
            try
            {
                source = provider();
            }
            catch (Exception exception)
            {
                MelonLogger.Warning(
                    $"[ProductPresentationProfile] Discovery visual provider for " +
                    $"'{product.ID}' failed: {exception.Message}");
                return;
            }

            if (source == null)
                return;

            GameObject? root = null;
            try
            {
                root = new GameObject(DiscoveryVisualRootName);
                root.transform.SetParent(setter.transform, false);
                root.SetActive(false);
                GameObject visual = Object.Instantiate(source);
                visual.transform.SetParent(root.transform, false);
                ApplyVisualTransform(
                    visual.transform,
                    profile,
                    ProductPresentationContext.FunctionalProduct);
                ApplyDiscoveryMixColor(
                    visual,
                    metadata,
                    properties);
                visual.SetActive(true);

                ResetMultiTypeVisuals(setter);
                root.SetActive(true);
            }
            catch (Exception exception)
            {
                if (root != null)
                    Object.Destroy(root);
                MelonLogger.Warning(
                    $"[ProductPresentationProfile] Could not show the discovery " +
                    $"visual for '{product.ID}'; retaining the native fallback: " +
                    exception.Message);
            }
        }

        private static void ApplyDiscoveryMixColor(
            GameObject visual,
            CustomProductDefinitionMetadata metadata,
            EffectList properties)
        {
            if (!ProductMixingProfiles.TryGet(
                    metadata.ProductKind.Id,
                    out ProductMixingProfile? mixingProfile) ||
                mixingProfile == null ||
                !mixingProfile.UsePropertyColorMixing)
            {
                return;
            }

            var samples =
                new List<ProductMixingColorSample>(properties.Count);
            for (int i = 0; i < properties.Count; i++)
            {
                S1Effects.Effect property = properties[i];
                if (property == null)
                    continue;
                Color32 color = property.ProductColor;
                samples.Add(
                    new ProductMixingColorSample(
                        (int)property.Tier,
                        new ProductMixingColorValue(
                            color.r,
                            color.g,
                            color.b,
                            color.a)));
            }

            Color mixedColor =
                ProductMixingColorContract.CalculatePrimaryColor(
                    mixingProfile.MixerMap,
                    samples).ToColor32();
            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(true);
            var propertyBlock = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                renderer.GetPropertyBlock(propertyBlock);
                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0;
                     materialIndex < materials.Length;
                     materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                        continue;
                    if (material.HasProperty("_BaseColor"))
                        propertyBlock.SetColor("_BaseColor", mixedColor);
                    if (material.HasProperty("_Color"))
                        propertyBlock.SetColor("_Color", mixedColor);
                }

                renderer.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }

        private static void ResetMultiTypeVisuals(
            S1Product.MultiTypeVisualsSetter setter)
        {
            setter.WeedVisuals?.ResetVisuals();
            setter.MethVisuals?.ResetVisuals();
            setter.CocaineVisuals?.ResetVisuals();
            setter.ShroomVisuals?.ResetVisuals();
        }

        private static void RemoveDiscoveryVisual(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(
                        child.name,
                        DiscoveryVisualRootName,
                        StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                    Object.Destroy(child.gameObject);
                }
            }
        }

        private static void BuildAndCommit(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfileRegistration registration)
        {
            ProductPresentationProfile profile = registration.Profile;
            CustomProductPresentationState state =
                product.PresentationState ??
                throw new InvalidOperationException(
                    "Presentation state was not initialized.");
            var created = new List<Object>();
            var sources =
                new Dictionary<Func<GameObject?>, GameObject?>();

            var stored = state.BaselineStoredItem;
            var held = state.BaselineEquippable;
            var station = state.BaselineStationItem;
            var functional = state.BaselineFunctionalProduct;
            var icon = state.BaselineIcon;
            var consumption = state.BaselineConsumeAnimation;
            GameObject? registeredAvatarPrefab = null;
            bool generatedIconPending = false;

            try
            {
                stored = BuildStored(
                    product,
                    profile,
                    sources,
                    created,
                    state.BaselineStoredItem);
                station = BuildStation(
                    product,
                    profile,
                    sources,
                    created,
                    state.TemplateStationItem ?? state.BaselineStationItem);
                functional = BuildFunctional(
                    product,
                    profile,
                    sources,
                    created,
                    state.BaselineFunctionalProduct);
                icon =
                    BuildIcon(
                        product,
                        profile,
                        sources,
                        created,
                        state.BaselineIcon,
                        out generatedIconPending);
                consumption =
                    BuildConsumption(
                        product,
                        profile,
                        created,
                        state.BaselineConsumeAnimation);
                held = BuildHeld(
                    product,
                    profile,
                    sources,
                    created,
                    state.BaselineEquippable,
                    out registeredAvatarPrefab);
            }
            catch
            {
                DestroyAll(created);
                throw;
            }

            S1Product.ProductDefinition definition = product.Definition;
            definition.StoredItem = stored;
            definition.Equippable = held;
            definition.StationItem = station;
            definition.FunctionalProduct = functional;
            definition.Icon = icon;
            definition.ConsumeAnimation = consumption;

            List<Object> previous = state.GeneratedObjects;
            UnregisterHeldAvatar(product, state);
            state.GeneratedObjects = created;
            state.RegisteredAvatarPrefab = registeredAvatarPrefab;
            state.AppliedRegistration = registration;
            state.IsGeneratedIconPending = generatedIconPending;
            DestroyAll(previous);
            QueuePendingGeneratedIcon(product, registration, state);
        }

        private static S1Storage.StoredItem? BuildStored(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            Dictionary<Func<GameObject?>, GameObject?> sources,
            List<Object> created,
            S1Storage.StoredItem? fallback)
        {
            GameObject? source =
                GetVisualSource(
                    product,
                    profile,
                    ProductPresentationContext.Stored,
                    sources);
            if (source == null)
                return fallback;
            if (fallback == null)
                return MissingScaffold(product, profile, ProductPresentationContext.Stored, fallback);

            var clone =
                Object.Instantiate(fallback, GetCacheRoot().transform);
            PrepareCachedPrefab(clone.gameObject);
            created.Add(clone.gameObject);
            if (!CrossType.Is(clone, out S1Product.Product_Stored stored))
            {
                return InvalidScaffold(
                    product,
                    profile,
                    ProductPresentationContext.Stored,
                    clone,
                    fallback,
                    created,
                    "Product_Stored");
            }

            stored.Visuals =
                ReplaceVisual(
                    stored.gameObject,
                    stored.Visuals,
                    source,
                    profile,
                    ProductPresentationContext.Stored);
            ApplyGeneratedMixColor(
                stored.Visuals.VisualsContainer.gameObject,
                product,
                created);
            return clone;
        }

        private static S1Equipping.Equippable? BuildHeld(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            Dictionary<Func<GameObject?>, GameObject?> sources,
            List<Object> created,
            S1Equipping.Equippable? fallback,
            out GameObject? registeredAvatarPrefab)
        {
            registeredAvatarPrefab = null;
            GameObject? source =
                GetVisualSource(
                    product,
                    profile,
                    ProductPresentationContext.Held,
                    sources);
            if (source == null)
                return fallback;
            if (fallback == null)
                return MissingScaffold(product, profile, ProductPresentationContext.Held, fallback);

            var clone =
                Object.Instantiate(fallback, GetCacheRoot().transform);
            PrepareCachedPrefab(clone.gameObject);
            created.Add(clone.gameObject);
            if (!CrossType.Is(clone, out S1Product.Product_Equippable held))
            {
                return InvalidScaffold(
                    product,
                    profile,
                    ProductPresentationContext.Held,
                    clone,
                    fallback,
                    created,
                    "Product_Equippable");
            }

            held.Visuals =
                ReplaceVisual(
                    held.gameObject,
                    held.Visuals,
                    source,
                    profile,
                    ProductPresentationContext.Held);
            ApplyGeneratedMixColor(
                held.Visuals.VisualsContainer.gameObject,
                product,
                created);
            held.ModelContainer = held.Visuals.VisualsContainer;
            GameObject avatarSource =
                ResolveAvatarHeldSource(product, profile, source, sources);
            held.AvatarEquippable =
                BuildAvatarEquippable(
                    product,
                    held.AvatarEquippable,
                    avatarSource,
                    profile,
                    created);
            registeredAvatarPrefab = held.AvatarEquippable.gameObject;
            return clone;
        }

        private static S1Station.StationItem? BuildStation(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            Dictionary<Func<GameObject?>, GameObject?> sources,
            List<Object> created,
            S1Station.StationItem? fallback)
        {
            GameObject? source =
                GetVisualSource(
                    product,
                    profile,
                    ProductPresentationContext.Station,
                    sources);
            if (source == null)
                return product.PresentationState?.BaselineStationItem;
            if (fallback == null)
                return MissingScaffold(product, profile, ProductPresentationContext.Station, fallback);

            var clone =
                Object.Instantiate(fallback, GetCacheRoot().transform);
            PrepareCachedPrefab(clone.gameObject);
            created.Add(clone.gameObject);
            if (!CrossType.Is(clone, out S1Station.ProductStationItem station))
            {
                return InvalidScaffold(
                    product,
                    profile,
                    ProductPresentationContext.Station,
                    clone,
                    product.PresentationState?.BaselineStationItem,
                    created,
                    "ProductStationItem");
            }

            station.Visuals =
                ReplaceStationVisual(
                    station.gameObject,
                    station.Visuals,
                    source,
                    profile);
            ApplyGeneratedMixColor(
                station.Visuals.VisualsContainer.gameObject,
                product,
                created);
            return clone;
        }

        private static S1Product.FunctionalProduct? BuildFunctional(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            Dictionary<Func<GameObject?>, GameObject?> sources,
            List<Object> created,
            S1Product.FunctionalProduct? fallback)
        {
            GameObject? source =
                GetVisualSource(
                    product,
                    profile,
                    ProductPresentationContext.FunctionalProduct,
                    sources);
            if (source == null)
                return fallback;
            if (fallback == null)
            {
                return MissingScaffold(
                    product,
                    profile,
                    ProductPresentationContext.FunctionalProduct,
                    fallback);
            }

            var clone =
                Object.Instantiate(fallback, GetCacheRoot().transform);
            PrepareCachedPrefab(clone.gameObject);
            created.Add(clone.gameObject);
            if (!CrossType.Is(clone, out S1Product.FunctionalProduct functional))
            {
                return InvalidScaffold(
                    product,
                    profile,
                    ProductPresentationContext.FunctionalProduct,
                    clone,
                    fallback,
                    created,
                    "FunctionalProduct");
            }

            functional.Visuals =
                ReplaceVisual(
                    functional.gameObject,
                    functional.Visuals,
                    source,
                    profile,
                    ProductPresentationContext.FunctionalProduct);
            ApplyGeneratedMixColor(
                functional.Visuals.VisualsContainer.gameObject,
                product,
                created);
            if (profile.UseFunctionalProductConvexMeshColliders)
            {
                ReplaceWithConvexMeshColliders(
                    functional.gameObject,
                    functional.Visuals.VisualsContainer.gameObject);
            }
            else
            {
                EnsureCollider(functional.Visuals.VisualsContainer.gameObject);
            }
            return clone;
        }

        private static Sprite? BuildIcon(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            Dictionary<Func<GameObject?>, GameObject?> sources,
            List<Object> created,
            Sprite? fallback,
            out bool generatedIconPending)
        {
            generatedIconPending = false;
            if (profile.IconProvider != null)
            {
                try
                {
                    Sprite? sprite = profile.IconProvider();
                    if (sprite != null)
                        return sprite;
                    return ProviderFailed(
                        product,
                        profile,
                        ProductPresentationContext.Icon,
                        "returned null",
                        fallback);
                }
                catch (Exception exception)
                {
                    return ProviderFailed(
                        product,
                        profile,
                        ProductPresentationContext.Icon,
                        exception.Message,
                        fallback);
                }
            }

            if (!profile.GenerateIconFromLooseVisual)
                return MissingProvider(
                    product,
                    profile,
                    ProductPresentationContext.Icon,
                    "no icon provider was configured",
                    fallback);

            generatedIconPending = true;
            return fallback;
        }

        internal static bool GeneratedIconWorkComplete
        {
            get
            {
                lock (GeneratedIconQueueGate)
                {
                    return !_isProcessingGeneratedIcons &&
                           GeneratedIconQueue.Count == 0;
                }
            }
        }

        private static void QueuePendingGeneratedIcon(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfileRegistration registration,
            CustomProductPresentationState state)
        {
            if (!state.IsGeneratedIconPending ||
                state.IsGeneratedIconQueued ||
                !registration.Profile.GenerateIconFromLooseVisual)
            {
                return;
            }

            bool startProcessor = false;
            lock (GeneratedIconQueueGate)
            {
                if (state.IsGeneratedIconQueued)
                    return;

                state.IsGeneratedIconQueued = true;
                GeneratedIconQueue.Enqueue(
                    new GeneratedIconRequest(product, registration, state));
                if (!_isProcessingGeneratedIcons)
                {
                    _isProcessingGeneratedIcons = true;
                    startProcessor = true;
                }
            }

            if (startProcessor)
                MelonCoroutines.Start(ProcessGeneratedIconQueue());
        }

        private static IEnumerator ProcessGeneratedIconQueue()
        {
            while (true)
            {
                GeneratedIconRequest? request;
                lock (GeneratedIconQueueGate)
                {
                    if (GeneratedIconQueue.Count == 0)
                    {
                        _isProcessingGeneratedIcons = false;
                        yield break;
                    }

                    request = GeneratedIconQueue.Dequeue();
                }

                CustomProductPresentationState state = request.State;
                ProductIconRenderRigArbiter.CaptureLease? renderLease = null;
                try
                {
                    if (!state.IsGeneratedIconPending ||
                        !ReferenceEquals(
                            state.AppliedRegistration,
                            request.Registration))
                    {
                        continue;
                    }

                    const int readinessFrames = 300;
                    int readinessFrame = 0;
                    while (!IconFactory.IsItemIconGeneratorReady &&
                           readinessFrame < readinessFrames)
                    {
                        readinessFrame++;
                        yield return null;
                    }

                    if (!IconFactory.IsItemIconGeneratorReady)
                    {
                        LogGeneratedIconFailure(
                            request,
                            "the native item-icon rendering rig did not become ready");
                        continue;
                    }

                    renderLease = ProductIconRenderRigArbiter.Enqueue();
                    while (!ProductIconRenderRigArbiter.TryAcquire(renderLease))
                    {
                        if (!state.IsGeneratedIconPending ||
                            !ReferenceEquals(
                                state.AppliedRegistration,
                                request.Registration))
                        {
                            ProductIconRenderRigArbiter.Cancel(renderLease);
                            renderLease = null;
                            break;
                        }

                        yield return null;
                    }

                    if (renderLease == null)
                        continue;

                    const int maxRetries = 30;
                    string lastError = "the native renderer returned no visible pixels";
                    for (int attempt = 0; attempt <= maxRetries; attempt++)
                    {
                        yield return null;
                        yield return new WaitForEndOfFrame();

                        GeneratedIconAttemptResult result =
                            TryGenerateIcon(
                                request,
                                out Sprite? icon,
                                out Texture2D? texture,
                                out lastError);
                        if (result == GeneratedIconAttemptResult.Success)
                        {
                            request.Product.Definition.Icon = icon;
                            state.GeneratedObjects.Add(texture!);
                            state.GeneratedObjects.Add(icon!);
                            state.IsGeneratedIconPending = false;
                            MelonLogger.Msg(
                                $"[ProductPresentationProfile] Generated loose icon for " +
                                $"'{request.Product.ProductId}' after {attempt + 1} render attempt(s).");
                            break;
                        }

                        if (result == GeneratedIconAttemptResult.Failure)
                            break;
                    }

                    if (state.IsGeneratedIconPending)
                        LogGeneratedIconFailure(request, lastError);

                    // IconFactory restores the shared rig synchronously. Keep
                    // ownership through a full settled frame so another subject
                    // cannot capture a transition between the outgoing model and
                    // the next queued model.
                    yield return null;
                    yield return new WaitForEndOfFrame();
                }
                finally
                {
                    if (renderLease != null)
                        ProductIconRenderRigArbiter.Release(renderLease);

                    state.IsGeneratedIconQueued = false;
                    if (state.IsGeneratedIconPending &&
                        state.AppliedRegistration != null &&
                        !ReferenceEquals(
                            state.AppliedRegistration,
                            request.Registration))
                    {
                        QueuePendingGeneratedIcon(
                            request.Product,
                            state.AppliedRegistration,
                            state);
                    }
                }
            }
        }

        private static GeneratedIconAttemptResult TryGenerateIcon(
            GeneratedIconRequest request,
            out Sprite? icon,
            out Texture2D? texture,
            out string error)
        {
            icon = null;
            texture = null;
            ProductPresentationProfile profile = request.Registration.Profile;
            if (!profile.TryGetVisualProvider(
                    ProductPresentationContext.Loose,
                    out Func<GameObject?>? provider) ||
                provider == null)
            {
                error = "no loose visual provider was configured";
                return GeneratedIconAttemptResult.Failure;
            }

            GameObject? source;
            try
            {
                source = provider();
            }
            catch (Exception exception)
            {
                error = $"the loose visual provider failed: {exception.Message}";
                return GeneratedIconAttemptResult.Failure;
            }

            if (source == null)
            {
                error = "the loose visual provider returned null";
                return GeneratedIconAttemptResult.Failure;
            }

            GameObject model = Object.Instantiate(source);
            var generatedMaterials = new List<Object>();
            try
            {
                ApplyGeneratedMixColor(
                    model,
                    request.Product,
                    generatedMaterials);
                if (profile.GeneratedIconTransform != null)
                {
                    profile.GeneratedIconTransform.ApplyTo(model.transform);
                }
                else
                {
                    ApplyVisualTransform(
                        model.transform,
                        profile,
                        ProductPresentationContext.Loose);
                }
                Texture2D? renderedTexture =
                    IconFactory.GenerateIcon(
                        model.transform,
                        profile.GeneratedIconSize,
                        bakeSkinnedMeshes: true,
                        fitToCamera: profile.FitGeneratedIconToCamera,
                        cameraFill: profile.GeneratedIconCameraFill);
                if (renderedTexture == null)
                {
                    error = "the native renderer returned no visible pixels";
                    return GetGeneratedTextureFailureResult(
                        rendererProducedTexture: false);
                }

                if (!TryCreateUiTexture(
                        renderedTexture,
                        request.Product.ProductId,
                        out texture,
                        out error))
                {
                    return GetGeneratedTextureFailureResult(
                        rendererProducedTexture: true);
                }
            }
            finally
            {
                Object.Destroy(model);
                DestroyAll(generatedMaterials);
            }

            icon = global::S1API.Utils.ImageUtils.TextureToSprite(texture);
            if (icon != null)
            {
                error = string.Empty;
                return GeneratedIconAttemptResult.Success;
            }

            Object.Destroy(texture);
            texture = null;
            error = "the generated texture could not be converted to a sprite";
            return GeneratedIconAttemptResult.Failure;
        }

        internal static GeneratedIconAttemptResult
            GetGeneratedTextureFailureResult(bool rendererProducedTexture)
        {
            return rendererProducedTexture
                ? GeneratedIconAttemptResult.Failure
                : GeneratedIconAttemptResult.Retry;
        }

        private static bool TryCreateUiTexture(
            Texture2D renderedTexture,
            string productId,
            out Texture2D? uiTexture,
            out string error)
        {
            uiTexture = null;
            try
            {
                byte[]? encoded = renderedTexture.EncodeToPNG();
                if (encoded == null || encoded.Length == 0)
                {
                    error = "the generated texture could not be encoded as PNG";
                    return false;
                }

                uiTexture =
                    new Texture2D(
                        2,
                        2,
                        TextureFormat.RGBA32,
                        mipChain: false)
                    {
                        name = $"S1API_ProductIcon_{productId}",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                if (!uiTexture.LoadImage(encoded, markNonReadable: false))
                {
                    Object.Destroy(uiTexture);
                    uiTexture = null;
                    error =
                        "the generated PNG could not be decoded into a UI texture";
                    return false;
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                if (uiTexture != null)
                    Object.Destroy(uiTexture);
                uiTexture = null;
                error =
                    $"generated icon texture normalization failed: "
                    + exception.Message;
                return false;
            }
            finally
            {
                Object.Destroy(renderedTexture);
            }
        }

        private static void LogGeneratedIconFailure(
            GeneratedIconRequest request,
            string reason)
        {
            string message =
                $"Product presentation context 'Icon' for " +
                $"'{request.Product.ProductId}' could not be applied because {reason}.";
            if (request.Registration.Profile.IsRequired(
                    ProductPresentationContext.Icon))
            {
                MelonLogger.Error($"[ProductPresentationProfile] {message}");
            }
            else
            {
                MelonLogger.Warning(
                    $"[ProductPresentationProfile] {message} " +
                    "Preserving the template fallback.");
            }
        }

        private static S1Product.ProductConsumeAnimation? BuildConsumption(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            List<Object> created,
            S1Product.ProductConsumeAnimation? fallback)
        {
            if (profile.ConsumptionPrefabProvider == null)
            {
                return MissingProvider(
                    product,
                    profile,
                    ProductPresentationContext.Consumption,
                    "no consumption prefab provider was configured",
                    fallback);
            }

            GameObject? source;
            try
            {
                source = profile.ConsumptionPrefabProvider();
            }
            catch (Exception exception)
            {
                return ProviderFailed(
                    product,
                    profile,
                    ProductPresentationContext.Consumption,
                    exception.Message,
                    fallback);
            }

            if (source == null)
            {
                return ProviderFailed(
                    product,
                    profile,
                    ProductPresentationContext.Consumption,
                    "returned null",
                    fallback);
            }

            GameObject clone =
                Object.Instantiate(source, GetCacheRoot().transform);
            PrepareCachedPrefab(clone);
            created.Add(clone);
            S1Product.ProductConsumeAnimation? animation =
                clone.GetComponent<S1Product.ProductConsumeAnimation>();
            if (animation != null)
                return animation;

            created.Remove(clone);
            Object.Destroy(clone);
            return RequireOrFallback(
                product,
                profile,
                ProductPresentationContext.Consumption,
                "the supplied prefab does not contain ProductConsumeAnimation",
                fallback);
        }

        private static GameObject? GetVisualSource(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            ProductPresentationContext context,
            Dictionary<Func<GameObject?>, GameObject?> sources)
        {
            if (!profile.TryGetVisualProvider(context, out Func<GameObject?>? provider) ||
                provider == null)
            {
                return MissingProvider<GameObject?>(
                    product,
                    profile,
                    context,
                    "no visual provider or loose-visual fallback was configured",
                    null);
            }

            if (sources.TryGetValue(provider, out GameObject? retained))
            {
                return retained ??
                       ProviderFailed<GameObject?>(
                           product,
                           profile,
                           context,
                           "returned null or failed during an earlier context",
                           null);
            }

            try
            {
                GameObject? source = provider();
                sources.Add(provider, source);
                if (source != null)
                    return source;
                return ProviderFailed<GameObject?>(
                    product,
                    profile,
                    context,
                    "returned null",
                    null);
            }
            catch (Exception exception)
            {
                sources.Add(provider, null);
                return ProviderFailed<GameObject?>(
                    product,
                    profile,
                    context,
                    exception.Message,
                    null);
            }
        }

        private static StaticProductVisualsSetter ReplaceVisual(
            GameObject scaffold,
            S1Product.ProductVisualsSetter? oldSetter,
            GameObject source,
            ProductPresentationProfile profile,
            ProductPresentationContext context)
        {
            Transform parent =
                oldSetter != null && oldSetter.VisualsContainer != null &&
                oldSetter.VisualsContainer != scaffold.transform
                    ? oldSetter.VisualsContainer.parent
                    : scaffold.transform;

            if (oldSetter != null &&
                oldSetter.VisualsContainer != null &&
                oldSetter.VisualsContainer != scaffold.transform)
            {
                oldSetter.VisualsContainer.gameObject.SetActive(false);
            }

            GameObject visual = CreateVisual(source, parent, profile, context);

            StaticProductVisualsSetter setter =
                scaffold.AddComponent<StaticProductVisualsSetter>();
            setter.VisualsContainer = visual.transform;
            return setter;
        }

        private static S1Product.ProductVisualsSetter ReplaceStationVisual(
            GameObject scaffold,
            S1Product.ProductVisualsSetter? oldSetter,
            GameObject source,
            ProductPresentationProfile profile)
        {
            S1Station.IngredientPiece? ingredientPiece =
                scaffold.GetComponentInChildren<S1Station.IngredientPiece>(true);
            if (ingredientPiece == null ||
                ingredientPiece.ModelContainer == null ||
                oldSetter == null ||
                oldSetter.VisualsContainer == null)
            {
                return ReplaceVisual(
                    scaffold,
                    oldSetter,
                    source,
                    profile,
                    ProductPresentationContext.Station);
            }

            Transform? replacementParent =
                oldSetter.VisualsContainer != scaffold.transform
                    ? oldSetter.VisualsContainer.parent
                    : scaffold.transform;
            GameObject visual =
                CreateVisual(
                    source,
                    replacementParent ?? scaffold.transform,
                    profile,
                    ProductPresentationContext.Station);

            Transform interactionRoot = ingredientPiece.transform;
            RetireStationModel(ingredientPiece.ModelContainer, interactionRoot);

            // Keep the profile-authored pose while moving the replacement under
            // the native rigidbody/Draggable object that owns station interaction.
            visual.transform.SetParent(interactionRoot, true);
            EnsureCollider(visual);
            ingredientPiece.ModelContainer = visual.transform;

            StaticStationProductVisualsSetter setter =
                scaffold.AddComponent<StaticStationProductVisualsSetter>();
            setter.VisualsContainer = visual.transform;

            // The cached prefab is active so native instantiation preserves its
            // active state. Freeze only the physics child until Initialize()
            // activates it through the station-specific visuals setter.
            interactionRoot.gameObject.SetActive(false);
            return setter;
        }

        private static GameObject CreateVisual(
            GameObject source,
            Transform parent,
            ProductPresentationProfile profile,
            ProductPresentationContext context)
        {
            GameObject visual = Object.Instantiate(source);
            visual.transform.SetParent(parent, false);
            ApplyVisualTransform(visual.transform, profile, context);
            visual.SetActive(true);
            return visual;
        }

        private static void RetireStationModel(
            Transform modelContainer,
            Transform interactionRoot)
        {
            if (modelContainer != interactionRoot)
            {
                modelContainer.gameObject.SetActive(false);
                return;
            }

            Renderer[] renderers =
                modelContainer.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;

            Collider[] colliders =
                modelContainer.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private static S1AvatarEquipping.AvatarEquippable BuildAvatarEquippable(
            CustomProductDefinitionRegistration product,
            S1AvatarEquipping.AvatarEquippable template,
            GameObject source,
            ProductPresentationProfile profile,
            List<Object> created)
        {
            var root =
                new GameObject(
                    $"{product.ProductId} Held Avatar Presentation");
            root.transform.SetParent(GetCacheRoot().transform, false);
            PrepareCachedPrefab(root);
            created.Add(root);
            GameObject visual = Object.Instantiate(source);
            visual.transform.SetParent(root.transform, false);
            ApplyGeneratedMixColor(visual, product, created);
            if (profile.TryGetAvatarHeldTransform(
                    out ProductPresentationTransform? avatarTransform))
            {
                avatarTransform?.ApplyTo(visual.transform);
            }

            S1AvatarEquipping.AvatarEquippable avatar =
                root.AddComponent<S1AvatarEquipping.AvatarEquippable>();
            avatar.AlignmentPoint = root.transform;
            if (template != null)
            {
                avatar.Suspiciousness = template.Suspiciousness;
                avatar.Hand = template.Hand;
                avatar.TriggerType = template.TriggerType;
                avatar.AnimationTrigger = template.AnimationTrigger;
            }

            string assetPath =
                $"S1API/ProductPresentation/{product.ProductId}/Held";
            avatar.AssetPath = assetPath;
            if (!AvatarEquippableRegistry.RegisterAvatarEquippable(assetPath, root))
            {
                throw new InvalidOperationException(
                    $"Could not register third-person held presentation path '{assetPath}'.");
            }

            return avatar;
        }

        private static GameObject ResolveAvatarHeldSource(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            GameObject heldSource,
            Dictionary<Func<GameObject?>, GameObject?> sources)
        {
            Func<GameObject?>? provider = profile.AvatarHeldVisualProvider;
            if (provider == null)
                return heldSource;

            if (sources.TryGetValue(provider, out GameObject? cached))
                return cached ?? heldSource;

            GameObject? source;
            try
            {
                source = provider();
            }
            catch (Exception exception)
            {
                sources.Add(provider, null);
                MelonLogger.Warning(
                    $"[ProductPresentationProfile] Avatar-held visual provider for " +
                    $"'{product.ProductId}' failed; using the held visual instead: " +
                    exception.Message);
                return heldSource;
            }

            sources.Add(provider, source);
            if (source != null)
                return source;

            MelonLogger.Warning(
                $"[ProductPresentationProfile] Avatar-held visual provider for " +
                $"'{product.ProductId}' returned null; using the held visual instead.");
            return heldSource;
        }

        private static void ApplyVisualTransform(
            Transform target,
            ProductPresentationProfile profile,
            ProductPresentationContext context)
        {
            if (profile.TryGetVisualTransform(
                    context,
                    out ProductPresentationTransform? presentationTransform))
            {
                presentationTransform?.ApplyTo(target);
            }
        }

        private static void ApplyGeneratedMixColor(
            GameObject visual,
            CustomProductDefinitionRegistration product,
            List<Object> created)
        {
            Color32? generatedMixColor = product.Metadata?.GeneratedMixColor;
            if (!generatedMixColor.HasValue)
                return;

            Color color = generatedMixColor.Value;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0;
                 rendererIndex < renderers.Length;
                 rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] sourceMaterials = renderer.sharedMaterials;
                var coloredMaterials = new Material[sourceMaterials.Length];
                for (int materialIndex = 0;
                     materialIndex < sourceMaterials.Length;
                     materialIndex++)
                {
                    Material sourceMaterial = sourceMaterials[materialIndex];
                    if (sourceMaterial == null)
                        continue;

                    var coloredMaterial = new Material(sourceMaterial)
                    {
                        name = sourceMaterial.name + "_S1API_MixedColor"
                    };
                    if (coloredMaterial.HasProperty("_BaseColor"))
                        coloredMaterial.SetColor("_BaseColor", color);
                    if (coloredMaterial.HasProperty("_Color"))
                        coloredMaterial.SetColor("_Color", color);
                    coloredMaterials[materialIndex] = coloredMaterial;
                    created.Add(coloredMaterial);
                }

                renderer.sharedMaterials = coloredMaterials;
            }
        }

        private static GameObject GetCacheRoot()
        {
            if (_cacheRoot != null)
                return _cacheRoot;

            var root = new GameObject("S1API_ProductPresentationCache");
            root.hideFlags = HideFlags.HideAndDontSave;
            root.transform.position = new Vector3(0f, -10000f, 0f);
            Object.DontDestroyOnLoad(root);
            _cacheRoot = root;
            return root;
        }

        private static void PrepareCachedPrefab(GameObject prefab)
        {
            prefab.hideFlags = HideFlags.HideAndDontSave;
        }

        private static void EnsureCollider(GameObject visual)
        {
            if (visual.GetComponentInChildren<Collider>(true) != null)
                return;

            AddBoxCollider(visual);
        }

        private static void AddBoxCollider(GameObject visual)
        {
            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = new Bounds(
                visual.transform.InverseTransformPoint(renderers[0].bounds.center),
                Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds rendererBounds = renderers[i].bounds;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            bounds.Encapsulate(
                                visual.transform.InverseTransformPoint(
                                    new Vector3(
                                        x == 0 ? min.x : max.x,
                                        y == 0 ? min.y : max.y,
                                        z == 0 ? min.z : max.z)));
                        }
                    }
                }
            }

            BoxCollider collider = visual.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        private static void ReplaceWithConvexMeshColliders(
            GameObject scaffold,
            GameObject visual)
        {
            Collider[] inheritedColliders =
                scaffold.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < inheritedColliders.Length; i++)
                inheritedColliders[i].enabled = false;

            MeshFilter[] filters =
                visual.GetComponentsInChildren<MeshFilter>(true);
            int added = 0;
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (TryAddConvexMeshCollider(filter))
                    added++;
            }

            if (added == 0)
                AddBoxCollider(visual);
        }

        private static bool TryAddConvexMeshCollider(MeshFilter filter)
        {
            Mesh? mesh = filter.sharedMesh;
            if (mesh == null ||
                mesh.bounds.size.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            MeshCollider collider =
                filter.gameObject.AddComponent<MeshCollider>();
            try
            {
                collider.convex = true;
                collider.sharedMesh = mesh;
                if (collider.enabled &&
                    collider.convex &&
                    collider.sharedMesh != null &&
                    (!collider.gameObject.activeInHierarchy ||
                     collider.bounds.size.sqrMagnitude > Mathf.Epsilon))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                MelonLogger.Warning(
                    $"[ProductPresentationProfile] Could not create a convex " +
                    $"mesh collider from '{mesh.name}': {exception.Message}");
            }

            collider.enabled = false;
            Object.Destroy(collider);
            return false;
        }

        private static T MissingScaffold<T>(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            ProductPresentationContext context,
            T fallback)
        {
            return RequireOrFallback(
                product,
                profile,
                context,
                "the representation template does not provide the required native scaffold",
                fallback);
        }

        private static T InvalidScaffold<T>(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            ProductPresentationContext context,
            Component clone,
            T fallback,
            List<Object> created,
            string requiredComponent)
        {
            created.Remove(clone.gameObject);
            Object.Destroy(clone.gameObject);
            return RequireOrFallback(
                product,
                profile,
                context,
                $"the template scaffold does not contain {requiredComponent}",
                fallback);
        }

        private static T ProviderFailed<T>(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            ProductPresentationContext context,
            string reason,
            T fallback)
        {
            return RequireOrFallback(
                product,
                profile,
                context,
                $"the provider failed: {reason}",
                fallback);
        }

        private static T MissingProvider<T>(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            ProductPresentationContext context,
            string reason,
            T fallback)
        {
            if (profile.IsRequired(context))
            {
                throw new InvalidOperationException(
                    $"Product presentation context '{context}' for '{product.ProductId}' could " +
                    $"not be applied because {reason}.");
            }

            return fallback;
        }

        private static T RequireOrFallback<T>(
            CustomProductDefinitionRegistration product,
            ProductPresentationProfile profile,
            ProductPresentationContext context,
            string reason,
            T fallback)
        {
            string message =
                $"Product presentation context '{context}' for '{product.ProductId}' could not " +
                $"be applied because {reason}.";
            if (profile.IsRequired(context))
                throw new InvalidOperationException(message);

            MelonLogger.Warning(
                $"[ProductPresentationProfile] {message} Preserving the template fallback.");
            return fallback;
        }

        private static void RestoreBaseline(
            CustomProductDefinitionRegistration product)
        {
            CustomProductPresentationState state =
                product.PresentationState ??
                throw new InvalidOperationException(
                    "Presentation state was not initialized.");
            S1Product.ProductDefinition definition = product.Definition;
            definition.Icon = state.BaselineIcon;
            definition.StoredItem = state.BaselineStoredItem;
            definition.Equippable = state.BaselineEquippable;
            definition.StationItem = state.BaselineStationItem;
            definition.FunctionalProduct = state.BaselineFunctionalProduct;
            definition.ConsumeAnimation = state.BaselineConsumeAnimation;
            UnregisterHeldAvatar(product, state);
            DestroyAll(state.GeneratedObjects);
            state.GeneratedObjects = new List<Object>();
            state.RegisteredAvatarPrefab = null;
            state.AppliedRegistration = null;
            state.IsGeneratedIconPending = false;
        }

        private static void UnregisterHeldAvatar(
            CustomProductDefinitionRegistration product,
            CustomProductPresentationState state)
        {
            if (state.RegisteredAvatarPrefab == null)
                return;

            string assetPath =
                $"S1API/ProductPresentation/{product.ProductId}/Held";
            RuntimeResourceRegistry.UnregisterAssetIfMatches(
                assetPath,
                state.RegisteredAvatarPrefab);
        }

        private static bool GeneratedObjectsAreAlive(IReadOnlyList<Object> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] == null)
                    return false;
            }

            return true;
        }

        private static void DestroyAll(IReadOnlyList<Object> objects)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                    Object.Destroy(objects[i]);
            }
        }

    }
}
