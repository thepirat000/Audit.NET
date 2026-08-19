#if EF_CORE
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
using System.Threading;
using System.Threading.Tasks;

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
                            ColumnName = GetColumnName(prop, entry.Metadata),
                            NewValue = HasPropertyValue(context, entry, prop.Name, propEntry.CurrentValue, out var overridenCurrentValue) ? overridenCurrentValue : propEntry.CurrentValue,
                            OriginalValue = HasPropertyValue(context, entry, prop.Name, propEntry.OriginalValue, out var overridenOriginalValue) ? overridenOriginalValue : propEntry.OriginalValue
                        });
                    }
                }
            }
            
#if EF_CORE_8_OR_GREATER
            AddChangesFromComplexProperties(context, entry, entry.ComplexProperties, result);
#endif
#if EF_CORE_10_OR_GREATER
            AddChangesFromComplexCollections(context, entry, entry.ComplexCollections, result);
#endif

            return result;
        }

        private Dictionary<string, ColumnValueChange> GetChangesByColumn(IAuditDbContext context, EntityEntry entry)
        {
            var changes = GetChanges(context, entry);

            return changes.ToDictionary(k => k.ColumnName, v => new ColumnValueChange { OriginalValue = v.OriginalValue, NewValue = v.NewValue });
        }

#if EF_CORE_8_OR_GREATER
        /// <summary>
        /// Adds the change values from the complex properties recursively
        /// </summary>
        private void AddChangesFromComplexProperties(IAuditDbContext context, EntityEntry entry, IEnumerable<ComplexPropertyEntry> complexProperties, List<EventEntryChange> result, string prefix = null)
        {
            foreach (var complexEntry in complexProperties)
            {
                var isJson = complexEntry.Metadata.ComplexType.GetContainerColumnName() != null;

                var complexPropertyPath = isJson ? GetComplexPropertyPath(prefix, complexEntry.Metadata.Name) : null;

                // Process the primitive properties
                foreach (var propEntry in complexEntry.Properties)
                {
                    if (propEntry.IsModified && IncludeProperty(context, complexEntry.Metadata.ClrType, propEntry.Metadata.Name))
                    {
                        var columnName = isJson
                            ? GetColumnNameFromComplexProperty(propEntry.Metadata, complexPropertyPath)
                            : GetColumnName(propEntry.Metadata, null);

                        result.Add(new EventEntryChange()
                        {
                            ColumnName = columnName,
                            NewValue = HasPropertyValue(context, entry, complexEntry.Metadata.ClrType, propEntry.Metadata.Name, propEntry.CurrentValue, out var overridenCurrentValue) ? overridenCurrentValue : propEntry.CurrentValue,
                            OriginalValue = HasPropertyValue(context, entry, complexEntry.Metadata.ClrType, propEntry.Metadata.Name, propEntry.OriginalValue, out var overridenOriginalValue) ? overridenOriginalValue : propEntry.OriginalValue
                        });
                    }
                }

#if EF_CORE_10_OR_GREATER
                AddChangesFromComplexCollections(context, entry, complexEntry.ComplexCollections, result, complexPropertyPath);
#endif
                // Recursively process complex properties
                AddChangesFromComplexProperties(context, entry, complexEntry.ComplexProperties, result, complexPropertyPath);
            }
        }
#endif

#if EF_CORE_10_OR_GREATER
        /// <summary>
        /// Adds the change values from the complex collections.
        /// </summary>
        private void AddChangesFromComplexCollections(IAuditDbContext context, EntityEntry entry, IEnumerable<ComplexCollectionEntry> complexCollections, List<EventEntryChange> result, string prefix = null)
        {
            foreach (var complexCollection in complexCollections)
            {
                if (!complexCollection.IsModified || !IncludeProperty(context, entry.Metadata.ClrType, complexCollection.Metadata.Name))
                {
                    continue;
                }

                var complexCollectionPath = GetComplexPropertyPath(prefix, complexCollection.Metadata.Name);

                object originalValue = entry.State == EntityState.Added ? null : entry.OriginalValues[complexCollection.Metadata];
                object newValue = entry.State == EntityState.Deleted ? null : entry.CurrentValues[complexCollection.Metadata];
                
                if (HasPropertyValue(context, entry, complexCollection.Metadata.Name, originalValue, out var overriddenValue))
                {
                    originalValue = overriddenValue;
                }

                if (HasPropertyValue(context, entry, complexCollection.Metadata.Name, newValue, out overriddenValue))
                {
                    newValue = overriddenValue;
                }
                
                result.Add(new EventEntryChange()
                {
                    ColumnName = complexCollectionPath,
                    OriginalValue = originalValue,
                    NewValue = newValue
                });
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
                    result.Add(GetColumnName(prop, entry.Metadata), value);
                }
            }

#if EF_CORE_8_OR_GREATER
            AddColumnValuesFromComplexProperties(context, entry, entry.ComplexProperties, result);
#endif
#if EF_CORE_10_OR_GREATER
            AddColumnValuesFromComplexCollections(context, entry, entry.ComplexCollections, result);
#endif
            return result;
        }
        
#if EF_CORE_8_OR_GREATER
        /// <summary>
        /// Adds the column values from the complex properties recursively
        /// </summary>
        private void AddColumnValuesFromComplexProperties(IAuditDbContext context, EntityEntry entry, IEnumerable<ComplexPropertyEntry> complexProperties, Dictionary<string, object> result, string prefix = null)
        {
            foreach (var complexEntry in complexProperties)
            {
                var isJson = complexEntry.Metadata.ComplexType.GetContainerColumnName() != null;

                var complexPropertyPath = isJson ? GetComplexPropertyPath(prefix, complexEntry.Metadata.Name) : null;
                
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

                        var columnName = isJson 
                            ? GetColumnNameFromComplexProperty(propEntry.Metadata, complexPropertyPath)
                            : GetColumnName(propEntry.Metadata, null);

                        result.Add(columnName, value);
                    }
                }

#if EF_CORE_10_OR_GREATER
                AddColumnValuesFromComplexCollections(context, entry, complexEntry.ComplexCollections, result, complexPropertyPath);
#endif
                // Recursively process complex properties
                AddColumnValuesFromComplexProperties(context, entry, complexEntry.ComplexProperties, result, complexPropertyPath);
            }
        }
#endif

#if EF_CORE_10_OR_GREATER
        /// <summary>
        /// Adds the column values from the complex collections recursively
        /// </summary>
        private void AddColumnValuesFromComplexCollections(IAuditDbContext context, EntityEntry entry, IEnumerable<ComplexCollectionEntry> complexCollections, Dictionary<string, object> result, string prefix = null)
        {
            foreach (var complexCollectionMetadata in complexCollections.Select(c => c.Metadata))
            {
                var complexCollectionPath = GetComplexPropertyPath(prefix, complexCollectionMetadata.Name);

                if (!IncludeProperty(context, entry.Metadata.ClrType, complexCollectionPath))
                {
                    continue;
                }

                object value = entry.State == EntityState.Deleted
                    ? entry.OriginalValues[complexCollectionMetadata]
                    : entry.CurrentValues[complexCollectionMetadata];

                if (HasPropertyValue(context, entry, entry.Metadata.ClrType, complexCollectionPath, value, out var overrideValue))
                {
                    value = overrideValue;
                }

                result.Add(complexCollectionPath, value);
            }
        }
#endif

#if EF_CORE_5_OR_GREATER
        /// <summary>Gets the name of the column.</summary>
        internal static string GetColumnName(IProperty prop, IEntityType metadata)
        {
            var declaringType = GetDeclaringType(prop);

            var entityType = metadata ?? declaringType;

            // Try resolving against the runtime entity type (TPC / TPH)
            var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

            if (storeObject.HasValue)
            {
                var columnName = prop.GetColumnName(storeObject.Value);
                if (columnName != null)
                {
                    return columnName;
                }
            }

            // Fallback for TPT: try declaring type table
            if (metadata != null)
            {
                var declaringStoreObject = StoreObjectIdentifier.Create(declaringType, StoreObjectType.Table);

                if (declaringStoreObject.HasValue)
                {
                    var columnName = prop.GetColumnName(declaringStoreObject.Value);
                    if (columnName != null)
                    {
                        return columnName;
                    }
                }
            }

            // Final fallback (annotation-based)
            return GetFallbackColumnName(prop);
        }
#else
        internal static string GetColumnName(IProperty prop, IEntityType metadata = null)
        {
            return prop.Relational().ColumnName ?? prop.Name;
        }
#endif

#if EF_CORE_8_OR_GREATER
        internal static string GetColumnNameFromComplexProperty(IProperty prop, string prefix)
        {
            return GetComplexPropertyPath(prefix, GetFallbackColumnName(prop));
        }

        private static string GetComplexPropertyPath(string prefix, string name)
        {
            return string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
        }

        private static ITypeBase GetDeclaringType(IProperty prop)
        {
            return prop.DeclaringType;
        }
#else
        private static IEntityType GetDeclaringType(IProperty prop)
        {
            return prop.DeclaringEntityType;
        }
#endif

        private static string GetFallbackColumnName(IProperty prop)
        {
#if EF_CORE_8_OR_GREATER
            return prop.GetColumnName() ?? prop.GetDefaultColumnName();
#elif EF_CORE_7_OR_GREATER
            return prop.GetColumnName() ?? prop.GetDefaultColumnName();
#else
            return prop.GetColumnName() ?? prop.GetDefaultColumnBaseName();
#endif
        }

        // Determines if the property should be included or is ignored
        private static bool IncludeProperty(IAuditDbContext context, EntityEntry entry, string propName)
        {
            var entityType = GetDefiningType(context.DbContext, entry)?.ClrType;

            if (entityType == null)
            {
                return true;
            }

            return IncludeProperty(context, entityType, propName);
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
                        result[GetColumnName(prop, entry.Metadata)] = entry.Property(prop.Name).CurrentValue;
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
                result.Add(GetColumnName(prop.Metadata, entry.Metadata), prop.CurrentValue); 
            }
            return result;
        }

        private bool TryBeginCreateAuditEvent(IAuditDbContext context, out EntityFrameworkEvent efEvent, out IReadOnlyList<EntityEntry> modifiedEntries)
        {
            var dbContext = context.DbContext;
            modifiedEntries = GetModifiedEntries(context);
            if (modifiedEntries.Count == 0)
            {
                efEvent = null;
                return false;
            }
            var dbConnection = IsRelational(dbContext) ? dbContext.Database.GetDbConnection() : null;
            var clientConnectionId = GetClientConnectionId(dbConnection);
            efEvent = new EntityFrameworkEvent()
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
            return true;
        }

        private static void ReloadOriginalValuesIfNeeded(IAuditDbContext context, EntityEntry entry)
        {
            if (!context.ReloadDatabaseValues || entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                return;
            }
            // Note: GetDatabaseValues() doesn't return complex collections, so SetValues sets them to null. This is an EF Core limitation.
            // When ReloadDatabaseValues is true, complex collections will be set to null in the OriginalValues of the event entry.
            var dbValues = entry.GetDatabaseValues();
            if (dbValues != null)
            {
                entry.OriginalValues.SetValues(dbValues);
            }
        }

        private static async Task ReloadOriginalValuesIfNeededAsync(IAuditDbContext context, EntityEntry entry, CancellationToken cancellationToken = default)
        {
            if (!context.ReloadDatabaseValues || entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                return;
            }
            var dbValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            if (dbValues != null)
            {
                entry.OriginalValues.SetValues(dbValues);
            }
        }

        private EventEntry CreateEventEntry(IAuditDbContext context, EntityEntry entry)
        {
            var entity = entry.Entity;
            var validationResults = context.ExcludeValidationResults ? null : DbContextHelper.GetValidationResults(entity);
            var entityName = GetEntityName(context.DbContext, entry);
            return new EventEntry()
            {
                Valid = validationResults == null,
                ValidationResults = validationResults?.Select(x => x.ErrorMessage).ToList(),
                Entity = context.IncludeEntityObjects ? entity : null,
                Entry = entry,
                Action = GetStateName(entry.State),
                Changes = entry.State == EntityState.Modified && !context.MapChangesByColumn ? GetChanges(context, entry) : null,
                ChangesByColumn = entry.State == EntityState.Modified && context.MapChangesByColumn ? GetChangesByColumn(context, entry) : null,
                Table = entityName.Table,
                Schema = entityName.Schema,
#if EF_CORE_5_OR_GREATER
                Name = entry.Metadata.DisplayName(),
#endif
                ColumnValues = GetColumnValues(context, entry)
            };
        }

        /// <summary>
        /// Creates the Audit Event.
        /// </summary>
        public EntityFrameworkEvent CreateAuditEvent(IAuditDbContext context)
        {
            if (!TryBeginCreateAuditEvent(context, out var efEvent, out var modifiedEntries))
            {
                return null;
            }
            foreach (var entry in modifiedEntries)
            {
                ReloadOriginalValuesIfNeeded(context, entry);
                efEvent.Entries.Add(CreateEventEntry(context, entry));
            }
            return efEvent;
        }

        /// <summary>
        /// Creates the Audit Event asynchronously.
        /// </summary>
        public async Task<EntityFrameworkEvent> CreateAuditEventAsync(IAuditDbContext context, CancellationToken cancellationToken = default)
        {
            if (!TryBeginCreateAuditEvent(context, out var efEvent, out var modifiedEntries))
            {
                return null;
            }
            foreach (var entry in modifiedEntries)
            {
                await ReloadOriginalValuesIfNeededAsync(context, entry, cancellationToken);
                efEvent.Entries.Add(CreateEventEntry(context, entry));
            }
            return efEvent;
        }

        private static void UpdateEventEntry(IAuditDbContext context, EventEntry efEntry)
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

        private void UpdateAuditEventConnectionId(EntityFrameworkEvent efEvent, IAuditDbContext context)
        {
            var clientConnectionId = TryGetClientConnectionId(context.DbContext);
            if (clientConnectionId != null)
            {
                efEvent.ConnectionId = clientConnectionId;
            }
        }

        /// <summary>
        /// Updates column values and primary keys on the Audit Event after the EF save operation completes.
        /// </summary>
        public void UpdateAuditEvent(EntityFrameworkEvent efEvent, IAuditDbContext context)
        {
            foreach (var efEntry in efEvent.Entries)
            {
                UpdateEventEntry(context, efEntry);
                if (context.ReloadDatabaseValuesAfterSave)
                {
                    ReloadAfterSave(context, efEntry.Entry, efEntry);
                }
            }
            UpdateAuditEventConnectionId(efEvent, context);
        }

        /// <summary>
        /// Updates column values and primary keys on the Audit Event after the EF save operation completes, asynchronously.
        /// </summary>
        public async Task UpdateAuditEventAsync(EntityFrameworkEvent efEvent, IAuditDbContext context, CancellationToken cancellationToken = default)
        {
            foreach (var efEntry in efEvent.Entries)
            {
                UpdateEventEntry(context, efEntry);
                if (context.ReloadDatabaseValuesAfterSave)
                {
                    await ReloadAfterSaveAsync(context, efEntry.Entry, efEntry, cancellationToken);
                }
            }
            UpdateAuditEventConnectionId(efEvent, context);
        }

        private void ReloadAfterSave(IAuditDbContext context, EntityEntry entry, EventEntry efEntry)
        {
            ApplyReloadAfterSave(context, entry, efEntry, entry.GetDatabaseValues());
        }

        private async Task ReloadAfterSaveAsync(IAuditDbContext context, EntityEntry entry, EventEntry efEntry, CancellationToken cancellationToken = default)
        {
            ApplyReloadAfterSave(context, entry, efEntry, await entry.GetDatabaseValuesAsync(cancellationToken));
        }

        private void ApplyReloadAfterSave(IAuditDbContext context, EntityEntry entry, EventEntry efEntry, PropertyValues dbValues)
        {
            if (dbValues == null)
            {
                return;
            }

            foreach (var prop in entry.Metadata.GetProperties())
            {
                if (!IncludeProperty(context, entry, prop.Name))
                {
                    continue;
                }

                var columnName = GetColumnName(prop, entry.Metadata);

                var dbValue = GetDatabaseValue(dbValues, prop);

                if (HasPropertyValue(context, entry, prop.Name, dbValue, out var overrideValue))
                {
                    dbValue = overrideValue;
                }

                if (efEntry.ColumnValues.ContainsKey(columnName))
                {
                    efEntry.ColumnValues[columnName] = dbValue;
                }
            }

#if EF_CORE_8_OR_GREATER
            ReloadAfterSaveComplexProperties(context, entry, dbValues, entry.ComplexProperties, efEntry.ColumnValues);
#endif
        }

#if EF_CORE_8_OR_GREATER
        private void ReloadAfterSaveComplexProperties(IAuditDbContext context, EntityEntry entry, PropertyValues dbValues, IEnumerable<ComplexPropertyEntry> complexProperties, IDictionary<string, object> columnValues, string prefix = null)
        {
            foreach (var complexEntry in complexProperties)
            {
                var isJson = complexEntry.Metadata.ComplexType.GetContainerColumnName() != null;

                var complexPropertyPath = isJson ? GetComplexPropertyPath(prefix, complexEntry.Metadata.Name) : null;

                foreach (var propEntryMetadata in complexEntry.Properties.Select(p => p.Metadata))
                {
                    if (!IncludeProperty(context, complexEntry.Metadata.ClrType, propEntryMetadata.Name))
                    {
                        continue;
                    }

                    var dbValue = GetDatabaseValue(dbValues, propEntryMetadata);

                    if (HasPropertyValue(context, entry, complexEntry.Metadata.ClrType, propEntryMetadata.Name, dbValue, out var overrideValue))
                    {
                        dbValue = overrideValue;
                    }

                    var columnName = isJson
                        ? GetColumnNameFromComplexProperty(propEntryMetadata, complexPropertyPath)
                        : GetColumnName(propEntryMetadata, null);

                    if (columnValues.ContainsKey(columnName))
                    {
                        columnValues[columnName] = dbValue;
                    }
                }

                ReloadAfterSaveComplexProperties(context, entry, dbValues, complexEntry.ComplexProperties, columnValues, complexPropertyPath);
            }
        }
#endif

        private static object GetDatabaseValue(PropertyValues dbValues, IProperty prop)
        {
#if EF_CORE_8_OR_GREATER
            return dbValues[prop];
#else
            return dbValues.GetValue<object>(prop.Name);
#endif
        }

        private static string GetAmbientTransactionId()
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