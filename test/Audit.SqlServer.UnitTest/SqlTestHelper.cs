#if NET45
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif

namespace Audit.SqlServer.UnitTest
{
    internal static class SqlTestHelper
    {
        internal static string CnnStringAudit = TestHelper.GetConnectionString("Audit");
        internal static string CnnStringMaster = TestHelper.GetConnectionString("master");

        internal const string DatabaseName = "AuditSqlServerTests";
        internal const string TableName = "AuditEvent";

        internal static string GetConnectionString()
        {
            return CnnStringAudit;
        }

        internal static void EnsureDatabaseCreated()
        {
            using (var cnn = new SqlConnection(CnnStringMaster))
            using (var cmd = new SqlCommand($"IF DB_ID('{DatabaseName}') IS NULL CREATE DATABASE {DatabaseName}", cnn))
            {
                cnn.Open();
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    if (e.Message.Contains("already exists"))
                    {
                        return;
                    }
                    throw;
                }
            }

            var tableCreate = $@"IF OBJECT_ID('{TableName}') IS NULL CREATE TABLE [{TableName}]
                (
	                EventId BIGINT IDENTITY(1,1) NOT NULL,
	                InsertedDate datetimeoffset NOT NULL DEFAULT(GETDATE()),
	                LastUpdatedDate datetimeoffset NOT NULL DEFAULT(GETDATE()),
	                EventType NVARCHAR(MAX),
	                [Data] NVARCHAR(MAX) NOT NULL,
	                CONSTRAINT PK_Event PRIMARY KEY (EventId)
                )";

            using (var cnn = new SqlConnection(CnnStringAudit))
            using (var cmd = new SqlCommand(tableCreate, cnn))
            {
                cnn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
