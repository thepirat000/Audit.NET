using System.Linq;
using Audit.Core;
using System.Data.SqlClient;
#if NETCOREAPP1_0
using Microsoft.EntityFrameworkCore;
#endif

namespace Audit.SqlServer.Providers
{
    /// <summary>
    /// SQL Server data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: SQL connection string
    /// - TableName: Table name
    /// - JsonColumnName: Column name where the JSON will be stored
    /// - IdColumnName: Column name with the primary key
    /// - LastUpdatedDateColumnName: datetime column to update when replacing events (NULL to ignore)
    /// </remarks>
    public class SqlDataProvider : AuditDataProvider
    {
        private string _connectionString;
        private string _schema = null;
        private string _tableName = "Event";
        private string _idColumnName = "Id";
        private string _jsonColumnName = "Data";
        private string _lastUpdatedDateColumnName = null;

        /// <summary>
        /// The SQL connection string
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        /// <summary>
        /// The SQL events Table Name
        /// </summary>
        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        /// <summary>
        /// The Column Name that stores the JSON
        /// </summary>
        public string JsonColumnName
        {
            get { return _jsonColumnName; }
            set { _jsonColumnName = value; }
        }

        /// <summary>
        /// The Column Name that stores the Last Updated Date (NULL to ignore)
        /// </summary>
        public string LastUpdatedDateColumnName
        {
            get { return _lastUpdatedDateColumnName; }
            set { _lastUpdatedDateColumnName = value; }
        }

        /// <summary>
        /// The Column Name that is the primary ley
        /// </summary>
        public string IdColumnName
        {
            get { return _idColumnName; }
            set { _idColumnName = value; }
        }

        /// <summary>
        /// The Schema Name to use (NULL to ignore)
        /// </summary>
        public string Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var json = new SqlParameter("json", auditEvent.ToJson());
            using (var ctx = new AuditDbContext(_connectionString))
            {
                var cmdText = string.Format("INSERT INTO {0} ([{1}]) OUTPUT CONVERT(NVARCHAR(MAX), INSERTED.[{2}]) AS [Id] VALUES (@json)", FullTableName, _jsonColumnName, _idColumnName);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, json);
                return result.FirstOrDefault();
#elif NETCOREAPP1_0
                var result = ctx.FakeIdSet.FromSql(cmdText, json);
                return result.FirstOrDefault().Id;
#endif
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var json = auditEvent.ToJson();
            using (var ctx = new AuditDbContext(_connectionString))
            {
                var ludScript = _lastUpdatedDateColumnName != null ? string.Format(", [{0}] = GETUTCDATE()", _lastUpdatedDateColumnName) : string.Empty;
                var cmdText = string.Format("UPDATE {0} SET [{1}] = @json{2} WHERE [{3}] = @eventId",
                        FullTableName, _jsonColumnName, ludScript, _idColumnName);
                ctx.Database.ExecuteSqlCommand(cmdText, new SqlParameter("@json", json), new SqlParameter("@eventId", eventId));
            }
        }

        private string FullTableName
        {
            get
            {
                return _schema != null
                    ? string.Format("[{0}].[{1}]", _schema, _tableName)
                    : string.Format("[{0}]", _tableName);
            }
        }

    }
}
