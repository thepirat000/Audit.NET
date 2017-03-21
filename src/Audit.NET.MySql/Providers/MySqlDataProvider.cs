using System.Linq;
using Audit.Core;
using MySql.Data.MySqlClient;

namespace Audit.MySql.Providers
{
    /// <summary>
    /// MySQL Server data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: MySQL connection string
    /// - TableName: Table name (default is 'event')
    /// - JsonColumnName: Column name where the JSON will be stored (default is 'data')
    /// - IdColumnName: Column name with the primary key (default is 'id')
    /// </remarks>
    public class MySqlDataProvider : AuditDataProvider
    {
        private string _connectionString;
        private string _tableName = "event";
        private string _idColumnName = "id";
        private string _jsonColumnName = "data";

        /// <summary>
        /// The MySQL connection string
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        /// <summary>
        /// The MySQL events Table Name
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
        /// The Column Name that is the primary ley
        /// </summary>
        public string IdColumnName
        {
            get { return _idColumnName; }
            set { _idColumnName = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var jsonParam = new MySqlParameter("@value", auditEvent.ToJson());
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmdText = string.Format("INSERT INTO `{0}` (`{1}`) VALUES(@value); SELECT LAST_INSERT_ID();", _tableName, _jsonColumnName);
                cnn.Open();
                var cmd = cnn.CreateCommand();
                cmd.CommandText = cmdText;
                cmd.Parameters.Add(jsonParam);
                object id = cmd.ExecuteScalar();
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var jsonParam = new MySqlParameter("@value", auditEvent.ToJson());
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmdText = string.Format("UPDATE `{0}` SET `{1}` = @value WHERE `{2}` = @id;", _tableName, _jsonColumnName, _idColumnName);
                cnn.Open();
                var cmd = cnn.CreateCommand();
                cmd.CommandText = cmdText;
                cmd.Parameters.Add(jsonParam);
                cmd.Parameters.Add(idParam);
                int rows = cmd.ExecuteNonQuery();
            }
        }
    }
}
