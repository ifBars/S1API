using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using S1API.Products;
using S1API.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
#if IL2CPPMELON
using Il2CppInterop.Runtime;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Product = Il2CppScheduleOne.Product;
using S1UI = Il2CppScheduleOne.UI;
#elif MONOMELON
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Product = ScheduleOne.Product;
using S1UI = ScheduleOne.UI;
#endif

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Replaces native type-switched package contents on individual runtime objects.
    /// </summary>
    internal static class ProductPackagingContentRuntime
    {
        private const string GeneratedRootNamePrefix =
            "S1API_PackagingContent_";
        private static readonly object IconGate = new object();
        private static readonly object IconQueueGate = new object();
        private static readonly object FailureGate = new object();
        private static readonly Dictionary<
            ProductPackagingContentKey,
            GeneratedPackagingIcon> GeneratedIcons =
                new Dictionary<
                    ProductPackagingContentKey,
                    GeneratedPackagingIcon>();
        private static readonly HashSet<string> LoggedFailures =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly Queue<ProductPackagingContentProfileRegistration>
            GeneratedIconQueue =
                new Queue<ProductPackagingContentProfileRegistration>();
        private static readonly HashSet<ProductPackagingContentKey>
            QueuedGeneratedIcons =
                new HashSet<ProductPackagingContentKey>();
        private static int _iconGeneratorInstanceId;
        private static int _iconQueueGeneration;
        private static bool _isProcessingGeneratedIcons;
        private static int _nativeIconBatchDepth;
        private static int _nativeIconFallbackDepth;

        private sealed class GeneratedPackagingIcon
        {
            internal GeneratedPackagingIcon(Texture2D texture, Sprite icon)
            {
                Texture = texture;
                Icon = icon;
            }

            internal Texture2D Texture { get; }

            internal Sprite Icon { get; }
        }

        internal static bool TryApply(
            S1Product.MultiTypeVisualsSetter setter,
            S1Product.ProductItemInstance product)
        {
            if (setter == null)
                return false;

            if (product == null ||
                string.IsNullOrWhiteSpace(product.PackagingID) ||
                !ProductPackagingContentProfileRegistry.TryResolve(
                    product.ID,
                    product.PackagingID,
                    out ProductPackagingContentProfileRegistration? registration) ||
                registration == null)
            {
                RemoveOwnedRoots(setter.transform, null);
                return false;
            }

            string expectedRootName = GetGeneratedRootName(registration.Key);
            Transform? existing =
                FindOwnedRoot(setter.transform, expectedRootName);
            RemoveOwnedRoots(setter.transform, existing);

            if (existing != null)
            {
                if (!TryDisableNativeVisuals(setter, registration, "runtime visual"))
                {
                    DestroyOwnedRoot(existing.gameObject);
                    return false;
                }

                existing.gameObject.SetActive(true);
                return true;
            }

            if (!TryCreateContentRoot(
                    setter,
                    registration,
                    expectedRootName,
                    out GameObject? generatedRoot))
            {
                return false;
            }

            if (!TryDisableNativeVisuals(
                    setter,
                    registration,
                    "runtime visual"))
            {
                DestroyOwnedRoot(generatedRoot!);
                return false;
            }

            generatedRoot!.SetActive(true);
            return true;
        }

        internal static bool TryGeneratePackagingIcon(
            S1DevUtilities.IconGenerator generator,
            string packagingId,
            string productId,
            out Texture2D? texture)
        {
            texture = null;
            if (generator == null ||
                string.IsNullOrWhiteSpace(productId) ||
                string.IsNullOrWhiteSpace(packagingId) ||
                !ProductPackagingContentProfileRegistry.TryResolve(
                    productId,
                    packagingId,
                    out ProductPackagingContentProfileRegistration? registration) ||
                registration == null)
            {
                return false;
            }

            lock (IconGate)
            {
                return TryGeneratePackagingIconCore(
                    generator,
                    registration,
                    out texture);
            }
        }

        internal static void BeginNativeIconBatch()
        {
            _nativeIconBatchDepth++;
        }

        internal static void EndNativeIconBatch()
        {
            if (_nativeIconBatchDepth > 0)
                _nativeIconBatchDepth--;
        }

        internal static bool TryDeferNativeBatchPackagingIcon(
            string packagingId,
            string productId)
        {
            if (_nativeIconBatchDepth <= 0 ||
                string.IsNullOrWhiteSpace(productId) ||
                string.IsNullOrWhiteSpace(packagingId) ||
                !ProductPackagingContentProfileRegistry.TryResolve(
                    productId,
                    packagingId,
                    out ProductPackagingContentProfileRegistration? registration) ||
                registration == null)
            {
                return false;
            }

            QueueGeneratedIcon(registration);
            _nativeIconFallbackDepth++;
            return true;
        }

        internal static void EndNativeIconFallback()
        {
            if (_nativeIconFallbackDepth > 0)
                _nativeIconFallbackDepth--;
        }

        internal static bool IsNativeIconFallbackActive =>
            _nativeIconFallbackDepth > 0;

        internal static bool TryGetPackagingIcon(
            string productId,
            string packagingId,
            out Sprite? icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(productId) ||
                string.IsNullOrWhiteSpace(packagingId) ||
                !ProductPackagingContentProfileRegistry.TryResolve(
                    productId,
                    packagingId,
                    out ProductPackagingContentProfileRegistration? registration) ||
                registration == null)
            {
                return false;
            }

            S1DevUtilities.IconGenerator generator;
            try
            {
                generator = IconFactory.S1IconGenerator;
            }
            catch
            {
                return false;
            }

            if (generator == null)
                return false;

            lock (IconGate)
            {
                EnsureIconCacheMatches(generator);
                if (GeneratedIcons.TryGetValue(
                        registration.Key,
                        out GeneratedPackagingIcon? existing))
                {
                    if (existing.Icon != null && existing.Texture != null)
                    {
                        icon = existing.Icon;
                        return true;
                    }

                    DestroyGeneratedIcon(existing);
                    GeneratedIcons.Remove(registration.Key);
                }
            }

            QueueGeneratedIcon(registration);
            return TryGetLooseProductIcon(
                registration,
                productId,
                out icon);
        }

        internal static void QueueRegisteredIconsForLoading()
        {
            ProductPackagingContentProfileRegistration[] registrations =
                ProductPackagingContentProfileRegistry.Snapshot();
            for (int i = 0; i < registrations.Length; i++)
                QueueGeneratedIcon(registrations[i]);
        }

        internal static void ResetForSceneChange()
        {
            lock (IconGate)
            {
                DestroyGeneratedIcons();
                _iconGeneratorInstanceId = 0;
            }

            lock (IconQueueGate)
            {
                _iconQueueGeneration++;
                GeneratedIconQueue.Clear();
                QueuedGeneratedIcons.Clear();
                _isProcessingGeneratedIcons = false;
            }

            _nativeIconBatchDepth = 0;
            _nativeIconFallbackDepth = 0;

            lock (FailureGate)
                LoggedFailures.Clear();
        }

        internal static bool GeneratedIconWorkComplete
        {
            get
            {
                lock (IconQueueGate)
                {
                    return !_isProcessingGeneratedIcons &&
                           GeneratedIconQueue.Count == 0;
                }
            }
        }

        internal static int CachedIconCountForTesting
        {
            get
            {
                lock (IconGate)
                    return GeneratedIcons.Count;
            }
        }

        internal static string GetGeneratedRootNameForTesting(
            string productId,
            string packagingId)
        {
            return GetGeneratedRootName(
                new ProductPackagingContentKey(productId, packagingId));
        }

        internal static bool MatchesRegistrationForTesting(
            string? productId,
            string? packagingId,
            string registeredProductId,
            string registeredPackagingId)
        {
            return MatchesRegistration(
                productId,
                packagingId,
                new ProductPackagingContentKey(
                    registeredProductId,
                    registeredPackagingId));
        }

        private static bool TryGeneratePackagingIconCore(
            S1DevUtilities.IconGenerator generator,
            ProductPackagingContentProfileRegistration registration,
            out Texture2D? texture)
        {
            texture = null;
            Transform? shellSource =
                FindPackagingShell(generator, registration.Key.PackagingId);
            if (shellSource == null)
            {
                LogFailureOnce(
                    registration,
                    "composite icon",
                    "the active native icon rig has no matching packaging shell");
                return false;
            }

            bool? mainContainerWasActive =
                generator.MainContainer != null
                    ? generator.MainContainer.gameObject.activeSelf
                    : null;
            bool? itemContainerWasActive =
                generator.ItemContainer != null
                    ? generator.ItemContainer.gameObject.activeSelf
                    : null;
            AmbientMode originalAmbientMode = RenderSettings.ambientMode;
            Color originalAmbientLight = RenderSettings.ambientLight;
            Color originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            Color originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            Color originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            GameObject? shell = null;

            try
            {
                shell =
                    Object.Instantiate(
                        shellSource.gameObject,
                        shellSource.parent,
                        worldPositionStays: false);
                S1Product.MultiTypeVisualsSetter setter =
                    shell.GetComponentInChildren<
                        S1Product.MultiTypeVisualsSetter>(true);
                if (setter == null)
                {
                    LogFailureOnce(
                        registration,
                        "composite icon",
                        "the matching native shell has no content anchor");
                    return false;
                }

                if (!TryCreateContentRoot(
                        setter,
                        registration,
                        GetGeneratedRootName(registration.Key),
                        out GameObject? generatedRoot))
                {
                    return false;
                }

                if (!TryDisableNativeVisuals(
                        setter,
                        registration,
                        "composite icon"))
                {
                    return false;
                }

                int iconLayer = setter.gameObject.layer;
                S1DevUtilities.LayerUtility.SetLayerRecursively(
                    generatedRoot!,
                    iconLayer);
                generatedRoot!.SetActive(true);
                shell.SetActive(true);
                texture = generator.GetTexture(setter.transform.parent);
                if (texture == null)
                {
                    LogFailureOnce(
                        registration,
                        "composite icon",
                        "the native icon renderer returned no texture");
                    return false;
                }

                if (!IconFactory.HasVisibleContent(texture))
                {
                    Object.Destroy(texture);
                    texture = null;
                    LogFailureOnce(
                        registration,
                        "composite icon",
                        "the native icon renderer returned no visible pixels");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                if (texture != null)
                {
                    Object.Destroy(texture);
                    texture = null;
                }

                LogFailureOnce(
                    registration,
                    "composite icon",
                    exception.Message);
                return false;
            }
            finally
            {
                if (shell != null)
                {
                    shell.SetActive(false);
                    Object.Destroy(shell);
                }
                if (mainContainerWasActive.HasValue &&
                    generator.MainContainer != null)
                {
                    generator.MainContainer.gameObject.SetActive(
                        mainContainerWasActive.Value);
                }

                if (itemContainerWasActive.HasValue &&
                    generator.ItemContainer != null)
                {
                    generator.ItemContainer.gameObject.SetActive(
                        itemContainerWasActive.Value);
                }

                RenderSettings.ambientMode = originalAmbientMode;
                RenderSettings.ambientLight = originalAmbientLight;
                RenderSettings.ambientSkyColor = originalAmbientSkyColor;
                RenderSettings.ambientEquatorColor =
                    originalAmbientEquatorColor;
                RenderSettings.ambientGroundColor =
                    originalAmbientGroundColor;
            }
        }

        private static void QueueGeneratedIcon(
            ProductPackagingContentProfileRegistration registration)
        {
            lock (IconGate)
            {
                if (GeneratedIcons.TryGetValue(
                        registration.Key,
                        out GeneratedPackagingIcon? generated) &&
                    generated.Icon != null &&
                    generated.Texture != null)
                {
                    return;
                }
            }

            bool startProcessor = false;
            int generation;
            lock (IconQueueGate)
            {
                if (!QueuedGeneratedIcons.Add(registration.Key))
                    return;

                GeneratedIconQueue.Enqueue(registration);
                generation = _iconQueueGeneration;
                if (!_isProcessingGeneratedIcons)
                {
                    _isProcessingGeneratedIcons = true;
                    startProcessor = true;
                }
            }

            if (startProcessor)
            {
                try
                {
                    MelonCoroutines.Start(
                        ProcessGeneratedIconQueue(generation));
                }
                catch (Exception exception)
                {
                    lock (IconQueueGate)
                    {
                        if (generation == _iconQueueGeneration)
                        {
                            GeneratedIconQueue.Clear();
                            QueuedGeneratedIcons.Clear();
                            _isProcessingGeneratedIcons = false;
                        }
                    }

                    LogFailureOnce(
                        registration,
                        "composite icon queue",
                        exception.Message);
                }
            }
        }

        private static IEnumerator ProcessGeneratedIconQueue(int generation)
        {
            while (true)
            {
                ProductPackagingContentProfileRegistration? registration;
                lock (IconQueueGate)
                {
                    if (generation != _iconQueueGeneration)
                        yield break;

                    if (GeneratedIconQueue.Count == 0)
                    {
                        _isProcessingGeneratedIcons = false;
                        yield break;
                    }

                    registration = GeneratedIconQueue.Dequeue();
                }

                const int readinessFrames = 300;
                int readinessFrame = 0;
                while (!IconFactory.IsItemIconGeneratorReady &&
                       readinessFrame < readinessFrames)
                {
                    readinessFrame++;
                    yield return null;
                }

                if (generation != _iconQueueGeneration)
                    yield break;

                if (!IconFactory.IsItemIconGeneratorReady)
                {
                    LogFailureOnce(
                        registration,
                        "composite icon",
                        "the native item-icon rendering rig did not become ready");
                    CompleteQueuedIcon(registration.Key, generation);
                    continue;
                }

                ProductIconRenderRigArbiter.CaptureLease renderLease =
                    ProductIconRenderRigArbiter.Enqueue();
                try
                {
                    while (!ProductIconRenderRigArbiter.TryAcquire(renderLease))
                    {
                        if (generation != _iconQueueGeneration)
                            yield break;

                        yield return null;
                    }

                    yield return null;
                    yield return new WaitForEndOfFrame();

                    if (generation != _iconQueueGeneration)
                        yield break;

                    try
                    {
                        S1DevUtilities.IconGenerator generator =
                            IconFactory.S1IconGenerator;
                        bool iconCached = false;
                        lock (IconGate)
                        {
                            EnsureIconCacheMatches(generator);
                            if (!GeneratedIcons.ContainsKey(registration.Key) &&
                                TryGeneratePackagingIconCore(
                                    generator,
                                    registration,
                                    out Texture2D? texture) &&
                                texture != null)
                            {
                                iconCached =
                                    TryCacheGeneratedIcon(registration, texture);
                            }
                        }

                        if (iconCached)
                            RefreshMatchingItemUis(registration);
                    }
                    catch (Exception exception)
                    {
                        LogFailureOnce(
                            registration,
                            "composite icon",
                            exception.Message);
                    }

                    // TryGeneratePackagingIconCore restores the native rig in
                    // its finally block. Hold the lease through a full settled
                    // frame before the next queued subject can capture.
                    yield return null;
                    yield return new WaitForEndOfFrame();
                }
                finally
                {
                    ProductIconRenderRigArbiter.Release(renderLease);
                    CompleteQueuedIcon(registration.Key, generation);
                }
            }
        }

        private static bool TryGetLooseProductIcon(
            ProductPackagingContentProfileRegistration registration,
            string productId,
            out Sprite? icon)
        {
            icon = null;
            try
            {
                icon =
                    global::S1API.Items.ItemManager.GetDefinition(productId)
                        ?.Icon;
                return icon != null;
            }
            catch (Exception exception)
            {
                LogFailureOnce(
                    registration,
                    "loose-icon lookup",
                    exception.Message);
                return false;
            }
        }

        private static void RefreshMatchingItemUis(
            ProductPackagingContentProfileRegistration registration)
        {
            try
            {
                S1UI.ItemSlotUI[] slotUis =
                    Object.FindObjectsOfType<S1UI.ItemSlotUI>(
                        includeInactive: true);
                for (int i = 0; i < slotUis.Length; i++)
                {
                    S1UI.ItemSlotUI slotUi = slotUis[i];
                    if (slotUi == null)
                        continue;

                    S1ItemFramework.ItemInstance? item =
                        slotUi.assignedSlot?.ItemInstance;
                    if (!TryGetProductPackaging(
                            item,
                            out string? productId,
                            out string? packagingId) ||
                        !MatchesRegistration(
                            productId,
                            packagingId,
                            registration.Key))
                    {
                        continue;
                    }

                    slotUi.UpdateUI();
                }
            }
            catch (Exception exception)
            {
                LogFailureOnce(
                    registration,
                    "bound item UI refresh",
                    exception.Message);
            }
        }

        private static bool TryGetProductPackaging(
            S1ItemFramework.ItemInstance? item,
            out string? productId,
            out string? packagingId)
        {
            productId = null;
            packagingId = null;
            if (item == null)
                return false;

            S1Product.ProductItemInstance? product;
#if IL2CPPMELON
            product = item.TryCast<S1Product.ProductItemInstance>();
#else
            product = item as S1Product.ProductItemInstance;
#endif
            if (product == null)
                return false;

            productId = product.ID;
            packagingId = product.PackagingID;
            return true;
        }

        private static bool MatchesRegistration(
            string? productId,
            string? packagingId,
            ProductPackagingContentKey key)
        {
            return string.Equals(
                       productId,
                       key.ProductId,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       packagingId,
                       key.PackagingId,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static void CompleteQueuedIcon(
            ProductPackagingContentKey key,
            int generation)
        {
            lock (IconQueueGate)
            {
                if (generation == _iconQueueGeneration)
                    QueuedGeneratedIcons.Remove(key);
            }
        }

        private static bool TryCacheGeneratedIcon(
            ProductPackagingContentProfileRegistration registration,
            Texture2D texture)
        {
            Sprite? generated = null;
            try
            {
                texture.Apply();
                generated =
                    Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                if (generated == null)
                {
                    Object.Destroy(texture);
                    LogFailureOnce(
                        registration,
                        "composite icon",
                        "the rendered texture could not be converted to a sprite");
                    return false;
                }

                generated.name =
                    $"{registration.Key.ProductId}_" +
                    $"{registration.Key.PackagingId}_S1API";
                GeneratedIcons.Add(
                    registration.Key,
                    new GeneratedPackagingIcon(texture, generated));
                return true;
            }
            catch (Exception exception)
            {
                if (generated != null)
                    Object.Destroy(generated);
                Object.Destroy(texture);
                LogFailureOnce(
                    registration,
                    "composite icon",
                    exception.Message);
                return false;
            }
        }

        private static Transform? FindPackagingShell(
            S1DevUtilities.IconGenerator generator,
            string packagingId)
        {
            if (generator.Visuals == null)
                return null;

            for (int i = 0; i < generator.Visuals.Count; i++)
            {
                var visuals = generator.Visuals[i];
                if (visuals != null &&
                    string.Equals(
                        visuals.PackagingID,
                        packagingId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return visuals.TopLevelTransform;
                }
            }

            return null;
        }

        private static bool TryCreateContentRoot(
            S1Product.MultiTypeVisualsSetter setter,
            ProductPackagingContentProfileRegistration registration,
            string rootName,
            out GameObject? generatedRoot)
        {
            Transform parent = setter.transform;
            generatedRoot = new GameObject(rootName);
            generatedRoot.transform.SetParent(parent, false);
            generatedRoot.SetActive(false);

            if (registration.Profile.Source ==
                ProductPackagingContentSource.NativeFilledVisualScaffold)
            {
                if (TryCreateNativeScaffold(
                    setter,
                    registration,
                    generatedRoot))
                {
                    return true;
                }

                generatedRoot = null;
                return false;
            }

            GameObject? source;
            try
            {
                source = registration.Profile.ContentProvider!();
            }
            catch (Exception exception)
            {
                LogFailureOnce(
                    registration,
                    "content provider",
                    exception.Message);
                DestroyOwnedRoot(generatedRoot);
                generatedRoot = null;
                return false;
            }

            if (source == null)
            {
                LogFailureOnce(
                    registration,
                    "content provider",
                    "the provider returned null");
                DestroyOwnedRoot(generatedRoot);
                generatedRoot = null;
                return false;
            }

            try
            {
                if (registration.Profile.Source ==
                    ProductPackagingContentSource.CompleteFilledVisual)
                {
                    AddContentClone(
                        source,
                        generatedRoot.transform,
                        registration.Profile.CompleteVisualTransform,
                        null);
                    return true;
                }

                if (registration.Profile.Placements.Count == 0)
                {
                    AddContentClone(
                        source,
                        generatedRoot.transform,
                        null,
                        null);
                    return true;
                }

                for (int i = 0; i < registration.Profile.Placements.Count; i++)
                {
                    AddContentClone(
                        source,
                        generatedRoot.transform,
                        registration.Profile.Placements[i],
                        null);
                }

                return true;
            }
            catch (Exception exception)
            {
                LogFailureOnce(
                    registration,
                    "content composition",
                    exception.Message);
                DestroyOwnedRoot(generatedRoot);
                generatedRoot = null;
                return false;
            }
        }

        private static bool TryCreateNativeScaffold(
            S1Product.MultiTypeVisualsSetter setter,
            ProductPackagingContentProfileRegistration registration,
            GameObject generatedRoot)
        {
            GameObject? source =
                GetNativeScaffoldSource(
                    setter,
                    registration.Profile.NativeVisualTemplate!.Value);
            if (source == null)
            {
                LogFailureOnce(
                    registration,
                    "native visual scaffold",
                    $"the selected template " +
                    $"'{registration.Profile.NativeVisualTemplate!.Value}' is " +
                    "unavailable in this packaging context");
                DestroyOwnedRoot(generatedRoot);
                return false;
            }

            try
            {
                AddContentClone(
                    source,
                    generatedRoot.transform,
                    registration.Profile.CompleteVisualTransform,
                    registration.Profile.NativeVisualCustomizer);
                return true;
            }
            catch (Exception exception)
            {
                LogFailureOnce(
                    registration,
                    "native visual scaffold",
                    exception.Message);
                DestroyOwnedRoot(generatedRoot);
                return false;
            }
        }

        private static GameObject? GetNativeScaffoldSource(
            S1Product.MultiTypeVisualsSetter setter,
            ProductPackagingVisualTemplate template)
        {
            Transform? container;
            switch (template)
            {
                case ProductPackagingVisualTemplate.Marijuana:
                    container = setter.WeedVisuals?.VisualsContainer;
                    break;
                case ProductPackagingVisualTemplate.Methamphetamine:
                    container = setter.MethVisuals?.VisualsContainer;
                    break;
                case ProductPackagingVisualTemplate.Cocaine:
                    container = setter.CocaineVisuals?.VisualsContainer;
                    break;
                case ProductPackagingVisualTemplate.Shrooms:
                    container = setter.ShroomVisuals?.VisualsContainer;
                    break;
                default:
                    return null;
            }

            return container?.gameObject;
        }

        private static void AddContentClone(
            GameObject source,
            Transform parent,
            ProductPresentationTransform? placement,
            Action<GameObject>? customize)
        {
            Vector3 authoredPosition = source.transform.localPosition;
            Quaternion authoredRotation = source.transform.localRotation;
            Vector3 authoredScale = source.transform.localScale;
            GameObject content = Object.Instantiate(source);
            content.transform.SetParent(parent, false);
            if (placement != null)
            {
                placement.ApplyTo(content.transform);
            }
            else
            {
                content.transform.localPosition = authoredPosition;
                content.transform.localRotation = authoredRotation;
                content.transform.localScale = authoredScale;
            }

            customize?.Invoke(content);
            content.SetActive(true);
        }

        private static bool TryDisableNativeVisuals(
            S1Product.MultiTypeVisualsSetter setter,
            ProductPackagingContentProfileRegistration registration,
            string operation)
        {
            try
            {
                setter.WeedVisuals?.ResetVisuals();
                setter.MethVisuals?.ResetVisuals();
                setter.CocaineVisuals?.ResetVisuals();
                setter.ShroomVisuals?.ResetVisuals();
                return true;
            }
            catch (Exception exception)
            {
                LogFailureOnce(registration, operation, exception.Message);
                return false;
            }
        }

        private static Transform? FindOwnedRoot(
            Transform parent,
            string expectedName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(
                        child.name,
                        expectedName,
                        StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void RemoveOwnedRoots(
            Transform parent,
            Transform? retainedRoot)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == retainedRoot ||
                    !child.name.StartsWith(
                        GeneratedRootNamePrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                DestroyOwnedRoot(child.gameObject);
            }
        }

        private static void DestroyOwnedRoot(GameObject root)
        {
            if (root == null)
                return;

            root.SetActive(false);
            Object.Destroy(root);
        }

        private static string GetGeneratedRootName(
            ProductPackagingContentKey key)
        {
            string identity =
                key.ProductId.ToUpperInvariant() + "\0" +
                key.PackagingId.ToUpperInvariant();
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < identity.Length; i++)
            {
                char value = identity[i];
                hash ^= (byte)value;
                hash *= prime;
                hash ^= (byte)(value >> 8);
                hash *= prime;
            }

            return GeneratedRootNamePrefix + hash.ToString("X16");
        }

        private static void EnsureIconCacheMatches(
            S1DevUtilities.IconGenerator generator)
        {
            int generatorInstanceId = generator.GetInstanceID();
            if (_iconGeneratorInstanceId == generatorInstanceId)
                return;

            DestroyGeneratedIcons();
            _iconGeneratorInstanceId = generatorInstanceId;
        }

        private static void DestroyGeneratedIcons()
        {
            foreach (GeneratedPackagingIcon generated in GeneratedIcons.Values)
                DestroyGeneratedIcon(generated);
            GeneratedIcons.Clear();
        }

        private static void DestroyGeneratedIcon(
            GeneratedPackagingIcon generated)
        {
            if (generated.Icon != null)
                Object.Destroy(generated.Icon);
            if (generated.Texture != null)
                Object.Destroy(generated.Texture);
        }

        private static void LogFailureOnce(
            ProductPackagingContentProfileRegistration registration,
            string operation,
            string reason)
        {
            string failureKey =
                registration.Key.ProductId.ToUpperInvariant() + "\0" +
                registration.Key.PackagingId.ToUpperInvariant() + "\0" +
                operation;
            lock (FailureGate)
            {
                if (!LoggedFailures.Add(failureKey))
                    return;
            }

            MelonLogger.Warning(
                $"[ProductPackagingContentProfile] {operation} for product " +
                $"'{registration.Key.ProductId}' and packaging " +
                $"'{registration.Key.PackagingId}' failed: {reason}. " +
                "Preserving the native presentation fallback.");
        }
    }
}
