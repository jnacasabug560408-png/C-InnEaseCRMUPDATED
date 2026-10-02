using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a booking channel (Direct, OTA, Corporate, etc.)
    /// </summary>
    public class BookingChannel
    {
        public int Id { get; set; }
        public string ChannelName { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
    }
}
