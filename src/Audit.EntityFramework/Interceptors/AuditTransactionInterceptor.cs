#if EF_CORE_5_OR_GREATER
using Audit.Core;
using Audit.Core.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Audit.EntityFramework.Interceptors
{
    /// <summary>
    /// <para>
    ///     Allows interception of operations related to a <see cref="DbTransaction" /> such as Start, Commit and Rollback.
    /// </para>
    /// <para>
    ///     Transaction interceptors can be used to view, change, or suppress operations on <see cref="DbTransaction" />, and
    ///     to modify the result before it is returned to EF.
    ///  </para>
    /// </summary>
    public class AuditTransactionInterceptor : DbTransactionInterceptor
    {
        private readonly DbContextHelper _dbContextHelper = new DbContextHelper();
        private IAuditScope _currentScope;

        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is "{database}:{transaction}"). 
        /// Can contain the following placeholders: 
        /// - {database}: Replaced with the database name
        /// - {transaction}: Replaced with the transaction ID
        /// - {context}: Replaced with the DbContext type name
        /// </summary>
        public string AuditEventType { get; set; }

        #region "Transaction Start"

        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result)
        {
            if (Core.Configuration.AuditDisabled)
            {
                return result;
            }

            var auditEvent = new AuditEventTransactionEntityFramework
            {
                TransactionEvent = CreateAuditEvent(connection, eventData, "Start")
            };
            _currentScope = CreateAuditScope(auditEvent);

            return result;
        }

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = new CancellationToken())
        {
            if (Core.Configuration.AuditDisabled)
            {
                return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
            }

            var auditEvent = new AuditEventTransactionEntityFramework
            {
                TransactionEvent = CreateAuditEvent(connection, eventData, "Start")
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent, cancellationToken);

            return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
        }

        public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData, DbTransaction result)
        {
            if (Core.Configuration.AuditDisabled)
            {
                return result;
            }

            UpdateExecutedEventSuccess(eventData);
            EndScope();

            return result;
        }
        
        public override async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (Core.Configuration.AuditDisabled)
            {
                return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
            }

            UpdateExecutedEventSuccess(eventData);
            await EndScopeAsync();

            return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
        }

        #endregion

        #region "Transaction Commit"

        public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result)
        {
            if (Core.Configuration.AuditDisabled)
            {
                return result;
            }

            var auditEvent = new AuditEventTransactionEntityFramework
            {
                TransactionEvent = CreateAuditEvent(transaction, eventData, "Commit")
            };
            _currentScope = CreateAuditScope(auditEvent);

            return result;
        }

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = new CancellationToken())
        {
            if (Core.Configuration.AuditDisabled)
            {
                return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
            }

            var auditEvent = new AuditEventTransactionEntityFramework
            {
                TransactionEvent = CreateAuditEvent(transaction, eventData, "Commit")
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent, cancellationToken);

            return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (Core.Configuration.AuditDisabled)
            {
                return;
            }

            UpdateExecutedEventSuccess(eventData);
            EndScope();
        }

        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (Core.Configuration.AuditDisabled)
            {
                return;
            }

            UpdateExecutedEventSuccess(eventData);
            await EndScopeAsync();
        }

        #endregion

        #region "Transaction Rollback"

        public override InterceptionResult TransactionRollingBack(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result)
        {
            if (Core.Configuration.AuditDisabled)
            {
                return result;
            }

            var auditEvent = new AuditEventTransactionEntityFramework
            {
                TransactionEvent = CreateAuditEvent(transaction, eventData, "Rollback")
            };
            _currentScope = CreateAuditScope(auditEvent);

            return result;
        }

        public override async ValueTask<InterceptionResult> TransactionRollingBackAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = new CancellationToken())
        {
            if (Core.Configuration.AuditDisabled)
            {
                return await base.TransactionRollingBackAsync(transaction, eventData, result, cancellationToken);
            }

            var auditEvent = new AuditEventTransactionEntityFramework
            {
                TransactionEvent = CreateAuditEvent(transaction, eventData, "Rollback")
            };
            _currentScope = await CreateAuditScopeAsync(auditEvent, cancellationToken);

            return await base.TransactionRollingBackAsync(transaction, eventData, result, cancellationToken);
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        {
            if (Core.Configuration.AuditDisabled)
            {
                return;
            }

            UpdateExecutedEventSuccess(eventData);
            EndScope();
        }

        public override async Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (Core.Configuration.AuditDisabled)
            {
                return;
            }

            UpdateExecutedEventSuccess(eventData);
            await EndScopeAsync();
        }

        #endregion

        #region "Transaction Failed"
        
        public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
        {
            UpdateFailedEvent(eventData);
            EndScope();
        }

        public override async Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData,
            CancellationToken cancellationToken = new CancellationToken())
        {
            UpdateFailedEvent(eventData);
            await EndScopeAsync();
        }

        private void UpdateFailedEvent(TransactionErrorEventData eventData)
        {
            var tranEvent = _currentScope?.GetTransactionEntityFrameworkEvent();
            if (tranEvent == null)
            {
                return;
            }
            tranEvent.Success = false;
            tranEvent.Action = eventData.Action;
            tranEvent.TransactionId = eventData.TransactionId.ToString();
            tranEvent.ErrorMessage = eventData.Exception.GetExceptionInfo();
        }

        #endregion

        private TransactionEvent CreateAuditEvent(DbConnection connection, TransactionStartingEventData eventData, string action)
        {
            return new TransactionEvent()
            {
                Action = action,
                ConnectionId = _dbContextHelper.GetClientConnectionId(connection),
                DbConnectionId = eventData.ConnectionId.ToString(),
                Database = connection.Database,
                IsAsync = eventData.IsAsync,
                ContextId = eventData.Context?.ContextId.ToString(),
                TransactionId = eventData.TransactionId.ToString(),
                EventIdCode = eventData.EventIdCode,
                Message = eventData.ToString(),
                DbContext = eventData.Context
            };
        }

        private TransactionEvent CreateAuditEvent(DbTransaction transaction, TransactionEventData eventData, string action)
        {
            return new TransactionEvent()
            {
                Action = action,
                ConnectionId = _dbContextHelper.GetClientConnectionId(transaction.Connection),
                DbConnectionId = eventData.ConnectionId.ToString(),
                Database = transaction.Connection?.Database,
                IsAsync = eventData.IsAsync,
                ContextId = eventData.Context?.ContextId.ToString(),
                TransactionId = eventData.TransactionId.ToString(),
                EventIdCode = eventData.EventIdCode,
                Message = eventData.ToString(),
                DbContext = eventData.Context
            };
        }

        private void UpdateExecutedEventSuccess(TransactionEndEventData eventData)
        {
            var tranEvent = _currentScope?.GetTransactionEntityFrameworkEvent();
            if (tranEvent == null)
            {
                return;
            }

            tranEvent.Success = true;
            tranEvent.ErrorMessage = null;
        }

        private IAuditScope CreateAuditScope(AuditEventTransactionEntityFramework tranEvent)
        {
            var context = tranEvent.TransactionEvent.DbContext as IAuditDbContext;

            var typeName = tranEvent.TransactionEvent.DbContext?.GetType().Name;
            var eventType = (this.AuditEventType ?? context?.AuditEventType ?? "{database}:{transaction}")
                .Replace("{context}", typeName)
                .Replace("{database}", tranEvent.TransactionEvent.Database)
                .Replace("{transaction}", tranEvent.TransactionEvent.TransactionId);

            if (context?.ExtraFields?.Count > 0)
            {
                tranEvent.CustomFields = new Dictionary<string, object>(context.ExtraFields);
            }

            var factory = _dbContextHelper.GetAuditScopeFactory(tranEvent.TransactionEvent.DbContext);
            var dataProvider = _dbContextHelper.GetDataProvider(tranEvent.TransactionEvent.DbContext);

            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = tranEvent,
                SkipExtraFrames = 3,
                DataProvider = dataProvider
            };

            var scope = factory.Create(options);
            context?.OnScopeCreated(scope);
            return scope;
        }

        private async Task<IAuditScope> CreateAuditScopeAsync(AuditEventTransactionEntityFramework tranEvent, CancellationToken cancellationToken)
        {
            var context = tranEvent.TransactionEvent.DbContext as IAuditDbContext;

            var typeName = tranEvent.TransactionEvent.DbContext?.GetType().Name;
            var eventType = (this.AuditEventType ?? context?.AuditEventType ?? "{database}:{transaction}")
                .Replace("{context}", typeName)
                .Replace("{database}", tranEvent.TransactionEvent.Database)
                .Replace("{transaction}", tranEvent.TransactionEvent.TransactionId);

            if (context?.ExtraFields?.Count > 0)
            {
                tranEvent.CustomFields = new Dictionary<string, object>(context.ExtraFields);
            }

            var factory = _dbContextHelper.GetAuditScopeFactory(tranEvent.TransactionEvent.DbContext);
            var dataProvider = _dbContextHelper.GetDataProvider(tranEvent.TransactionEvent.DbContext);

            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = tranEvent,
                SkipExtraFrames = 3,
                DataProvider = dataProvider
            };

            var scope = await factory.CreateAsync(options, cancellationToken);
            context?.OnScopeCreated(scope);
            return scope;
        }

        private void EndScope()
        {
            if (_currentScope == null)
            {
                return;
            }

            var context = _currentScope.GetTransactionEntityFrameworkEvent()?.DbContext as IAuditDbContext;

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

            var context = _currentScope.GetTransactionEntityFrameworkEvent()?.DbContext as IAuditDbContext;

            context?.OnScopeSaving(_currentScope);
            await _currentScope.DisposeAsync();
            context?.OnScopeSaved(_currentScope);
        }
    }
}
#endif