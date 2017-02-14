using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Configurates the OptOut mode
    /// </summary>
    /// <typeparam name="T">The AuditDbContext type</typeparam>
    public interface IExcludeConfigurator<T>
        where T : IAuditDbContext
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
        /// <summary>
        /// Specifies the function that determines whether an entity is exluded or not.
        /// </summary>
        /// <param name="predicate">A function to test each entity type for a condition.</param>
        IExcludeConfigurator<T> IgnoreAny(Func<Type, bool> predicate);
    }
}