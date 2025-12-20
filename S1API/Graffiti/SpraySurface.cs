#if (IL2CPPMELON)
using S1Graffiti = Il2CppScheduleOne.Graffiti;
using Guid = Il2CppSystem.Guid;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Graffiti = ScheduleOne.Graffiti;
using Guid = System.Guid;
#endif

using System;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Graffiti
{
    /// <summary>
    /// Represents a surface that can be spray painted with graffiti.
    /// </summary>
    public sealed class SpraySurface
    {
        /// <summary>
        /// INTERNAL: The in-game spray surface instance.
        /// </summary>
        internal readonly S1Graffiti.SpraySurface S1SpraySurface;

        /// <summary>
        /// INTERNAL: Creates a SpraySurface wrapper.
        /// </summary>
        /// <param name="spraySurface">The in-game spray surface instance.</param>
        internal SpraySurface(S1Graffiti.SpraySurface spraySurface) =>
            S1SpraySurface = spraySurface;

        /// <summary>
        /// The globally unique identifier for this spray surface.
        /// </summary>
        public System.Guid GUID
        {
            get
            {
#if (IL2CPPMELON)
                var il2cppGuid = S1SpraySurface.GUID;
                return new System.Guid(il2cppGuid.ToString());
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
                return S1SpraySurface.GUID;
#endif
            }
        }

        /// <summary>
        /// The world position of the bottom-left reference point of this spray surface.
        /// </summary>
        public Vector3 Position
        {
            get
            {
                object? bottomLeftPoint = ReflectionUtils.TryGetFieldOrProperty(S1SpraySurface, "BottomLeftPoint");
                if (bottomLeftPoint is Transform transform && transform != null)
                    return transform.position;
                return Vector3.zero;
            }
        }

        /// <summary>
        /// The number of strokes drawn on this surface.
        /// </summary>
        public int StrokeCount =>
            S1SpraySurface.DrawingStrokeCount;

        /// <summary>
        /// The number of painted pixels on this surface.
        /// </summary>
        public int PaintedPixelCount =>
            S1SpraySurface.DrawingPaintedPixelCount;

        /// <summary>
        /// Whether the drawing has been finalized (player closed the graffiti UI).
        /// </summary>
        public bool HasDrawingBeenFinalized =>
            S1SpraySurface.HasDrawingBeenFinalized;

        /// <summary>
        /// The output texture for the drawing on this surface.
        /// </summary>
        public Texture DrawingOutputTexture =>
            S1SpraySurface.DrawingOutputTexture;

        /// <summary>
        /// Event fired when the drawing on this surface changes.
        /// </summary>
        public event Action OnDrawingChanged
        {
            add => S1SpraySurface.onDrawingChanged += value;
            remove => S1SpraySurface.onDrawingChanged -= value;
        }
    }
}

