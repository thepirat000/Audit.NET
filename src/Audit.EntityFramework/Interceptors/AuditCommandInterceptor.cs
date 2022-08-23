#if EF_CORE_3 || EF_CORE_5 || EF_CORE_6
using Audit.Core;
using Audit.Core.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.Interceptors
{
    /// <summary>
    /// <para>
    ///     Allows interception of commands sent to a relational database for auditing purposes.
    /// </para>
    /// <para>
    ///     Use <see cref="DbContextOptionsBuilder.AddInterceptors(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[])" />
    ///     to register application interceptors.
    /// </para>
    /// </summary>
    public class AuditCommandInterceptor : DbCommandInterceptor
    {
        /// <summary>
        /// Boolean value to indicate whether to log the command parameter values. By default (when null) it will depend on EnableSensitiveDataLogging setting on the DbContext.
        /// </summary>
        public bool? LogParameterValues { get; set; }
        /// <summary>
        /// Boolean value to indicate whether to exclude the events handled by ReaderExecuting. Default is false to include the ReaderExecuting events.
        /// </summary>
        public bool ExcludeReaderEvents { get; set; }
        
        /// <summary>
        /// Boolean value to indicate whether to exclude the events handled by NonQueryExecuting. Default is false to include the NonQueryExecuting events.
        /// </summary>
        public bool ExcludeNonQueryEvents { get; set; }
        
        /// <summary>
        /// Boolean value to indicate whether to exclude the events handled by ScalarExecuting. Default is false to include the ScalarExecuting events.
        /// </summary>
        public bool ExcludeScalarEvents { get; set; }

        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is the execute method name). 
        /// Can contain the following placeholders: 
        /// - {database}: Replaced with the database name 
        /// - {method}: Replaced with the execute method name (ExecuteReader, ExecuteNonQuery or ExecuteScalar) 
        /// </summary>
        public string AuditEventType { get; set; } = "{method}";

        /// <summary>
        /// Boolean value to indicate whether to include the query results to the audit output. Default is false.
        /// </summary>
        public bool IncludeReaderResults { get; set; }
        
        private readonly DbContextHelper _dbContextHelper = new DbContextHelper();
        private IAuditScope _currentScope;

#region "Reader"
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            if (Core.Configuration.AuditDisabled || ExcludeReaderEvents)
            {
                return result;
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateEvent(command, eventData)
            };
            _currentScope = CreateAuditScope(auditEvent);
            return result;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            if (ExcludeReaderEvents)
            {
                return result;
            }
            var newDataReader = UpdateExecutedEvent(eventData, result);
            if (newDataReader != null)
            {
                result = newDataReader;
            }
            EndScope();
            return result;
        }

#if EF_CORE_3
        public override async Task<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
#else
        public async override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
#endif
        {
            if (Core.Configuration.AuditDisabled || ExcludeReaderEvents)
            {
                return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateEvent(command, eventData)
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent);
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

#if EF_CORE_3
        public override async Task<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
#else
        public async override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
#endif
        {
            if (ExcludeReaderEvents)
            {
                return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
            }
            var newDataReader = UpdateExecutedEvent(eventData, result);
            if (newDataReader != null)
            {
                result = newDataReader;
            }

            await EndScopeAsync();
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }
#endregion

#region "Non-query"
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            if (Core.Configuration.AuditDisabled || ExcludeNonQueryEvents)
            {
                return result;
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateEvent(command, eventData)
            };
            _currentScope = CreateAuditScope(auditEvent);
            return result;
        }
        
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            if (ExcludeNonQueryEvents)
            {
                return result;
            }
            UpdateExecutedEvent(eventData);
            EndScope();
            return result;
        }
#if EF_CORE_3
        public override async Task<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
#else
        public async override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
#endif
        {
            if (Core.Configuration.AuditDisabled || ExcludeNonQueryEvents)
            {
                return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateEvent(command, eventData)
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent);
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
#if EF_CORE_3
        public override async Task<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
#else
        public async override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
#endif
        {
            if (ExcludeNonQueryEvents)
            {
                return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
            }
            UpdateExecutedEvent(eventData);
            await EndScopeAsync();
            return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }
#endregion

#region "Scalar"
        // Scalar
        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            if (Core.Configuration.AuditDisabled || ExcludeScalarEvents)
            {
                return result;
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateEvent(command, eventData)
            };
            _currentScope = CreateAuditScope(auditEvent);
            return result;
        }
        
        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            if (ExcludeScalarEvents)
            {
                return result;
            }
            UpdateExecutedEvent(eventData);
            EndScope();
            return result;
        }
#if EF_CORE_3
        public override async Task<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
#else
        public async override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
#endif
        {
            if (Core.Configuration.AuditDisabled || ExcludeScalarEvents)
            {
                return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateEvent(command, eventData)
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent);
            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
#if EF_CORE_3
        public override async Task<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
#else
        public async override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
#endif
        {
            if (ExcludeScalarEvents)
            {
                return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
            }
            UpdateExecutedEvent(eventData);
            await EndScopeAsync();
            return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }
#endregion

#region "CommandFailed"
        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            UpdateFailedEvent(eventData);
            EndScope();
        }
        
        public override async Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            UpdateFailedEvent(eventData);
            await EndScopeAsync();
        }
#endregion

        /// <summary>
        /// Creates a Command event from the command data
        /// </summary>
        /// <param name="command">The DB command to be executed</param>
        /// <param name="eventData">The event data</param>
        protected virtual CommandEvent CreateEvent(DbCommand command, CommandEventData eventData)
        {
            return new CommandEvent()
            {
                CommandText = command.CommandText,
                CommandType = command.CommandType,
                ConnectionId = _dbContextHelper.GetClientConnectionId(command.Connection),
                DbConnectionId = eventData.ConnectionId.ToString(),
                Database = command.Connection?.Database,
                Parameters = GetParameters(command, eventData),
                IsAsync = eventData.IsAsync,
                Method = eventData.ExecuteMethod, 
                ContextId = eventData.Context?.ContextId.ToString(),
                TransactionId = eventData.Context?.Database.CurrentTransaction?.TransactionId.ToString()
            };
        }

        /// <summary>
        /// Updates a Command event from the command executed data
        /// </summary>
        /// <param name="eventData">The event data</param>
        /// <param name="result">The original DbDataReader reference</param>
        protected virtual DbDataReader UpdateExecutedEvent(CommandExecutedEventData eventData, DbDataReader result = null)
        {
            var cmdEvent = _currentScope?.GetCommandEntityFrameworkEvent();
            if (cmdEvent == null)
            {
                return null;
            }
            
            cmdEvent.Success = true;
            cmdEvent.ErrorMessage = null;
            if (eventData.ExecuteMethod == DbCommandMethod.ExecuteReader)
            {
                if (IncludeReaderResults && result != null)
                {
                    cmdEvent.Result = SerializeDataReader(result, out DbDataReader newDataReader);
                    return newDataReader;
                }
            }
            else
            {
                cmdEvent.Result = eventData.Result;
            }

            return null;
        }
       
        /// <summary>
        /// Updated a Command event from the command failed data
        /// </summary>
        /// <param name="eventData">The event data</param>
        protected virtual void UpdateFailedEvent(CommandErrorEventData eventData)
        {
            var cmdEvent = _currentScope?.GetCommandEntityFrameworkEvent();
            if (cmdEvent == null)
            {
                return;
            }
            cmdEvent.Success = false;
            cmdEvent.ErrorMessage = eventData.Exception?.GetExceptionInfo();
        }

        /// <summary>
        /// Serializes the result DB data reader and returns a new data reader to be overriden to the EF result.
        /// </summary>
        protected virtual List<Dictionary<string, object>> SerializeDataReader(DbDataReader reader, out DbDataReader newDataReader)
        {
            if (reader == null)
            {
                newDataReader = null;
                return null;
            }

            var dataTable = new DataTable();
            dataTable.Load(reader);
            newDataReader = dataTable.CreateDataReader();

            return dataTable.AsEnumerable().Select(
                row => dataTable.Columns.Cast<DataColumn>().ToDictionary(
                    column => column.ColumnName,
                    column => row[column]
                )).ToList();
        }
        
        private void EndScope()
        {
            _currentScope?.Dispose();
        }

        private async Task EndScopeAsync()
        {
            if (_currentScope != null)
            {
                await _currentScope.DisposeAsync();
            }
        }

        private Dictionary<string, object> GetParameters(DbCommand command, CommandEventData eventData)
        {
            bool logParams = LogParameterValues ?? eventData.LogParameterValues;
            if (!logParams)
            {
                return null;
            }
            return command.Parameters.Cast<DbParameter>().ToDictionary(p => p.ParameterName, p => p.Value);
        }

        private IAuditScope CreateAuditScope(AuditEventCommandEntityFramework cmdEvent)
        {
            var eventType = AuditEventType?
                .Replace("{database}", cmdEvent.CommandEvent.Database)
                .Replace("{method}", cmdEvent.CommandEvent.Method.ToString());
            var factory = Core.Configuration.AuditScopeFactory;
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = cmdEvent,
                SkipExtraFrames = 3
            };
            return factory.Create(options);
        }

        private async Task<IAuditScope> CreateAuditScopeAsync(AuditEventCommandEntityFramework cmdEvent)
        {
            var eventType = AuditEventType?
                .Replace("{database}", cmdEvent.CommandEvent.Database)
                .Replace("{method}", cmdEvent.CommandEvent.Method.ToString());
            var factory = Core.Configuration.AuditScopeFactory;
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = cmdEvent,
                SkipExtraFrames = 3
            };
            return await factory.CreateAsync(options);
        }
    }
}
#endif