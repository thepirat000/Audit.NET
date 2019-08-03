using System;
using System.Linq;
using Audit.Core;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
#if NETSTANDARD2_1
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
#if NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.EntityFrameworkCore;
#endif

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

        public SqlDataProvider()
        {
        }

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
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var parameters = GetParametersForInsert(auditEvent);
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetInsertCommandText(auditEvent);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, parameters);
                return result.ToList().FirstOrDefault();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, parameters);
                return result.ToList().FirstOrDefault()?.Id;
#elif NETSTANDARD2_1
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, parameters);
                return result.ToList().FirstOrDefault()?.Id;
#endif
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var parameters = GetParametersForInsert(auditEvent);
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetInsertCommandText(auditEvent);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, parameters);
                return (await result.ToListAsync()).FirstOrDefault();
#elif NETSTANDARD1_3 || NETSTANDARD2_0 
                var result = ctx.FakeIdSet.FromSql(cmdText, parameters);
                return (await result.ToListAsync()).FirstOrDefault()?.Id;
#elif NETSTANDARD2_1
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, parameters);
                return (await result.ToListAsync()).FirstOrDefault()?.Id;
#endif
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var parameters = GetParametersForReplace(eventId, auditEvent);
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetReplaceCommandText(auditEvent);
#if NETSTANDARD2_1
                ctx.Database.ExecuteSqlRaw(cmdText, parameters);
#else
                ctx.Database.ExecuteSqlCommand(cmdText, parameters);
#endif
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var parameters = GetParametersForReplace(eventId, auditEvent);
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetReplaceCommandText(auditEvent);
#if NETSTANDARD1_3
                await ctx.Database.ExecuteSqlCommandAsync(cmdText, default(CancellationToken), parameters);
#elif NETSTANDARD2_1
                await ctx.Database.ExecuteSqlRawAsync(cmdText, default(CancellationToken), parameters);
#else
                await ctx.Database.ExecuteSqlCommandAsync(cmdText, parameters);
#endif
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            if (JsonColumnNameBuilder == null)
            {
                return null;
            }
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(null)))
            {
                var cmdText = GetSelectCommandText(null);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, new SqlParameter("@eventId", eventId));
                var json = result.FirstOrDefault();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, new SqlParameter("@eventId", eventId));
                var json = result.FirstOrDefault()?.Id;
#elif NETSTANDARD2_1
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, new SqlParameter("@eventId", eventId));
                var json = result.FirstOrDefault()?.Id;
#endif
                if (json != null)
                {
                    return AuditEvent.FromJson<T>(json);
                }
            }
            return null;
        }

        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            if (JsonColumnNameBuilder == null)
            {
                return null;
            }
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(null)))
            {
                var cmdText = GetSelectCommandText(null);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, new SqlParameter("@eventId", eventId));
                var json = await result.FirstOrDefaultAsync();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, new SqlParameter("@eventId", eventId));
                var json = (await result.FirstOrDefaultAsync())?.Id;
#elif NETSTANDARD2_1
                var result = ctx.FakeIdSet.FromSqlRaw(cmdText, new SqlParameter("@eventId", eventId));
                var json = (await result.FirstOrDefaultAsync())?.Id;
#endif
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
                    columns.Add(column.Name);
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
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    values.Add($"@c{i}");
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
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new SqlParameter($"@c{i}", CustomColumns[i].Value.Invoke(auditEvent)));
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
                for (int i = 0; i < CustomColumns.Count; i++)
                {
                    parameters.Add(new SqlParameter($"@c{i}", CustomColumns[i].Value.Invoke(auditEvent)));
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
            if (CustomColumns != null && CustomColumns.Any())
            {
                for(int i = 0; i < CustomColumns.Count; i++)
                {
                    sets.Add($"[{CustomColumns[i].Name}] = @c{i}");
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
    }
}
