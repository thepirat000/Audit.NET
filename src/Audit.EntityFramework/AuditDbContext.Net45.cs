#if NET45
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Data.Entity;

namespace Audit.EntityFramework
{
    /// <summary>
    /// The base DbContext class for Audit.
    /// NET 4.5
    /// </summary>
    public abstract partial class AuditDbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            SetConfig();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(DbCompiledModel model) : base(model)
        {
            SetConfig();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        {
            SetConfig();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        {
            SetConfig();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
        {
            SetConfig();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext) : base(objectContext, dbContextOwnsObjectContext)
        {
            SetConfig();
        }

        /// <summary>
        /// Gets the entities changes for an entry entity.
        /// </summary>
        /// <param name="entry">The entry.</param>
        private List<EventEntryChange> GetChanges(DbEntityEntry entry)
        {
            var result = new List<EventEntryChange>();
            foreach (var propName in entry.CurrentValues.PropertyNames)
            {
                var current = entry.CurrentValues[propName];
                var original = entry.OriginalValues[propName];
                if (current == null && original == null)
                {
                    continue;
                }
                if (original == null || !original.Equals(current))
                {
                    result.Add(new EventEntryChange()
                    {
                        ColumnName = EntityKeyHelper.Instance.GetColumnName(entry.Entity.GetType(), entry.Property(propName).Name, this),
                        NewValue = current,
                        OriginalValue = original
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the column values for an insert/delete operation.
        /// </summary>
        /// <param name="entry">The entity entry.</param>
        private Dictionary<string, object> GetColumnValues(DbEntityEntry entry)
        {
            var result = new Dictionary<string, object>();
            var propertyNames = entry.State != EntityState.Deleted ? entry.CurrentValues.PropertyNames : entry.OriginalValues.PropertyNames;
            foreach (var propName in propertyNames)
            {
                var value = (entry.State == EntityState.Added) ? entry.CurrentValues[propName] : entry.OriginalValues[propName];
                result.Add(propName, value);
            }
            return result;
        }

        /// <summary>
        /// Gets the name of the table/entity.
        /// </summary>
        private string GetEntityName(object entity)
        {
            var entityType = entity.GetType();
            return EntityKeyHelper.Instance.GetTableName(entityType, this) ?? entityType.Name;
        }
        /// <summary>
        /// Gets the primary key values for an entity
        /// </summary>
        private Dictionary<string, object> GetPrimaryKey(DbEntityEntry entry)
        {
            return EntityKeyHelper.Instance.GetPrimaryKeyValues(entry.Entity, this);
        }

        /// <summary>
        /// Tries to get the current transaction identifier.
        /// </summary>
        /// <param name="clientConnectionId">The client ConnectionId.</param>
        private string GetCurrentTransactionId(string clientConnectionId)
        {
            var curr = Database.CurrentTransaction;
            if (curr == null)
            {
                return null;
            }
            // Get the transaction id
            var underlyingTran = curr.UnderlyingTransaction;
            return GetTransactionId(underlyingTran, clientConnectionId);
        }

        /// <summary>
        /// Creates the Audit Event.
        /// </summary>
        /// <param name="includeEntities">To indicate if it must incluide the serialized entities.</param>
        /// <param name="mode">The option mode to include/exclude entities from Audit.</param>
        private EntityFrameworkEvent CreateAuditEvent(bool includeEntities, AuditOptionMode mode)
        {
            var modifiedEntries = GetModifiedEntries(mode);
            if (modifiedEntries.Count == 0)
            {
                return null;
            }
            var clientConnectionId = GetClientConnectionId(Database.Connection);
            var efEvent = new EntityFrameworkEvent()
            {
                Entries = new List<EventEntry>(),
                Database = Database.Connection.Database,
                ConnectionId = clientConnectionId,
                TransactionId = GetCurrentTransactionId(clientConnectionId)
            };
            foreach (var entry in modifiedEntries)
            {
                var entity = entry.Entity;
                var validationResults = entry.GetValidationResult();
                efEvent.Entries.Add(new EventEntry()
                {
                    Valid = validationResults.IsValid,
                    ValidationResults = validationResults.ValidationErrors.Select(x => x.ErrorMessage).ToList(),
                    Entity = includeEntities ? entity : null,
                    Action = GetStateName(entry.State),
                    Changes = entry.State == EntityState.Modified ? GetChanges(entry) : null,
                    ColumnValues = GetColumnValues(entry),
                    PrimaryKey = GetPrimaryKey(entry),
                    Table = GetEntityName(entity)
                });
            }
            return efEvent;
        }

        /// <summary>
        /// Saves the changes asynchronously.
        /// </summary>
        public override async Task<int> SaveChangesAsync()
        {
            return await SaveChangesAsync(default(CancellationToken));
        }
    }
}
#endif