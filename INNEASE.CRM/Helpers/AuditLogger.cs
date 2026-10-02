using System;
using System.Data.SqlClient;
using InnEase.Infrastructure.Data;
using InnEase.Domain.Models;

namespace INNEASE.CRM.Helpers
{
    /// <summary>
    /// Centralized audit/activity logger for all CRM modules.
    /// Call AuditLogger.LogAction(...) from any module to record user actions.
    /// </summary>
    public static class AuditLogger
    {
        /// <summary>
        /// The currently logged-in user's full name. Set on login.
        /// </summary>
        public static string CurrentUser { get; set; } = "System";

        /// <summary>
        /// Logs an action with an explicit user name.
        /// </summary>
        public static void LogAction(string user, string action, string details, string module)
        {
            try
            {
                using (var conn = DatabaseService.GetConnection())
                {
                    int userId = 0;
                    using (var cmd = new SqlCommand("SELECT Id FROM Users WHERE FullName=@fn", conn))
                    {
                        cmd.Parameters.AddWithValue("@fn", user);
                        var res = cmd.ExecuteScalar();
                        if (res != null) userId = (int)res;
                    }

                    using (var cmd = new SqlCommand("INSERT INTO UserActivities (UserId, UserName, Action, Details, Module) VALUES (@uid, @un, @a, @d, @m)", conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@un",  user);
                        cmd.Parameters.AddWithValue("@a",   action);
                        cmd.Parameters.AddWithValue("@d",   details);
                        cmd.Parameters.AddWithValue("@m",   module);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Logs an action using the currently logged-in user (AuditLogger.CurrentUser).
        /// </summary>
        public static void LogAction(string action, string details, string module)
        {
            LogAction(CurrentUser, action, details, module);
        }
    }
}
