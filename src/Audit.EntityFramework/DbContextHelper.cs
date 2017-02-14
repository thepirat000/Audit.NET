#if NETCOREAPP1_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#elif NET45
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;
#endif
using Audit.Core.Extensions;
using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Audit.EntityFramework
{
    public partial class DbContextHelper
    {
        // Entities Include/Ignore attributes cache
        private static readonly Dictionary<Type, bool?> EntitiesIncludeIgnoreAttrCache = new Dictionary<Type, bool?>();
        // AuditDbContext Attribute cache
        private static Dictionary<Type, AuditDbContextAttribute> _auditAttributeCache = new Dictionary<Type, AuditDbContextAttribute>();

        /// <summary>
        /// Sets the configuration values from attribute, local and global
        /// </summary>
        public void SetConfig(IAuditDbContext context)
        {
            var type = context.GetType();
            if (!_auditAttributeCache.ContainsKey(type))
            {
                _auditAttributeCache[type] = type.GetTypeInfo().GetCustomAttribute(typeof(AuditDbContextAttribute)) as AuditDbContextAttribute;
            }
            var attrConfig = _auditAttributeCache[type]?.InternalConfig;
            var localConfig = Audit.EntityFramework.Configuration.GetConfigForType(type);
            var globalConfig = Audit.EntityFramework.Configuration.GetConfigForType(typeof(AuditDbContext));

            context.Mode = attrConfig?.Mode ?? localConfig?.Mode ?? globalConfig?.Mode ?? AuditOptionMode.OptOut;
            context.IncludeEntityObjects = attrConfig?.IncludeEntityObjects ?? localConfig?.IncludeEntityObjects ?? globalConfig?.IncludeEntityObjects ?? false;
            context.AuditEventType = attrConfig?.AuditEventType ?? localConfig?.AuditEventType ?? globalConfig?.AuditEventType;
        }

        /// <summary>
        /// Gets the validation results, return NULL if there are no validation errors.
        /// </summary>
        public static List<ValidationResult> GetValidationResults(object entity)
        {
            var validationContext = new ValidationContext(entity);
            var validationResults = new List<ValidationResult>();
            bool valid = Validator.TryValidateObject(entity, validationContext, validationResults, true);
            return valid ? null : validationResults;
        }

        /// <summary>
        /// Gets the name for an entity state.
        /// </summary>
        public static string GetStateName(EntityState state)
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
        public void SaveScope(IAuditDbContext context, AuditScope scope, EntityFrameworkEvent @event)
        {
            (scope.Event as AuditEventEntityFramework).EntityFrameworkEvent = @event;
            context.OnScopeSaving(scope);
            scope.Save();
        }

        // Determines whether to include the entity on the audit log or not
#if NETCOREAPP1_0
        private bool IncludeEntity(IAuditDbContext context, EntityEntry entry, AuditOptionMode mode)
#elif NET45
        private bool IncludeEntity(IAuditDbContext context, DbEntityEntry entry, AuditOptionMode mode)
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
                var localConfig = EntityFramework.Configuration.GetConfigForType(context.GetType());
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
        public AuditScope CreateAuditScope(IAuditDbContext context, EntityFrameworkEvent efEvent)
        {
            var typeName = context.GetType().Name;
            var eventType = context.AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var auditEfEvent = new AuditEventEntityFramework();
            auditEfEvent.EntityFrameworkEvent = efEvent;
            var scope = AuditScope.Create(eventType, null, null, EventCreationPolicy.Manual, context.AuditDataProvider, auditEfEvent, 3);
            if (context.ExtraFields != null)
            {
                foreach (var field in context.ExtraFields)
                {
                    scope.SetCustomField(field.Key, field.Value);
                }
            }
            context.OnScopeCreated(scope);
            return scope;
        }

        /// <summary>
        /// Gets the modified entries to process.
        /// </summary>
#if NETCOREAPP1_0
        public List<EntityEntry> GetModifiedEntries(IAuditDbContext context)
#elif NET45
        public List<DbEntityEntry> GetModifiedEntries(IAuditDbContext context)
#endif
        {
            return context.DbContext.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged
                         && x.State != EntityState.Detached
                         && IncludeEntity(context, x, context.Mode))
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

        public string GetClientConnectionId(DbConnection connection)
        {
            // Get the connection id (returns NULL if the connection is not open)
            var sqlConnection = connection as SqlConnection;
            var connId = sqlConnection?.ClientConnectionId;
            return connId.HasValue && !connId.Equals(Guid.Empty) ? connId.Value.ToString() : null;
        }

        /// <summary>
        /// Saves the changes asynchronously.
        /// </summary>
        public async Task<int> SaveChangesAsync(IAuditDbContext context, Func<Task<int>> baseSaveChanges)
        {
            var dbContext = context.DbContext;
            if (context.AuditDisabled)
            {
                return await baseSaveChanges();
            }
            var efEvent = CreateAuditEvent(context);
            if (efEvent == null)
            {
                return await baseSaveChanges();
            }
            var scope = CreateAuditScope(context, efEvent);
            try
            {
                efEvent.Result = await baseSaveChanges();
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = ex.GetExceptionInfo();
                SaveScope(context, scope, efEvent);
                throw;
            }
            efEvent.Success = true;
            SaveScope(context, scope, efEvent);
            return efEvent.Result;
        }

        /// <summary>
        /// Saves the changes synchronously.
        /// </summary>
        public int SaveChanges(IAuditDbContext context, Func<int> baseSaveChanges)
        {
            if (context.AuditDisabled)
            {
                return baseSaveChanges();
            }
            var efEvent = CreateAuditEvent(context);
            if (efEvent == null)
            {
                return baseSaveChanges();
            }
            var scope = CreateAuditScope(context, efEvent);
            try
            {
                efEvent.Result = baseSaveChanges();
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = ex.GetExceptionInfo();
                SaveScope(context, scope, efEvent);
                throw;
            }
            efEvent.Success = true;
            SaveScope(context, scope, efEvent);
            return efEvent.Result;
        }
    }
}
