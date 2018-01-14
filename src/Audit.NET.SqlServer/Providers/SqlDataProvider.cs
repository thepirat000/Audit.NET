using System;
using System.Linq;
using Audit.Core;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Threading;
#if NETSTANDARD1_3 || NETSTANDARD2_0
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
        public Func<AuditEvent, string> JsonColumnNameBuilder { get; set; } = ev => "Data";
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

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var json = new SqlParameter("json", auditEvent.ToJson());
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetInsertCommandText(auditEvent);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, json);
                return result.FirstOrDefault();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, json);
                return result.FirstOrDefault().Id;
#endif
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var json = new SqlParameter("json", auditEvent.ToJson());
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetInsertCommandText(auditEvent);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, json);
                return await result.FirstOrDefaultAsync();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, json);
                return (await result.FirstOrDefaultAsync()).Id;
#endif
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var json = auditEvent.ToJson();
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetReplaceCommandText(auditEvent);
                ctx.Database.ExecuteSqlCommand(cmdText, new SqlParameter("@json", json), new SqlParameter("@eventId", eventId));
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var json = auditEvent.ToJson();
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(auditEvent)))
            {
                var cmdText = GetReplaceCommandText(auditEvent);
                await ctx.Database.ExecuteSqlCommandAsync(cmdText, default(CancellationToken), new SqlParameter("@json", json), new SqlParameter("@eventId", eventId));
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(null)))
            {
                var cmdText = GetSelectCommandText(null);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, new SqlParameter("eventId", eventId));
                var json = result.FirstOrDefault();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, new SqlParameter("eventId", eventId));
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
            using (var ctx = new AuditContext(ConnectionStringBuilder?.Invoke(null)))
            {
                var cmdText = GetSelectCommandText(null);
#if NET45
                var result = ctx.Database.SqlQuery<string>(cmdText, new SqlParameter("eventId", eventId));
                var json = await result.FirstOrDefaultAsync();
#elif NETSTANDARD1_3 || NETSTANDARD2_0
                var result = ctx.FakeIdSet.FromSql(cmdText, new SqlParameter("eventId", eventId));
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
            return string.Format("INSERT INTO {0} ([{1}]) OUTPUT CONVERT(NVARCHAR(MAX), INSERTED.[{2}]) AS [Id] VALUES (@json)", 
                GetFullTableName(auditEvent), 
                JsonColumnNameBuilder.Invoke(auditEvent), 
                IdColumnNameBuilder.Invoke(auditEvent)); 
        }

        private string GetReplaceCommandText(AuditEvent auditEvent)
        {
            var ludScript = LastUpdatedDateColumnNameBuilder != null ? string.Format(", [{0}] = GETUTCDATE()", LastUpdatedDateColumnNameBuilder.Invoke(auditEvent)) : string.Empty;
            var cmdText = string.Format("UPDATE {0} SET [{1}] = @json{2} WHERE [{3}] = @eventId",
                GetFullTableName(auditEvent), JsonColumnNameBuilder.Invoke(auditEvent), ludScript, IdColumnNameBuilder.Invoke(auditEvent));
            return cmdText;
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
