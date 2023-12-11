using System;
#if EF_CORE
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Audit.EntityFramework.Core.UnitTest
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
