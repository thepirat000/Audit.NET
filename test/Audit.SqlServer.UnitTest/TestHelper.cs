using System;
using Audit.IntegrationTest;
using Microsoft.Data.SqlClient;

namespace Audit.SqlServer.UnitTest
{
    internal static class TestHelper
    {
        public static string GetConnectionString(string database)
        {
            var env = AzureSettings.SqlServerConnectionString;

            return new SqlConnectionStringBuilder(env) { InitialCatalog = database }.ConnectionString;

        }
    }
}
