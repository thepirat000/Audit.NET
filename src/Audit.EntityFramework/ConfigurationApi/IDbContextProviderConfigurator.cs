#if NET7_0_OR_GREATER
using System;
using Audit.Core;
using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.ConfigurationApi;

public interface IDbContextProviderConfigurator<TDbContext, out TEntity>
    where TDbContext : DbContext
    where TEntity : class, new()
{
    /// <summary>
    /// Use the given DbContextBuilder to create the DbContext instance.
    /// </summary>
    /// <param name="dbContextBuilder">A function that given an EF audit event, returns a custom DbContext to use for storing the audit events.</param>
    IDbContextProviderEntityConfigurator<TEntity> DbContextBuilder(Func<AuditEvent, TDbContext> dbContextBuilder);

    /// <summary>
    /// Use the given DbContext instance.
    /// </summary>
    /// <param name="dbContext">The DbContext instance to use.</param>
    IDbContextProviderEntityConfigurator<TEntity> DbContext(TDbContext dbContext);

    /// <summary>
    /// Use the given DbContextOptions to create the DbContext instance. Alternative to UseDbContext.
    /// </summary>
    /// <param name="dbContextOptions">The DbContextOptions to use.</param>
    IDbContextProviderEntityConfigurator<TEntity> UseDbContextOptions(DbContextOptions<TDbContext> dbContextOptions);
}

public interface IDbContextProviderConfigurator
{
    /// <summary>
    /// Use the given DbContextBuilder to create the DbContext instance.
    /// </summary>
    /// <param name="dbContextBuilder">A function that given an EF audit event, returns a custom DbContext to use for storing the audit events.</param>
    IDbContextProviderEntityConfigurator DbContextBuilder(Func<AuditEvent, DbContext> dbContextBuilder);

    /// <summary>
    /// Use the given DbContext instance.
    /// </summary>
    /// <param name="dbContext">The DbContext instance to use.</param>
    IDbContextProviderEntityConfigurator DbContext(DbContext dbContext);

    /// <summary>
    /// Use the given DbContextOptions to create the DbContext instance. Alternative to UseDbContext.
    /// </summary>
    /// <param name="dbContextOptions">The DbContextOptions to use.</param>
    IDbContextProviderEntityConfigurator UseDbContextOptions(DbContextOptions dbContextOptions);

    /// <summary>
    /// Use the given DbContextOptions given as a function of the AuditEvent to create the DbContext instance. Alternative to UseDbContext.
    /// </summary>
    /// <param name="dbContextOptions">The DbContextOptions to use.</param>
    IDbContextProviderEntityConfigurator UseDbContextOptions(Func<AuditEvent, DbContextOptions> dbContextOptions);
}
#endif