using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using MySqlConnector;

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
        /// <summary>
        /// The MySQL connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The MySQL events Table Name
        /// </summary>
        public string TableName { get; set; } = "event";

        /// <summary>
        /// The Column Name that stores the JSON
        /// </summary>
        public string JsonColumnName { get; set; } = "data";

        /// <summary>
        /// The Column Name that is the primary ley
        /// </summary>
        public string IdColumnName { get; set; } = "id";

        /// <summary>
        /// A collection of custom columns to be added when saving the audit event 
        /// </summary>
        public List<CustomColumn> CustomColumns { get; set; } = new List<CustomColumn>();

        public MySqlDataProvider()
        {
        }

        public MySqlDataProvider(Action<Configuration.IMySqlServerProviderConfigurator> config)
        {
            var mysqlConfig = new Configuration.MySqlServerProviderConfigurator();
            if (config != null)
            {
                config.Invoke(mysqlConfig);
                ConnectionString = mysqlConfig._connectionString;
                IdColumnName = mysqlConfig._idColumnName;
                JsonColumnName = mysqlConfig._jsonColumnName;
                TableName = mysqlConfig._tableName;
                CustomColumns = mysqlConfig._customColumns;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            using (var cnn = new MySqlConnection(ConnectionString))
            {
                var cmd = GetInsertCommand(cnn, auditEvent);
                object id = cmd.ExecuteScalar();
                return id;
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            using (var cnn = new MySqlConnection(ConnectionString))
            {
                var cmd = GetInsertCommand(cnn, auditEvent);
                object id = await cmd.ExecuteScalarAsync(cancellationToken);
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            using (var cnn = new MySqlConnection(ConnectionString))
            {
                var cmd = GetReplaceCommand(cnn, eventId, auditEvent);
                cmd.ExecuteNonQuery();
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            using (var cnn = new MySqlConnection(ConnectionString))
            {
                var cmd = GetReplaceCommand(cnn, eventId, auditEvent);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(ConnectionString))
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

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var idParam = new MySqlParameter("@id", eventId);
            using (var cnn = new MySqlConnection(ConnectionString))
            {
                var cmd = GetSelectCommand(cnn, idParam);
                using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync(cancellationToken);
                        var json = await reader.GetFieldValueAsync<string>(0, cancellationToken);
                        return AuditEvent.FromJson<T>(json);
                    }
                }
            }
            return null;
        }

        protected MySqlCommand GetInsertCommand(MySqlConnection cnn, AuditEvent auditEvent)
        {
            var cmdText = string.Format("INSERT INTO `{0}` ({1}) VALUES({2}); SELECT LAST_INSERT_ID();", 
                TableName,
                GetColumnsForInsert(),
                GetParameterNamesForInsert());
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.Parameters.AddRange(GetParameterValues(auditEvent).ToArray());
            return cmd;
        }

        protected MySqlCommand GetReplaceCommand(MySqlConnection cnn, object eventId, AuditEvent auditEvent)
        {
            var cmdText = string.Format("UPDATE `{0}` SET {1} WHERE `{2}` = @id;", 
                TableName, 
                GetSetForUpdate(auditEvent), 
                IdColumnName);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.Parameters.AddRange(GetParameterValues(auditEvent).ToArray());
            cmd.Parameters.Add(new MySqlParameter("@id", eventId));
            return cmd;
        }

        private string GetSetForUpdate(AuditEvent auditEvent)
        {
            var sets = new List<string>();
            if (JsonColumnName != null)
            {
                sets.Add($"`{JsonColumnName}` = @value");
            }
            if (CustomColumns != null && CustomColumns.Any())
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    sets.Add($"`{CustomColumns[i].Name}` = @c{i}");
                }
            }
            return string.Join(", ", sets);
        }

        protected MySqlCommand GetSelectCommand(MySqlConnection cnn, MySqlParameter idParam)
        {
            var cmdText = string.Format("SELECT `{0}` FROM `{1}` WHERE `{2}` = @id;", JsonColumnName, TableName, IdColumnName);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.Parameters.Add(idParam);
            return cmd;
        }

        private string GetColumnsForInsert()
        {
            var columns = new List<string>();
            if (JsonColumnName != null)
            {
                columns.Add(JsonColumnName);
            }
            if (CustomColumns != null)
            {
                foreach (var column in CustomColumns)
                {
                    columns.Add(column.Name);
                }
            }
            return string.Join(", ", columns.Select(c => $"`{c}`"));
        }

        private string GetParameterNamesForInsert()
        {
            var names = new List<string>();
            if (JsonColumnName != null)
            {
                names.Add("@value");
            }
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    names.Add($"@c{i}");
                }
            }
            return string.Join(", ", names);
        }

        private List<MySqlParameter> GetParameterValues(AuditEvent auditEvent)
        {
            var parameters = new List<MySqlParameter>();
            if (JsonColumnName != null)
            {
                parameters.Add(new MySqlParameter("@value", auditEvent.ToJson()));
            }
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new MySqlParameter($"@c{i}", CustomColumns[i].Value?.Invoke(auditEvent)));
                }
            }
            return parameters;
        }
    }
}
