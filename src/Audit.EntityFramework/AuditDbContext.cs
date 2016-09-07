using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Audit.Core;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.SqlClient;
#if NETCOREAPP1_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#elif NET45
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif

namespace Audit.EntityFramework
{
    /// <summary>
    /// The base DbContext class for Audit
    /// (Common).
    /// </summary>
    public abstract partial class AuditDbContext : DbContext
    {
#region Contructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext() : base()
        {
            SetConfig();
        }
        #endregion

#region Properties
        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is the context name). 
        /// Can contain the following placeholders: 
        ///  - {context}: replaced with the Db Context type name.
        ///  - {database}: replaced with the database name.
        /// </summary>
        protected virtual string AuditEventType { get; set; }

        /// <summary>
        /// Indicates if the Audit is disabled.
        /// Default is false.
        /// </summary>
        protected virtual bool AuditDisabled { get; set; }

        /// <summary>
        /// To indicate if the output should contain the modified entities objects. (Default is false)
        /// </summary>
        protected virtual bool IncludeEntityObjects { get; set; }

        /// <summary>
        /// To indicate the audit operation mode. (Default if OptOut). 
        ///  - OptOut: All the entities are tracked by default, except those decorated with the AuditIgnore attribute. 
        ///  - OptIn: No entity is tracked by default, except those decorated with the AuditInclude attribute.
        /// </summary>
        protected virtual AuditOptionMode Mode { get; set; }
#endregion

#region Private fields
        // Entities Include/Ignore attributes cache
        private static readonly Dictionary<Type, bool?> EntitiesIncludedCache = new Dictionary<Type, bool?>();
        // AuditDbContext Attribute cache
        private static AuditDbContextAttribute _auditAttribute;
#endregion

#region Private methods
        private void SetConfig()
        {
            if (_auditAttribute == null)
            {
                _auditAttribute = GetType().GetTypeInfo().GetCustomAttribute(typeof(AuditDbContextAttribute)) as AuditDbContextAttribute ?? new AuditDbContextAttribute();
            }
            Mode = _auditAttribute.Mode;
            IncludeEntityObjects = _auditAttribute.IncludeEntityObjects;
            AuditEventType = _auditAttribute.AuditEventType;
        }
        /// <summary>
        /// Gets the validation results, return NULL if there are no validation errors.
        /// </summary>
        private static List<ValidationResult> GetValidationResults(object entity)
        {
            var validationContext = new ValidationContext(entity);
            var validationResults = new List<ValidationResult>();
            bool valid = Validator.TryValidateObject(entity, validationContext, validationResults, true);
            return valid ? null : validationResults;
        }

        /// <summary>
        /// Gets the name for an entity state.
        /// </summary>
        private static string GetStateName(EntityState state)
        {
            switch (state)
            {
                case EntityState.Added:
                    return "Insert";
                case EntityState.Modified:
                    return "Update";
                case EntityState.Deleted:
                    return "Delete";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Saves the scope.
        /// </summary>
        private void SaveScope(AuditScope scope, EntityFrameworkEvent @event)
        {
            scope.SetCustomField("EntityFrameworkEvent", @event);
            scope.Save();
        }

        /// <summary>
        /// Gets the exception info for an exception
        /// </summary>
        private static string GetExceptionInfo(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }
            string exceptionInfo = $"({exception.GetType().Name}) {exception.Message}";
            Exception inner = exception;
            while ((inner = inner.InnerException) != null)
            {
                exceptionInfo += " -> " + inner.Message;
            }
            return exceptionInfo;
        }

        // Determines whether to include the entity on the audit log or not
#if NETCOREAPP1_0
        private static bool IncludeEntity(EntityEntry entry, AuditOptionMode mode)
#elif NET45
        private static  bool IncludeEntity(DbEntityEntry entry, AuditOptionMode mode)
#endif
        {
            var type = entry.Entity.GetType();
            if (!EntitiesIncludedCache.ContainsKey(type))
            {
                var includeAttr = type.GetTypeInfo().GetCustomAttribute(typeof(AuditIncludeAttribute));
                if (includeAttr != null)
                {
                    EntitiesIncludedCache[type] = true; // Type Included
                }
                else
                {
                    var ignoreAttr = type.GetTypeInfo().GetCustomAttribute(typeof(AuditIgnoreAttribute));
                    if (ignoreAttr != null)
                    {
                        EntitiesIncludedCache[type] = false; // Type Ignored
                    }
                    else
                    {
                        EntitiesIncludedCache[type] = null; // No attribute specified
                    }
                }
            }
            if (mode == AuditOptionMode.OptIn)
            {
                return EntitiesIncludedCache[type] != null && EntitiesIncludedCache[type].Value;
            }
            return EntitiesIncludedCache[type] == null || EntitiesIncludedCache[type].Value;
        }

        /// <summary>
        /// Creates the Audit scope.
        /// </summary>
        private AuditScope CreateAuditScope(EntityFrameworkEvent efEvent)
        {
            var typeName = GetType().Name;
            var eventType = AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var scope = AuditScope.Create(eventType, null, EventCreationPolicy.Manual);
            return scope;
        }

        /// <summary>
        /// Gets the modified entries to process.
        /// </summary>
#if NETCOREAPP1_0
        private static List<EntityEntry> GetModifiedEntries(DbContext context, AuditOptionMode mode)
#elif NET45
        private static List<DbEntityEntry> GetModifiedEntries(DbContext context, AuditOptionMode mode)
#endif
        {
            return context.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged
                         && x.State != EntityState.Detached
                         && IncludeEntity(x, mode))
                .ToList();
        }

        private static string GetTransactionId(DbTransaction transaction, DbConnection connection)
        {
            // Get the transaction id
            var propIntTran = transaction.GetType().GetProperty("InternalTransaction", BindingFlags.NonPublic | BindingFlags.Instance);
            object intTran = propIntTran?.GetValue(transaction);
            var propTranId = intTran?.GetType().GetProperty("TransactionId", BindingFlags.NonPublic | BindingFlags.Instance);
            var tranId = (int)(long)propTranId?.GetValue(intTran);
            // Get the connection id
            var sqlConnection = connection as SqlConnection;
            var connId = sqlConnection?.ClientConnectionId.ToString();
            return string.Format("{0}_{1}", connId, tranId);
        }
        #endregion

#region Public methods
        /// <summary>
        /// Saves the changes synchronously.
        /// </summary>
        public override int SaveChanges()
        {
            if (AuditDisabled)
            {
                return base.SaveChanges();
            }
            var efEvent = CreateAuditEvent(this, IncludeEntityObjects, Mode);
            if (efEvent == null)
            {
                return base.SaveChanges();
            }
            var scope = CreateAuditScope(efEvent);
            try
            {
                efEvent.Result = base.SaveChanges();
                efEvent.Success = true;
                SaveScope(scope, efEvent);
                return efEvent.Result;
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = GetExceptionInfo(ex);
                SaveScope(scope, efEvent);
                throw;
            }
        }

        /// <summary>
        /// Saves the changes asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (AuditDisabled)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            var efEvent = CreateAuditEvent(this, IncludeEntityObjects, Mode);
            if (efEvent == null)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            var scope = CreateAuditScope(efEvent);
            try
            {
                efEvent.Result = await base.SaveChangesAsync(cancellationToken);
                efEvent.Success = true;
                SaveScope(scope, efEvent);
                return efEvent.Result;
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = GetExceptionInfo(ex);
                SaveScope(scope, efEvent);
                throw;
            }
        }
#endregion
    }
}
