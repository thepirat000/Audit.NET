using Audit.Core;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    /// - DataJsonColumnName: Column name where the event data will be stored (default is NULL to ignore)
    /// - DataJsonType: Data type of the JsonColumn. (JSON, JSONB). (default is 'JSON')
    /// - IdColumnName: Column name with the primary key (default is 'id')
    /// - LastUpdatedDateColumnName: datetime column to update when replacing events (NULL to ignore)
    /// - CustomColumns: A collection of custom columns to be added when saving the audit event 
    /// </remarks>
    public class PostgreSqlDataProvider : AuditDataProvider
    {
        /// <summary>
        /// The connection string
        /// </summary>
        public Setting<string> ConnectionString { get; set; }
        /// <summary>
        /// Gets or sets the schema builder.
        /// </summary>
        public Setting<string> Schema { get; set; }
        /// <summary>
        /// Gets or sets the table name builder.
        /// </summary>
        public Setting<string> TableName { get; set; } = "event";
        /// <summary>
        /// Gets or sets the identifier column name builder.
        /// </summary>
        public Setting<string> IdColumnName { get; set; } = "id";
        /// <summary>
        /// Gets or sets the JSON data column name to use for storing the event data. Optional. If not set, the JSON representation of the audit event will not be stored.
        /// </summary>
        /// <value>The data column name builder.</value>
        public Setting<string> DataJsonColumnName { get; set; } = new();
        /// <summary>
        /// Gets or sets the function that returns the JSON string to store in the data column. By default, it's the result of calling AuditEvent.ToJson().
        /// </summary>
        public Func<AuditEvent, string> DataJsonStringBuilder { get; set; } = ev => ev.ToJson();
        /// <summary>
        /// Gets or sets the last updated date column name builder.
        /// </summary>
        /// <value>The last updated date column name builder.</value>
        public Setting<string> LastUpdatedDateColumnName { get; set; }

        /// <summary>
        /// The column data type for the JSON column, used to cast when inserting/updating the event data. Default is 'JSON'.
        /// </summary>
        public string DataJsonType { get; set; } = "JSON";

        /// <summary>
        /// A collection of custom columns to be added when saving the audit event 
        /// </summary>
        public List<CustomColumn> CustomColumns { get; set; } = [];

        public PostgreSqlDataProvider()
        {
        }

        public PostgreSqlDataProvider(Action<Configuration.IPostgreSqlProviderConfigurator> config)
        {
            var pgConfig = new Configuration.PostgreSqlProviderConfigurator();
            if (config != null)
            {
                config.Invoke(pgConfig);
                ConnectionString = pgConfig._connectionString;
                DataJsonColumnName = pgConfig._dataColumnName;
                DataJsonStringBuilder = pgConfig._dataJsonStringBuilder ?? (ev => ev.ToJson());
                DataJsonType = pgConfig._dataColumnType.ToString();
                IdColumnName = pgConfig._idColumnName;
                LastUpdatedDateColumnName = pgConfig._lastUpdatedColumnName;
                Schema = pgConfig._schema;
                TableName = pgConfig._tableName;
                CustomColumns = pgConfig._customColumns;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            using var cnn = GetDbConnection(auditEvent);
            var cmd = GetInsertCommand(cnn, auditEvent);
            var id = cmd.ExecuteScalar();
            return id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            await using var cnn = GetDbConnection(auditEvent);
            var cmd = GetInsertCommand(cnn, auditEvent);
            var id = await cmd.ExecuteScalarAsync(cancellationToken);
            return id;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            using var cnn = GetDbConnection(auditEvent);
            var cmd = GetReplaceCommand(cnn, auditEvent, eventId);
            cmd.ExecuteNonQuery();
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            await using var cnn = GetDbConnection(auditEvent);
            var cmd = GetReplaceCommand(cnn, auditEvent, eventId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Creates and returns a new NpgsqlConnection instance configured for the specified audit event.
        /// </summary>
        /// <param name="auditEvent">The audit event for which to generate a database connection. Cannot be null.</param>
        /// <returns>A new NpgsqlConnection initialized with a connection string appropriate for the specified audit event.</returns>
        public virtual NpgsqlConnection GetDbConnection(AuditEvent auditEvent)
        {
            return new NpgsqlConnection(GetConnectionString(auditEvent));
        }

        public override T GetEvent<T>(object eventId)
        {
            if (GetDataColumnName(null) == null)
            {
                return null;
            }
            using var cnn = new NpgsqlConnection(GetConnectionString(null));
            var cmd = GetSelectCommand(cnn, eventId);
            using var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                var json = reader.GetString(0);
                return AuditEvent.FromJson<T>(json);
            }

            return null;
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            if (GetDataColumnName(null) == null)
            {
                return null;
            }
            await using var cnn = new NpgsqlConnection(GetConnectionString(null));
            var cmd = GetSelectCommand(cnn, eventId);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (reader.HasRows)
            {
                await reader.ReadAsync(cancellationToken);
                var json = reader.GetString(0);
                return AuditEvent.FromJson<T>(json);
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
            if (GetDataColumnName(null) == null)
            {
                return [];
            }
            var where = string.IsNullOrWhiteSpace(whereExpression) ? "" : $"WHERE {whereExpression}";
            var sql = $@"SELECT ""{GetDataColumnName(null)}"" FROM {GetFullTableName(null)} {where}";
            return EnumerateEventsByFullSql<T>(sql);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Postgres WHERE, ORDER BY and LIMIT expression.
        /// </summary>
        /// <param name="whereExpression">Any valid PostgreSQL where expression for the events table.
        /// The query executed will be SELECT * FROM public.eventb WHERE <paramref name="whereExpression"/>;.
        /// For example: EnumerateEvents("data ? CustomerId") will return the events whose JSON representation includes CustomerId property on its root.
        /// </param>
        /// <param name="orderByExpression">Any valid PostgreSQL order by expression for the events table.</param>
        /// <param name="limitExpression">Any valid PostgreSQL limit expression for the events table.</param>
        /// <remarks>
        /// JSONB data querying: http://schinckel.net/2014/05/25/querying-json-in-postgres/, https://www.postgresql.org/docs/9.5/static/functions-json.html
        /// </remarks>
        public IEnumerable<T> EnumerateEvents<T>(string whereExpression, string orderByExpression, string limitExpression) where T : AuditEvent
        {
            if (GetDataColumnName(null) == null)
            {
                return [];
            }
            var selectExpression = $@"""{GetDataColumnName(null)}"" FROM {GetFullTableName(null)}";
            var where = string.IsNullOrWhiteSpace(whereExpression) ? "" : $" WHERE {whereExpression}";
            var orderBy = string.IsNullOrWhiteSpace(orderByExpression) ? "" : $" ORDER BY {orderByExpression}";
            var limit = string.IsNullOrWhiteSpace(limitExpression) ? "" : $" LIMIT {limitExpression}";
            var finalSql = $@"SELECT {selectExpression} {where} {orderBy} {limit}";
            return EnumerateEventsByFullSql<T>(finalSql);
        }

        /// <summary>
        /// Returns an enumeration of audit events for the given Postgres SELECT expression.
        /// </summary>
        /// <param name="fullSql">The string with the SELECT expression</param>
        protected IEnumerable<T> EnumerateEventsByFullSql<T>(string fullSql) where T : AuditEvent
        {
            using var cnn = new NpgsqlConnection(GetConnectionString(null));
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = fullSql;
            var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                if (!dr.IsDBNull(0))
                {
                    var data = dr.GetFieldValue<string>(0);
                    yield return Core.Configuration.JsonAdapter.Deserialize<T>(data);
                }
            }
        }

        protected NpgsqlCommand GetInsertCommand(NpgsqlConnection cnn, AuditEvent auditEvent)
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

        protected NpgsqlCommand GetReplaceCommand(NpgsqlConnection cnn, AuditEvent auditEvent, object eventId)
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
                sets.Add($@"""{GetDataColumnName(auditEvent)}"" = {GetDataColumnValue()}");
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

        protected NpgsqlCommand GetSelectCommand(NpgsqlConnection cnn, object eventId)
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
                values.Add(GetDataColumnValue());
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

        private string GetDataColumnValue()
        {
            return string.IsNullOrWhiteSpace(DataJsonType) || DataJsonType == Configuration.DataType.String.ToString() 
                ? "@data" 
                : $"CAST (@data AS {DataJsonType})";
        }

        protected string GetConnectionString(AuditEvent auditEvent)
        {
            return ConnectionString.GetValue(auditEvent);
        }

        protected string GetSchema(AuditEvent auditEvent)
        {
            var schema = Schema.GetValue(auditEvent);
            return string.IsNullOrWhiteSpace(schema) ? "" : (@"""" + schema + @""".");
        }

        protected string GetTableName(AuditEvent auditEvent)
        {
            return TableName.GetValue(auditEvent);
        }

        protected string GetFullTableName(AuditEvent auditEvent)
        {
            return string.Format($@"{GetSchema(auditEvent)}""{GetTableName(auditEvent)}""");
        }

        protected string GetIdColumnName(AuditEvent auditEvent)
        {
            return IdColumnName.GetValue(auditEvent);
        }

        protected string GetDataColumnName(AuditEvent auditEvent)
        {
            return DataJsonColumnName.GetValue(auditEvent);
        }

        protected string GetLastUpdatedDateColumnName(AuditEvent auditEvent)
        {
            return LastUpdatedDateColumnName.GetValue(auditEvent);
        }
    }
}
