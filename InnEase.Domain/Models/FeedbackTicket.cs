using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a guest feedback or complaint ticket.
    /// </summary>
    public class FeedbackTicket
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; }
        public string Category { get; set; }  // Complaint, Suggestion, Compliment
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }  // Pending, InProgress, Resolved, Closed
        public string Priority { get; set; }  // Low, Medium, High, Critical
        public string AssignedTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }
}
