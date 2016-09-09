using System;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Configurates the OptOut mode
    /// </summary>
    /// <typeparam name="T">The AuditDbContext type</typeparam>
    public interface IExcludeConfigurator<T>
        where T : AuditDbContext
    {
        /// <summary>
        /// Ignores the given entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the EF entity to ignore.</typeparam>
        IExcludeConfigurator<T> Ignore<TEntity>();
        /// <summary>
        /// Ignores the given entity type.
        /// </summary>
        /// <param name="entityType">The entity type to ignore.</param>
        IExcludeConfigurator<T> Ignore(Type entityType);
    }
}