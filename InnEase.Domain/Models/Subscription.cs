using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a subscription plan for tenants in the InnEase CRM system.
    /// </summary>
    public class Subscription
    {
        public int Id { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public decimal MonthlyPricePesos { get; set; }
        public decimal YearlyPricePesos { get; set; }
        public int MaxRooms { get; set; }
        public int MaxUsers { get; set; }
        public bool IncludesAnalytics { get; set; }
        public bool IncludesReports { get; set; }
        public bool IncludesAPIAccess { get; set; }
        public int TrialDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Represents an active subscription assigned to a tenant.
    /// </summary>
    public class TenantSubscription
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int SubscriptionId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsYearly { get; set; }
        public bool IsActive { get; set; }
        public bool IsTrial { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
