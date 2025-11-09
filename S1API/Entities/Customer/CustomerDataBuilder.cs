#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
using S1GameTime = Il2CppScheduleOne.GameTime;
using S1Props = Il2CppScheduleOne.Effects;
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
using S1GameTime = ScheduleOne.GameTime;
using S1Props = ScheduleOne.Effects;
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.GameTime;
using UnityEngine;
using S1API.Properties;
using S1API.Economy;
using S1API.Products;
using S1API.Properties.Interfaces;
using S1API.Properties.Internal;

namespace S1API.Entities.Customer
{
    /// <summary>
    /// Builder for composing CustomerData at runtime without asset bundles.
    /// Public surface uses strings/primitives only.
    /// </summary>
    public sealed class CustomerDataBuilder
    {
        private readonly S1Economy.CustomerData _data;

        // Runtime instantiation is disabled; only NPCPrefabBuilder can create this
        internal CustomerDataBuilder()
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
        /// Sets the preferred order day using the API Day enum.
        /// </summary>
        public CustomerDataBuilder WithPreferredOrderDay(Day day)
        {
            _data.PreferredOrderDay = (S1GameTime.EDay)(int)day;
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
        /// Sets standards from string (e.g., "VeryLow", "Low", "Moderate", "High", "VeryHigh").
        /// </summary>
        public CustomerDataBuilder WithStandards(string standards)
        {
            if (!string.IsNullOrEmpty(standards) && Enum.TryParse(typeof(S1Economy.ECustomerStandard), standards, true, out var parsed))
                _data.Standards = (S1Economy.ECustomerStandard)parsed;
            return this;
        }

        /// <summary>
        /// Sets standards using the strongly-typed enum.
        /// </summary>
        public CustomerDataBuilder WithStandards(CustomerStandard standards)
        {
            _data.Standards = (S1Economy.ECustomerStandard)(int)standards;
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
        /// Sets product type affinities using the enum.
        /// Replaces any existing default affinity data.
        /// </summary>
        public CustomerDataBuilder WithAffinities(IEnumerable<(DrugType drugType, float affinity)> entries)
        {
            if (entries == null)
                return this;
            _data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
            foreach (var (type, aff) in entries)
            {
                _data.DefaultAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                {
                    DrugType = (S1Product.EDrugType)(int)type,
                    Affinity = Mathf.Clamp(aff, -1f, 1f)
                });
            }
            return this;
        }

        /// <summary>
        /// Adds or overrides a single product type affinity entry.
        /// </summary>
        public CustomerDataBuilder WithAffinity(DrugType drugType, float affinity)
        {
            if (_data.DefaultAffinityData == null)
                _data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
            S1Economy.ProductTypeAffinity existing = null;
            foreach (var a in _data.DefaultAffinityData.ProductAffinities)
            {
                if (a != null && (int)a.DrugType == (int)drugType)
                {
                    existing = a;
                    break;
                }
            }
            if (existing == null)
            {
                _data.DefaultAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                {
                    DrugType = (S1Product.EDrugType)(int)drugType,
                    Affinity = Mathf.Clamp(affinity, -1f, 1f)
                });
            }
            else
            {
                existing.Affinity = Mathf.Clamp(affinity, -1f, 1f);
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

            var results = new List<S1Props.Effect>();
            string[] searchPaths = { "Properties/Tier1", "Properties/Tier2", "Properties/Tier3", "Properties/Tier4", "Properties/Tier5" };
            foreach (var path in searchPaths)
            {
                var props = Resources.LoadAll<S1Props.Effect>(path);
                if (props == null || props.Length == 0)
                    continue;
                foreach (var name in propertyNames)
                {
                    if (string.IsNullOrEmpty(name))
                        continue;
                    S1Props.Effect found = null;
                    foreach (var p in props)
                    {
                        if (p != null && string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            found = p;
                            break;
                        }
                    }
                    if (found != null && !results.Contains(found))
                        results.Add(found);
                }
            }
            _data.PreferredProperties = ToIl2CppList(results);
            return this;
        }

        /// <summary>
        /// Tries to assign preferred properties by ID using in-game Resources.
        /// IDs must match existing property assets.
        /// </summary>
        public CustomerDataBuilder WithPreferredPropertiesById(params string[] propertyIds)
        {
            if (propertyIds == null || propertyIds.Length == 0)
                return this;

            var results = new List<S1Props.Effect>();
            string[] searchPaths = { "Properties/Tier1", "Properties/Tier2", "Properties/Tier3", "Properties/Tier4", "Properties/Tier5" };
            foreach (var path in searchPaths)
            {
                var props = Resources.LoadAll<S1Props.Effect>(path);
                if (props == null || props.Length == 0)
                    continue;
                foreach (var id in propertyIds)
                {
                    if (string.IsNullOrEmpty(id))
                        continue;
                    S1Props.Effect found = null;
                    foreach (var p in props)
                    {
                        if (p != null && string.Equals(p.ID, id, StringComparison.OrdinalIgnoreCase))
                        {
                            found = p;
                            break;
                        }
                    }
                    if (found != null && !results.Contains(found))
                        results.Add(found);
                }
            }
            _data.PreferredProperties = ToIl2CppList(results);
            return this;
        }

        /// <summary>
        /// Assigns preferred properties from wrappers.
        /// </summary>
        public CustomerDataBuilder WithPreferredProperties(params ProductPropertyWrapper[] wrappers)
        {
            if (wrappers == null || wrappers.Length == 0)
                return this;
            var results = new List<S1Props.Effect>();
            for (int i = 0; i < wrappers.Length; i++)
            {
                var w = wrappers[i];
                if (w == null)
                    continue;
                var inner = w.InnerProperty;
                if (inner != null && !results.Contains(inner))
                    results.Add(inner);
            }
            _data.PreferredProperties = ToIl2CppList(results);
            return this;
        }

        /// <summary>
        /// Assigns preferred properties from API property abstractions (tokens or wrappers).
        /// Does not expose game types to mod developers.
        /// </summary>
        public CustomerDataBuilder WithPreferredProperties(params PropertyBase[] properties)
        {
            if (properties == null || properties.Length == 0)
                return this;

            var resolved = PropertyResolver.ResolveToGameProperties(properties);
            _data.PreferredProperties = ToIl2CppList(resolved);
            return this;
        }

        internal S1Economy.CustomerData BuildInternal() => _data;

#if (IL2CPPMELON)
        private static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            var list = new Il2CppSystem.Collections.Generic.List<T>();
            if (source == null)
                return list;
            for (int i = 0; i < source.Count; i++)
                list.Add(source[i]);
            return list;
        }
#else
        private static System.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.List<T> source)
        {
            return source;
        }
#endif
    }
}