#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
using S1Product = Il2CppScheduleOne.Product;
using S1GameTime = Il2CppScheduleOne.GameTime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
using S1Product = ScheduleOne.Product;
using S1GameTime = ScheduleOne.GameTime;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Products;

namespace S1API.Economy
{
    /// <summary>
    /// Read-only wrapper for a base game Contract quest instance.
    /// </summary>
    public sealed class Contract
    {
        internal readonly S1Quests.Contract S1Contract;

        internal Contract(S1Quests.Contract contract)
        {
            S1Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        }

        /// <summary>
        /// Base payment amount for the contract.
        /// </summary>
        public float Payment => S1Contract.Payment;

        /// <summary>
        /// Delivery window start time (hhmm) if enabled; otherwise 0.
        /// </summary>
        public int WindowStartTime => S1Contract.DeliveryWindow != null ? S1Contract.DeliveryWindow.WindowStartTime : 0;

        /// <summary>
        /// Delivery window end time (hhmm) if enabled; otherwise 0.
        /// </summary>
        public int WindowEndTime => S1Contract.DeliveryWindow != null ? S1Contract.DeliveryWindow.WindowEndTime : 0;

        /// <summary>
        /// Total ordered quantity across all entries.
        /// </summary>
        public int TotalQuantity
        {
            get
            {
                int total = 0;
                var list = S1Contract.ProductList;
                if (list != null && list.entries != null)
                {
                    for (int i = 0; i < list.entries.Count; i++)
                    {
                        total += Math.Max(0, list.entries[i].Quantity);
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// Enumerates order lines for this contract.
        /// </summary>
        public IEnumerable<(string productId, int quantity, Quality minQuality)> GetOrders()
        {
            var list = S1Contract.ProductList;
            if (list == null || list.entries == null)
                yield break;
            for (int i = 0; i < list.entries.Count; i++)
            {
                var e = list.entries[i];
                yield return (e.ProductID, Math.Max(0, e.Quantity), e.Quality.ToAPI());
            }
        }
    }
}


