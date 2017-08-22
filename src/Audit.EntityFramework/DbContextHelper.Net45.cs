#if NET45
using System.Collections.Generic;
using System.Data.Entity;
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
        /// <param name="dbContext">The database context.</param>
        private List<EventEntryChange> GetChanges(DbContext dbContext, DbEntityEntry entry)
        {
            var result = new List<EventEntryChange>();
            foreach (var propName in entry.CurrentValues.PropertyNames)
            {
                if (entry.Property(propName).IsModified)
                {
                    result.Add(new EventEntryChange()
                    {
                        ColumnName = EntityKeyHelper.Instance.GetColumnName(entry.Entity.GetType(), entry.Property(propName).Name, dbContext),
                        NewValue = entry.CurrentValues[propName],
                        OriginalValue = entry.OriginalValues[propName]
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the column values.
        /// </summary>
        /// <param name="entry">The entity entry.</param>
        private Dictionary<string, object> GetColumnValues(DbEntityEntry entry)
        {
            var result = new Dictionary<string, object>();
            var propertyNames = entry.State != EntityState.Deleted ? entry.CurrentValues.PropertyNames : entry.OriginalValues.PropertyNames;
            foreach (var propName in propertyNames)
            {
                var value = (entry.State != EntityState.Deleted) ? entry.CurrentValues[propName] : entry.OriginalValues[propName];
                result.Add(propName, value);
            }
            return result;
        }

        /// <summary>
        /// Gets the name of the table/entity.
        /// </summary>
        private string GetEntityName(DbContext dbContext, object entity)
        {
            var entityType = entity.GetType();
            return EntityKeyHelper.Instance.GetTableName(entityType, dbContext) ?? entityType.Name;
        }

        /// <summary>
        /// Tries to get the current transaction identifier.
        /// </summary>
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
        private static Dictionary<string, object> GetPrimaryKey(DbContext dbContext, object entity)
        {
            return EntityKeyHelper.Instance.GetPrimaryKeyValues(entity, dbContext);
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
            var clientConnectionId = GetClientConnectionId(dbContext.Database.Connection);
            var efEvent = new EntityFrameworkEvent()
            {
                Entries = new List<EventEntry>(),
                Database = dbContext.Database.Connection.Database,
                ConnectionId = clientConnectionId,
                TransactionId = GetCurrentTransactionId(dbContext, clientConnectionId)
            };
            foreach (var entry in modifiedEntries)
            {
                var entity = entry.Entity;
                var validationResults = entry.GetValidationResult();
                efEvent.Entries.Add(new EventEntry()
                {
                    Valid = validationResults.IsValid,
                    ValidationResults = validationResults.ValidationErrors.Select(x => x.ErrorMessage).ToList(),
                    Entity = context.IncludeEntityObjects ? entity : null,
                    Entry = entry,
                    Action = GetStateName(entry.State),
                    Changes = entry.State == EntityState.Modified ? GetChanges(dbContext, entry) : null,
                    Table = GetEntityName(dbContext, entity),
                    ColumnValues = GetColumnValues(entry)
                });
            }
            return efEvent;
        }
    }
}
#endif