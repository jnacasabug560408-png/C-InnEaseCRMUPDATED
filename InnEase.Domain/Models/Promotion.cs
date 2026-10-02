using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a promotional voucher or discount code.
    /// </summary>
    public class Promotion
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public int MaxUsage { get; set; }
    }
}
