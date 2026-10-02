using System;
using System.Data;
using System.Data.SqlClient;

namespace InnEase.Infrastructure.Data
{
    public static class DatabaseService
    {
        // Connection string provided by the user
        private static readonly string _connectionString = "Server=db71296.public.databaseasp.net; Database=db71296; User Id=db71296; Password=crmtenant1; Encrypt=False; MultipleActiveResultSets=True;";

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
