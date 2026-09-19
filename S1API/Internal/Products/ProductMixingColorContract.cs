using System;
using System.Collections.Generic;
using S1API.Products;

namespace S1API.Internal.Products
{
    internal readonly struct ProductMixingColorValue : IEquatable<ProductMixingColorValue>
    {
        internal ProductMixingColorValue(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        internal byte Red { get; }

        internal byte Green { get; }

        internal byte Blue { get; }

        internal byte Alpha { get; }

        internal UnityEngine.Color32 ToColor32() =>
            new UnityEngine.Color32(Red, Green, Blue, Alpha);

        public bool Equals(ProductMixingColorValue other) =>
            Red == other.Red &&
            Green == other.Green &&
            Blue == other.Blue &&
            Alpha == other.Alpha;

        public override bool Equals(object? obj) =>
            obj is ProductMixingColorValue other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Red, Green, Blue, Alpha);
    }

    internal readonly struct ProductMixingColorSample
    {
        internal ProductMixingColorSample(
            int tier,
            ProductMixingColorValue color)
        {
            Tier = tier;
            Color = color;
        }

        internal int Tier { get; }

        internal ProductMixingColorValue Color { get; }
    }

    internal static class ProductMixingColorContract
    {
        internal static ProductMixingColorValue CalculatePrimaryColor(
            ProductMixingMap mixerMap,
            IReadOnlyList<ProductMixingColorSample> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            ProductMixingColorValue baseColor = GetBaseColor(mixerMap);
            if (properties.Count == 0)
                return baseColor;

            ProductMixingColorSample primary = properties[0];
            for (int i = 1; i < properties.Count; i++)
            {
                if (properties[i].Tier < primary.Tier)
                    primary = properties[i];
            }

            float influence = mixerMap switch
            {
                ProductMixingMap.Marijuana => primary.Tier * 0.15f,
                ProductMixingMap.Methamphetamine => primary.Tier * 0.2f,
                ProductMixingMap.Cocaine => primary.Tier * 0.13f,
                ProductMixingMap.Shrooms => primary.Tier / 5f,
                _ => throw new ArgumentOutOfRangeException(nameof(mixerMap))
            };
            return Lerp(baseColor, primary.Color, influence);
        }

        private static ProductMixingColorValue GetBaseColor(
            ProductMixingMap mixerMap)
        {
            return mixerMap switch
            {
                ProductMixingMap.Marijuana =>
                    new ProductMixingColorValue(90, 100, 70, byte.MaxValue),
                ProductMixingMap.Methamphetamine =>
                    new ProductMixingColorValue(
                        byte.MaxValue,
                        byte.MaxValue,
                        byte.MaxValue,
                        byte.MaxValue),
                ProductMixingMap.Cocaine =>
                    new ProductMixingColorValue(
                        byte.MaxValue,
                        byte.MaxValue,
                        byte.MaxValue,
                        byte.MaxValue),
                ProductMixingMap.Shrooms =>
                    new ProductMixingColorValue(168, 125, 43, byte.MaxValue),
                _ => throw new ArgumentOutOfRangeException(nameof(mixerMap))
            };
        }

        private static ProductMixingColorValue Lerp(
            ProductMixingColorValue start,
            ProductMixingColorValue end,
            float amount)
        {
            float clamped = Math.Max(0f, Math.Min(1f, amount));
            return new ProductMixingColorValue(
                LerpByte(start.Red, end.Red, clamped),
                LerpByte(start.Green, end.Green, clamped),
                LerpByte(start.Blue, end.Blue, clamped),
                LerpByte(start.Alpha, end.Alpha, clamped));
        }

        private static byte LerpByte(byte start, byte end, float amount) =>
            (byte)(start + ((end - start) * amount));
    }
}
