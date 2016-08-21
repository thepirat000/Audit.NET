using System.Data.SqlClient;
using System.Linq;
using Audit.Core;

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

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var json = auditEvent.ToJson();
            using (var ctx = new Audit.SqlServer.Entities(_connectionString))
            {
                var cmdText = string.Format("INSERT INTO [{0}] ([{1}]) OUTPUT INSERTED.[{2}] VALUES (@json)", _tableName, _jsonColumnName, _idColumnName);
                var eventId = ctx.Database.SqlQuery<long>(cmdText, new SqlParameter("@json", json)).FirstOrDefault();
                return eventId;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        { 
            var json = auditEvent.ToJson();
            using (var ctx = new Audit.SqlServer.Entities(_connectionString))
            {
                var ludScript = _lastUpdatedDateColumnName != null ? string.Format(", [{0}] = GETUTCDATE()", _lastUpdatedDateColumnName) : string.Empty;
                var cmdText =
                    string.Format(
                        "UPDATE [{0}] SET [{1}] = @json{2} WHERE [{3}] = @eventId",
                        _tableName, _jsonColumnName, ludScript, _idColumnName);
                ctx.Database.ExecuteSqlCommand(cmdText, new SqlParameter("@json", json), new SqlParameter("@eventId", eventId));
            }
        }

        private void TestConnection()
        {
            using (var ctx = new Audit.SqlServer.Entities(_connectionString))
            {
                ctx.Database.Connection.Open();
                ctx.Database.Connection.Close();
            }
        }
    }
}
