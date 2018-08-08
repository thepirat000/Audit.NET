using System;
using System.Linq;
using System.Threading.Tasks;
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

        public MySqlDataProvider()
        {
        }

        public MySqlDataProvider(Action<Configuration.IMySqlServerProviderConfigurator> config)
        {
            var mysqlConfig = new Configuration.MySqlServerProviderConfigurator();
            if (config != null)
            {
                config.Invoke(mysqlConfig);
                _connectionString = mysqlConfig._connectionString;
                _idColumnName = mysqlConfig._idColumnName;
                _jsonColumnName = mysqlConfig._jsonColumnName;
                _tableName = mysqlConfig._tableName;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var jsonParam = new MySqlParameter("@value", auditEvent.ToJson());
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmd = GetInsertCommand(cnn, jsonParam);
                object id = cmd.ExecuteScalar();
                return id;
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var jsonParam = new MySqlParameter("@value", auditEvent.ToJson());
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmd = GetInsertCommand(cnn, jsonParam);
                object id = await cmd.ExecuteScalarAsync();
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var jsonParam = new MySqlParameter("@value", auditEvent.ToJson());
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmd = GetReplaceCommand(cnn, jsonParam, idParam);
                cmd.ExecuteNonQuery();
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var jsonParam = new MySqlParameter("@value", auditEvent.ToJson());
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmd = GetReplaceCommand(cnn, jsonParam, idParam);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public override T GetEvent<T>(object eventId) 
        {
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmd = GetSelectCommand(cnn, idParam);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        var json = reader.GetFieldValue<string>(0);
                        return AuditEvent.FromJson<T>(json);
                    }
                }
            }
            return null;
        }

        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(_connectionString))
            {
                var cmd = GetSelectCommand(cnn, idParam);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        var json = await reader.GetFieldValueAsync<string>(0);
                        return AuditEvent.FromJson<T>(json);
                    }
                }
            }
            return null;
        }

        private MySqlCommand GetInsertCommand(MySqlConnection cnn, MySqlParameter valueParam)
        {
            var cmdText = string.Format("INSERT INTO `{0}` (`{1}`) VALUES(@value); SELECT LAST_INSERT_ID();", _tableName, _jsonColumnName);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.Parameters.Add(valueParam);
            return cmd;
        }

        private MySqlCommand GetReplaceCommand(MySqlConnection cnn, MySqlParameter valueParam, MySqlParameter idParam)
        {
            var cmdText = string.Format("UPDATE `{0}` SET `{1}` = @value WHERE `{2}` = @id;", _tableName, _jsonColumnName, _idColumnName);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.Parameters.Add(valueParam);
            cmd.Parameters.Add(idParam);
            return cmd;
        }

        private MySqlCommand GetSelectCommand(MySqlConnection cnn, MySqlParameter idParam)
        {
            var cmdText = string.Format("SELECT `{0}` FROM `{1}` WHERE `{2}` = @id;", _jsonColumnName, _tableName, _idColumnName);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.Parameters.Add(idParam);
            return cmd;
        }
    }
}
