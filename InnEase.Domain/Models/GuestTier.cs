using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents guest tier/segment metrics for reporting
    /// </summary>
    public class GuestTier
    {
        public string TierName { get; set; }
        public int Count { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgSpend { get; set; }
    }
}
