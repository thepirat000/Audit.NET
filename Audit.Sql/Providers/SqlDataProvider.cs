using System.Data.SqlClient;
using Audit.Core;

namespace Audit.SqlServer.Providers
{
    /// <summary>
    /// SQL Server data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - AuditConnectionString: SQL connection string
    /// - AuditEventTable: Table name
    /// </remarks>
    public class SqlDataProvider : AuditDataProvider
    {
        private string _connectionString;
        private string _tableName = "Event";
        private string _columnName = "Data";
        private bool _shouldTestConnection = true;

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        public bool ShouldTestConnection
        {
            get { return _shouldTestConnection; }
            set { _shouldTestConnection = value; }
        }

        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        }

        public override void WriteEvent(AuditEvent auditEvent)
        { 
            var json = auditEvent.ToJson();
            using (var ctx = new Audit.SqlServer.Entities(_connectionString))
            {
                ctx.Database.ExecuteSqlCommand(string.Format("INSERT INTO [{0}] ([{1}]) VALUES (@json)", _tableName, _columnName), new SqlParameter("@json", json));
            }
        }

        public override bool TestConnection()
        {
            if (!_shouldTestConnection)
            {
                return true;
            }
            using (var ctx = new Audit.SqlServer.Entities(_connectionString))
            {
                try
                {
                    ctx.Database.Connection.Open();
                    ctx.Database.Connection.Close();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
