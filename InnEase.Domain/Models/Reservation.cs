using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a room reservation / booking record.
    /// </summary>
    public class Reservation
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }        // Standard, Deluxe, Suite, Presidential
        public decimal NightlyRate { get; set; }    // Per-night rate for the room type
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; }          // Pending, Confirmed, CheckedIn, CheckedOut, Cancelled
        public decimal TotalAmount { get; set; }
        public string SpecialRequests { get; set; }
        public string BookingSource { get; set; }
        public string PromoCode { get; set; }
        public DateTime CreatedDate { get; set; }

        // ── Payment ──────────────────────────────────────────────────
        public string PaymentStatus { get; set; }   // "Paid" or "Pending Payment"
        public string PaymentMethod { get; set; }   // Cash, Credit Card, Debit Card, GCash
        public string CardNumber { get; set; }       // Last 4 digits stored (masked)
        public DateTime? PaymentDate { get; set; }

        public int Nights => Math.Max(1, (CheckOutDate - CheckInDate).Days);
    }
}
