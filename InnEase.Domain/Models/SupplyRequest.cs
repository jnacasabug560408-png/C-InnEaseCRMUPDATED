using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a request for supplies made by managers or staff.
    /// </summary>
    public class SupplyRequest
    {
        public int Id { get; set; }
        public int SupplyId { get; set; }
        public string SupplyName { get; set; }
        public int RequestedQuantity { get; set; }
        public string RequestedBy { get; set; }
        public string Department { get; set; }
        public string Priority { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? FulfilledDate { get; set; }
        public string Notes { get; set; }
    }
}
