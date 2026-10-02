using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a branch location in the hotel management system.
    /// </summary>
    public class Branch
    {
        public int Id { get; set; }
        public string BranchName { get; set; }
        public string BranchCode { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ManagerName { get; set; }
        public bool IsActive { get; set; }
    }
}
