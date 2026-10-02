using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents payment history for tenant subscriptions.
    /// </summary>
    public class BillingHistory
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionReference { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
