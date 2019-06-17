using Audit.Core;
using Audit.NET.PostgreSql;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.PostgreSql.Providers
{
    /// <summary>
    /// PostgreSQL Server data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: SQL connection string
    /// - TableName: Table name (default is 'event')
    /// - JsonColumnName: Column name where the event data will be stored (default is 'data')
    /// - IdColumnName: Column name with the primary key (default is 'id')
    /// - LastUpdatedDateColumnName: datetime column to update when replacing events (NULL to ignore)
    /// - DataType: Data type to cast when storing the data. (JSON, JSONB). (default is 'JSON')
    /// - CustomColumns: A collection of custom columns to be added when saving the audit event 
    /// </remarks>
    public class PostgreSqlDataProvider : AuditDataProvider
    {
        private string _connectionString;
        private string _schema = null;
        private string _tableName = "event";
        private string _idColumnName = "id";
        private string _dataColumnName = "data";
        private string _lastUpdatedDateColumnName = null;
        private string _dataType = "JSON";
        private List<CustomColumn> _customColumns = new List<CustomColumn>();

        /// <summary>
        /// The connection string
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        /// <summary>
        /// The events Table Name
        /// </summary>
        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        /// <summary>
        /// The Column Name that stores the data
        /// </summary>
        public string DataColumnName
        {
            get { return _dataColumnName; }
            set { _dataColumnName = value; }
        }

        /// <summary>
        /// The column data type to cast when inserting/updating the event data.
        /// </summary>
        public string DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
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

        /// <summary>
        /// A collection of custom columns to be added when saving the audit event 
        /// </summary>
        public List<CustomColumn> CustomColumns
        {
            get => _customColumns;
            set => _customColumns = value;
        }

        public PostgreSqlDataProvider()
        {
        }

        public PostgreSqlDataProvider(Action<Configuration.IPostgreSqlProviderConfigurator> config)
        {
            var pgConfig = new Configuration.PostgreSqlProviderConfigurator();
            if (config != null)
            {
                config.Invoke(pgConfig);
                _connectionString = pgConfig._connectionString;
                _dataColumnName = pgConfig._dataColumnName;
                _dataType = pgConfig._dataColumnType.ToString();
                _idColumnName = pgConfig._idColumnName;
                _lastUpdatedDateColumnName = pgConfig._lastUpdatedColumnName;
                _schema = pgConfig._schema;
                _tableName = pgConfig._tableName;
                _customColumns = pgConfig._customColumns;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                var cmd = GetInsertCommand(cnn, auditEvent);
                var id = cmd.ExecuteScalar();
                return id;
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                var cmd = GetInsertCommand(cnn, auditEvent);
                var id = await cmd.ExecuteScalarAsync();
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                var cmd = GetReplaceCommand(cnn, auditEvent, eventId);
                cmd.ExecuteNonQuery();
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                var cmd = GetReplaceCommand(cnn, auditEvent, eventId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                var cmd = GetSelectCommand(cnn, eventId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        var json = reader.GetString(0);
                        return AuditEvent.FromJson<T>(json);
                    }
                }
            }
            return null;
        }

        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                var cmd = GetSelectCommand(cnn, eventId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        await reader.ReadAsync();
                        var json = reader.GetString(0);
                        return AuditEvent.FromJson<T>(json);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Postgre WHERE expression.
        /// </summary>
        /// <param name="whereExpression">Any valid PostgreSQL where expression for the events table.
        /// The query executed will be SELECT * FROM public.eventb WHERE <paramref name="whereExpression"/>;.
        /// For example: EnumerateEvents("data ? CustomerId") will return the events whose JSON representation includes CustomerId property on its root.
        /// </param>
        /// <remarks>
        /// JSONB data querying: http://schinckel.net/2014/05/25/querying-json-in-postgres/, https://www.postgresql.org/docs/9.5/static/functions-json.html
        /// </remarks>
        public IEnumerable<AuditEvent> EnumerateEvents(string whereExpression)
        {
            return EnumerateEvents<AuditEvent>(whereExpression);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Postgre WHERE expression.
        /// </summary>
        /// <param name="whereExpression">Any valid PostgreSQL where expression for the events table.
        /// The query executed will be SELECT * FROM public.eventb WHERE <paramref name="whereExpression"/>;.
        /// For example: EnumerateEvents("data ? CustomerId") will return the events whose JSON representation includes CustomerId property on its root.
        /// </param>
        /// <remarks>
        /// JSONB data querying: http://schinckel.net/2014/05/25/querying-json-in-postgres/, https://www.postgresql.org/docs/9.5/static/functions-json.html
        /// </remarks>
        public IEnumerable<T> EnumerateEvents<T>(string whereExpression) where T : AuditEvent
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                cnn.Open();
                var cmd = cnn.CreateCommand();
                var schema = GetSchema();
                var where = string.IsNullOrWhiteSpace(whereExpression) ? "" : $"WHERE {whereExpression}";
                cmd.CommandText = $@"SELECT ""{_dataColumnName}"" FROM {schema}""{_tableName}"" {where}";
                var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var data = dr.GetFieldValue<string>(0);
                    yield return JsonConvert.DeserializeObject<T>(data);
                }
            }
        }

        private NpgsqlCommand GetInsertCommand(NpgsqlConnection cnn, AuditEvent auditEvent)
        {
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = GetInsertCommandText(auditEvent);
            cmd.Parameters.AddRange(GetParametersForInsert(auditEvent));
            return cmd;
        }

        private string GetInsertCommandText(AuditEvent auditEvent)
        {
            return string.Format(@"insert into {0} ({1}) values ({2}) RETURNING (""{3}"")",
                GetFullTableName(auditEvent),
                GetColumnsForInsert(auditEvent),
                GetValuesForInsert(auditEvent),
                _idColumnName);
        }

        private string GetFullTableName(AuditEvent auditEvent)
        {
            return string.Format($@"{GetSchema()}""{_tableName}""");
        }

        private NpgsqlCommand GetReplaceCommand(NpgsqlConnection cnn, AuditEvent auditEvent, object eventId)
        {
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = GetReplaceCommandText(auditEvent);
            cmd.Parameters.AddRange(GetParametersForReplace(eventId, auditEvent));
            return cmd;
        }

        private string GetReplaceCommandText(AuditEvent auditEvent)
        {
            return string.Format(@"update {0} SET {1} WHERE ""{2}"" = @id",
                GetFullTableName(auditEvent),
                GetSetForUpdate(auditEvent),
                _idColumnName);
        }

        private object GetSetForUpdate(AuditEvent auditEvent)
        {
            var ludScript = string.IsNullOrWhiteSpace(_lastUpdatedDateColumnName) ? null : $@"""{_lastUpdatedDateColumnName}"" = CURRENT_TIMESTAMP";
            var sets = new List<string>();
            if (_dataColumnName != null)
            {
                var data = string.IsNullOrWhiteSpace(_dataType) ? "@data" : $"CAST (@data AS {_dataType})";
                sets.Add($@"""{_dataColumnName}"" = {data}");
            }
            if (ludScript != null)
            {
                sets.Add(ludScript);
            }
            if (CustomColumns != null && CustomColumns.Any())
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    sets.Add($@"""{CustomColumns[i].Name}"" = @c{i}");
                }
            }
            return string.Join(", ", sets);
        }

        private NpgsqlCommand GetSelectCommand(NpgsqlConnection cnn, object eventId)
        {
            cnn.Open();
            var cmd = cnn.CreateCommand();
            var schema = GetSchema();
            cmd.CommandText = $@"select ""{_dataColumnName}"" from {schema}""{_tableName}"" WHERE ""{_idColumnName}"" = @id";
            var idParameter = cmd.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.Value = eventId;
            cmd.Parameters.Add(idParameter);
            return cmd;
        }

        private string GetSchema()
        {
            return string.IsNullOrWhiteSpace(_schema) ? "" : (@"""" + _schema + @""".");
        }

        private string GetColumnsForInsert(AuditEvent auditEvent)
        {
            var columns = new List<string>();
            if (_dataColumnName != null)
            {
                columns.Add(_dataColumnName);
            }
            if (CustomColumns != null)
            {
                foreach (var column in CustomColumns)
                {
                    columns.Add(column.Name);
                }
            }
            return string.Join(", ", columns.Select(c => $@"""{c}"""));
        }

        private string GetValuesForInsert(AuditEvent auditEvent)
        {
            var values = new List<string>();
            if (_dataColumnName != null)
            {
                var data = string.IsNullOrWhiteSpace(_dataType) ? "@data" : $"CAST (@data AS {_dataType})";
                values.Add(data);
            }
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    values.Add($"@c{i}");
                }
            }
            return string.Join(", ", values);
        }

        private NpgsqlParameter[] GetParametersForInsert(AuditEvent auditEvent)
        {
            var parameters = new List<NpgsqlParameter>();
            if (_dataColumnName != null)
            {
                parameters.Add(new NpgsqlParameter("data", auditEvent.ToJson()));
            }
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new NpgsqlParameter($"c{i}", CustomColumns[i].Value.Invoke(auditEvent)));
                }
            }
            return parameters.ToArray();
        }

        private NpgsqlParameter[] GetParametersForReplace(object eventId, AuditEvent auditEvent)
        {
            var parameters = new List<NpgsqlParameter>();
            if (_dataColumnName != null)
            {
                parameters.Add(new NpgsqlParameter("data", auditEvent.ToJson()));
            }
            parameters.Add(new NpgsqlParameter("id", eventId));
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new NpgsqlParameter($"c{i}", CustomColumns[i].Value.Invoke(auditEvent)));
                }
            }
            return parameters.ToArray();
        }
    }

}
