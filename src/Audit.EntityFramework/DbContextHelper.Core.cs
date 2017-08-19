#if NETSTANDARD1_5 || NETSTANDARD2_0
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
        /// <param name="dbContext">The database context.</param>
        /// <param name="entry">The entry.</param>
        private List<EventEntryChange> GetChanges(DbContext dbContext, EntityEntry entry)
        {
            var result = new List<EventEntryChange>();
            var props = dbContext.Model.FindEntityType(entry.Entity.GetType()).GetProperties();
            foreach (var prop in props)
            {
                PropertyEntry propEntry = entry.Property(prop.Name);
                if (propEntry.IsModified)
                {
                    result.Add(new EventEntryChange()
                    {
                        ColumnName = GetColumnName(prop),
                        NewValue = propEntry.CurrentValue,
                        OriginalValue = propEntry.OriginalValue
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        private static string GetColumnName(IProperty prop)
        {
            return prop.SqlServer().ColumnName ?? prop.Name;
        }

        /// <summary>
        /// Gets the column values for an insert/delete operation.
        /// </summary>
        /// <param name="entry">The entity entry.</param>
        private Dictionary<string, object> GetColumnValues(DbContext dbContext, EntityEntry entry)
        {
            var result = new Dictionary<string, object>();
            var props = dbContext.Model.FindEntityType(entry.Entity.GetType()).GetProperties();
            foreach (var prop in props)
            {
                PropertyEntry propEntry = entry.Property(prop.Name);
                var value = (entry.State != EntityState.Deleted) ? propEntry.CurrentValue : propEntry.OriginalValue;
                result.Add(prop.Name, value);
            }
            return result;
        }

        /// <summary>
        /// Gets the name of the entity.
        /// </summary>
        private static string GetEntityName(IEntityType entityType)
        {
            return entityType.SqlServer().TableName ?? entityType.Name;
        }

        /// <summary>
        /// Gets the primary key values for an entity
        /// </summary>
        private static Dictionary<string, object> GetPrimaryKey(IEntityType entityType, object entity)
        {
            var result = new Dictionary<string, object>();
            var pkDefinition = entityType.FindPrimaryKey();
            if (pkDefinition != null)
            {
                foreach (var pkProp in pkDefinition.Properties)
                {
                    var value = entity.GetType().GetTypeInfo().GetProperty(pkProp.Name).GetValue(entity);
                    result.Add(pkProp.Name, value);
                }
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
                TransactionId = GetCurrentTransactionId(dbContext, clientConnectionId)
            };
            foreach (var entry in modifiedEntries)
            {
                var entity = entry.Entity;
                var validationResults = DbContextHelper.GetValidationResults(entity);
                var entityType = dbContext.Model.FindEntityType(entry.Entity.GetType());
                efEvent.Entries.Add(new EventEntry()
                {
                    Valid = validationResults == null,
                    ValidationResults = validationResults?.Select(x => x.ErrorMessage).ToList(),
                    Entity = context.IncludeEntityObjects ? entity : null,
                    Action = DbContextHelper.GetStateName(entry.State),
                    Changes = entry.State == EntityState.Modified ? GetChanges(dbContext, entry) : null,
                    ColumnValues = GetColumnValues(dbContext, entry),
                    PrimaryKey = GetPrimaryKey(entityType, entity),
                    Table = GetEntityName(entityType)
                });
            }
            return efEvent;
        }

        /// <summary>
        /// Tries to get the current transaction identifier.
        /// </summary>
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