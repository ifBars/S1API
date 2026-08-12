using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using S1API.Internal.Products;
using S1API.Internal.Utils;
using S1API.Items.Buildable;
using S1API.Logging;
using S1API.Rendering;
using UnityEngine;

namespace S1API.Internal.Building
{
    /// <summary>
    /// Defers furniture thumbnail rendering until the gameplay scene provides the native icon rig.
    /// Definitions remain available during pre-load so native save restoration can resolve them.
    /// </summary>
    internal static class FurnitureIconRuntime
    {
        private const int RenderRigWaitFrames = 600;
        private static readonly Log Logger = new Log("FurnitureIconRuntime");
        private static readonly object Gate = new object();
        private static readonly Queue<Request> Pending = new Queue<Request>();
        private static bool _processing;

        internal static void Queue(
            BuildableItemDefinition definition,
            Transform model,
            int resolution,
            bool isolateMaterials)
        {
            bool startProcessor = false;
            lock (Gate)
            {
                Pending.Enqueue(new Request(
                    definition,
                    model,
                    resolution,
                    isolateMaterials));
                if (!_processing)
                {
                    _processing = true;
                    startProcessor = true;
                }
            }

            if (startProcessor)
                MelonCoroutines.Start(ProcessQueue());
        }

        private static IEnumerator ProcessQueue()
        {
            try
            {
                int readinessFrame = 0;
                while (!IconFactory.IsItemIconGeneratorReady && readinessFrame < RenderRigWaitFrames)
                {
                    readinessFrame++;
                    yield return null;
                }

                if (!IconFactory.IsItemIconGeneratorReady)
                {
                    int requestCount = DrainPendingRequests();
                    Logger.Warning(
                        $"Could not generate {requestCount} furniture icon(s): " +
                        "the native item-icon rendering rig did not become ready. " +
                        "The definitions retain their native fallback icons.");
                    yield break;
                }

                while (TryDequeue(out Request? request))
                {
                    IEnumerator requestProcessor = ProcessRequest(request!);
                    try
                    {
                        while (requestProcessor.MoveNext())
                            yield return requestProcessor.Current;
                    }
                    finally
                    {
                        (requestProcessor as System.IDisposable)?.Dispose();
                    }
                }
            }
            finally
            {
                FinishProcessing();
            }
        }

        private static IEnumerator ProcessRequest(Request request)
        {
            if (request.Model == null)
            {
                Logger.Warning(
                    $"Could not generate furniture icon for '{request.Definition.ID}': " +
                    "the source model was destroyed before capture.");
                yield break;
            }

            ProductIconRenderRigArbiter.CaptureLease renderLease =
                ProductIconRenderRigArbiter.Enqueue();
            GameObject? iconModel = null;
            try
            {
                int acquisitionFrame = 0;
                bool leaseAcquired = false;
                while (!(leaseAcquired = ProductIconRenderRigArbiter.TryAcquire(renderLease)) &&
                       acquisitionFrame < RenderRigWaitFrames)
                {
                    acquisitionFrame++;
                    yield return null;
                }

                if (!leaseAcquired)
                {
                    Logger.Warning(
                        $"Could not generate furniture icon for '{request.Definition.ID}': " +
                        "the shared item-icon rendering rig remained busy.");
                    yield break;
                }

                if (!TryCreatePreview(request, out iconModel, out string failure))
                {
                    Logger.Warning(
                        $"Could not generate furniture icon for '{request.Definition.ID}': {failure}.");
                    yield break;
                }

                const int maxRetries = 30;
                bool generated = false;
                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    // Let the newly activated preview complete Update and render before capture.
                    yield return null;
                    yield return new WaitForEndOfFrame();

                    RenderAttemptResult result = TryRender(
                        request,
                        iconModel!,
                        out Sprite? icon,
                        out failure);
                    if (result == RenderAttemptResult.Success)
                    {
                        request.Definition.Icon = icon!;
                        generated = true;
                        break;
                    }

                    if (result == RenderAttemptResult.Failure)
                        break;
                }

                if (!generated)
                {
                    Logger.Warning(
                        $"Could not generate furniture icon for '{request.Definition.ID}': {failure}.");
                }

                Object.Destroy(iconModel);
                iconModel = null;

                // Keep the shared rig lease through cleanup and a settled frame so the next
                // queued subject cannot capture the outgoing preview.
                yield return null;
                yield return new WaitForEndOfFrame();
            }
            finally
            {
                if (iconModel != null)
                    Object.Destroy(iconModel);

                ProductIconRenderRigArbiter.Release(renderLease);
            }
        }

        private static bool TryCreatePreview(
            Request request,
            out GameObject? iconModel,
            out string failure)
        {
            iconModel = null;
            try
            {
                if (request.Model == null)
                {
                    failure = "the source model was destroyed before capture";
                    return false;
                }

                iconModel = request.IsolateMaterials
                    ? FurnitureVisualCloner.CloneOwnedVisual(request.Model.gameObject)
                    : InactiveObjectCloner.CloneGameObject(request.Model.gameObject);
                if (iconModel == null)
                {
                    failure = "the source model could not be cloned";
                    return false;
                }

                iconModel.name = $"{request.Model.name}_IconPreview";
                foreach (Collider collider in iconModel.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
                iconModel.SetActive(true);
                failure = string.Empty;
                return true;
            }
            catch (System.Exception exception)
            {
                failure = $"preview creation failed: {exception.Message}";
                if (iconModel != null)
                    Object.Destroy(iconModel);
                iconModel = null;
                return false;
            }
        }

        private static RenderAttemptResult TryRender(
            Request request,
            GameObject iconModel,
            out Sprite? icon,
            out string failure)
        {
            icon = null;
            try
            {
                icon = IconFactory.GenerateIconSprite(
                    iconModel.transform,
                    request.Resolution);
                if (icon == null)
                {
                    failure = "the native renderer returned no visible pixels";
                    return RenderAttemptResult.Retry;
                }

                failure = string.Empty;
                return RenderAttemptResult.Success;
            }
            catch (System.Exception exception)
            {
                failure = $"rendering failed: {exception.Message}";
                return RenderAttemptResult.Failure;
            }
        }

        private static bool TryDequeue(out Request? request)
        {
            lock (Gate)
            {
                if (Pending.Count == 0)
                {
                    request = null;
                    return false;
                }

                request = Pending.Dequeue();
                return true;
            }
        }

        private static int DrainPendingRequests()
        {
            lock (Gate)
            {
                int requestCount = Pending.Count;
                Pending.Clear();
                return requestCount;
            }
        }

        private static void FinishProcessing()
        {
            bool restart;
            lock (Gate)
            {
                _processing = false;
                restart = Pending.Count != 0;
                if (restart)
                    _processing = true;
            }

            if (restart)
                MelonCoroutines.Start(ProcessQueue());
        }

        private enum RenderAttemptResult
        {
            Success,
            Retry,
            Failure,
        }

        private sealed class Request
        {
            internal Request(
                BuildableItemDefinition definition,
                Transform model,
                int resolution,
                bool isolateMaterials)
            {
                Definition = definition;
                Model = model;
                Resolution = resolution;
                IsolateMaterials = isolateMaterials;
            }

            internal BuildableItemDefinition Definition { get; }
            internal Transform Model { get; }
            internal int Resolution { get; }
            internal bool IsolateMaterials { get; }
        }
    }
}
