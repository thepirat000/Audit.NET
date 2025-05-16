#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

#pragma warning disable EF1001
#pragma warning disable S3011

namespace Audit.EntityFramework.Providers
{
    /// <summary>
    /// Audit Data Provider using Entity Framework Core to store audit events in multiple entities or tables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider is suitable for scenarios where audit events are distributed across different entities or tables or when a single audit event spans multiple unrelated tables.
    /// Use the alternative generic <see cref="DbContextDataProvider{TDbContext,TEntity}"/> for scenarios where all audit events are stored in a single entity or table and event replacement (update) is needed.
    /// </para>
    /// <para>
    /// Note: Replacing events is not supported with this provider. The <see cref="EventCreationPolicy.InsertOnStartReplaceOnEnd"/> is not allowed when using this data provider.
    /// </para>
    /// </remarks>
    public class DbContextDataProvider : AuditDataProvider 
    {
        /// <summary>
        /// Provides the Db Context instance to use.
        /// </summary>
        public Func<AuditEvent, DbContext> DbContextBuilder { get; set; }

        /// <summary>
        /// Provides the Db Context Options to use when creating a new instance of the DbContextBuilder. Alternative to DbContextBuilder.
        /// </summary>
        public Setting<DbContextOptions> DbContextOptions { get; set; }

        /// <summary>
        /// Provides the function that maps an AuditEvent to Entities to be saved in the database.
        /// This method should return the entity to be inserted.
        /// The DbContext must have a DbSet of the Entity type defined.
        /// If this method returns NULL, the event will be skipped.
        /// </summary> 
        public Func<AuditEvent, IEnumerable<object>> EntityBuilder { get; set; }
       
        /// <summary>
        /// Whether to dispose the DbContext after each operation. Default is false.
        /// </summary>
        public bool DisposeDbContext { get; set; }

        /// <summary>
        /// Creates a new instance of DbContextDataProvider
        /// </summary>
        public DbContextDataProvider()
        {
        }

        /// <summary>
        /// Creates a new instance of DbContextDataProvider with the given configuration
        /// </summary>
        /// <param name="config">The configuration to use</param>
        public DbContextDataProvider(Action<IDbContextProviderConfigurator> config)
        {
            var dbContextProviderConfig = new DbContextProviderConfigurator();
            if (config != null)
            {
                config.Invoke(dbContextProviderConfig);
                DbContextBuilder = dbContextProviderConfig._dbContextBuilder;
                DbContextOptions = dbContextProviderConfig._dbContextOptions;
                EntityBuilder = dbContextProviderConfig._entityConfiguration._entityBuilder;
                DisposeDbContext = dbContextProviderConfig._entityConfiguration._disposeDbContext;
            }
        }

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var entities = EntityBuilder.Invoke(auditEvent)?.ToList();

            if (entities == null || entities.Count == 0 || entities.TrueForAll(e => e == null))
            {
                return null;
            }

            var dbContext = GetDbContext(auditEvent);
            
            AddEntities(dbContext, entities);

            dbContext.SaveChanges();

            var ids = GetPrimaryKeysValues(dbContext, entities);

            if (DisposeDbContext)
            {
                dbContext.Dispose();
            }

            return ids.Count == 1 ? ids[0] : ids;
        }

        /// <inheritdoc />
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var entities = EntityBuilder.Invoke(auditEvent)?.ToList();

            if (entities == null || entities.Count == 0 || entities.TrueForAll(e => e == null))
            {
                return null;
            }

            var dbContext = GetDbContext(auditEvent);

            await AddEntitiesAsync(dbContext, entities, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            var ids = GetPrimaryKeysValues(dbContext, entities);

            if (DisposeDbContext)
            {
                await dbContext.DisposeAsync();
            }

            return ids.Count == 1 ? ids[0] : ids;
        }

        private static void AddEntities(DbContext dbContext, List<object> entities)
        {
            foreach (var entity in entities.FindAll(e => e != null))
            {
                AddEntity(dbContext, entity);
            }
        }

        private static async Task AddEntitiesAsync(DbContext dbContext, List<object> entities, CancellationToken cancellationToken)
        {
            foreach (var entity in entities.FindAll(e => e != null))
            {
                await AddEntityAsync(dbContext, entity, cancellationToken);
            }
        }

        private static void AddEntity(DbContext dbContext, object entity)
        {
            dynamic dbSet = dbContext.Set(entity.GetType());
            dbSet.Add((dynamic)entity);
        }

        private static async Task AddEntityAsync(DbContext dbContext, object entity, CancellationToken cancellationToken)
        {
            dynamic dbSet = dbContext.Set(entity.GetType());
            await dbSet.AddAsync((dynamic)entity, cancellationToken);
        }

        internal DbContext GetDbContext(AuditEvent auditEvent)
        {
            var dbContext = DbContextBuilder?.Invoke(auditEvent);

            if (dbContext != null)
            {
                return dbContext;
            }

            var options = DbContextOptions.GetValue(auditEvent);

            if (options == null)
            {
                throw new InvalidOperationException("DbContextBuilder or DbContextOptions must be provided.");
            }

            dbContext = (DbContext)Activator.CreateInstance(options.ContextType, options);

            return dbContext;
        }

        private static List<object[]> GetPrimaryKeysValues(DbContext dbContext, List<object> entities)
        {
            var primaryKeys = new List<object[]>();

            foreach (var entity in entities.FindAll(e => e != null))
            {
                primaryKeys.Add(GetPrimaryKeyValue(dbContext.Entry(entity)));
            }

            return primaryKeys;
        }

        private static object[] GetPrimaryKeyValue(EntityEntry entry)
        {
            var values = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(prop => prop.CurrentValue).ToArray();

            return values;
        }
    }
}
#endif
