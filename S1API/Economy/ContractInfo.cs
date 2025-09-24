#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
using S1Product = Il2CppScheduleOne.Product;
using S1Map = Il2CppScheduleOne.Map;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Quests = ScheduleOne.Quests;
using S1Product = ScheduleOne.Product;
using S1Map = ScheduleOne.Map;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Products;

namespace S1API.Economy
{
    /// <summary>
    /// Builder/DTO for offering a contract to a customer.
    /// </summary>
    public sealed class ContractInfo
    {
        /// <summary>
        /// A single product order line in the contract.
        /// </summary>
        public sealed class OrderLine
        {
            /// <summary>
            /// Product registry ID.
            /// </summary>
            public string ProductId { get; set; }

            /// <summary>
            /// Quantity of this product to deliver.
            /// </summary>
            public int Quantity { get; set; }

            /// <summary>
            /// Minimum acceptable quality.
            /// </summary>
            public Quality MinQuality { get; set; }
        }

        /// <summary>
        /// Contract payment (base, excluding bonuses).
        /// </summary>
        public float Payment { get; set; }

        /// <summary>
        /// Ordered products.
        /// </summary>
        public List<OrderLine> Orders { get; } = new List<OrderLine>();

        /// <summary>
        /// Delivery location GUID (optional). If null or invalid, a reasonable location will be chosen.
        /// </summary>
        public string DeliveryLocationGuid { get; set; }

        /// <summary>
        /// Optional delivery window. If not set, game defaults are used.
        /// </summary>
        public (int startTime, int endTime)? DeliveryWindow { get; set; }

        /// <summary>
        /// Whether this is a counter offer.
        /// </summary>
        public bool IsCounterOffer { get; set; }

        /// <summary>
        /// Whether the offer expires automatically.
        /// </summary>
        public bool Expires { get; set; } = true;

        /// <summary>
        /// Minutes until expiry if <see cref="Expires"/> is true.
        /// </summary>
        public int ExpiresAfterMinutes { get; set; } = 600;

        /// <summary>
        /// Optional pickup schedule index; 0 is typical for immediate offers.
        /// </summary>
        public int PickupScheduleIndex { get; set; } = 0;

        /// <summary>
        /// Adds a product by API definition.
        /// </summary>
        public ContractInfo AddProduct(Products.ProductDefinition definition, int quantity, Quality minQuality)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return AddProductById(definition.ID, quantity, minQuality);
        }

        /// <summary>
        /// Adds a product by registry ID.
        /// </summary>
        public ContractInfo AddProductById(string productId, int quantity, Quality minQuality)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            Orders.Add(new OrderLine
            {
                ProductId = productId,
                Quantity = Math.Max(1, quantity),
                MinQuality = minQuality
            });
            return this;
        }

        /// <summary>
        /// Sets the delivery window in 24h hhmm format.
        /// </summary>
        public ContractInfo WithWindow(int startTime, int endTime)
        {
            DeliveryWindow = (ClampTime(startTime), ClampTime(endTime));
            return this;
        }

        private static int ClampTime(int hhmm)
        {
            if (hhmm < 0) return 0;
            if (hhmm > 2359) return 2359;
            return hhmm;
        }

        /// <summary>
        /// INTERNAL: Converts to the game's ContractInfo type.
        /// </summary>
        internal S1Quests.ContractInfo ToInternal()
        {
            var s1List = new S1Product.ProductList();
            for (int i = 0; i < Orders.Count; i++)
            {
                var line = Orders[i];
                s1List.entries.Add(new S1Product.ProductList.Entry
                {
                    ProductID = line.ProductId,
                    Quantity = Math.Max(1, line.Quantity),
                    Quality = line.MinQuality.ToInternal()
                });
            }

            var window = new S1Quests.QuestWindowConfig
            {
                IsEnabled = DeliveryWindow.HasValue,
                WindowStartTime = DeliveryWindow?.startTime ?? 0,
                WindowEndTime = DeliveryWindow?.endTime ?? 0
            };

            return new S1Quests.ContractInfo(
                payment: Math.Max(0f, Payment),
                products: s1List,
                deliveryLocationGUID: DeliveryLocationGuid ?? string.Empty,
                deliveryWindow: window,
                expires: Expires,
                expiresAfter: Math.Max(0, ExpiresAfterMinutes),
                pickupScheduleIndex: Math.Max(0, PickupScheduleIndex),
                isCounterOffer: IsCounterOffer
            );
        }
    }
}


