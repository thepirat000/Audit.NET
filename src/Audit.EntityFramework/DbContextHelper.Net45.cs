#if NET45
using Audit.EntityFramework.ConfigurationApi;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Helper class with common behavior for AuditDbContext and AuditIdentityDbContext
    /// </summary>
    public partial class DbContextHelper
    {
        /// <summary>
        /// Gets the entities changes for an entry entity.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="context">The audit database context.</param>
        private List<EventEntryChange> GetChanges(IAuditDbContext context, DbEntityEntry entry)
        {
            var dbContext = context.DbContext;
            var result = new List<EventEntryChange>();
            foreach (var propName in entry.CurrentValues.PropertyNames)
            {
                var prop = entry.Property(propName);
                if (prop.IsModified)
                {
                    if (IncludeProperty(context, entry, prop.Name))
                    {
                        var colName = EntityKeyHelper.Instance.GetColumnName(entry.Entity.GetType(), prop.Name, dbContext);
                        result.Add(new EventEntryChange()
                        {
                            ColumnName = colName,
                            NewValue = HasPropertyValue(context, entry, prop.Name, entry.CurrentValues[propName], out object currValue) ? currValue : entry.CurrentValues[propName],
                            OriginalValue = HasPropertyValue(context, entry, prop.Name, entry.OriginalValues[propName], out object origValue) ? origValue : entry.OriginalValues[propName]
                        });
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the column values.
        /// </summary>
        /// <param name="context">The audit db context.</param>
        /// <param name="entry">The entity entry.</param>
        private Dictionary<string, object> GetColumnValues(IAuditDbContext context, DbEntityEntry entry)
        {
            var dbContext = context.DbContext;
            var result = new Dictionary<string, object>();
            var propertyNames = entry.State != EntityState.Deleted ? entry.CurrentValues.PropertyNames : entry.OriginalValues.PropertyNames;
            foreach (var propName in propertyNames)
            {
                if (IncludeProperty(context, entry, propName))
                {
                    var colName = EntityKeyHelper.Instance.GetColumnName(entry.Entity.GetType(), propName, dbContext);
                    object value = entry.State != EntityState.Deleted
                        ? entry.CurrentValues[propName]
                        : entry.OriginalValues[propName];
                    if (HasPropertyValue(context, entry, propName, value, out object overrideValue))
                    {
                        value = overrideValue;
                    }
                    result.Add(colName, value);
                }
            }
            return result;
        }

        // Determines if a property should be included or is ignored
        private bool IncludeProperty(IAuditDbContext context, DbEntityEntry entry, string propName)
        {
            var entityType = ObjectContext.GetObjectType(entry.Entity?.GetType());
            if (entityType == null)
            {
                return true;
            }
            var ignoredProperties = EnsurePropertiesIgnoreAttrCache(entityType);
            if (ignoredProperties != null && ignoredProperties.Contains(propName))
            {
                // Property ignored by AuditIgnore attribute
                return false;
            }
            if (entityType != null && context.EntitySettings != null && context.EntitySettings.TryGetValue(entityType, out EfEntitySettings settings))
            {
                // Property ignored by configuration
                return !settings.IgnoredProperties.Contains(propName);
            }
            return true;
        }

        // Determines if a property value should be overriden with a pre-configured value
        private bool HasPropertyValue(IAuditDbContext context, DbEntityEntry entry, string propName, object currentValue, out object value)
        {
            value = null;
            var entityType = ObjectContext.GetObjectType(entry.Entity?.GetType());
            if (entityType == null)
            {
                return false;
            }
            var overrideProperties = EnsurePropertiesOverrideAttrCache(entityType);
            if (overrideProperties != null && overrideProperties.ContainsKey(propName))
            {
                // Property overriden with AuditOverride attribute
                value = overrideProperties[propName].Value;
                return true;
            }
            if (context.EntitySettings != null && context.EntitySettings.TryGetValue(entityType, out EfEntitySettings settings))
            {
                if (settings.OverrideProperties.ContainsKey(propName))
                {
                    // property overriden with a constant value
                    value = settings.OverrideProperties[propName];
                    return true;
                }
                if (settings.FormatProperties.ContainsKey(propName))
                {
                    // property overriden with a func value
                    value = settings.FormatProperties[propName].Invoke(currentValue);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the name of the table/entity.
        /// </summary>
        private EntityName GetEntityName(DbContext dbContext, object entity)
        {
            return EntityKeyHelper.Instance.GetTableName(entity.GetType(), dbContext);
        }

        /// <summary>
        /// Tries to get the current transaction identifier.
        /// </summary>
        /// <param name="dbContext">The DB Context.</param>
        /// <param name="clientConnectionId">The client ConnectionId.</param>
        private string GetCurrentTransactionId(DbContext dbContext, string clientConnectionId)
        {
            var curr = dbContext.Database.CurrentTransaction;
            if (curr == null)
            {
                return null;
            }
            // Get the transaction id
            var underlyingTran = curr.UnderlyingTransaction;
            return GetTransactionId(underlyingTran, clientConnectionId);
        }

        /// <summary>
        /// Gets the primary key values for an entity
        /// </summary>
        private static Dictionary<string, object> GetPrimaryKey(DbContext dbContext, DbEntityEntry entry)
        {
            return EntityKeyHelper.Instance.GetPrimaryKeyValues(entry.Entity, dbContext);
        }

        /// <summary>
        /// Gets the foreign keys values for an entity
        /// </summary>
        private static Dictionary<string, object> GetForeignKeys(DbContext dbContext, DbEntityEntry entry)
        {
            return EntityKeyHelper.Instance.GetForeignKeysValues(entry.Entity, dbContext);
        }

        /// <summary>
        /// Creates the Audit Event.
        /// </summary>
        public EntityFrameworkEvent CreateAuditEvent(IAuditDbContext context)
        {
            var dbContext = context.DbContext;
            var modifiedEntries = GetModifiedEntries(context);
            if (modifiedEntries.Count == 0 && !context.IncludeIndependantAssociations)
            {
                return null;
            }

            var clientConnectionId = GetClientConnectionId(dbContext.Database.Connection);
            var efEvent = new EntityFrameworkEvent()
            {
                Entries = new List<EventEntry>(),
                Database = dbContext.Database.Connection.Database,
                ConnectionId = clientConnectionId,
                TransactionId = !context.ExcludeTransactionId ? GetCurrentTransactionId(dbContext, clientConnectionId) : null,
                DbContext = dbContext,
                Associations = context.IncludeIndependantAssociations ? GetAssociationEntries(context, context.IncludeEntityObjects) : null
            };

            if (modifiedEntries.Count == 0 && efEvent.Associations == null)
            {
                return null;
            }

            foreach (var entry in modifiedEntries)
            {
                var entity = entry.Entity;
                var validationResults = context.ExcludeValidationResults ? null : entry.GetValidationResult();
                var entityName = GetEntityName(dbContext, entity);
                efEvent.Entries.Add(new EventEntry()
                {
                    Valid = validationResults?.IsValid ?? true,
                    ValidationResults = validationResults?.ValidationErrors.Select(x => x.ErrorMessage).ToList(),
                    Entity = context.IncludeEntityObjects ? entity : null,
                    Entry = entry,
                    Action = GetStateName(entry.State),
                    Changes = entry.State == EntityState.Modified ? GetChanges(context, entry) : null,
                    Table = entityName.Table,
                    Schema = entityName.Schema,
                    ColumnValues = GetColumnValues(context, entry)
                });
            }
            return efEvent;
        }

        private List<AssociationEntry> GetAssociationEntries(IAuditDbContext context, bool includeEntityObjects)
        {
            var dbContext = context.DbContext;
            var result = new List<AssociationEntry>();
            var objectContext = ((IObjectContextAdapter)dbContext).ObjectContext;
            foreach (var association in objectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Where(e => e.IsRelationship)
                                            .Concat(objectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Deleted).Where(e => e.IsRelationship)))
            {
                var key1 = association.State == EntityState.Added ? (EntityKey)association.CurrentValues[0] : (EntityKey)association.OriginalValues[0];
                var key2 = association.State == EntityState.Added ? (EntityKey)association.CurrentValues[1] : (EntityKey)association.OriginalValues[1];
                var e1 = objectContext.GetObjectByKey(key1);
                var e2 = objectContext.GetObjectByKey(key2);
                if (IncludeEntity(context, e1.GetType(), context.Mode) || IncludeEntity(context, e2.GetType(), context.Mode))
                {
                    var pk1 = EntityKeyHelper.Instance.GetPrimaryKeyValues(e1, dbContext);
                    var pk2 = EntityKeyHelper.Instance.GetPrimaryKeyValues(e2, dbContext);
                    var e1Name = GetEntityName(dbContext, e1);
                    var e2Name = GetEntityName(dbContext, e2);
                    result.Add(new AssociationEntry()
                    {
                        Action = association.State == EntityState.Added ? "Insert" : "Delete",
                        Table = association.EntitySet.Table ?? association.EntitySet.Name,
                        Records = new []
                        {
                            new AssociationEntryRecord()
                            {
                                InternalEntity = e1,
                                Table = e1Name.Table,
                                Schema = e1Name.Schema,
                                Entity = includeEntityObjects ? e1 : null,
                                PrimaryKey = pk1
                            },
                            new AssociationEntryRecord()
                            {
                                InternalEntity = e2,
                                Table = e2Name.Table,
                                Schema = e2Name.Schema,
                                Entity = includeEntityObjects ? e2 : null,
                                PrimaryKey = pk2
                            }
                        }
                    });
                }
            }
            return result.Count == 0 ? null : result;
        }
    }
}
#endif
      