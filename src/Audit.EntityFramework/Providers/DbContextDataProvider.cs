#if NET7_0_OR_GREATER
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Audit.EntityFramework.Providers
{
    /// <summary>
    /// Generic Audit Data Provider using Entity Framework Core to write and read audit events
    /// </summary>
    public class DbContextDataProvider<TDbContext, TEntity> : AuditDataProvider 
        where TDbContext : DbContext
        where TEntity : class, new()
    {
        /// <summary>
        /// Provides the Db Context instance to use.
        /// </summary>
        public Func<AuditEvent, TDbContext> DbContextBuilder { get; set; }

        /// <summary>
        /// Provides the Db Context Options to use when creating a new instance of the DbContextBuilder. Alternative to DbContextBuilder.
        /// </summary>
        public Setting<DbContextOptions<TDbContext>> DbContextOptions { get; set; }

        /// <summary>
        /// Provides the mapping function to map the AuditEvent to the Entity instance.
        /// </summary>
        public Action<AuditEvent, TEntity> Mapper { get; set; }

        /// <summary>
        /// Whether to dispose the DbContextBuilder after each operation.
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
        public DbContextDataProvider(Action<IDbContextProviderConfigurator<TDbContext, TEntity>> config)
        {
            var dbContextProviderConfig = new DbContextProviderConfigurator<TDbContext, TEntity>();
            if (config != null)
            {
                config.Invoke(dbContextProviderConfig);
                DbContextBuilder = dbContextProviderConfig._dbContextBuilder;
                DbContextOptions = dbContextProviderConfig._dbContextOptions;
                Mapper = dbContextProviderConfig._entityConfiguration._mapper;
                DisposeDbContext = dbContextProviderConfig._entityConfiguration._disposeDbContext;
            }
            
        }

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var dbContext = GetDbContext(auditEvent);

            var entity = new TEntity();

            Mapper.Invoke(auditEvent, entity);
            
            var dbSet = dbContext.Set<TEntity>();

            dbSet.Add(entity);

            dbContext.SaveChanges();

            var id = GetPrimaryKeyValue(dbContext.Entry(entity));

            if (DisposeDbContext)
            {
                dbContext.Dispose();
            }

            return id;
        }

        /// <inheritdoc />
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var dbContext = GetDbContext(auditEvent);

            var entity = new TEntity();

            var dbSet = dbContext.Set<TEntity>();

            Mapper.Invoke(auditEvent, entity);
            
            await dbSet.AddAsync(entity, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            var id = GetPrimaryKeyValue(dbContext.Entry(entity));

            if (DisposeDbContext)
            {
                await dbContext.DisposeAsync();
            }

            return id;
        }

        /// <inheritdoc />
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var dbContext = GetDbContext(auditEvent);

            var dbSet = dbContext.Set<TEntity>();

            var primaryKey = (object[])eventId;

            var entity = dbSet.Find(primaryKey);

            if (entity == null)
            {
                throw new InvalidOperationException($"Entity of type {typeof(TEntity).Name} with id {eventId} not found.");
            }

            Mapper.Invoke(auditEvent, entity);

            dbContext.SaveChanges();

            if (DisposeDbContext)
            {
                dbContext.Dispose();
            }
        }

        /// <inheritdoc />
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var dbContext = GetDbContext(auditEvent);

            var dbSet = dbContext.Set<TEntity>();

            var primaryKey = (object[])eventId;

            var entity = await dbSet.FindAsync(primaryKey, cancellationToken: cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException($"Entity of type {typeof(TEntity).Name} with id {eventId} not found.");
            }

            Mapper.Invoke(auditEvent, entity);

            await dbContext.SaveChangesAsync(cancellationToken);

            if (DisposeDbContext)
            {
                await dbContext.DisposeAsync();
            }
        }

        internal TDbContext GetDbContext(AuditEvent auditEvent)
        {
            var dbContext = DbContextBuilder?.Invoke(auditEvent);

            if (dbContext != null)
            {
                return dbContext;
            }

            var options = DbContextOptions.GetValue(auditEvent);

            if (options != null)
            {
                dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), options);
            }
            else
            {
                dbContext = Activator.CreateInstance<TDbContext>();
            }

            return dbContext;
        }

        private static object[] GetPrimaryKeyValue(EntityEntry entry)
        {
            var values = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(prop => prop.CurrentValue).ToArray();

            return values;
        }

        public override T GetEvent<T>(object eventId)
        {
            throw new NotImplementedException($"GetEvent is not implemented on {nameof(DbContextDataProvider<TDbContext, TEntity>)}");
        }

        public override Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException($"GetEventAsync is not implemented on {nameof(DbContextDataProvider<TDbContext, TEntity>)}");
        }
    }
}
#endif
