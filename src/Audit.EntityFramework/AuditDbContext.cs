using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Audit.Core;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Audit.EntityFramework.ConfigurationApi;
using Audit.Core.Extensions;
#if NETCOREAPP1_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#elif NET45
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
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
        /// To indicate the audit operation mode. (Default is OptOut). 
        ///  - OptOut: All the entities are tracked by default, except those decorated with the AuditIgnore attribute. 
        ///  - OptIn: No entity is tracked by default, except those decorated with the AuditInclude attribute.
        /// </summary>
        protected virtual AuditOptionMode Mode { get; set; }

        /// <summary>
        /// To indicate the Audit Data Provider to use. (Default is NULL to use the configured default data provider). 
        /// </summary>
        protected virtual AuditDataProvider AuditDataProvider { get; set; }
        #endregion

        #region Private fields
        // Entities Include/Ignore attributes cache
        private static readonly Dictionary<Type, bool?> EntitiesIncludeIgnoreAttrCache = new Dictionary<Type, bool?>();
        // AuditDbContext Attribute cache
        private static Dictionary<Type, AuditDbContextAttribute> _auditAttributeCache = new Dictionary<Type, AuditDbContextAttribute>();
        // User defined fields that will be stored as Custom Fields on the audit event
        private Dictionary<string, object> _extraFields;
        #endregion

        #region Private methods
        private void SetConfig()
        {
            var type = GetType();
            if (!_auditAttributeCache.ContainsKey(type))
            {
                _auditAttributeCache[type] = GetType().GetTypeInfo().GetCustomAttribute(typeof(AuditDbContextAttribute)) as AuditDbContextAttribute;
            }
            var attrConfig = _auditAttributeCache[type]?.InternalConfig;
            var localConfig = Audit.EntityFramework.Configuration.GetConfigForType(GetType());
            var globalConfig = Audit.EntityFramework.Configuration.GetConfigForType(typeof(AuditDbContext));

            Mode = attrConfig?.Mode ?? localConfig?.Mode ?? globalConfig?.Mode ?? AuditOptionMode.OptOut;
            IncludeEntityObjects = attrConfig?.IncludeEntityObjects ?? localConfig?.IncludeEntityObjects ?? globalConfig?.IncludeEntityObjects ?? false;
            AuditEventType = attrConfig?.AuditEventType ?? localConfig?.AuditEventType ?? globalConfig?.AuditEventType;
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
            (scope.Event as AuditEventEntityFramework).EntityFrameworkEvent = @event;
            OnScopeSaving(scope);
            scope.Save();
        }

        // Determines whether to include the entity on the audit log or not
#if NETCOREAPP1_0
        private bool IncludeEntity(EntityEntry entry, AuditOptionMode mode)
#elif NET45
        private bool IncludeEntity(DbEntityEntry entry, AuditOptionMode mode)
#endif
        {
            var type = entry.Entity.GetType();
#if NET45
            type = ObjectContext.GetObjectType(type);
#endif
            bool? result = EnsureEntitiesIncludeIgnoreAttrCache(type); //true:excluded false=ignored null=unknown
            if (result == null)
            {
                // No static attributes, check the filters
                var localConfig = EntityFramework.Configuration.GetConfigForType(GetType());
                var globalConfig = EntityFramework.Configuration.GetConfigForType(typeof(AuditDbContext));
                var included = EvalIncludeFilter(type, localConfig, globalConfig);
                var ignored = EvalIgnoreFilter(type, localConfig, globalConfig);
                result = included ? true : ignored ? false : (bool?)null;
            }
            if (mode == AuditOptionMode.OptIn)
            {
                // Include only explicitly included entities
                return result.GetValueOrDefault();
            }
            // Include all, except the explicitly ignored entities
            return result == null || result.Value;
        }

        private bool? EnsureEntitiesIncludeIgnoreAttrCache(Type type)
        {
            if (!EntitiesIncludeIgnoreAttrCache.ContainsKey(type))
            {
                var includeAttr = type.GetTypeInfo().GetCustomAttribute(typeof(AuditIncludeAttribute));
                if (includeAttr != null)
                {
                    EntitiesIncludeIgnoreAttrCache[type] = true; // Type Included by IncludeAttribute
                }
                else if (type.GetTypeInfo().GetCustomAttribute(typeof(AuditIgnoreAttribute)) != null)
                {
                    EntitiesIncludeIgnoreAttrCache[type] = false; // Type Ignored by IgnoreAttribute
                }
                else
                {
                    EntitiesIncludeIgnoreAttrCache[type] = null; // No attribute
                }
            }
            return EntitiesIncludeIgnoreAttrCache[type];
        }
        /// <summary>
        /// Gets the include value for a given entity type.
        /// </summary>
        private bool EvalIncludeFilter(Type type, EfSettings localConfig, EfSettings globalConfig)
        {
            var includedExplicit = localConfig?.IncludedTypes.Contains(type) ?? globalConfig?.IncludedTypes.Contains(type) ?? false;
            if (includedExplicit)
            {
                return true;
            }
            var includedFilter = localConfig?.IncludedTypesFilter ?? globalConfig?.IncludedTypesFilter;
            if (includedFilter != null)
            {
                return includedFilter.Invoke(type);
            }
            return false;
        }

        /// <summary>
        /// Gets the exclude value for a given entity type.
        /// </summary>
        private bool EvalIgnoreFilter(Type type, EfSettings localConfig, EfSettings globalConfig)
        {
            var ignoredExplicit = localConfig?.IgnoredTypes.Contains(type) ?? globalConfig?.IgnoredTypes.Contains(type) ?? false;
            if (ignoredExplicit)
            {
                return true;
            }
            var ignoredFilter = localConfig?.IgnoredTypesFilter ?? globalConfig?.IgnoredTypesFilter;
            if (ignoredFilter != null)
            {
                return ignoredFilter.Invoke(type);
            }
            return false;
        }

        /// <summary>
        /// Creates the Audit scope.
        /// </summary>
        private AuditScope CreateAuditScope(EntityFrameworkEvent efEvent)
        {
            var typeName = GetType().Name;
            var eventType = AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var auditEfEvent = new AuditEventEntityFramework();
            auditEfEvent.EntityFrameworkEvent = efEvent;
            var scope = AuditScope.Create(eventType, null, null, EventCreationPolicy.Manual, AuditDataProvider, auditEfEvent, 2);
            if (_extraFields != null)
            {
                foreach(var field in _extraFields)
                {
                    scope.SetCustomField(field.Key, field.Value);
                }
            }
            OnScopeCreated(scope);
            return scope;
        }

        /// <summary>
        /// Gets the modified entries to process.
        /// </summary>
#if NETCOREAPP1_0
        private List<EntityEntry> GetModifiedEntries(AuditOptionMode mode)
#elif NET45
        private List<DbEntityEntry> GetModifiedEntries(AuditOptionMode mode)
#endif
        {
            return ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged
                         && x.State != EntityState.Detached
                         && IncludeEntity(x, mode))
                .ToList();
        }

        /// <summary>
        /// Gets a unique ID for the current SQL transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="clientConnectionId">The client connection identifier.</param>
        /// <returns>System.String.</returns>
        private static string GetTransactionId(DbTransaction transaction, string clientConnectionId)
        {
            var propIntTran = transaction.GetType().GetTypeInfo().GetProperty("InternalTransaction", BindingFlags.NonPublic | BindingFlags.Instance);
            object intTran = propIntTran?.GetValue(transaction);
            var propTranId = intTran?.GetType().GetTypeInfo().GetProperty("TransactionId", BindingFlags.NonPublic | BindingFlags.Instance);
            var tranId = (int)(long)propTranId?.GetValue(intTran);
            return string.Format("{0}_{1}", clientConnectionId, tranId);
        }

        private string GetClientConnectionId(DbConnection connection)
        {
            // Get the connection id (returns NULL if the connection is not open)
            var sqlConnection = connection as SqlConnection;
            var connId = sqlConnection?.ClientConnectionId;
            return connId.HasValue && !connId.Equals(Guid.Empty) ? connId.Value.ToString() : null;
        }

        /// <summary>
        /// Called after the audit scope is created.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        protected virtual void OnScopeCreated(AuditScope auditScope)
        {
        }
        /// <summary>
        /// Called after the EF operation execution and before the AuditScope saving.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        protected virtual void OnScopeSaving(AuditScope auditScope)
        {
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Adds a custom field to the audit scope.
        /// The value will be serialized when SaveChanges takes place.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        public void AddAuditCustomField(string fieldName, object value)
        {
            if (_extraFields == null)
            {
                _extraFields = new Dictionary<string, object>();
            }
            _extraFields.Add(fieldName, value);
        }

        /// <summary>
        /// Saves the changes synchronously.
        /// </summary>
        public override int SaveChanges()
        {
            if (AuditDisabled)
            {
                return base.SaveChanges();
            }
            var efEvent = CreateAuditEvent(IncludeEntityObjects, Mode);
            if (efEvent == null)
            {
                return base.SaveChanges();
            }
            var scope = CreateAuditScope(efEvent);
            try
            {
                efEvent.Result = base.SaveChanges();
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = ex.GetExceptionInfo();
                SaveScope(scope, efEvent);
                throw;
            }
            efEvent.Success = true;
            SaveScope(scope, efEvent);
            return efEvent.Result;
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
            var efEvent = CreateAuditEvent(IncludeEntityObjects, Mode);
            if (efEvent == null)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            var scope = CreateAuditScope(efEvent);
            try
            {
                efEvent.Result = await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = ex.GetExceptionInfo();
                SaveScope(scope, efEvent);
                throw;
            }
            efEvent.Success = true;
            SaveScope(scope, efEvent);
            return efEvent.Result;
        }
#endregion
    }
}
