using System;

namespace InnEase.Domain.Models
{
    /// <summary>
    /// Defines the four role-based access levels in the InnEase CRM system.
    /// </summary>
    public enum UserRole
    {
        SuperAdmin,
        Admin,
        Manager,
        Staff
    }
}
