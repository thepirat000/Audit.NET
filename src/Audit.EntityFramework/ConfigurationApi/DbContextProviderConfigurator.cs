#if NET7_0_OR_GREATER
using System;
using Audit.Core;
using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.ConfigurationApi;

public class DbContextProviderConfigurator<TDbContext, TEntity> : IDbContextProviderConfigurator<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class, new()
{
    internal Func<AuditEvent, TDbContext> _dbContextBuilder;
    internal Setting<DbContextOptions<TDbContext>> _dbContextOptions;
    internal DbContextProviderEntityConfigurator<TEntity> _entityConfiguration = new();

    public IDbContextProviderEntityConfigurator<TEntity> DbContextBuilder(Func<AuditEvent, TDbContext> dbContextBuilder)
    {
        _dbContextBuilder = dbContextBuilder;
        _dbContextOptions = new();

        _entityConfiguration = new DbContextProviderEntityConfigurator<TEntity>();
        return _entityConfiguration;
    }

    public IDbContextProviderEntityConfigurator<TEntity> DbContext(TDbContext dbContext)
    {
        _dbContextBuilder = _ => dbContext;
        _dbContextOptions = new();

        _entityConfiguration = new DbContextProviderEntityConfigurator<TEntity>();
        return _entityConfiguration;
    }

    public IDbContextProviderEntityConfigurator<TEntity> UseDbContextOptions(DbContextOptions<TDbContext> dbContextOptions)
    {
        _dbContextBuilder = null;
        _dbContextOptions = dbContextOptions;

        _entityConfiguration = new DbContextProviderEntityConfigurator<TEntity>();
        return _entityConfiguration;
    }
}
#endif