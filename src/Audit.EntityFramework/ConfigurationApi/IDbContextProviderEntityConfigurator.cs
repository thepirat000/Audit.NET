#if NET7_0_OR_GREATER
using System;
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
#endif