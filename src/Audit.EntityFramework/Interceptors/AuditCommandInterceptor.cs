#if EF_CORE_5_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;
using Audit.Core.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
        /// Boolean value to indicate whether to log the command parameter values. By default, (when null) it will depend on EnableSensitiveDataLogging setting on the DbContext.
        /// </summary>
        public bool? LogParameterValues { get; set; }

        /// <summary>
        /// Boolean value to indicate whether to exclude the events handled by ReaderExecuting. Default is false to include the ReaderExecuting events.
        /// </summary>
        public bool ExcludeReaderEvents { get; set; }

        /// <summary>
        /// Predicate to include the ReaderExecuting events based on the event data. By default, all the ReaderExecuting events are included.
        /// This predicate is ignored if ExcludeReaderEvents is set to true.
        /// </summary>
        public Func<CommandEventData, bool> IncludeReaderEventsPredicate { get; set; }
        
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
        /// - {context}: Replaced with the DbContext type name
        /// </summary>
        public string AuditEventType { get; set; }

        /// <summary>
        /// Boolean value to indicate whether to include the query results to the audit output. Default is false.
        /// </summary>
        public bool IncludeReaderResults { get; set; }
        
        private readonly DbContextHelper _dbContextHelper = new DbContextHelper();
        private IAuditScope _currentScope;

        /// <summary>
        /// Returns the current audit scope, if any
        /// </summary>
        /// <returns></returns>
        protected IAuditScope GetAuditScope()
        {
            return _currentScope;
        }

#region "Reader"
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            if (Core.Configuration.AuditDisabled || ExcludeReaderEvents || IncludeReaderEventsPredicate?.Invoke(eventData) == false)
            {
                return result;
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateAuditEvent(command, eventData)
            };
            _currentScope = CreateAuditScope(auditEvent);
            return result;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            if (_currentScope == null)
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

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Core.Configuration.AuditDisabled || ExcludeReaderEvents || IncludeReaderEventsPredicate?.Invoke(eventData) == false)
            {
                return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateAuditEvent(command, eventData)
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent, cancellationToken);
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (_currentScope == null)
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
                CommandEvent = CreateAuditEvent(command, eventData)
            };
            _currentScope = CreateAuditScope(auditEvent);
            return result;
        }
        
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            if (_currentScope == null)
            {
                return result;
            }
            UpdateExecutedEvent(eventData);
            EndScope();
            return result;
        }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Core.Configuration.AuditDisabled || ExcludeNonQueryEvents)
            {
                return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateAuditEvent(command, eventData)
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent, cancellationToken);
            return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (_currentScope == null)
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
                CommandEvent = CreateAuditEvent(command, eventData)
            };
            _currentScope = CreateAuditScope(auditEvent);
            return result;
        }
        
        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            if (_currentScope == null)
            {
                return result;
            }
            UpdateExecutedEvent(eventData);
            EndScope();
            return result;
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            if (Core.Configuration.AuditDisabled || ExcludeScalarEvents)
            {
                return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
            }
            var auditEvent = new AuditEventCommandEntityFramework
            {
                CommandEvent = CreateAuditEvent(command, eventData)
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent, cancellationToken);
            return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            if (_currentScope == null)
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
        protected virtual CommandEvent CreateAuditEvent(DbCommand command, CommandEventData eventData)
        {
            return new CommandEvent()
            {
                CommandText = command.CommandText,
                CommandType = command.CommandType,
#if NET6_0_OR_GREATER
                CommandSource = eventData.CommandSource,
#endif
                ConnectionId = _dbContextHelper.GetClientConnectionId(command.Connection),
                DbConnectionId = eventData.ConnectionId.ToString(),
                Database = command.Connection?.Database,
                Parameters = GetParameters(command, eventData),
                IsAsync = eventData.IsAsync,
                Method = eventData.ExecuteMethod, 
                ContextId = eventData.Context?.ContextId.ToString(),
                TransactionId = eventData.Context?.Database.CurrentTransaction?.TransactionId.ToString(),
                DbContext = eventData.Context
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
        protected virtual Dictionary<string, List<Dictionary<string, object>>> SerializeDataReader(DbDataReader reader, out DbDataReader newDataReader)
        {
            if (reader == null)
            {
                newDataReader = null;
                return null;
            }
            
            // Create the data table from the original reader
            var dataSet = new DataSet();
            do
            {
                var table = new DataTable();
                table.Load(reader);
                dataSet.Tables.Add(table);
            } while (!reader.IsClosed);
            
            newDataReader = dataSet.CreateDataReader();

            var resultData = dataSet.Tables.Cast<DataTable>().ToDictionary(k => k.TableName, t =>
                t.AsEnumerable()
                    .Select(row => t.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => row[c])).ToList());

            return resultData;
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
            var context = cmdEvent.CommandEvent.DbContext as IAuditDbContext;

            if (context?.AuditDisabled == true)
            {
                return null;
            }

            var options = GetAuditScopeOptions(cmdEvent);

            var factory = _dbContextHelper.GetAuditScopeFactory(cmdEvent.CommandEvent.DbContext);

            var scope = factory.Create(options);
            context?.OnScopeCreated(scope);
            return scope;
        }

        private async Task<IAuditScope> CreateAuditScopeAsync(AuditEventCommandEntityFramework cmdEvent, CancellationToken cancellationToken)
        {
            var context = cmdEvent.CommandEvent.DbContext as IAuditDbContext;

            if (context?.AuditDisabled == true)
            {
                return null;
            }

            var options = GetAuditScopeOptions(cmdEvent);

            var factory = _dbContextHelper.GetAuditScopeFactory(cmdEvent.CommandEvent.DbContext);

            var scope = await factory.CreateAsync(options, cancellationToken);
            context?.OnScopeCreated(scope);
            return scope;
        }

        private AuditScopeOptions GetAuditScopeOptions(AuditEventCommandEntityFramework cmdEvent)
        {
            var dbContext = cmdEvent.CommandEvent.DbContext;
            var auditDbContext = cmdEvent.CommandEvent.DbContext as IAuditDbContext;

            var typeName = dbContext.GetType().Name;
            var eventType = (this.AuditEventType ?? auditDbContext?.AuditEventType ?? "{method}")
                .Replace("{context}", typeName)
                .Replace("{database}", cmdEvent.CommandEvent.Database)
                .Replace("{method}", cmdEvent.CommandEvent.Method.ToString());

            if (auditDbContext?.ExtraFields?.Count > 0)
            {
                cmdEvent.CustomFields = new Dictionary<string, object>(auditDbContext.ExtraFields);
            }

            var dataProvider = _dbContextHelper.GetDataProvider(cmdEvent.CommandEvent.DbContext);

            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = cmdEvent,
                SkipExtraFrames = 3,
                DataProvider = dataProvider
            };

            return options;
        }

        private void EndScope()
        {
            if (_currentScope == null)
            {
                return;
            }

            var context = _currentScope.GetCommandEntityFrameworkEvent()?.DbContext as IAuditDbContext;

            context?.OnScopeSaving(_currentScope);
            _currentScope.Dispose();
            context?.OnScopeSaved(_currentScope);
        }

        private async Task EndScopeAsync()
        {
            if (_currentScope == null)
            {
                return;
            }

            var context = _currentScope.GetCommandEntityFrameworkEvent()?.DbContext as IAuditDbContext;

            context?.OnScopeSaving(_currentScope);
            await _currentScope.DisposeAsync();
            context?.OnScopeSaved(_currentScope);
        }
    }
}
#endif