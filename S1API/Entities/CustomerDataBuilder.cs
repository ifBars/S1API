#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
using S1GameTime = Il2CppScheduleOne.GameTime;
using S1Props = Il2CppScheduleOne.Properties;
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
using S1GameTime = ScheduleOne.GameTime;
using S1Props = ScheduleOne.Properties;
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Builder for composing CustomerData at runtime without asset bundles.
    /// Public surface uses strings/primitives only.
    /// </summary>
    public sealed class CustomerDataBuilder
    {
        private readonly S1Economy.CustomerData _data;

        public CustomerDataBuilder()
        {
            _data = ScriptableObject.CreateInstance<S1Economy.CustomerData>();
            _data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
        }

        public CustomerDataBuilder WithSpending(float minWeekly, float maxWeekly)
        {
            _data.MinWeeklySpend = Mathf.Max(0f, minWeekly);
            _data.MaxWeeklySpend = Mathf.Max(_data.MinWeeklySpend, maxWeekly);
            return this;
        }

        public CustomerDataBuilder WithOrdersPerWeek(int min, int max)
        {
            _data.MinOrdersPerWeek = Mathf.Clamp(min, 0, 7);
            _data.MaxOrdersPerWeek = Mathf.Clamp(Mathf.Max(min, max), 0, 7);
            return this;
        }

        /// <summary>
        /// Sets the preferred order day using names like "Monday", "Tuesday", ... or numeric 0..6.
        /// </summary>
        public CustomerDataBuilder WithPreferredOrderDay(string day)
        {
            if (!string.IsNullOrEmpty(day) && Enum.TryParse(typeof(S1GameTime.EDay), day, true, out var parsed))
                _data.PreferredOrderDay = (S1GameTime.EDay)parsed;
            return this;
        }

        /// <summary>
        /// Sets the order time in 24h integer format (e.g., 930 for 9:30AM, 1745 for 5:45PM).
        /// </summary>
        public CustomerDataBuilder WithOrderTime(int hhmm)
        {
            _data.OrderTime = Mathf.Clamp(hhmm, 0, 2359);
            return this;
        }

        /// <summary>
        /// Sets standards from string (e.g., "Low", "Moderate", "High").
        /// </summary>
        public CustomerDataBuilder WithStandards(string standards)
        {
            if (!string.IsNullOrEmpty(standards) && Enum.TryParse(typeof(S1Economy.ECustomerStandard), standards, true, out var parsed))
                _data.Standards = (S1Economy.ECustomerStandard)parsed;
            return this;
        }

        public CustomerDataBuilder AllowDirectApproach(bool allow)
        {
            _data.CanBeDirectlyApproached = allow;
            return this;
        }

        public CustomerDataBuilder GuaranteeFirstSample(bool guarantee)
        {
            _data.GuaranteeFirstSampleSuccess = guarantee;
            return this;
        }

        public CustomerDataBuilder WithMutualRelationRequirement(float minAt50, float maxAt100)
        {
            _data.MinMutualRelationRequirement = Mathf.Clamp(minAt50, 0f, 5f);
            _data.MaxMutualRelationRequirement = Mathf.Clamp(maxAt100, 0f, 5f);
            return this;
        }

        public CustomerDataBuilder WithCallPoliceChance(float chance)
        {
            _data.CallPoliceChance = Mathf.Clamp01(chance);
            return this;
        }

        public CustomerDataBuilder WithDependence(float baseAddiction, float dependenceMultiplier = 1f)
        {
            _data.BaseAddiction = Mathf.Clamp01(baseAddiction);
            _data.DependenceMultiplier = Mathf.Clamp(dependenceMultiplier, 0f, 2f);
            return this;
        }

        /// <summary>
        /// Sets product type affinities by drug-type name. e.g., ("Weed", 0.3f), ("Cocaine", -0.5f).
        /// </summary>
        public CustomerDataBuilder WithAffinities(IEnumerable<(string drugType, float affinity)> entries)
        {
            if (entries == null)
                return this;
            _data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
            foreach (var (name, aff) in entries)
            {
                if (string.IsNullOrEmpty(name))
                    continue;
                if (Enum.TryParse(typeof(S1Product.EDrugType), name, true, out var parsed))
                {
                    _data.DefaultAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                    {
                        DrugType = (S1Product.EDrugType)parsed,
                        Affinity = Mathf.Clamp(aff, -1f, 1f)
                    });
                }
            }
            return this;
        }

        /// <summary>
        /// Tries to assign preferred properties by asset name using in-game Resources.
        /// Names must match existing property assets.
        /// </summary>
        public CustomerDataBuilder WithPreferredPropertiesByName(params string[] propertyNames)
        {
            if (propertyNames == null || propertyNames.Length == 0)
                return this;

            var results = new List<S1Props.Property>();
            string[] searchPaths = { "Properties/Tier1", "Properties/Tier2", "Properties/Tier3", "Properties/Tier4", "Properties/Tier5" };
            foreach (var path in searchPaths)
            {
                var props = Resources.LoadAll<S1Props.Property>(path);
                if (props == null || props.Length == 0)
                    continue;
                foreach (var name in propertyNames)
                {
                    if (string.IsNullOrEmpty(name))
                        continue;
                    var found = props.FirstOrDefault(p => p != null && string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
                    if (found != null && !results.Contains(found))
                        results.Add(found);
                }
            }
            _data.PreferredProperties = results;
            return this;
        }

        internal S1Economy.CustomerData BuildInternal() => _data;
    }
}


