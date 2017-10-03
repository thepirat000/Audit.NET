using Audit.Core;
using Newtonsoft.Json;
using Npgsql;
using System.Collections.Generic;

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

        public override object InsertEvent(AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                cnn.Open();
                var cmd = cnn.CreateCommand();
                var schema = string.IsNullOrWhiteSpace(_schema) ? "" : (_schema + ".");
                var data = string.IsNullOrWhiteSpace(_dataType) ? "@data" : $"CAST (@data AS {_dataType})";
                cmd.CommandText = $@"insert into {schema}""{_tableName}"" (""{_dataColumnName}"") values ({data}) RETURNING id";
                var parameter = cmd.CreateParameter();
                parameter.ParameterName = "data";
                parameter.Value = auditEvent.ToJson();
                cmd.Parameters.Add(parameter);
                var id = cmd.ExecuteScalar();
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            using (var cnn = new NpgsqlConnection(_connectionString))
            {
                cnn.Open();
                var cmd = cnn.CreateCommand();
                var schema = string.IsNullOrWhiteSpace(_schema) ? "" : (_schema + ".");
                var data = string.IsNullOrWhiteSpace(_dataType) ? "@data" : $"CAST (@data AS {_dataType})";
                var ludScript = string.IsNullOrWhiteSpace(_lastUpdatedDateColumnName) ? "" : $@", ""{_lastUpdatedDateColumnName}"" = CURRENT_TIMESTAMP";
                cmd.CommandText = $@"update {schema}""{_tableName}"" SET ""{_dataColumnName}"" = {data}{ludScript} WHERE ""{_idColumnName}"" = @id";
                var dataParameter = cmd.CreateParameter();
                dataParameter.ParameterName = "data";
                dataParameter.Value = auditEvent.ToJson();
                cmd.Parameters.Add(dataParameter);
                var idParameter = cmd.CreateParameter();
                idParameter.ParameterName = "id";
                idParameter.Value = eventId;
                cmd.Parameters.Add(idParameter);
                cmd.ExecuteNonQuery();
            }
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
                var schema = string.IsNullOrWhiteSpace(_schema) ? "" : (_schema + ".");
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
    }
}
