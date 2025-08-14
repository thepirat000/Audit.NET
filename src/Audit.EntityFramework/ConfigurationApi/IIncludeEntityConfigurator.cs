using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Configures the OptIn mode for including entities in the audit.
    /// </summary>
    /// <typeparam name="T">The AuditDbContext type</typeparam>
    public interface IIncludeEntityConfigurator<T>
    {
        /// <summary>
        /// Includes the given entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the EF entity to include.</typeparam>
        /// <param name="includePropertiesConfiguration">Optional configuration for the properties to include in the entity. By default, all properties are included.</param> 
        IIncludeEntityConfigurator<T> Include<TEntity>(Action<IIncludePropertyConfigurator<TEntity>> includePropertiesConfiguration = null);
        /// <summary>
        /// Includes the given entity type.
        /// </summary>
        /// <param name="entityType">The entity type to include.</param>
        IIncludeEntityConfigurator<T> Include(Type entityType);
        /// <summary>
        /// Specifies the function that determines whether an entity is included or not.
        /// </summary>
        /// <param name="predicate">A function to test each entity type for a condition.</param>
        IIncludeEntityConfigurator<T> IncludeAny(Func<Type, bool> predicate);
    }
}