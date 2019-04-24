#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
using Audit.EntityFramework.ConfigurationApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Audit.EntityFramework
{
    public partial class DbContextHelper
    {
        /// <summary>
        /// Gets the entities changes for this entry.
        /// </summary>
        /// <param name="context">The audit db context.</param>
        /// <param name="entry">The entry.</param>
        private List<EventEntryChange> GetChanges(IAuditDbContext context, EntityEntry entry)
        {
            var result = new List<EventEntryChange>();
            var props = entry.Metadata.GetProperties();
            foreach (var prop in props)
            {
                PropertyEntry propEntry = entry.Property(prop.Name);
                if (propEntry.IsModified)
                {
                    if (IncludeProperty(context, entry, prop.Name))
                    {
                        result.Add(new EventEntryChange()
                        {
                            ColumnName = GetColumnName(prop),
                            NewValue = HasPropertyValue(context, entry, prop.Name, propEntry.CurrentValue, out object currValue) ? currValue : propEntry.CurrentValue,
                            OriginalValue = HasPropertyValue(context, entry, prop.Name, propEntry.OriginalValue, out object origValue) ? origValue : propEntry.OriginalValue
                        });
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        private static string GetColumnName(IProperty prop)
        {
            return prop.Relational().ColumnName ?? prop.Name;
        }

        /// <summary>
        /// Gets the column values for an insert/delete operation.
        /// </summary>
        /// <param name="context">The audit Db Context.</param>
        /// <param name="entry">The entity entry.</param>
        private Dictionary<string, object> GetColumnValues(IAuditDbContext context, EntityEntry entry)
        {
            var dbContext = context.DbContext;
            var result = new Dictionary<string, object>();
            var props = entry.Metadata.GetProperties();
            foreach (var prop in props)
            {
                PropertyEntry propEntry = entry.Property(prop.Name);
                if (IncludeProperty(context, entry, prop.Name))
                {
                    object value = entry.State != EntityState.Deleted ? propEntry.CurrentValue : propEntry.OriginalValue;
                    if (HasPropertyValue(context, entry, prop.Name, value, out object overrideValue))
                    {
                        value = overrideValue;
                    }
                    result.Add(GetColumnName(prop), value);
                }
            }
            return result;
        }

        // Determines if the property should be included or is ignored
        private bool IncludeProperty(IAuditDbContext context, EntityEntry entry, string propName)
        {
            var entityType = GetDefiningType(context.DbContext, entry)?.ClrType;
            if (entityType == null)
            {
                return true;
            }
            var ignoredProperties = EnsurePropertiesIgnoreAttrCache(entityType); 
            if (ignoredProperties != null && ignoredProperties.Contains(propName))
            {
                // Property marked with AuditIgnore attribute
                return false;
            }
            if (context.EntitySettings != null && context.EntitySettings.TryGetValue(entityType, out EfEntitySettings settings))
            {
                // Check if its ignored from the configuration 
                return !settings.IgnoredProperties.Contains(propName);
            }
            return true;
        }

        // Determines if a property value should be overriden with a pre-configured value
        private bool HasPropertyValue(IAuditDbContext context, EntityEntry entry, string propName, object currentValue, out object value)
        {
            value = null;
            var entityType = GetDefiningType(context.DbContext, entry)?.ClrType;
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
        /// Gets the name of the entity.
        /// </summary>
        private static EntityName GetEntityName(DbContext dbContext, EntityEntry entry)
        {
            var result = new EntityName();
            var definingType = GetDefiningType(dbContext, entry);
            if (definingType == null)
            {
                return result;
            }
            var relational = definingType.Relational();
            result.Table = relational.TableName ?? definingType.Name;
            result.Schema = relational.Schema;
            return result;
        }


        private static IEntityType GetDefiningType(DbContext dbContext, EntityEntry entry)
        {
#if NETSTANDARD2_0 || NET461
            IEntityType definingType = entry.Metadata.DefiningEntityType ?? dbContext.Model.FindEntityType(entry.Metadata.ClrType);
#else
            IEntityType definingType = dbContext.Model.FindEntityType(entry.Entity.GetType());
#endif
            return definingType;
        }

        /// <summary>
        /// Gets the foreign key values for an entity
        /// </summary>
        private static Dictionary<string, object> GetForeignKeys(DbContext dbContext, EntityEntry entry)
        {
            var result = new Dictionary<string, object>();
            var foreignKeys = entry.Metadata.GetForeignKeys();
            if (foreignKeys != null)
            {
#if NETSTANDARD2_0
                //Filter ownership relations as they are not foreign keys
                foreignKeys = foreignKeys.Where(fk => !fk.IsOwnership);
#endif
                foreach (var fk in foreignKeys)
                {
                    foreach (var prop in fk.Properties)
                    {
                        result[GetColumnName(prop)] = entry.Property(prop.Name).CurrentValue;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the primary key values for an entity
        /// </summary>
        private static Dictionary<string, object> GetPrimaryKey(DbContext dbContext, EntityEntry entry)
        {
            var result = new Dictionary<string, object>();
            foreach(var prop in entry.Properties.Where(p => p.Metadata.IsPrimaryKey()))
            {
                result.Add(GetColumnName(prop.Metadata), prop.CurrentValue); 
            }
            return result;
        }

        /// <summary>
        /// Creates the Audit Event.
        /// </summary>
        public EntityFrameworkEvent CreateAuditEvent(IAuditDbContext context)
        {
            var dbContext = context.DbContext;
            var modifiedEntries = GetModifiedEntries(context);
            if (modifiedEntries.Count == 0)
            {
                return null;
            }
            var dbConnection = IsRelational(dbContext) ? dbContext.Database.GetDbConnection() : null;
            var clientConnectionId = GetClientConnectionId(dbConnection);
            var efEvent = new EntityFrameworkEvent()
            {
                Entries = new List<EventEntry>(),
                Database = dbConnection?.Database,
                ConnectionId = clientConnectionId,
                AmbientTransactionId = GetAmbientTransactionId(),
                TransactionId = GetCurrentTransactionId(dbContext, clientConnectionId),
                DbContext = dbContext
            };
            foreach (var entry in modifiedEntries)
            {
                var entity = entry.Entity;
                var validationResults = DbContextHelper.GetValidationResults(entity);
                var entityName = GetEntityName(dbContext, entry);
                efEvent.Entries.Add(new EventEntry()
                {
                    Valid = validationResults == null,
                    ValidationResults = validationResults?.Select(x => x.ErrorMessage).ToList(),
                    Entity = context.IncludeEntityObjects ? entity : null,
                    Entry = entry,
                    Action = DbContextHelper.GetStateName(entry.State),
                    Changes = entry.State == EntityState.Modified ? GetChanges(context, entry) : null,
                    Table = entityName.Table,
                    Schema = entityName.Schema,
                    ColumnValues = GetColumnValues(context, entry)
                });
            }
            return efEvent;
        }

        private string GetAmbientTransactionId()
        {
#if NETSTANDARD2_0 || NET461
            var tranInfo = System.Transactions.Transaction.Current?.TransactionInformation;
            if (tranInfo != null)
            {
                return tranInfo.DistributedIdentifier != Guid.Empty ? tranInfo.DistributedIdentifier.ToString() : tranInfo.LocalIdentifier;
            }
            return null;
#else
            return null;
#endif
        }

        /// <summary>
        /// Tries to get the current transaction identifier.
        /// </summary>
        /// <param name="dbContext">The DB context.</param>
        /// <param name="clientConnectionId">The client ConnectionId.</param>
        private string GetCurrentTransactionId(DbContext dbContext, string clientConnectionId)
        {
            if (clientConnectionId == null)
            {
                return null;
            }
            var dbtxmgr = dbContext.GetInfrastructure().GetService<IDbContextTransactionManager>();
            var relcon = dbtxmgr as IRelationalConnection;
            var dbtx = relcon.CurrentTransaction;
            var tx = dbtx?.GetDbTransaction();
            if (tx == null)
            {
                return null;
            }
            return GetTransactionId(tx, clientConnectionId);
        }

        private bool IsRelational(DbContext dbContext)
        {
            var provider = (IInfrastructure<IServiceProvider>)dbContext.Database;
            var relationalConnection = provider.Instance.GetService<IRelationalConnection>();
            return relationalConnection != null;
        }

    }
}
#endif