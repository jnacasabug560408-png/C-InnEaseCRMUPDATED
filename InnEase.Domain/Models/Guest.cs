using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a hotel guest record with profile and contact information.
    /// </summary>
    public class Guest
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Nationality { get; set; }
        public bool IsVIP { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime DateRegistered { get; set; }
        public string Notes { get; set; }
    }
}
