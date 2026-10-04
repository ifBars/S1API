using System;
using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class RuntimePreviewLayer
    {
        internal static int Resolve() =>
            Resolve(LayerMask.NameToLayer);

        internal static int Resolve(Func<string, int> findLayer)
        {
            int layer = findLayer("RuntimePreviewGeneration");
            return layer >= 0 ? layer : findLayer("IconGeneration");
        }
    }
}
