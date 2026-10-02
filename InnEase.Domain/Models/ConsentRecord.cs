using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a GDPR / data privacy consent record for a guest.
    /// </summary>
    public class ConsentRecord
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; }
        public bool MarketingConsent { get; set; }
        public bool DataProcessingConsent { get; set; }
        public bool ThirdPartyShareConsent { get; set; }
        public string ConsentMethod { get; set; }  // Online, InPerson, Phone
        public DateTime ConsentDate { get; set; }
        public string RecordedBy { get; set; }
    }
}
