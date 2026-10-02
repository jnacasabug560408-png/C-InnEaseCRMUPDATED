using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a supply item in the hotel inventory.
    /// </summary>
    public class Supply
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public int MinimumStock { get; set; }
        public string Unit { get; set; }
        public decimal UnitCost { get; set; }
        public string Supplier { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastRestockedDate { get; set; }
    }
}
