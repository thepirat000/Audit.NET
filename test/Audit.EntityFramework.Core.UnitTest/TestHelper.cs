using System;
using Audit.IntegrationTest;
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
            var env = TestCommon.SqlServerConnectionString;
            return new SqlConnectionStringBuilder(env) { InitialCatalog = database }.ConnectionString;

        }
    }
}
