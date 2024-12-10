using System;
using System.Linq;
using Audit.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Audit.SqlServer.Providers
{
    /// <summary>
    /// SQL Server data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ConnectionString: Database connection string
    /// - DbConnection: Alternatively to ConnectionString, you can pass a DbConnection object
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
        /// The Database connection string to use.
        /// </summary>
        public Setting<string> ConnectionString { get; set; }

        /// <summary>
        /// The Db Connection to use. Alternative to ConnectionString.
        /// </summary>
        public Setting<DbConnection> DbConnection { get; set; }

        /// <summary>
        /// The Db Context instance to use. Alternative to ConnectionString and DbConnection.
        /// </summary>
        public Setting<DbContext> DbContext { get; set; }

        /// <summary>
        /// The SQL events Table Name 
        /// </summary>
        public Setting<string> TableName { get; set; } = "Event";

        /// <summary>
        /// The Column Name that stores the JSON
        /// </summary>
        public Setting<string> JsonColumnName { get; set; }

        /// <summary>
        /// The Column Name that stores the Last Updated Date (NULL to ignore)
        /// </summary>
        public Setting<string> LastUpdatedDateColumnName { get; set; }

        /// <summary>
        /// The Column Name that is the primary ley
        /// </summary>
        public Setting<string> IdColumnName { get; set; }

        /// <summary>
        /// The Schema Name to use (NULL to ignore)
        /// </summary>
        public Setting<string> Schema { get; set; }
        
        /// <summary>
        /// A collection of custom columns to be added when saving the audit event 
        /// </summary>
        public List<CustomColumn> CustomColumns { get; set; } = new List<CustomColumn>();

        /// <summary>
        /// The DbContext options builder, to provide custom database options for the Default Audit DbContext. This setting is ignored if a DbContext instance is provided.
        /// </summary>
        [CLSCompliant(false)]
        public Setting<DbContextOptions> DbContextOptions { get; set; }

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
                ConnectionString = sqlConfig._connectionString;
                DbConnection = sqlConfig._dbConnection;
                DbContext = sqlConfig._dbContext;
                IdColumnName = sqlConfig._idColumnName;
                JsonColumnName = sqlConfig._jsonColumnName;
                LastUpdatedDateColumnName = sqlConfig._lastUpdatedColumnName;
                Schema = sqlConfig._schema;
                TableName = sqlConfig._tableName;
                CustomColumns = sqlConfig._customColumns;
                DbContextOptions = sqlConfig._dbContextOptions;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            object[] parameters = GetParametersForInsert(auditEvent);
            using var ctx = CreateContext(auditEvent);
            var cmdText = GetInsertCommandText(auditEvent);
#if NET7_0_OR_GREATER
            var result = ctx.Database.SqlQueryRaw<string>(cmdText, parameters);
            var id = result.ToList().FirstOrDefault();
#else
            var result = ctx.Set<AuditEventValueModel>().FromSqlRaw(cmdText, parameters);
            var id = result.ToList().FirstOrDefault()?.Value;
#endif
            return id;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var parameters = GetParametersForInsert(auditEvent);
            await using var ctx = CreateContext(auditEvent);
            var cmdText = GetInsertCommandText(auditEvent);
#if NET7_0_OR_GREATER
            var result = ctx.Database.SqlQueryRaw<string>(cmdText, parameters);
            var id = (await result.ToListAsync(cancellationToken)).FirstOrDefault();
#else
            var result = ctx.Set<AuditEventValueModel>().FromSqlRaw(cmdText, parameters);
            var id = (await result.ToListAsync(cancellationToken)).FirstOrDefault()?.Value;
#endif
            return id;
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var parameters = GetParametersForReplace(eventId, auditEvent);
            using var ctx = CreateContext(auditEvent);
            var cmdText = GetReplaceCommandText(auditEvent);
            ctx.Database.ExecuteSqlRaw(cmdText, parameters);
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var parameters = GetParametersForReplace(eventId, auditEvent);
            await using var ctx = CreateContext(auditEvent);
            var cmdText = GetReplaceCommandText(auditEvent);
            await ctx.Database.ExecuteSqlRawAsync(cmdText, parameters, cancellationToken);
        }

        public override T GetEvent<T>(object eventId)
        {
            if (JsonColumnName.GetDefault() == null)
            {
                return null;
            }

            using var ctx = CreateContext(null);
            var cmdText = GetSelectCommandText(null);
#if NET7_0_OR_GREATER
            var result = ctx.Database.SqlQueryRaw<string>(cmdText, new SqlParameter("@eventId", eventId));
            var json = result.FirstOrDefault();
#else
            var result = ctx.Set<AuditEventValueModel>().FromSqlRaw(cmdText, new SqlParameter("@eventId", eventId));
            var json = result.FirstOrDefault()?.Value;
#endif

            if (json != null)
            {
                return AuditEvent.FromJson<T>(json);
            }

            return null;
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {   
            if (JsonColumnName.GetDefault() == null)
            {
                return null;
            }

            await using var ctx = CreateContext(null);
            var cmdText = GetSelectCommandText(null);
#if NET7_0_OR_GREATER
            var result = ctx.Database.SqlQueryRaw<string>(cmdText, new SqlParameter("@eventId", eventId));
            var json = await result.FirstOrDefaultAsync(cancellationToken);
#else
            var result = ctx.Set<AuditEventValueModel>().FromSqlRaw(cmdText, new SqlParameter("@eventId", eventId));
            var json = (await result.FirstOrDefaultAsync(cancellationToken))?.Value;
#endif


            if (json != null)
            {
                return AuditEvent.FromJson<T>(json);
            }

            return null;
        }

        protected internal string GetFullTableName(AuditEvent auditEvent)
        {
            var schema = Schema.GetValue(auditEvent);
            var table = TableName.GetValue(auditEvent);

            return schema != null ? $"[{schema}].[{table}]" : $"[{table}]";
        }

        protected string GetInsertCommandText(AuditEvent auditEvent)
        {
            return string.Format("INSERT INTO {0} ({1}) OUTPUT CONVERT(NVARCHAR(MAX), INSERTED.[{2}]) AS [Value] VALUES ({3})", 
                GetFullTableName(auditEvent),
                GetColumnsForInsert(auditEvent), 
                IdColumnName.GetValue(auditEvent),
                GetValuesForInsert(auditEvent)); 
        }

        private string GetColumnsForInsert(AuditEvent auditEvent)
        {
            var columns = new List<string>();
            var jsonColumnName = JsonColumnName.GetValue(auditEvent);
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
            if (JsonColumnName.GetValue(auditEvent) != null)
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

        protected SqlParameter[] GetParametersForInsert(AuditEvent auditEvent)
        {
            var parameters = new List<SqlParameter>();
            if (JsonColumnName.GetValue(auditEvent) != null)
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

        protected SqlParameter[] GetParametersForReplace(object eventId, AuditEvent auditEvent)
        {
            var parameters = new List<SqlParameter>();
            if (JsonColumnName.GetValue(auditEvent) != null)
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

        protected string GetReplaceCommandText(AuditEvent auditEvent)
        {
            var cmdText = string.Format("UPDATE {0} SET {1} WHERE [{2}] = @eventId",
                GetFullTableName(auditEvent), 
                GetSetForUpdate(auditEvent), 
                IdColumnName.GetValue(auditEvent));
            return cmdText;
        }

        private string GetSetForUpdate(AuditEvent auditEvent)
        {
            var jsonColumnName = JsonColumnName.GetValue(auditEvent);
            var ludColumn = LastUpdatedDateColumnName.GetValue(auditEvent);
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

        protected string GetSelectCommandText(AuditEvent auditEvent)
        {
            var cmdText = string.Format("SELECT [{0}] As [Value] FROM {1} WHERE [{2}] = @eventId",
                JsonColumnName.GetValue(auditEvent),
                GetFullTableName(auditEvent), 
                IdColumnName.GetValue(auditEvent));
            return cmdText;
        }

        protected virtual DbContext CreateContext(AuditEvent auditEvent)
        {
            // Use the DbContext if provided
            var dbContext = DbContext.GetValue(auditEvent);
            
            if (dbContext != null)
            {
                return dbContext;
            }

            // Use the connection string or the db connection
            var ctxOptions = DbContextOptions.GetValue(auditEvent);
            var dbConnection = DbConnection.GetValue(auditEvent);

            if (dbConnection != null)
            {
                return ctxOptions != null ? new DefaultAuditDbContext(dbConnection, ctxOptions) : new DefaultAuditDbContext(dbConnection);
            }

            var connectionString = ConnectionString.GetValue(auditEvent);

            return ctxOptions != null ? new DefaultAuditDbContext(connectionString, ctxOptions) : new DefaultAuditDbContext(connectionString);
        }
    }
}
