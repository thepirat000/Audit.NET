using Audit.Core;
using Audit.NET.PostgreSql;
using Npgsql;
using NpgsqlTypes;
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
        private string _dataType = "JSON";
        private List<CustomColumn> _customColumns = new List<CustomColumn>();

        /// <summary>
        /// Gets or sets the connection string builder.
        /// </summary>
        public Func<AuditEvent, string> ConnectionStringBuilder { get; set; } = _ => "Server=127.0.0.1;Port=5432;User Id=postgres;Password=admin;Database=postgres;";
        /// <summary>
        /// Gets or sets the schema builder.
        /// </summary>
        public Func<AuditEvent, string> SchemaBuilder { get; set; } = _ => null;
        /// <summary>
        /// Gets or sets the table name builder.
        /// </summary>
        public Func<AuditEvent, string> TableNameBuilder { get; set; } = _ => "event";
        /// <summary>
        /// Gets or sets the identifier column name builder.
        /// </summary>
        public Func<AuditEvent, string> IdColumnNameBuilder { get; set; } = _ => "id";
        /// <summary>
        /// Gets or sets the data column name builder.
        /// </summary>
        /// <value>The data column name builder.</value>
        public Func<AuditEvent, string> DataColumnNameBuilder { get; set; } = _ => "data";
        /// <summary>
        /// Gets or sets the function that returns the JSON string to store in the data column. By default it's the result of calling AuditEvent.ToJson().
        /// </summary>
        public Func<AuditEvent, string> DataJsonStringBuilder { get; set; } = ev => ev.ToJson();
        /// <summary>
        /// Gets or sets the last updated date column name builder.
        /// </summary>
        /// <value>The last updated date column name builder.</value>
        public Func<AuditEvent, string> LastUpdatedDateColumnNameBuilder { get; set; } = _ => null;

        /// <summary>
        /// Sets a static connection string
        /// </summary>
        public string ConnectionString
        {
            set { ConnectionStringBuilder = _ => value; }
        }

        /// <summary>
        /// Sets the events Table Name
        /// </summary>
        public string TableName
        {
            set { TableNameBuilder = _ => value; }
        }

        /// <summary>
        /// Set the Column Name that stores the data
        /// </summary>
        public string DataColumnName
        {
            set { DataColumnNameBuilder = _ => value; }
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
        /// Sets the Column Name that stores the Last Updated Date (NULL to ignore)
        /// </summary>
        public string LastUpdatedDateColumnName
        {
            set { LastUpdatedDateColumnNameBuilder = _ => value; }
        }

        /// <summary>
        /// Sets the Column Name that is the primary ley
        /// </summary>
        public string IdColumnName
        {
            set { IdColumnNameBuilder = _ => value; }
        }

        /// <summary>
        /// Sets the Schema Name to use (NULL to ignore)
        /// </summary>
        public string Schema
        {
            set { SchemaBuilder = _ => value; }
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
                ConnectionStringBuilder = pgConfig._connectionStringBuilder;
                DataColumnNameBuilder = pgConfig._dataColumnNameBuilder;
                DataJsonStringBuilder = pgConfig._dataJsonStringBuilder ?? (ev => ev.ToJson());
                _dataType = pgConfig._dataColumnType.ToString();
                IdColumnNameBuilder = pgConfig._idColumnNameBuilder;
                LastUpdatedDateColumnNameBuilder = pgConfig._lastUpdatedColumnNameBuilder;
                SchemaBuilder = pgConfig._schemaBuilder;
                TableNameBuilder = pgConfig._tableNameBuilder;
                CustomColumns = pgConfig._customColumns;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(GetConnectionString(auditEvent)))
            {
                var cmd = GetInsertCommand(cnn, auditEvent);
                var id = cmd.ExecuteScalar();
                return id;
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(GetConnectionString(auditEvent)))
            {
                var cmd = GetInsertCommand(cnn, auditEvent);
                var id = await cmd.ExecuteScalarAsync();
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(GetConnectionString(auditEvent)))
            {
                var cmd = GetReplaceCommand(cnn, auditEvent, eventId);
                cmd.ExecuteNonQuery();
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(GetConnectionString(auditEvent)))
            {
                var cmd = GetReplaceCommand(cnn, auditEvent, eventId);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            using (var cnn = new NpgsqlConnection(GetConnectionString(null)))
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
            using (var cnn = new NpgsqlConnection(GetConnectionString(null)))
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
            using (var cnn = new NpgsqlConnection(GetConnectionString(null)))
            {
                cnn.Open();
                var cmd = cnn.CreateCommand();
                var schema = GetSchema(null);
                var where = string.IsNullOrWhiteSpace(whereExpression) ? "" : $"WHERE {whereExpression}";
                cmd.CommandText = $@"SELECT ""{GetDataColumnName(null)}"" FROM {schema}""{GetTableName(null)}"" {where}";
                var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var data = dr.GetFieldValue<string>(0);
                    yield return Core.Configuration.JsonAdapter.Deserialize<T>(data);
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
                GetIdColumnName(auditEvent));
        }

        private string GetFullTableName(AuditEvent auditEvent)
        {
            return string.Format($@"{GetSchema(auditEvent)}""{GetTableName(auditEvent)}""");
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
                GetIdColumnName(auditEvent));
        }

        private object GetSetForUpdate(AuditEvent auditEvent)
        {
            var ludColName = GetLastUpdatedDateColumnName(auditEvent);
            var ludScript = string.IsNullOrWhiteSpace(ludColName) ? null : $@"""{ludColName}"" = CURRENT_TIMESTAMP";
            var sets = new List<string>();
            if (GetDataColumnName(auditEvent) != null)
            {
                var data = string.IsNullOrWhiteSpace(_dataType) ? "@data" : $"CAST (@data AS {_dataType})";
                sets.Add($@"""{GetDataColumnName(auditEvent)}"" = {data}");
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
            var schema = GetSchema(null);
            cmd.CommandText = $@"select ""{GetDataColumnName(null)}"" from {schema}""{GetTableName(null)}"" WHERE ""{GetIdColumnName(null)}"" = @id";
            var idParameter = cmd.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.Value = eventId;
            cmd.Parameters.Add(idParameter);
            return cmd;
        }

        private string GetColumnsForInsert(AuditEvent auditEvent)
        {
            var columns = new List<string>();
            var dataColumnName = GetDataColumnName(auditEvent);
            if (dataColumnName != null)
            {
                columns.Add(dataColumnName);
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
            if (GetDataColumnName(auditEvent) != null)
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
            if (GetDataColumnName(auditEvent) != null)
            {
                parameters.Add(new NpgsqlParameter("data", DataJsonStringBuilder.Invoke(auditEvent)));
            }
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new NpgsqlParameter($"c{i}", CustomColumns[i].Value.Invoke(auditEvent) ?? DBNull.Value));
                }
            }
            return parameters.ToArray();
        }

        private NpgsqlParameter[] GetParametersForReplace(object eventId, AuditEvent auditEvent)
        {
            var parameters = new List<NpgsqlParameter>();
            if (GetDataColumnName(auditEvent) != null)
            {
                parameters.Add(new NpgsqlParameter("data", DataJsonStringBuilder.Invoke(auditEvent)));
            }
            parameters.Add(new NpgsqlParameter("id", eventId));
            if (CustomColumns != null)
            {
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new NpgsqlParameter($"c{i}", CustomColumns[i].Value.Invoke(auditEvent) ?? DBNull.Value));
                }
            }
            return parameters.ToArray();
        }
        
        private string GetConnectionString(AuditEvent auditEvent)
        {
            return ConnectionStringBuilder?.Invoke(auditEvent);
        }

        private string GetSchema(AuditEvent auditEvent)
        {
            var schema = SchemaBuilder?.Invoke(auditEvent);
            return string.IsNullOrWhiteSpace(schema) ? "" : (@"""" + schema + @""".");
        }

        private string GetTableName(AuditEvent auditEvent)
        {
            return TableNameBuilder?.Invoke(auditEvent);
        }

        private string GetIdColumnName(AuditEvent auditEvent)
        {
            return IdColumnNameBuilder?.Invoke(auditEvent);
        }

        private string GetDataColumnName(AuditEvent auditEvent)
        {
            return DataColumnNameBuilder?.Invoke(auditEvent);
        }

        private string GetLastUpdatedDateColumnName(AuditEvent auditEvent)
        {
            return LastUpdatedDateColumnNameBuilder?.Invoke(auditEvent);
        }
    }

}
