using System;
#if NET45
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace Audit.SqlServer.UnitTest
{
    internal static class TestHelper
    {
        public static string GetConnectionString(string database)
        {
            var env = Environment.GetEnvironmentVariable("SQL_SERVER_CONNECTION_STRING");
            if (env == null)
            {
                throw new Exception("Environment variable 'SQL_SERVER_CONNECTION_STRING' not set.");
            }
            return new SqlConnectionStringBuilder(env) { InitialCatalog = database }.ConnectionString;

        }
    }
}
