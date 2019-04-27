#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
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
        private static ConcurrentDictionary<Type, AuditDbContextAttribute> _auditAttributeCache = new ConcurrentDictionary<Type, AuditDbContextAttribute>();

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
            context.EntitySettings = MergeEntitySettings(attrConfig?.EntitySettings, localConfig?.EntitySettings, globalConfig?.EntitySettings);
            context.ExcludeTransactionId = attrConfig?.ExcludeTransactionId ?? localConfig?.ExcludeTransactionId ?? globalConfig?.ExcludeTransactionId ?? false;
#if NET45
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
        public void SaveScope(IAuditDbContext context, AuditScope scope, EntityFrameworkEvent @event)
        {
            UpdateAuditEvent(@event, context);
            (scope.Event as AuditEventEntityFramework).EntityFrameworkEvent = @event;
            context.OnScopeSaving(scope);
            scope.Save();
        }

        /// <summary>
        /// Saves the scope asynchronously.
        /// </summary>
        public async Task SaveScopeAsync(IAuditDbContext context, AuditScope scope, EntityFrameworkEvent @event)
        {
            UpdateAuditEvent(@event, context);
            (scope.Event as AuditEventEntityFramework).EntityFrameworkEvent = @event;
            context.OnScopeSaving(scope);
            await scope.SaveAsync();
        }

        /// <summary>
        /// Updates column values and primary keys on the Audit Event after the EF save operation completes.
        /// </summary>
        public void UpdateAuditEvent(EntityFrameworkEvent efEvent, IAuditDbContext context)
        {
            foreach (var efEntry in efEvent.Entries)
            {
                var entry = efEntry.Entry;
                efEntry.PrimaryKey = GetPrimaryKey(context.DbContext, entry);
                foreach(var pk in efEntry.PrimaryKey)
                {
                    if (efEntry.ColumnValues.ContainsKey(pk.Key))
                    {
                        efEntry.ColumnValues[pk.Key] = pk.Value;
                    }
                }
                var fks = GetForeignKeys(context.DbContext, entry);
                foreach (var fk in fks)
                {
#if NET45
                    // When deleting an entity, sometimes the foreign keys are set to NULL by EF. This only happens on EF6.
                    if (fk.Value == null)
                    {
                        continue;
                    }
#endif
                    if (efEntry.ColumnValues.ContainsKey(fk.Key))
                    {
                        efEntry.ColumnValues[fk.Key] = fk.Value;
                    }
                }
            }
#if NET45
            if (efEvent.Associations != null)
            {
                foreach (var association in efEvent.Associations)
                {
                    var e1 = association.Records[0].InternalEntity;
                    var e2 = association.Records[1].InternalEntity;
                    association.Records[0].PrimaryKey = EntityKeyHelper.Instance.GetPrimaryKeyValues(e1, context.DbContext);
                    association.Records[1].PrimaryKey = EntityKeyHelper.Instance.GetPrimaryKeyValues(e2, context.DbContext);
                }
            }
#endif
        }

        // Determines whether to include the entity on the audit log or not
        private bool IncludeEntity(IAuditDbContext context, object entity, AuditOptionMode mode)
        {
            var type = entity.GetType();
#if NET45
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
        public AuditScope CreateAuditScope(IAuditDbContext context, EntityFrameworkEvent efEvent)
        {
            var typeName = context.GetType().Name;
            var eventType = context.AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var auditEfEvent = new AuditEventEntityFramework
            {
                EntityFrameworkEvent = efEvent
            };
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
        /// Creates the Audit scope asynchronously.
        /// </summary>
        public async Task<AuditScope> CreateAuditScopeAsync(IAuditDbContext context, EntityFrameworkEvent efEvent)
        {
            var typeName = context.GetType().Name;
            var eventType = context.AuditEventType?.Replace("{context}", typeName).Replace("{database}", efEvent.Database) ?? typeName;
            var auditEfEvent = new AuditEventEntityFramework
            {
                EntityFrameworkEvent = efEvent
            };
            var scope = await AuditScope.CreateAsync(eventType, null, null, EventCreationPolicy.Manual, context.AuditDataProvider, auditEfEvent, 3);
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
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
        public List<EntityEntry> GetModifiedEntries(IAuditDbContext context)
#elif NET45
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
            var tranId = (int)(long)propTranId?.GetValue(intTran);
            return string.Format("{0}_{1}", clientConnectionId, tranId);
        }

        public string GetClientConnectionId(DbConnection connection)
        {
            if (connection == null)
            {
                return null;
            }
#if NETSTANDARD1_5 || NETSTANDARD2_0
            try
            {
                var connId = ((connection as dynamic).ClientConnectionId) as Guid?;
                return connId.HasValue && !connId.Value.Equals(Guid.Empty) ? connId.Value.ToString() : null;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                return null;
            }
#else
            // Get the connection id (returns NULL if the connection is not open)
            var sqlConnection = connection as System.Data.SqlClient.SqlConnection;
            var connId = sqlConnection?.ClientConnectionId;
            return connId.HasValue && !connId.Value.Equals(Guid.Empty) ? connId.Value.ToString() : null;
#endif
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
            var scope = await CreateAuditScopeAsync(context, efEvent);
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
