using System;
using System.Linq.Expressions;
#if EF_CORE
using EntityEntry = Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry;
#else
using EntityEntry = System.Data.Entity.Infrastructure.DbEntityEntry;
#endif

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// The settings configuration for an Entity
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public interface IContextEntitySetting<TEntity>
    {
        /// <summary>
        /// Ignore a column on the audit trail indicating its entity property
        /// </summary>
        /// <typeparam name="TProp">The property type</typeparam>
        /// <param name="property">The property selector. Must be a simple property expression i.e. x -> x.PropertyName</param>
        IContextEntitySetting<TEntity> Ignore<TProp>(Expression<Func<TEntity, TProp>> property);
        /// <summary>
        /// Ignore a column on the audit trail indicating its name
        /// </summary>
        /// <param name="propertyName">The entity's property name (case sensitive)</param>
        IContextEntitySetting<TEntity> Ignore(string propertyName);
        /// <summary>
        /// Overrides a column value to a value given as a function of the current value.
        /// </summary>
        /// <param name="property">The property selector. Must be a simple property expression i.e. x -> x.PropertyName</param>
        /// <param name="format">A function of the current value that returns the value to override</param>
        IContextEntitySetting<TEntity> Format<TProp>(Expression<Func<TEntity, TProp>> property, Func<TProp, object> format);
        /// <summary>
        /// Overrides a column value to a value given as a function of the current value.
        /// </summary>
        /// <param name="propertyName">The entity's property name (case sensitive)</param>
        /// <param name="format">A function of the current value that returns the value to override</param>
        IContextEntitySetting<TEntity> Format<TProp>(string propertyName, Func<TProp, object> format);
        /// <summary>
        /// Overrides a column value to a constant on the audit trail.
        /// </summary>
        /// <typeparam name="TProp">The property type</typeparam>
        /// <param name="property">The property selector. Must be a simple property expression i.e. x -> x.PropertyName</param>
        /// <param name="value">The constant value to store on the audit trail</param>
        IContextEntitySetting<TEntity> Override<TProp>(Expression<Func<TEntity, TProp>> property, object value);
        /// <summary>
        /// Overrides a column value to a constant on the audit trail.
        /// </summary>
        /// <param name="propertyName">The entity's property name (case sensitive)</param>
        /// <param name="value">The constant value to store on the audit trail</param>
        IContextEntitySetting<TEntity> Override(string propertyName, object value);
        /// <summary>
        /// Overrides a column value to a value given as a function of the current value.
        /// </summary>
        /// <param name="property">The property selector. Must be a simple property expression i.e. x -> x.PropertyName</param>
        /// <param name="valueSelector">A function of the EntityEntry that returns the new value to override</param>
        IContextEntitySetting<TEntity> Override<TProp>(Expression<Func<TEntity, TProp>> property, Func<EntityEntry, object> valueSelector);
        /// <summary>
        /// Overrides a column value to a value given as a function of the current value.
        /// </summary>
        /// <param name="propertyName">The entity's property name (case sensitive)</param>
        /// <param name="valueSelector">A function of the EntityEntry that returns the new value to override</param>
        IContextEntitySetting<TEntity> Override(string propertyName, Func<EntityEntry, object> valueSelector);
    }
}
 