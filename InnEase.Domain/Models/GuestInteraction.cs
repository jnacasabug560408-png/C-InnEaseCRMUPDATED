using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a logged guest interaction (check-in, check-out, or note).
    /// </summary>
    public class GuestInteraction
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; }
        public string InteractionType { get; set; }  // CheckIn, CheckOut, Note, Call, Email
        public string Details { get; set; }
        public string LoggedBy { get; set; }
        public DateTime InteractionDate { get; set; }
    }
}
