using System;
using System.Linq;
using Audit.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Audit.SqlServer.Providers
{

    /// <summary>
    /// SQL Server data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: SQL connection string
    /// - TableName: Table name (default is 'Event')
    /// - JsonColumnName: Column name where the JSON will be stored (default is 'Data')
    /// - IdColumnName: Column name with the primary key (default is 'EventId')
    /// - LastUpdatedDateColumnName: datetime column to update when replacing events (NULL to ignore)
    /// - Schema: The Schema Name to use 
    /// - CustomColumns: A collection of custom columns to be added when saving the audit event 
    /// </remarks>
    public class SqlDataProvider : AuditDataProvider
    {
        /// <summary>
        /// The SQL connection string builder
        /// </summary>
        public Func<AuditEvent, string> ConnectionStringBuilder { get; set; }
        /// <summary>
        /// The SQL connection string
        /// </summary>
        public string ConnectionString { set { ConnectionStringBuilder = _ => value; } }
        /// <summary>
        /// The SQL events Table Name builder
        /// </summary>
        public Func<AuditEvent, string> TableNameBuilder { get; set; } = ev => "Event";
        /// <summary>
        /// The SQL events Table Name 
        /// </summary>
        public string TableName { set { TableNameBuilder = _ => value; } }
        /// <summary>
        /// The Column Name that stores the JSON
        /// </summary>
        public Func<AuditEvent, string> JsonColumnNameBuilder { get; set; }
        /// <summary>
        /// The Column Name that stores the JSON
        /// </summary>
        public string JsonColumnName { set { JsonColumnNameBuilder = _ => value; } }
        /// <summary>
        /// The Column Name that stores the Last Updated Date (NULL to ignore)
        /// </summary>
        public Func<AuditEvent, string> LastUpdatedDateColumnNameBuilder { get; set; } = null;
        /// <summary>
        /// The Column Name that stores the Last Updated Date (NULL to ignore)
        /// </summary>
        public string LastUpdatedDateColumnName { set { LastUpdatedDateColumnNameBuilder = _ => value; } }
        /// <summary>
        /// The Column Name that is the primary ley
        /// </summary>
        public Func<AuditEvent, string> IdColumnNameBuilder { get; set; } = ev => "EventId";
        /// <summary>
        /// The Column Name that is the primary ley
        /// </summary>
        public string IdColumnName { set { IdColumnNameBuilder = _ => value; } }
        /// <summary>
        /// The Schema Name to use (NULL to ignore)
        /// </summary>
        public Func<AuditEvent, string> SchemaBuilder { get; set; } = null;
        /// <summary>
        /// The Schema Name to use (NULL to ignore)
        /// </summary>
        public string Schema { set { SchemaBuilder = _ => value; } }
        /// <summary>
        /// A collection of custom columns to be added when saving the audit event 
        /// </summary>
        public List<CustomColumn> CustomColumns { get; set; } = new List<CustomColumn>();
        /// <summary>
        /// The DbContext options builder, to provide custom database options for the DbContext
        /// </summary>
        [CLSCompliant(false)]
        public Func<AuditEvent, DbContextOptions> DbContextOptionsBuilder { get; set; } = null;

        public SqlDataProvider()
        {
        }

        [CLSCompliant(false)]
        public SqlDataProvider(Action<Configuration.ISqlServerProviderConfigurator> config)
        {
            var sqlConfig = new Configuration.SqlServerProviderConfigurator();
            if (config != null)
            {
                config.Invoke(sqlConfig);
                ConnectionStringBuilder = sqlConfig._connectionStringBuilder;
                IdColumnNameBuilder = sqlConfig._idColumnNameBuilder;
                JsonColumnNameBuilder = sqlConfig._jsonColumnNameBuilder;
                LastUpdatedDateColumnNameBuilder = sqlConfig._lastUpdatedColumnNameBuilder;
                SchemaBuilder = sqlConfig._schemaBuilder;
                TableNameBuilder = sqlConfig._tableNameBuilder;
                CustomColumns = sqlConfig._customColumns;
                DbContextOptionsBuilder = sqlConfig._dbContextOptionsBuilder;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var parameters = GetParametersForInsert(auditEvent);
            using (var ctx = CreateContext(auditEvent))
            {
                var cmdText = GetInsertCommandText(auditEvent);
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, parameters);
                return result.ToList().FirstOrDefault()?.Id;
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var parameters = GetParametersForInsert(auditEvent);
            using (var ctx = CreateContext(auditEvent))
            {
                var cmdText = GetInsertCommandText(auditEvent);
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, parameters);
                return (await result.ToListAsync(cancellationToken)).FirstOrDefault()?.Id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var parameters = GetParametersForReplace(eventId, auditEvent);
            using (var ctx = CreateContext(auditEvent))
            {
                var cmdText = GetReplaceCommandText(auditEvent);
                ctx.Database.ExecuteSqlRaw(cmdText, parameters);
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var parameters = GetParametersForReplace(eventId, auditEvent);
            using (var ctx = CreateContext(auditEvent))
            {
                var cmdText = GetReplaceCommandText(auditEvent);
                await ctx.Database.ExecuteSqlRawAsync(cmdText, parameters, cancellationToken);
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            if (JsonColumnNameBuilder == null)
            {
                return null;
            }
            using (var ctx = CreateContext(null))
            {
                var cmdText = GetSelectCommandText(null);
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, new SqlParameter("@eventId", eventId));
                var json = result.FirstOrDefault()?.Id;

                if (json != null)
                {
                    return AuditEvent.FromJson<T>(json);
                }
            }
            return null;
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {   
            if (JsonColumnNameBuilder == null)
            {
                return null;
            }
            using (var ctx = CreateContext(null))
            {
                var cmdText = GetSelectCommandText(null);
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, new SqlParameter("@eventId", eventId));
                var json = (await result.FirstOrDefaultAsync(cancellationToken))?.Id;

                if (json != null)
                {
                    return AuditEvent.FromJson<T>(json);
                }
            }
            return null;
        }

        private string GetFullTableName(AuditEvent auditEvent)
        {
            return SchemaBuilder != null 
                ? string.Format("[{0}].[{1}]", SchemaBuilder.Invoke(auditEvent), TableNameBuilder.Invoke(auditEvent))
                : string.Format("[{0}]", TableNameBuilder.Invoke(auditEvent));
        }

        private string GetInsertCommandText(AuditEvent auditEvent)
        {
            return string.Format("INSERT INTO {0} ({1}) OUTPUT CONVERT(NVARCHAR(MAX), INSERTED.[{2}]) AS [Id] VALUES ({3})", 
                GetFullTableName(auditEvent),
                GetColumnsForInsert(auditEvent), 
                IdColumnNameBuilder.Invoke(auditEvent),
                GetValuesForInsert(auditEvent)); 
        }

        private string GetColumnsForInsert(AuditEvent auditEvent)
        {
            var columns = new List<string>();
            var jsonColumnName = JsonColumnNameBuilder?.Invoke(auditEvent);
            if (jsonColumnName != null)
            {
                columns.Add(jsonColumnName);
            }
            if (CustomColumns != null)
            {
                foreach (var column in CustomColumns)
                {
                    if (column.Guard == null || column.Guard.Invoke(auditEvent))
                    {
                        columns.Add(column.Name);
                    }
                }
            }
            return string.Join(", ", columns.Select(c => $"[{c}]"));
        }

        private string GetValuesForInsert(AuditEvent auditEvent)
        {
            var values = new List<string>();
            if (JsonColumnNameBuilder != null)
            {
                values.Add("@json");
            }
            if (CustomColumns != null)
            {
                int i = 0;
                foreach (var column in CustomColumns)
                {
                    if (column.Guard == null || column.Guard.Invoke(auditEvent))
                    {
                        values.Add($"@c{i}");
                        i++;
                    }
                }
            }
            return string.Join(", ", values);
        }

        private SqlParameter[] GetParametersForInsert(AuditEvent auditEvent)
        {
            var parameters = new List<SqlParameter>();
            if (JsonColumnNameBuilder != null)
            {
                parameters.Add(new SqlParameter("@json", auditEvent.ToJson()));
            }
            if (CustomColumns != null)
            {
                int i = 0;
                foreach (var column in CustomColumns)
                {
                    if (column.Guard == null || column.Guard.Invoke(auditEvent))
                    {
                        parameters.Add(new SqlParameter($"@c{i}", column.Value.Invoke(auditEvent) ?? DBNull.Value));
                        i++;
                    }
                }
            }
            return parameters.ToArray();
        }

        private SqlParameter[] GetParametersForReplace(object eventId, AuditEvent auditEvent)
        {
            var parameters = new List<SqlParameter>();
            if (JsonColumnNameBuilder != null)
            {
                parameters.Add(new SqlParameter("@json", auditEvent.ToJson()));
            }
            parameters.Add(new SqlParameter("@eventId", eventId));
            if (CustomColumns != null)
            {
                int i = 0;
                foreach (var column in CustomColumns)
                {
                    if (column.Guard == null || column.Guard.Invoke(auditEvent))
                    {
                        parameters.Add(new SqlParameter($"@c{i}", column.Value.Invoke(auditEvent) ?? DBNull.Value));
                        i++;
                    }
                }
            }
            return parameters.ToArray();
        }

        private string GetReplaceCommandText(AuditEvent auditEvent)
        {
            var cmdText = string.Format("UPDATE {0} SET {1} WHERE [{2}] = @eventId",
                GetFullTableName(auditEvent), 
                GetSetForUpdate(auditEvent), 
                IdColumnNameBuilder.Invoke(auditEvent));
            return cmdText;
        }

        private string GetSetForUpdate(AuditEvent auditEvent)
        {
            var jsonColumnName = JsonColumnNameBuilder?.Invoke(auditEvent);
            var ludColumn = LastUpdatedDateColumnNameBuilder?.Invoke(auditEvent);
            var sets = new List<string>();
            if (jsonColumnName != null)
            {
                sets.Add($"[{jsonColumnName}] = @json");
            }
            if (ludColumn != null)
            {
                sets.Add($"[{ludColumn}] = GETUTCDATE()");
            }
            if (CustomColumns != null)
            {
                int i = 0;
                foreach (var column in CustomColumns)
                {
                    if (column.Guard == null || column.Guard.Invoke(auditEvent))
                    {
                        sets.Add($"[{column.Name}] = @c{i}");
                        i++;
                    }
                }
            }
            return string.Join(", ", sets);
        }

        private string GetSelectCommandText(AuditEvent auditEvent)
        {
            var cmdText = string.Format("SELECT [{0}] As [Id] FROM {1} WHERE [{2}] = @eventId",
                JsonColumnNameBuilder.Invoke(auditEvent),
                GetFullTableName(auditEvent), 
                IdColumnNameBuilder.Invoke(auditEvent));
            return cmdText;
        }

        private AuditContext CreateContext(AuditEvent auditEvent)
        {
            if (DbContextOptionsBuilder != null)
            {
                return new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent), DbContextOptionsBuilder.Invoke(auditEvent));
            }
            else
            {
                return new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent));
            }
        }

    }
}
