#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using Audit.Core;

namespace Audit.EntityFramework.ConfigurationApi;

public interface IDbContextProviderEntityConfigurator<out TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// Defines the Map function to map the AuditEvent to the Entity instance.
    /// </summary>
    /// <param name="mapper">Function that maps an entity type to its audited entity type.</param>
    IDbContextProviderEntityConfigurator<TEntity> Mapper(Action<AuditEvent, TEntity> mapper);

    /// <summary>
    /// Indicates if the DbContext should be disposed after saving the audit.
    /// </summary>
    /// <param name="dispose">Boolean value to indicate if the DbContext should be disposed after saving the audit.</param>
    IDbContextProviderEntityConfigurator<TEntity> DisposeDbContext(bool dispose = true);
}

public interface IDbContextProviderEntityConfigurator
{
    /// <summary>
    /// Defines the Map function to map the Audit Events to one or more Entities instances.
    /// </summary>
    /// <param name="entitiesBuilder">Function that creates the audited entities. The function should return a list of entities to be inserted, or NULL to skip the event.</param>
    IDbContextProviderEntityConfigurator EntityBuilder(Func<AuditEvent, IEnumerable<object>> entitiesBuilder);

    /// <summary>
    /// Defines the Map function to map the Audit Events to an Entity instance.
    /// </summary>
    /// <param name="entityBuilder">Function that creates the audited entity. The function should the entity to be inserted, or NULL to skip the event.</param>
    /// <returns></returns>
    IDbContextProviderEntityConfigurator EntityBuilder(Func<AuditEvent, object> entityBuilder);

    /// <summary>
    /// Indicates if the DbContext should be disposed after saving the audit.
    /// </summary>
    /// <param name="dispose">Boolean value to indicate if the DbContext should be disposed after saving the audit.</param>
    IDbContextProviderEntityConfigurator DisposeDbContext(bool dispose = true);
}
#endif