using System;
using S1API.Internal.Utils;

namespace S1API.Entities.Dealer
{
    /// <summary>
    /// Builder for configuring how a dealer should be recommended to the player.
    /// </summary>
    public sealed class DealerRecommendationBuilder
    {
        internal enum Trigger
        {
            DealCompleted = 0
        }

        internal sealed class RecommendationConfigData
        {
            public string CustomerId { get; set; } = string.Empty;
            public Trigger? RecommendationTrigger { get; set; }
        }

        private readonly RecommendationConfigData _data;

        internal DealerRecommendationBuilder()
        {
            _data = new RecommendationConfigData();
        }

        /// <summary>
        /// Uses the provided customer wrapper as the source of the recommendation.
        /// </summary>
        /// <param name="customer">The customer who should recommend the dealer.</param>
        public DealerRecommendationBuilder FromCustomer(NPC customer)
        {
            _data.CustomerId = customer?.ID ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Uses the provided customer NPC ID as the source of the recommendation.
        /// </summary>
        /// <param name="customerId">The recommending customer's NPC ID.</param>
        public DealerRecommendationBuilder FromCustomer(string customerId)
        {
            _data.CustomerId = customerId ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Uses the specified customer NPC type as the source of the recommendation.
        /// This overload works during prefab configuration when a wrapper instance is not available yet.
        /// </summary>
        /// <typeparam name="TCustomer">The recommending customer NPC type.</typeparam>
        public DealerRecommendationBuilder FromCustomer<TCustomer>() where TCustomer : NPC
        {
            _data.CustomerId = NPCTypeUtils.TryGetStaticNPCId(typeof(TCustomer)) ??
                               NPCTypeUtils.TryResolveNPCIdFromType(typeof(TCustomer)) ??
                               string.Empty;
            return this;
        }

        /// <summary>
        /// Triggers the recommendation when the configured customer completes a deal.
        /// </summary>
        public DealerRecommendationBuilder OnDealCompleted()
        {
            _data.RecommendationTrigger = Trigger.DealCompleted;
            return this;
        }

        internal RecommendationConfigData BuildInternal() => _data;
    }
}
