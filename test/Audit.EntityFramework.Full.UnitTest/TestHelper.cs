using System;
using Audit.IntegrationTest;
#if EF_CORE
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Audit.EntityFramework.Full.UnitTest
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