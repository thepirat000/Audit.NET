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
        internal const string SqlConnectionStringTemplate = "data source=localhost;initial catalog={0};integrated security=true;Encrypt=False;";
        internal const string DatabaseName = "AuditSqlServerTests";
        internal const string TableName = "AuditEvent";

        internal static string GetConnectionString()
        {
            return string.Format(SqlConnectionStringTemplate, DatabaseName);
        }

        internal static void EnsureDatabaseCreated()
        {
            using (var cnn = new SqlConnection(string.Format(SqlConnectionStringTemplate, "master")))
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

            using (var cnn = new SqlConnection(string.Format(SqlConnectionStringTemplate, DatabaseName)))
            using (var cmd = new SqlCommand(tableCreate, cnn))
            {
                cnn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
