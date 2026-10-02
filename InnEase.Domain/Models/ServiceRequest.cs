using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a service request from a guest.
    /// </summary>
    public class ServiceRequest
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }    // Standard, Deluxe, Suite, Presidential
        public string RequestType { get; set; }  // Housekeeping, RoomService, Maintenance, Concierge
        public string Description { get; set; }
        public string Status { get; set; }  // Pending, InProgress, Completed
        public string AssignedTo { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
