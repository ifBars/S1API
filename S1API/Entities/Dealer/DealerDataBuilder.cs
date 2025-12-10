#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
#endif

using System;
using UnityEngine;
using S1API.Economy;
using S1API.Map;

namespace S1API.Entities.Dealer
{
    /// <summary>
    /// Builder for composing dealer configuration at runtime without asset bundles.
    /// Public surface uses strings/primitives only.
    /// </summary>
    public sealed class DealerDataBuilder
    {
        internal class DealerConfigData
        {
            public float SigningFee { get; set; } = 500f;
            public float Cut { get; set; } = 0.2f;
            public DealerType DealerType { get; set; } = DealerType.PlayerDealer;
            public string HomeName { get; set; } = "Home";
            public Map.Building? Home { get; set; } = null;
            public bool SellInsufficientQualityItems { get; set; } = false;
            public bool SellExcessQualityItems { get; set; } = true;
            public string CompletedDealsVariable { get; set; } = string.Empty;
        }

        private readonly DealerConfigData _data;

        // Runtime instantiation is disabled; only NPCPrefabBuilder can create this
        internal DealerDataBuilder()
        {
            _data = new DealerConfigData();
        }

        /// <summary>
        /// Sets the signing fee required to recruit this dealer.
        /// </summary>
        public DealerDataBuilder WithSigningFee(float fee)
        {
            _data.SigningFee = Mathf.Max(0f, fee);
            return this;
        }

        /// <summary>
        /// Sets the dealer's commission cut (percentage of earnings they keep). Range: 0.0 to 1.0.
        /// </summary>
        public DealerDataBuilder WithCut(float percentage)
        {
            _data.Cut = Mathf.Clamp01(percentage);
            return this;
        }

        /// <summary>
        /// Sets the dealer type (PlayerDealer or CartelDealer).
        /// </summary>
        public DealerDataBuilder WithDealerType(DealerType type)
        {
            _data.DealerType = type;
            return this;
        }

        /// <summary>
        /// Sets the home building name for this dealer.
        /// </summary>
        public DealerDataBuilder WithHomeName(string name)
        {
            _data.HomeName = name ?? "Home";
            return this;
        }

        /// <summary>
        /// Sets the home building for this dealer using a Building wrapper.
        /// </summary>
        /// <param name="building">The Building wrapper for the dealer's home.</param>
        public DealerDataBuilder WithHome(Map.Building building)
        {
            _data.Home = building;
            if (building != null)
            {
                _data.HomeName = building.Name;
            }
            return this;
        }

        /// <summary>
        /// Allows the dealer to sell items below the customer's quality standards.
        /// </summary>
        public DealerDataBuilder AllowInsufficientQuality(bool allow)
        {
            _data.SellInsufficientQualityItems = allow;
            return this;
        }

        /// <summary>
        /// Allows the dealer to sell items above the customer's quality standards.
        /// </summary>
        public DealerDataBuilder AllowExcessQuality(bool allow)
        {
            _data.SellExcessQualityItems = allow;
            return this;
        }

        /// <summary>
        /// Sets the variable name to track completed deals for this dealer.
        /// </summary>
        public DealerDataBuilder WithCompletedDealsVariable(string varName)
        {
            _data.CompletedDealsVariable = varName ?? string.Empty;
            return this;
        }

        internal DealerConfigData BuildInternal() => _data;
    }
}

