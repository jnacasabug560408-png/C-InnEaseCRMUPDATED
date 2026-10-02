using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Represents a user activity log entry for tracking user actions
    /// </summary>
    public class UserActivity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Module { get; set; }
    }
}
