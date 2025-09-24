using System;

namespace S1API.Economy
{
    /// <summary>
    /// Lightweight DTO mirroring base game's ContractReceipt for analytics/history.
    /// </summary>
    public sealed class ContractReceipt
    {
        public int ReceiptId { get; set; } = -1;
        public string CustomerId { get; set; } = string.Empty;
        public float AmountPaid { get; set; } = 0f;
        public (int days, int time) CompletionTime { get; set; } = (0, 0);
        public (string id, int quantity)[] Items { get; set; } = Array.Empty<(string id, int quantity)>();
    }
}


