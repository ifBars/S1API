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
using S1API.Internal.Properties;

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

        /// <summary>
        /// Sets the customer's weekly spending range.
        /// The minimum is clamped to 0, and the maximum is clamped so it cannot be lower than the minimum.
        /// </summary>
        /// <param name="minWeekly">Minimum weekly spend target.</param>
        /// <param name="maxWeekly">Maximum weekly spend target.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithSpending(float minWeekly, float maxWeekly)
        {
            _data.MinWeeklySpend = Mathf.Max(0f, minWeekly);
            _data.MaxWeeklySpend = Mathf.Max(_data.MinWeeklySpend, maxWeekly);
            return this;
        }

        /// <summary>
        /// Sets how many orders this customer may place per week.
        /// Both values are clamped to the game's 0..7 range, and the maximum cannot be lower than the minimum.
        /// </summary>
        /// <param name="min">Minimum orders per week.</param>
        /// <param name="max">Maximum orders per week.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithOrdersPerWeek(int min, int max)
        {
            _data.MinOrdersPerWeek = Mathf.Clamp(min, 0, 7);
            _data.MaxOrdersPerWeek = Mathf.Clamp(Mathf.Max(min, max), 0, 7);
            return this;
        }

        /// <summary>
        /// Sets the preferred order day using names like "Monday", "Tuesday", and so on.
        /// Invalid values are ignored.
        /// </summary>
        /// <param name="day">Day name to parse.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithPreferredOrderDay(string day)
        {
            if (!string.IsNullOrEmpty(day) && Enum.TryParse(typeof(S1GameTime.EDay), day, true, out var parsed))
                _data.PreferredOrderDay = (S1GameTime.EDay)parsed;
            return this;
        }

        /// <summary>
        /// Sets the preferred order day using the API Day enum.
        /// </summary>
        /// <param name="day">Preferred order day.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithPreferredOrderDay(Day day)
        {
            _data.PreferredOrderDay = (S1GameTime.EDay)(int)day;
            return this;
        }

        /// <summary>
        /// Sets the order time in 24h integer format (e.g., 930 for 9:30AM, 1745 for 5:45PM).
        /// </summary>
        /// <param name="hhmm">24-hour time in HHMM form.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithOrderTime(int hhmm)
        {
            _data.OrderTime = Mathf.Clamp(hhmm, 0, 2359);
            return this;
        }

        /// <summary>
        /// Sets customer standards from a string such as "VeryLow", "Low", "Moderate", "High", or "VeryHigh".
        /// Invalid values are ignored.
        /// </summary>
        /// <param name="standards">Standards name to parse.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithStandards(string standards)
        {
            if (!string.IsNullOrEmpty(standards) && Enum.TryParse(typeof(S1Economy.ECustomerStandard), standards, true, out var parsed))
                _data.Standards = (S1Economy.ECustomerStandard)parsed;
            return this;
        }

        /// <summary>
        /// Sets standards using the strongly-typed enum.
        /// </summary>
        /// <param name="standards">Customer standards value.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithStandards(CustomerStandard standards)
        {
            _data.Standards = (S1Economy.ECustomerStandard)(int)standards;
            return this;
        }

        /// <summary>
        /// Controls whether the player can attempt to introduce themselves through a direct sample offer.
        /// </summary>
        /// <param name="allow">True to allow direct approach attempts; otherwise false.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder AllowDirectApproach(bool allow)
        {
            _data.CanBeDirectlyApproached = allow;
            return this;
        }

        /// <summary>
        /// Forces the first sample attempt to succeed, bypassing the normal mutual-relation chance check.
        /// </summary>
        /// <param name="guarantee">True to guarantee first-sample success.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder GuaranteeFirstSample(bool guarantee)
        {
            _data.GuaranteeFirstSampleSuccess = guarantee;
            return this;
        }

        /// <summary>
        /// Sets the mutual-relationship thresholds used when a customer is approached directly for the first time.
        /// The game checks the NPC's average relationship with mutual contacts and converts that value into a success chance.
        /// Values at or below <paramref name="minAt50"/> behave as the low end of the chance curve, and values at or above
        /// <paramref name="maxAt100"/> guarantee success. Values between them scale linearly.
        /// </summary>
        /// <param name="minAt50">
        /// Lower bound of the mutual-relationship success curve.
        /// Despite the historical name, this is better thought of as the point where the curve starts rather than a literal 50% threshold.
        /// </param>
        /// <param name="maxAt100">Mutual-relationship value that guarantees success.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithMutualRelationRequirement(float minAt50, float maxAt100)
        {
            _data.MinMutualRelationRequirement = Mathf.Clamp(minAt50, 0f, 5f);
            _data.MaxMutualRelationRequirement = Mathf.Clamp(maxAt100, 0f, 5f);
            return this;
        }

        /// <summary>
        /// Sets the chance that the customer calls the police after rejecting a direct approach.
        /// </summary>
        /// <param name="chance">Chance from 0 to 1.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithCallPoliceChance(float chance)
        {
            _data.CallPoliceChance = Mathf.Clamp01(chance);
            return this;
        }

        /// <summary>
        /// Sets the customer's starting addiction level and how quickly dependence builds over time.
        /// </summary>
        /// <param name="baseAddiction">Starting addiction level in the 0..1 range.</param>
        /// <param name="dependenceMultiplier">Dependence growth multiplier in the 0..2 range.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithDependence(float baseAddiction, float dependenceMultiplier = 1f)
        {
            _data.BaseAddiction = Mathf.Clamp01(baseAddiction);
            _data.DependenceMultiplier = Mathf.Clamp(dependenceMultiplier, 0f, 2f);
            return this;
        }

        /// <summary>
        /// Sets product type affinities by drug-type name. e.g., ("Weed", 0.3f), ("Cocaine", -0.5f).
        /// Ensures all drug types are initialized (unspecified ones default to neutral affinity).
        /// </summary>
        /// <param name="entries">Drug-type names and affinity values to apply.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithAffinities(IEnumerable<(string drugType, float affinity)> entries)
        {
            _data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
            
            // First, initialize all drug types with neutral affinity
            Array drugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
            foreach (var dt in drugTypes)
            {
                _data.DefaultAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                {
                    DrugType = (S1Product.EDrugType)dt,
                    Affinity = 0f
                });
            }
            
            // Then override with specified affinities
            if (entries != null)
            {
                foreach (var (name, aff) in entries)
                {
                    if (string.IsNullOrEmpty(name))
                        continue;
                    if (Enum.TryParse(typeof(S1Product.EDrugType), name, true, out var parsed))
                    {
                        var drugType = (S1Product.EDrugType)parsed;
                        // Find and update existing entry
                        S1Economy.ProductTypeAffinity existing = null;
                        foreach (var item in _data.DefaultAffinityData.ProductAffinities)
                        {
                            if (item != null && item.DrugType == drugType)
                            {
                                existing = item;
                                break;
                            }
                        }
                        if (existing != null)
                        {
                            existing.Affinity = Mathf.Clamp(aff, -1f, 1f);
                        }
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Sets product type affinities using the enum.
        /// Replaces any existing default affinity data.
        /// Ensures all drug types are initialized (unspecified ones default to neutral affinity).
        /// </summary>
        /// <param name="entries">Drug-type values and affinity values to apply.</param>
        /// <returns>The current builder for chaining.</returns>
        public CustomerDataBuilder WithAffinities(IEnumerable<(DrugType drugType, float affinity)> entries)
        {
            _data.DefaultAffinityData = new S1Economy.CustomerAffinityData();
            
            // First, initialize all drug types with neutral affinity
            Array drugTypes = Enum.GetValues(typeof(S1Product.EDrugType));
            foreach (var dt in drugTypes)
            {
                _data.DefaultAffinityData.ProductAffinities.Add(new S1Economy.ProductTypeAffinity
                {
                    DrugType = (S1Product.EDrugType)dt,
                    Affinity = 0f
                });
            }
            
            // Then override with specified affinities
            if (entries != null)
            {
                foreach (var (type, aff) in entries)
                {
                    var drugType = (S1Product.EDrugType)(int)type;
                    // Find and update existing entry
                    S1Economy.ProductTypeAffinity existing = null;
                    foreach (var item in _data.DefaultAffinityData.ProductAffinities)
                    {
                        if (item != null && item.DrugType == drugType)
                        {
                            existing = item;
                            break;
                        }
                    }
                    if (existing != null)
                    {
                        existing.Affinity = Mathf.Clamp(aff, -1f, 1f);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Adds or overrides a single product type affinity entry.
        /// </summary>
        /// <param name="drugType">Drug type to add or update.</param>
        /// <param name="affinity">Affinity value in the -1..1 range.</param>
        /// <returns>The current builder for chaining.</returns>
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
        /// <param name="propertyNames">Property asset names to resolve.</param>
        /// <returns>The current builder for chaining.</returns>
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
        /// <param name="propertyIds">Property IDs to resolve.</param>
        /// <returns>The current builder for chaining.</returns>
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
        /// <param name="wrappers">Wrapped properties to assign.</param>
        /// <returns>The current builder for chaining.</returns>
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
        /// <param name="properties">Property abstractions to resolve and assign.</param>
        /// <returns>The current builder for chaining.</returns>
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
