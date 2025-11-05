#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Items = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1Items = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Builder for configuring startup items and random cash for NPCs.
    /// Public surface uses strings/primitives only. All configurations are optional.
    /// </summary>
    public sealed class RandomInventoryItemsBuilder
    {
        private readonly List<string> _startupItems = new List<string>();
        private int? _randomCashMin;
        private int? _randomCashMax;
        private bool? _clearInventoryEachNight;

        /// <summary>
        /// Runtime instantiation is disabled; only NPCPrefabBuilder can create this.
        /// </summary>
        internal RandomInventoryItemsBuilder() { }

        /// <summary>
        /// Enables and configures random cash generation.
        /// </summary>
        /// <param name="min">Minimum cash amount.</param>
        /// <param name="max">Maximum cash amount.</param>
        public RandomInventoryItemsBuilder WithRandomCash(int min, int max)
        {
            _randomCashMin = Mathf.Max(0, Mathf.Min(min, max));
            _randomCashMax = Mathf.Max(0, Mathf.Max(min, max));
            return this;
        }

        /// <summary>
        /// Adds a single startup item that will be present in the NPC's inventory when spawned.
        /// </summary>
        /// <param name="itemId">The ID of the item to add.</param>
        public RandomInventoryItemsBuilder WithStartupItem(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                _startupItems.Add(itemId);
            }
            return this;
        }

        /// <summary>
        /// Adds multiple startup items that will be present in the NPC's inventory when spawned.
        /// </summary>
        /// <param name="itemIds">Array of item IDs to add.</param>
        public RandomInventoryItemsBuilder WithStartupItems(params string[] itemIds)
        {
            if (itemIds == null)
                return this;
            foreach (var itemId in itemIds)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    _startupItems.Add(itemId);
                }
            }
            return this;
        }

        /// <summary>
        /// Configures whether the inventory should be cleared each night.
        /// Default: true if only random items are configured, false if startup items are configured (to preserve them).
        /// </summary>
        /// <param name="clearEachNight">If true, inventory is cleared on sleep. If false, items persist across sleep cycles.</param>
        public RandomInventoryItemsBuilder WithClearInventoryEachNight(bool clearEachNight)
        {
            _clearInventoryEachNight = clearEachNight;
            return this;
        }

        /// <summary>
        /// Internal data structure returned by BuildInternal().
        /// </summary>
        internal sealed class InventoryDefaultsData
        {
            public List<string> StartupItems { get; set; }
            public int? RandomCashMin { get; set; }
            public int? RandomCashMax { get; set; }
            public bool? ClearInventoryEachNight { get; set; }
        }

        /// <summary>
        /// Builds the internal configuration data structure.
        /// </summary>
        internal InventoryDefaultsData BuildInternal()
        {
            // Auto-determine ClearInventoryEachNight if not explicitly set:
            // - If startup items are configured, default to false (preserve startup items)
            // - Otherwise, leave as null to use base game default
            bool? clearEachNight = _clearInventoryEachNight;
            if (!clearEachNight.HasValue && _startupItems.Count > 0)
            {
                clearEachNight = false;
            }

            return new InventoryDefaultsData
            {
                StartupItems = new List<string>(_startupItems),
                RandomCashMin = _randomCashMin,
                RandomCashMax = _randomCashMax,
                ClearInventoryEachNight = clearEachNight
            };
        }
    }
}

