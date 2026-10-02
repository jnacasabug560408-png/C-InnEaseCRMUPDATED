using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents an invoice/bill for a tenant's subscription.
    /// </summary>
    public class Invoice
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int TenantSubscriptionId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string BillingPeriod { get; set; }
        public string Status { get; set; }
        public DateTime? PaidDate { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
