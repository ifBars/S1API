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
using S1API.Map;

namespace S1API.Economy
{
    /// <summary>
    /// Builder for composing ContractInfo at runtime with a fluent API.
    /// Provides an easy way to create contracts for NPC customers.
    /// </summary>
    public sealed class ContractInfoBuilder
    {
        private float _payment;
        private readonly List<ContractInfo.OrderLine> _orders = new List<ContractInfo.OrderLine>();
        private DeliveryLocation _deliveryLocation;
        private (int startTime, int endTime)? _deliveryWindow;
        private bool _isCounterOffer;
        private bool _expires = true;
        private int _expiresAfterMinutes = 600;
        private int _pickupScheduleIndex = 0;

        /// <summary>
        /// Creates a new ContractInfoBuilder instance.
        /// </summary>
        public ContractInfoBuilder()
        {
        }

        /// <summary>
        /// Sets the contract payment amount.
        /// </summary>
        /// <param name="payment">The payment amount (must be >= 0)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithPayment(float payment)
        {
            _payment = Math.Max(0f, payment);
            return this;
        }

        /// <summary>
        /// Adds a product to the contract by product definition.
        /// </summary>
        /// <param name="definition">The product definition</param>
        /// <param name="quantity">Quantity to order (minimum 1)</param>
        /// <param name="minQuality">Minimum acceptable quality</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder AddProduct(ProductDefinition definition, int quantity, Quality minQuality)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _orders.Add(new ContractInfo.OrderLine
            {
                ProductId = definition.ID,
                Quantity = Math.Max(1, quantity),
                MinQuality = minQuality
            });
            return this;
        }

        /// <summary>
        /// Adds multiple products to the contract.
        /// </summary>
        /// <param name="products">Collection of (definition, quantity, minQuality) tuples</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder AddProducts(IEnumerable<(ProductDefinition definition, int quantity, Quality minQuality)> products)
        {
            if (products == null)
                return this;

            foreach (var (definition, quantity, minQuality) in products)
            {
                AddProduct(definition, quantity, minQuality);
            }
            return this;
        }

        /// <summary>
        /// Sets the delivery location using a DeliveryLocation wrapper.
        /// </summary>
        /// <param name="location">The delivery location</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithDeliveryLocation(DeliveryLocation location)
        {
            _deliveryLocation = location;
            return this;
        }

        /// <summary>
        /// Sets the delivery location by name using DeliveryLocations.GetByName().
        /// </summary>
        /// <param name="locationName">The delivery location name</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithDeliveryLocationByName(string locationName)
        {
            _deliveryLocation = DeliveryLocation.GetByName(locationName);
            return this;
        }

        /// <summary>
        /// Sets the delivery location by GUID using DeliveryLocations.GetByGuid().
        /// </summary>
        /// <param name="locationGuid">The delivery location GUID</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithDeliveryLocationByGuid(string locationGuid)
        {
            _deliveryLocation = DeliveryLocation.GetByGuid(locationGuid);
            return this;
        }

        /// <summary>
        /// Sets the delivery window in 24h hhmm format.
        /// </summary>
        /// <param name="startTime">Start time (e.g., 900 for 9:00 AM)</param>
        /// <param name="endTime">End time (e.g., 1700 for 5:00 PM)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithDeliveryWindow(int startTime, int endTime)
        {
            _deliveryWindow = (ClampTime(startTime), ClampTime(endTime));
            return this;
        }

        /// <summary>
        /// Sets the delivery window using TimeSpan for convenience.
        /// </summary>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithDeliveryWindow(TimeSpan startTime, TimeSpan endTime)
        {
            var startHhmm = (int)(startTime.TotalHours * 100) + startTime.Minutes;
            var endHhmm = (int)(endTime.TotalHours * 100) + endTime.Minutes;
            return WithDeliveryWindow(startHhmm, endHhmm);
        }

        /// <summary>
        /// Removes the delivery window constraint.
        /// </summary>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithoutDeliveryWindow()
        {
            _deliveryWindow = null;
            return this;
        }

        /// <summary>
        /// Marks this contract as a counter offer.
        /// </summary>
        /// <param name="isCounterOffer">Whether this is a counter offer</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder AsCounterOffer(bool isCounterOffer = true)
        {
            _isCounterOffer = isCounterOffer;
            return this;
        }

        /// <summary>
        /// Sets whether the contract expires automatically.
        /// </summary>
        /// <param name="expires">Whether the contract expires</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithExpiration(bool expires)
        {
            _expires = expires;
            return this;
        }

        /// <summary>
        /// Sets the expiration time in minutes.
        /// </summary>
        /// <param name="minutes">Minutes until expiry (minimum 0)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder ExpiresAfter(int minutes)
        {
            _expiresAfterMinutes = Math.Max(0, minutes);
            return this;
        }

        /// <summary>
        /// Sets the pickup schedule index.
        /// </summary>
        /// <param name="index">Pickup schedule index (minimum 0)</param>
        /// <returns>This builder instance for method chaining</returns>
        public ContractInfoBuilder WithPickupScheduleIndex(int index)
        {
            _pickupScheduleIndex = Math.Max(0, index);
            return this;
        }

        /// <summary>
        /// Creates a quick contract with a single product.
        /// </summary>
        /// <param name="definition">Product definition</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="payment">Payment amount</param>
        /// <param name="minQuality">Minimum quality (defaults to Standard)</param>
        /// <returns>A new ContractInfo instance</returns>
        public static ContractInfo QuickContract(ProductDefinition definition, int quantity, float payment, Quality minQuality = Quality.Standard)
        {
            return new ContractInfoBuilder()
                .WithPayment(payment)
                .AddProduct(definition, quantity, minQuality)
                .Build();
        }


        /// <summary>
        /// Builds the final ContractInfo instance.
        /// </summary>
        /// <returns>A new ContractInfo instance with the configured settings</returns>
        /// <exception cref="InvalidOperationException">Thrown if no products have been added</exception>
        public ContractInfo Build()
        {
            if (_orders.Count == 0)
                throw new InvalidOperationException("Contract must have at least one product");

            var contractInfo = new ContractInfo
            {
                Payment = _payment,
                DeliveryLocationGuid = _deliveryLocation?.GUID ?? string.Empty,
                DeliveryWindow = _deliveryWindow,
                IsCounterOffer = _isCounterOffer,
                Expires = _expires,
                ExpiresAfterMinutes = _expiresAfterMinutes,
                PickupScheduleIndex = _pickupScheduleIndex
            };

            // Copy orders to the contract info
            foreach (var order in _orders)
            {
                contractInfo.Orders.Add(order);
            }

            return contractInfo;
        }

        private static int ClampTime(int hhmm)
        {
            if (hhmm < 0) return 0;
            if (hhmm > 2359) return 2359;
            return hhmm;
        }
    }
}
