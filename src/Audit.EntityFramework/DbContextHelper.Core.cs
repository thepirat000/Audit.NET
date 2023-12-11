﻿#if EF_CORE
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
                            NewValue = HasPropertyValue(context, entry, prop.Name, propEntry.CurrentValue, out var overridenCurrentValue) ? overridenCurrentValue : propEntry.CurrentValue,
                            OriginalValue = HasPropertyValue(context, entry, prop.Name, propEntry.OriginalValue, out var overridenOriginalValue) ? overridenOriginalValue : propEntry.OriginalValue
                        });
                    }
                }
            }
            
#if EF_CORE_8_OR_GREATER
            AddChangesFromComplexProperties(context, entry, entry.ComplexProperties, result);
#endif

            return result;
        }

#if EF_CORE_8_OR_GREATER
        /// <summary>
        /// Adds the change values from the complex properties recursively
        /// </summary>
        private void AddChangesFromComplexProperties(IAuditDbContext context, EntityEntry entry, IEnumerable<ComplexPropertyEntry> complexProperties, List<EventEntryChange> result)
        {
            foreach (var complexEntry in complexProperties)
            {
                // Process the primitive properties
                foreach (var propEntry in complexEntry.Properties)
                {
                    if (propEntry.IsModified && IncludeProperty(context, complexEntry.Metadata.ClrType, propEntry.Metadata.Name))
                    {
                        result.Add(new EventEntryChange()
                        {
                            ColumnName = GetColumnName(propEntry.Metadata),
                            NewValue = HasPropertyValue(context, entry, complexEntry.Metadata.ClrType, propEntry.Metadata.Name, propEntry.CurrentValue, out var overridenCurrentValue) ? overridenCurrentValue : propEntry.CurrentValue,
                            OriginalValue = HasPropertyValue(context, entry, complexEntry.Metadata.ClrType, propEntry.Metadata.Name, propEntry.OriginalValue, out var overridenOriginalValue) ? overridenOriginalValue : propEntry.OriginalValue
                        });
                    }
                }

                // Recursively process complex properties
                AddChangesFromComplexProperties(context, entry, complexEntry.ComplexProperties, result);
            }
        }
#endif

        /// <summary>
        /// Gets the column values for an insert/delete operation.
        /// </summary>
        private Dictionary<string, object> GetColumnValues(IAuditDbContext context, EntityEntry entry)
        {
            var result = new Dictionary<string, object>();
            var props = entry.Metadata.GetProperties();
            foreach (var prop in props)
            {
                var propEntry = entry.Property(prop.Name);
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

#if EF_CORE_8_OR_GREATER
            AddColumnValuesFromComplexProperties(context, entry, entry.ComplexProperties, result);
#endif
            return result;
        }
        
#if EF_CORE_8_OR_GREATER
        /// <summary>
        /// Adds the column values from the complex properties recursively
        /// </summary>
        private void AddColumnValuesFromComplexProperties(IAuditDbContext context, EntityEntry entry, IEnumerable<ComplexPropertyEntry> complexProperties, Dictionary<string, object> result)
        {
            foreach (var complexEntry in complexProperties)
            {
                // Process the primitive properties
                foreach (var propEntry in complexEntry.Properties)
                {
                    if (IncludeProperty(context, complexEntry.Metadata.ClrType, propEntry.Metadata.Name))
                    {
                        var value = propEntry.CurrentValue;
                        if (HasPropertyValue(context, entry, complexEntry.Metadata.ClrType, propEntry.Metadata.Name, value, out object overrideValue))
                        {
                            value = overrideValue;
                        }

                        var columnName = GetColumnName(propEntry.Metadata);
                        result.Add(columnName, value);
                    }
                }

                // Recursively process complex properties
                AddColumnValuesFromComplexProperties(context, entry, complexEntry.ComplexProperties, result);
            }
        }
#endif

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        internal static string GetColumnName(IProperty prop)
        {
#if EF_CORE_8_OR_GREATER
            var storeObjectIdentifier = StoreObjectIdentifier.Create(prop.DeclaringType, StoreObjectType.Table);
            return storeObjectIdentifier.HasValue
                ? (prop.GetColumnName(storeObjectIdentifier.Value) ?? prop.GetDefaultColumnName())
                : prop.GetDefaultColumnName();
#elif EF_CORE_7_OR_GREATER
            var storeObjectIdentifier = StoreObjectIdentifier.Create(prop.DeclaringEntityType, StoreObjectType.Table);
            return storeObjectIdentifier.HasValue
                ? (prop.GetColumnName(storeObjectIdentifier.Value) ?? prop.GetDefaultColumnName())
                : prop.GetDefaultColumnName();
#elif EF_CORE_5_OR_GREATER
            var storeObjectIdentifier = StoreObjectIdentifier.Create(prop.DeclaringEntityType, StoreObjectType.Table);
            return storeObjectIdentifier.HasValue 
                ? prop.GetColumnName(storeObjectIdentifier.Value)
                : prop.GetDefaultColumnBaseName();
#else
            return prop.Relational().ColumnName ?? prop.Name;
#endif
        }

        // Determines if the property should be included or is ignored
        private bool IncludeProperty(IAuditDbContext context, EntityEntry entry, string propName)
        {
            var entityType = GetDefiningType(context.DbContext, entry)?.ClrType;
            if (entityType == null)
            {
                return true;
            }

            return IncludeProperty(context, entityType, propName);
        }

        // Determines if the property should be included or is ignored
        private bool IncludeProperty(IAuditDbContext context, Type entityType, string propName)
        {
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

            return HasPropertyValue(context, entry, entityType, propName, currentValue, out value);
        }

        private bool HasPropertyValue(IAuditDbContext context, EntityEntry entry, Type entityType, string propName, object currentValue, out object value)
        {
            value = null;
            var overrideProperties = EnsurePropertiesOverrideAttrCache(entityType);
            if (overrideProperties != null && overrideProperties.TryGetValue(propName, out var property))
            {
                // Property overriden with AuditOverride attribute
                value = property.Value;
                return true;
            }
            if (context.EntitySettings != null && context.EntitySettings.TryGetValue(entityType, out EfEntitySettings settings))
            {
                if (settings.OverrideProperties.ContainsKey(propName))
                {
                    // property overriden with a func value
                    value = settings.OverrideProperties[propName].Invoke(entry);
                    return true;
                }
                if (settings.FormatProperties.ContainsKey(propName))
                {
                    // property formatted
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
#if EF_CORE_5_OR_GREATER
            result.Table = definingType.GetTableName();
            result.Schema = definingType.GetSchema();
#else
            var relational = definingType.Relational();
            result.Table = relational.TableName ?? definingType.Name;
            result.Schema = relational.Schema;
#endif
            return result;
        }

#if EF_CORE_6_OR_GREATER
        private static IReadOnlyEntityType GetDefiningType(DbContext dbContext, EntityEntry entry)
#else
        private static IEntityType GetDefiningType(DbContext dbContext, EntityEntry entry)
#endif
        {
#if EF_CORE_5_OR_GREATER
            var definingType = entry.Metadata.FindOwnership()?.DeclaringEntityType ?? dbContext.Model.FindEntityType(entry.Metadata.Name);
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
#if EF_CORE_5_OR_GREATER
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
#if EF_CORE_5_OR_GREATER
                ContextId = dbContext.ContextId.ToString(),
#endif
                AmbientTransactionId = !context.ExcludeTransactionId ? GetAmbientTransactionId() : null,
                TransactionId = !context.ExcludeTransactionId ? GetCurrentTransactionId(dbContext, clientConnectionId) : null,
                DbContext = dbContext
            };
            foreach (var entry in modifiedEntries)
            {
                var entity = entry.Entity;
                var validationResults = context.ExcludeValidationResults ? null : DbContextHelper.GetValidationResults(entity);

                var entityName = GetEntityName(dbContext, entry);

                if (context.ReloadDatabaseValues && (entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
                {
                    var dbValues = entry.GetDatabaseValues();
                    if (dbValues != null)
                    {
                        entry.OriginalValues.SetValues(dbValues);
                    }
                }

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
#if EF_CORE_5_OR_GREATER
                    Name = entry.Metadata.DisplayName(),
#endif
                    ColumnValues = GetColumnValues(context, entry)
                });
            }
            return efEvent;
        }

        /// <summary>
        /// Updates column values and primary keys on the Audit Event after the EF save operation completes.
        /// </summary>
        public void UpdateAuditEvent(EntityFrameworkEvent efEvent, IAuditDbContext context)
        {
            // Update PK and FK
            foreach (var efEntry in efEvent.Entries)
            {
                var entry = efEntry.Entry;
                efEntry.PrimaryKey = GetPrimaryKey(context.DbContext, entry);
                foreach (var pk in efEntry.PrimaryKey)
                {
                    if (efEntry.ColumnValues.ContainsKey(pk.Key))
                    {
                        efEntry.ColumnValues[pk.Key] = pk.Value;
                    }
                }
                var fks = GetForeignKeys(context.DbContext, entry);
                foreach (var fk in fks)
                {
                    if (efEntry.ColumnValues.ContainsKey(fk.Key))
                    {
                        efEntry.ColumnValues[fk.Key] = fk.Value;
                    }

                    var change = efEntry.Changes?.FirstOrDefault(e => e.ColumnName == fk.Key);
                    if (change != null)
                    {
                        change.NewValue = fk.Value;
                    }
                }
            }
            // Update ConnectionId
            var clientConnectionId = TryGetClientConnectionId(context.DbContext);
            if (clientConnectionId != null)
            {
                efEvent.ConnectionId = clientConnectionId;
            }
        }

        private string GetAmbientTransactionId()
        {
#if EF_CORE_5_OR_GREATER
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