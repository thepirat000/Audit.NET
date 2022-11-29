#if EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#else
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Audit.EntityFramework
{
    public partial class DbContextHelper
    {
        // Entities Include/Ignore attributes cache
        private static readonly ConcurrentDictionary<Type, bool?> EntitiesIncludeIgnoreAttrCache = new ConcurrentDictionary<Type, bool?>();
        // Ignored properties per entity type attribute cache
        private static readonly ConcurrentDictionary<Type, HashSet<string>> PropertiesIgnoreAttrCache = new ConcurrentDictionary<Type, HashSet<string>>();
        // Overriden properties per entity type attribute cache
        private static readonly ConcurrentDictionary<Type, Dictionary<string, AuditOverrideAttribute>> PropertiesOverrideAttrCache = new ConcurrentDictionary<Type, Dictionary<string, AuditOverrideAttribute>>();

        // AuditDbContext Attribute cache
        private static readonly ConcurrentDictionary<Type, AuditDbContextAttribute> _auditAttributeCache = new ConcurrentDictionary<Type, AuditDbContextAttribute>();

        /// <summary>
        /// Sets the configuration values from attribute, local and global
        /// </summary>
        public void SetConfig(IAuditDbContext context)
        {
            var type = context.DbContext.GetType();
            if (!_auditAttributeCache.ContainsKey(type))
            {
                _auditAttributeCache[type] = type.GetTypeInfo().GetCustomAttribute(typeof(AuditDbContextAttribute)) as AuditDbContextAttribute;
            }
            var attrConfig = _auditAttributeCache[type]?.InternalConfig;
            var localConfig = Audit.EntityFramework.Configuration.GetConfigForType(type);
            var globalConfig = Audit.EntityFramework.Configuration.GetConfigForType(typeof(AuditDbContext));

            context.Mode = attrConfig?.Mode ?? localConfig?.Mode ?? globalConfig?.Mode ?? AuditOptionMode.OptOut;
            context.IncludeEntityObjects = attrConfig?.IncludeEntityObjects ?? localConfig?.IncludeEntityObjects ?? globalConfig?.IncludeEntityObjects ?? false;
            context.ExcludeValidationResults = attrConfig?.ExcludeValidationResults ?? localConfig?.ExcludeValidationResults ?? globalConfig?.ExcludeValidationResults ?? false;
            context.AuditEventType = attrConfig?.AuditEventType ?? localConfig?.AuditEventType ?? globalConfig?.AuditEventType;
            context.EntitySettings = MergeEntitySettings(attrConfig?.EntitySettings, localConfig?.EntitySettings, globalConfig?.EntitySettings);
            context.ExcludeTransactionId = attrConfig?.ExcludeTransactionId ?? localConfig?.ExcludeTransactionId ?? globalConfig?.ExcludeTransactionId ?? false;
            context.EarlySavingAudit = attrConfig?.EarlySavingAudit ?? localConfig?.EarlySavingAudit ?? globalConfig?.EarlySavingAudit ?? false;
#if EF_FULL
            context.IncludeIndependantAssociations = attrConfig?.IncludeIndependantAssociations ?? localConfig?.IncludeIndependantAssociations ?? globalConfig?.IncludeIndependantAssociations ?? false;
#endif
        }

        internal Dictionary<Type, EfEntitySettings> MergeEntitySettings(Dictionary<Type, EfEntitySettings> attr, Dictionary<Type, EfEntitySettings> local, Dictionary<Type, EfEntitySettings> global)
        {
            var settings = new List<Dictionary<Type, EfEntitySettings>>();
            if (global != null && global.Count > 0)
            {
                settings.Add(global);
            }
            if (local != null && local.Count > 0)
            {
                settings.Add(local);
            }
            if (attr != null && attr.Count > 0)
            {
                settings.Add(attr);
            }
            if (settings.Count == 0)
            {
                return null;
            }
            var merged = new Dictionary<Type, EfEntitySettings>(settings[0]);
            for (int i = 1; i < settings.Count; i++)
            {
                foreach(var kvp in settings[i])
                {
                    if (!merged.ContainsKey(kvp.Key))
                    {
                        merged[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        foreach (var ip in kvp.Value.IgnoredProperties)
                        {
                            merged[kvp.Key].IgnoredProperties.Add(ip);
                        }
                        foreach (var op in kvp.Value.OverrideProperties)
                        {
                            merged[kvp.Key].OverrideProperties[op.Key] = op.Value;
                        }
                    }
                }
            }
            return merged;
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
        public void SaveScope(IAuditDbContext context, IAuditScope scope, EntityFrameworkEvent @event)
        {
            UpdateAuditEvent(@event, context);
            (scope.Event as AuditEventEntityFramework).EntityFrameworkEvent = @event;
            context.OnScopeSaving(scope);
            scope.Save();
            context.OnScopeSaved(scope);
        }

        /// <summary>
        /// Saves the scope asynchronously.
        /// </summary>
        public async Task SaveScopeAsync(IAuditDbContext context, IAuditScope scope, EntityFrameworkEvent @event)
        {
            UpdateAuditEvent(@event, context);
            (scope.Event as AuditEventEntityFramework).EntityFrameworkEvent = @event;
            context.OnScopeSaving(scope);
            await scope.SaveAsync();
            context.OnScopeSaved(scope);
        }

        // Determines whether to include the entity on the audit log or not
        private bool IncludeEntity(IAuditDbContext context, object entity, AuditOptionMode mode)
        {
            var type = entity.GetType();
#if EF_FULL
            type = ObjectContext.GetObjectType(type);
#else
            if (type.FullName.StartsWith("Castle.Proxies."))
            {
                type = type.GetTypeInfo().BaseType;
            }
#endif
            bool ? result = EnsureEntitiesIncludeIgnoreAttrCache(type); //true:excluded false=ignored null=unknown
            if (result == null)
            {
                // No static attributes, check the filters
                var localConfig = EntityFramework.Configuration.GetConfigForType(context.DbContext.GetType());
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
                var includeAttr = type.GetTypeInfo().GetCustomAttribute(typeof(AuditIncludeAttribute), true);
                if (includeAttr != null)
                {
                    EntitiesIncludeIgnoreAttrCache[type] = true; // Type Included by IncludeAttribute
                }
                else if (type.GetTypeInfo().GetCustomAttribute(typeof(AuditIgnoreAttribute), true) != null)
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

        private HashSet<string> EnsurePropertiesIgnoreAttrCache(Type type)
        {
            if (!PropertiesIgnoreAttrCache.ContainsKey(type))
            {
                var ignoredProps = new HashSet<string>();
                foreach(var prop in type.GetTypeInfo().GetProperties())
                {
                    var ignoreAttr = prop.GetCustomAttribute(typeof(AuditIgnoreAttribute), true);
                    if (ignoreAttr != null)
                    {
                        ignoredProps.Add(prop.Name);
                    }
                }
                if (ignoredProps.Count > 0)
                {
                    PropertiesIgnoreAttrCache[type] = ignoredProps;
                }
                else
                {
                    PropertiesIgnoreAttrCache[type] = null;
                }
            }
            return PropertiesIgnoreAttrCache[type];
        }

        private Dictionary<string, AuditOverrideAttribute> EnsurePropertiesOverrideAttrCache(Type type)
        {
            if (!PropertiesOverrideAttrCache.ContainsKey(type))
            {
                var overrideProps = new Dictionary<string, AuditOverrideAttribute>();
                foreach (var prop in type.GetTypeInfo().GetProperties())
                {
                    var overrideAttr = prop.GetCustomAttribute<AuditOverrideAttribute>(true);
                    if (overrideAttr != null)
                    {
                        overrideProps[prop.Name] = overrideAttr;
                    }
                }
                if (overrideProps.Count > 0)
                {
                    PropertiesOverrideAttrCache[type] = overrideProps;
                }
                else
                {
                    PropertiesOverrideAttrCache[type] = null;
                }
            }
            return PropertiesOverrideAttrCache[type];
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
        public IAuditScope CreateAuditScope(IAuditDbContext context, EntityFrameworkEvent efEvent)
        {
            var typeName = context.DbContext.GetType().Name;
            var eventType = context.AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var auditEfEvent = new AuditEventEntityFramework
            {
                EntityFrameworkEvent = efEvent
            };
            if (context.ExtraFields != null && context.ExtraFields.Count > 0)
            {
                auditEfEvent.CustomFields = new Dictionary<string, object>(context.ExtraFields);
            }
            var factory = context.AuditScopeFactory ?? Core.Configuration.AuditScopeFactory;
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.Manual,
                DataProvider = context.AuditDataProvider,
                AuditEvent = auditEfEvent,
#if NET45 || NET461 || NETSTANDARD1_5 || NETSTANDARD2_0
                SkipExtraFrames = 4
#else
                SkipExtraFrames = 5
#endif
            };
            var scope = factory.Create(options);
            context.OnScopeCreated(scope);
            return scope;
        }

        /// <summary>
        /// Creates the Audit scope asynchronously.
        /// </summary>
        public async Task<IAuditScope> CreateAuditScopeAsync(IAuditDbContext context, EntityFrameworkEvent efEvent)
        {
            var typeName = context.DbContext.GetType().Name;
            var eventType = context.AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var auditEfEvent = new AuditEventEntityFramework
            {
                EntityFrameworkEvent = efEvent
            };
            if (context.ExtraFields != null && context.ExtraFields.Count > 0)
            {
                auditEfEvent.CustomFields = new Dictionary<string, object>(context.ExtraFields);
            }
            var factory = context.AuditScopeFactory ?? Core.Configuration.AuditScopeFactory;
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.Manual,
                DataProvider = context.AuditDataProvider,
                AuditEvent = auditEfEvent,
                SkipExtraFrames = 3
            };
            var scope = await factory.CreateAsync(options);
            context.OnScopeCreated(scope);
            return scope;
        }

        /// <summary>
        /// Gets the modified entries to process.
        /// </summary>
#if EF_CORE
        public List<EntityEntry> GetModifiedEntries(IAuditDbContext context)
#else
        public List<DbEntityEntry> GetModifiedEntries(IAuditDbContext context)
#endif
        {
            return context.DbContext.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged
                         && x.State != EntityState.Detached
                         && IncludeEntity(context, x.Entity, context.Mode))
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
            var tranId = propTranId?.GetValue(intTran);
            return string.Format("{0}_{1}", clientConnectionId, tranId ?? 0);
        }

        public string GetClientConnectionId(DbContext dbContext)
        {
#if EF_CORE
            var connection = IsRelational(dbContext) ? dbContext.Database.GetDbConnection() : null;
#else
            var connection = dbContext.Database.Connection;
#endif
            return GetClientConnectionId(connection);
        }

        public string GetClientConnectionId(DbConnection dbConnection)
        {
            if (dbConnection == null)
            {
                return null;
            }
#if EF_CORE && (NETSTANDARD1_5 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET472 || NET5_0_OR_GREATER)
            try
            {
                var connId = ((dbConnection as dynamic).ClientConnectionId) as Guid?;
                return connId.HasValue && !connId.Value.Equals(Guid.Empty) ? connId.Value.ToString() : null;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return null;
            }
#else
            // Get the connection id (returns NULL if the connection is not open)
            var sqlConnection = dbConnection as System.Data.SqlClient.SqlConnection;
            var connId = sqlConnection?.ClientConnectionId;
            return connId.HasValue && !connId.Value.Equals(Guid.Empty) ? connId.Value.ToString() : null;
#endif
        }

        /// <summary>
        /// Saves the changes asynchronously.
        /// </summary>
        public async Task<int> SaveChangesAsync(IAuditDbContext context, Func<Task<int>> baseSaveChanges)
        {
            return (await SaveChangesGetAuditAsync(context, baseSaveChanges)).Result;
        }

        /// <summary>
        /// Saves the changes asynchronously and returns the audit event generated.
        /// </summary>
        public async Task<EntityFrameworkEvent> SaveChangesGetAuditAsync(IAuditDbContext context, Func<Task<int>> baseSaveChanges)
        {
            if (context.AuditDisabled)
            {
                return new EntityFrameworkEvent() { Result = await baseSaveChanges() };
            }
            var efEvent = CreateAuditEvent(context);
            if (efEvent == null)
            {
                return new EntityFrameworkEvent() { Result = await baseSaveChanges() };
            }
            var scope = await CreateAuditScopeAsync(context, efEvent);
            if (context.EarlySavingAudit)
            {
                await SaveScopeAsync(context, scope, efEvent);
            }
            try
            {
                efEvent.Result = await baseSaveChanges();
            }
            catch (Exception ex)
            {
                efEvent.Success = false;
                efEvent.ErrorMessage = ex.GetExceptionInfo();
                await SaveScopeAsync(context, scope, efEvent);
                throw;
            }
            efEvent.Success = true;
            await SaveScopeAsync(context, scope, efEvent);
            return efEvent;
        }

        /// <summary>
        /// Saves the changes synchronously.
        /// </summary>
        public int SaveChanges(IAuditDbContext context, Func<int> baseSaveChanges)
        {
            return SaveChangesGetAudit(context, baseSaveChanges).Result;
        }

        /// <summary>
        /// Saves the changes and returns the audit event generated.
        /// </summary>
        public EntityFrameworkEvent SaveChangesGetAudit(IAuditDbContext context, Func<int> baseSaveChanges)
        {
            if (context.AuditDisabled)
            {
                return new EntityFrameworkEvent() { Result = baseSaveChanges() };
            }
            var efEvent = CreateAuditEvent(context);
            if (efEvent == null)
            {
                return new EntityFrameworkEvent() { Result = baseSaveChanges() };
            }
            var scope = CreateAuditScope(context, efEvent);
            if (context.EarlySavingAudit)
            {
                SaveScope(context, scope, efEvent);
            }
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
            return efEvent;
        }

        public IAuditScope BeginSaveChanges(IAuditDbContext context)
        {
            if (context.AuditDisabled)
            {
                return null;
            }
            var efEvent = CreateAuditEvent(context);
            if (efEvent == null)
            {
                return null;
            }
            var scope = CreateAuditScope(context, efEvent);
            if (context.EarlySavingAudit)
            {
                SaveScope(context, scope, efEvent);
            }
            return scope;
        }

        public async Task<IAuditScope> BeginSaveChangesAsync(IAuditDbContext context)
        {
            if (context.AuditDisabled)
            {
                return null;
            }
            var efEvent = CreateAuditEvent(context);
            if (efEvent == null)
            {
                return null;
            }
            var scope = await CreateAuditScopeAsync(context, efEvent);
            if (context.EarlySavingAudit)
            {
                await SaveScopeAsync(context, scope, efEvent);
            }
            return scope;
        }

        public void EndSaveChanges(IAuditDbContext context, IAuditScope scope, int result, Exception exception = null)
        {
            var efEvent = scope.GetEntityFrameworkEvent();
            if (efEvent == null)
            {
                return;
            }
            efEvent.Success = exception == null;
            efEvent.Result = result;
            efEvent.ErrorMessage = exception?.GetExceptionInfo();
            SaveScope(context, scope, efEvent);
        }

        public async Task EndSaveChangesAsync(IAuditDbContext context, IAuditScope scope, int result, Exception exception = null)
        {
            var efEvent = scope.GetEntityFrameworkEvent();
            if (efEvent == null)
            {
                return;
            }
            efEvent.Success = exception == null;
            efEvent.Result = result;
            efEvent.ErrorMessage = exception?.GetExceptionInfo();
            await SaveScopeAsync(context, scope, efEvent);
        }
    }
}
