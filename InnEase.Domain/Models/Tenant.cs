using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a tenant (hotel/property) in the multi-tenant InnEase CRM system.
    /// </summary>
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PropertyName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public decimal MonthlyPaymentAmount { get; set; }
        public string BillingCycle { get; set; } // "Monthly" or "Yearly"
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
